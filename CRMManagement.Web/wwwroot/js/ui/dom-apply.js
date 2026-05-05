// English comments only inside code.
(function () {
    "use strict";

    function ensureUiEditKeys() {
        try {
            const root = document.body;
            if (!root) return;

            // Keys must be stable across UI edit mode vs normal mode.
            // UI edit mode hides the TopBar/Sidebar, which previously shifted the sequential numbering.
            // To keep selectors like [data-uiedit-key="24"] stable, we exclude those regions from key assignment.
            const excludedScope = "header[data-topbar], aside[data-sidebar=\"main\"]";

            let maxKey = 0;
            root.querySelectorAll("[data-uiedit-key]").forEach(el => {
                if (el && el.closest && el.closest(excludedScope)) return;
                const n = Number(el.getAttribute("data-uiedit-key"));
                if (Number.isFinite(n) && n > maxKey) maxKey = n;
            });

            let next = maxKey + 1;
            const walker = document.createTreeWalker(root, NodeFilter.SHOW_ELEMENT, {
                acceptNode: (node) => {
                    const el = node;
                    const tag = (el.tagName || "").toLowerCase();

                    if (tag === "script" || tag === "style") return NodeFilter.FILTER_REJECT;
                    if (el.closest && el.closest(excludedScope)) return NodeFilter.FILTER_REJECT;

                    return NodeFilter.FILTER_ACCEPT;
                }
            });
            let node = walker.currentNode;
            while (node) {
                const el = node;
                const tag = (el.tagName || "").toLowerCase();
                if (tag && tag !== "script" && tag !== "style") {
                    if (!el.getAttribute("data-uiedit-key")) {
                        el.setAttribute("data-uiedit-key", String(next++));
                    }
                }
                node = walker.nextNode();
            }
        } catch { }
    }

    function parse(x) {
        if (!x) return null;
        if (typeof x === "object") return x;
        try {
            const v = JSON.parse(x);
            // Handle accidental double-encoding: "{...}" -> "{...}" (string) -> {...} (object)
            if (typeof v === "string") {
                try { return JSON.parse(v); } catch { return v; }
            }
            return v;
        } catch {
            return null;
        }
    }

    // Ensure tables have deterministic keys so stored selectors like:
    // table[data-ui-table="t1"] keep working without modifying the page source.
    function ensureTableKeys() {
        const tables = Array.from(document.querySelectorAll("table"));
        tables.forEach((t, i) => {
            if (!t.getAttribute("data-ui-table")) {
                t.setAttribute("data-ui-table", "t" + (i + 1));
            }
        });
    }

    function applyActions(selector, actions) {
        if (!selector || !Array.isArray(actions)) return;

        function setElementClass(el, value) {
            try {
                const v = String(value ?? "");
                if (!el) return;
                // SVG elements may have className as SVGAnimatedString.
                if (el.className && typeof el.className === "object" && typeof el.className.baseVal === "string") {
                    el.className.baseVal = v;
                    return;
                }
                el.className = v;
            } catch { }
        }

        function sanitizeHtml(html) {
            try {
                const t = document.createElement("template");
                t.innerHTML = String(html ?? "");

                // Drop scripts entirely.
                t.content.querySelectorAll("script").forEach(s => s.remove());

                // Remove dangerous elements.
                t.content.querySelectorAll("base,meta,object,embed").forEach(el => el.remove());

                // Remove inline event handlers (onclick, onload, etc.) and dangerous URLs.
                t.content.querySelectorAll("*").forEach(el => {
                    Array.from(el.attributes || []).forEach(a => {
                        const n = String(a && a.name || "").toLowerCase();
                        if (n.startsWith("on")) el.removeAttribute(a.name);
                    });
                    ["href", "src", "action", "formaction", "xlink:href"].forEach(attr => {
                        const val = (el.getAttribute(attr) || "").trim().toLowerCase().replace(/[\s\x00-\x1f]/g, "");
                        if (val.startsWith("javascript:") || val.startsWith("data:text/html")) {
                            el.removeAttribute(attr);
                        }
                    });
                });

                return t.innerHTML;
            } catch {
                return "";
            }
        }

        function applyInsertHtml(targetSelector, html, position) {
            const pos = String(position || "after").toLowerCase();
            const safe = sanitizeHtml(html);
            if (!safe) return;

            document.querySelectorAll(targetSelector).forEach(el => {
                try {
                    if (pos === "before") el.insertAdjacentHTML("beforebegin", safe);
                    else if (pos === "after") el.insertAdjacentHTML("afterend", safe);
                    else if (pos === "prepend") el.insertAdjacentHTML("afterbegin", safe);
                    else if (pos === "append") el.insertAdjacentHTML("beforeend", safe);
                    else el.insertAdjacentHTML("afterend", safe);
                } catch { }
            });

            // New DOM nodes should receive keys for stable selection/moves.
            ensureUiEditKeys();
        }

        const table = document.querySelector(selector);

        actions.forEach(a => {
            if (!a || !a.type) return;

            if (a.type === "tableColHide" && table) {
                const idx = Number(a.colIndex);
                if (!Number.isFinite(idx) || idx < 0) return;

                const th = table.querySelector(`thead tr > th:nth-child(${idx + 1})`);
                if (th) th.style.display = "none";

                table.querySelectorAll("tbody tr").forEach(tr => {
                    const td = tr.querySelector(`td:nth-child(${idx + 1})`);
                    if (td) td.style.display = "none";
                });

                return;
            }

            if (a.type === "text") {
                document.querySelectorAll(selector).forEach(el => { el.textContent = String(a.value ?? ""); });
            }
            if (a.type === "insertHtml") {
                applyInsertHtml(selector, a.html ?? a.value, a.position);
            }

            if (a.type === "remove") {
                try {
                    document.querySelectorAll(selector).forEach(el => {
                        try {
                            if (!el || el.nodeType !== 1) return;
                            const tag = String(el.tagName || "").toLowerCase();
                            if (tag === "html" || tag === "body" || tag === "head") return;
                            if (!el.parentElement) return;
                            el.remove();
                        } catch { }
                    });
                } catch { }
            }

            // Move an existing element relative to another one.
            // Schema: { type:"move", containerSelector, referenceSelector, position:"before"|"after"|"append"|"prepend" }
            if (a.type === "move") {
                try {
                    const source = document.querySelector(selector);
                    if (!source) return;

                    const pos = String(a.position || "after").toLowerCase();
                    const refSel = String(a.referenceSelector || "").trim();
                    const containerSel = String(a.containerSelector || "").trim();

                    let container = containerSel ? document.querySelector(containerSel) : null;
                    let ref = refSel ? document.querySelector(refSel) : null;

                    if (ref && ref.parentElement) {
                        // Prefer the reference element's real parent to avoid cross-container confusion.
                        if (!container || container !== ref.parentElement) container = ref.parentElement;
                    }

                    if (!container) container = source.parentElement;
                    if (!container) return;

                    if (pos === "append") {
                        container.appendChild(source);
                    } else if (pos === "prepend") {
                        container.insertBefore(source, container.firstChild);
                    } else if (pos === "before") {
                        if (!ref) return;
                        container.insertBefore(source, ref);
                    } else {
                        // after
                        if (!ref) return;
                        container.insertBefore(source, ref.nextSibling);
                    }
                } catch { }
            }

            // Move an existing element into a container and absolutely position it (free placement).
            // Schema: { type:"moveAbs", containerSelector, left, top }
            if (a.type === "moveAbs") {
                try {
                    const source = document.querySelector(selector);
                    if (!source) return;

                    // Freeze current rendered size before taking it out of flow.
                    try {
                        const r = source.getBoundingClientRect();
                        if (Number.isFinite(r.width) && Number.isFinite(r.height)) {
                            // Font-based icons can change size when moved between containers with different font-size.
                            try {
                                const tag = String(source.tagName || "").toLowerCase();
                                const cls = String(source.className || "");
                                const iconLike = tag === "i" || /\bfa[srbld]?\b/.test(cls) || /\bfa-\S+/.test(cls) || /\bmdi\b/.test(cls) || /\bmdi-\S+/.test(cls);
                                const cs = getComputedStyle(source);
                                const fs = String(cs.fontSize || "").trim();
                                const lh = String(cs.lineHeight || "").trim();

                                if (iconLike) {
                                    if (!source.style.fontSize && fs) source.style.fontSize = fs;
                                    if (!source.style.lineHeight && lh) source.style.lineHeight = lh;
                                } else {
                                    if (!source.style.width) source.style.width = r.width + "px";
                                    if (!source.style.height) source.style.height = r.height + "px";
                                }
                            } catch { }
                        }
                    } catch { }

                    const containerSel = String(a.containerSelector || "").trim();
                    const container = containerSel ? document.querySelector(containerSel) : source.parentElement;
                    if (!container) return;
                    if (container === source) return;
                    if (source.contains && source.contains(container)) return;

                    // Ensure container is a positioning context.
                    try {
                        const cs = getComputedStyle(container);
                        if (String(cs.position || "").toLowerCase() === "static") {
                            container.style.position = "relative";
                        }
                    } catch { }

                    try { container.appendChild(source); } catch { }

                    const left = Number(a.left);
                    const top = Number(a.top);
                    if (!Number.isFinite(left) || !Number.isFinite(top)) return;

                    source.style.position = "absolute";
                    source.style.left = left + "px";
                    source.style.top = top + "px";
                    try {
                        source.style.right = "";
                        source.style.bottom = "";
                    } catch { }

                    // New DOM nodes should receive keys for stable selection/moves.
                    ensureUiEditKeys();
                } catch { }
            }
            if (a.type === "value") {
                document.querySelectorAll(selector).forEach(el => {
                    try {
                        if (el && ("value" in el)) el.value = String(a.value ?? "");
                    } catch { }
                });
            }

            if (a.type === "selectOptions") {
                const options = Array.isArray(a.options) ? a.options : [];
                document.querySelectorAll(selector).forEach(el => {
                    try {
                        if (!el || String(el.tagName || "").toLowerCase() !== "select") return;
                        el.replaceChildren();
                        options.forEach(o => {
                            const value = (o && o.value !== undefined) ? String(o.value) : "";
                            const text = (o && o.text !== undefined) ? String(o.text) : value;
                            const opt = document.createElement("option");
                            opt.value = value;
                            opt.textContent = text;
                            if (o && o.selected) opt.selected = true;
                            el.appendChild(opt);
                        });
                    } catch { }
                });
            }

            if (a.type === "selectValue") {
                const v = String(a.value ?? "");
                document.querySelectorAll(selector).forEach(el => {
                    try {
                        if (!el || String(el.tagName || "").toLowerCase() !== "select") return;
                        el.value = v;
                    } catch { }
                });
            }
            if (a.type === "style") {
                const prop = String(a.name ?? "").trim();
                if (!prop || prop === "cssText" || prop.startsWith("-")) return;
                document.querySelectorAll(selector).forEach(el => { el.style[prop] = String(a.value ?? ""); });
            }
            if (a.type === "attr") {
                const name = String(a.name ?? "").trim();
                if (!name) return;
                // Block event handler attributes and dangerous attrs to prevent stored XSS.
                if (/^on/i.test(name)) return;
                if (["srcdoc", "formaction"].includes(name.toLowerCase())) return;
                const value = (a.value === undefined || a.value === null) ? "" : String(a.value);
                document.querySelectorAll(selector).forEach(el => {
                    try {
                        if (value === "") el.removeAttribute(name);
                        else el.setAttribute(name, value);
                    } catch { }
                });
            }

            if (a.type === "classSet") {
                const cls = String(a.value ?? "");
                document.querySelectorAll(selector).forEach(el => setElementClass(el, cls));
            }

            if (a.type === "classRemove") {
                const cls = String(a.value ?? "").trim();
                if (!cls) return;
                const parts = cls.split(/\s+/).filter(Boolean);
                document.querySelectorAll(selector).forEach(el => {
                    try {
                        parts.forEach(c => el.classList.remove(c));
                    } catch { }
                });
            }

            if (a.type === "classAdd") {
                const cls = String(a.value ?? "").trim();
                if (!cls) return;
                document.querySelectorAll(selector).forEach(el => cls.split(/\s+/).forEach(c => el.classList.add(c)));
            }
        });
    }

    function run() {
        ensureTableKeys();
        ensureUiEditKeys();

        // Middleware-injected JSON
        const dom = parse(window.__UI_DOM__);
        const root = dom && dom.dom ? dom.dom : dom;

        // Expected: { overrides: [{ selector, actions: [...] }] }
        const overrides = root && Array.isArray(root.overrides) ? root.overrides : null;
        if (!overrides) return;

        overrides.forEach(o => {
            if (!o || !o.selector) return;
            applyActions(o.selector, o.actions || []);
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", run);
    } else {
        run();
    }
})();
