// English comments only inside code.
(function () {
    "use strict";

    // Assign deterministic data-uiedit-key attributes.
    // IMPORTANT: do not re-walk the entire DOM on every click; that makes the editor feel slow.
    // We do one full initialization, then incrementally key any dynamically added elements.
    const __uieditExcludedScope = "header[data-topbar], aside[data-sidebar=\"main\"]";
    let __uieditKeysInitialized = false;
    let __uieditNextKey = 1;

    function shouldKeyElement(el) {
        try {
            if (!el || el.nodeType !== 1) return false;
            const tag = String(el.tagName || "").toLowerCase();
            if (tag === "script" || tag === "style") return false;
            if (el.closest && el.closest(__uieditExcludedScope)) return false;
            return true;
        } catch {
            return false;
        }
    }

    function initUiEditKeysOnce() {
        try {
            if (__uieditKeysInitialized) return;
            const root = document.body;
            if (!root) return;

            // Keep keys stable: never rewrite existing keys; only assign missing ones.
            let maxKey = 0;
            root.querySelectorAll("[data-uiedit-key]").forEach(el => {
                if (!shouldKeyElement(el)) return;
                const n = Number(el.getAttribute("data-uiedit-key"));
                if (Number.isFinite(n) && n > maxKey) maxKey = n;
            });
            __uieditNextKey = Math.max(1, maxKey + 1);

            // Full initialization once.
            const walker = document.createTreeWalker(root, NodeFilter.SHOW_ELEMENT, {
                acceptNode: (node) => (shouldKeyElement(node) ? NodeFilter.FILTER_ACCEPT : NodeFilter.FILTER_REJECT)
            });

            let node = walker.currentNode;
            while (node) {
                const el = node;
                if (!el.getAttribute("data-uiedit-key")) {
                    el.setAttribute("data-uiedit-key", String(__uieditNextKey++));
                }
                node = walker.nextNode();
            }

            __uieditKeysInitialized = true;
        } catch { }
    }

    function assignKeysInSubtree(rootNode) {
        try {
            if (!rootNode) return;
            const root = (rootNode.nodeType === 1) ? rootNode : (rootNode.parentElement || null);
            if (!root) return;

            // If subtree is inside excluded regions, ignore.
            if (root.closest && root.closest(__uieditExcludedScope)) return;

            const walker = document.createTreeWalker(root, NodeFilter.SHOW_ELEMENT, {
                acceptNode: (node) => (shouldKeyElement(node) ? NodeFilter.FILTER_ACCEPT : NodeFilter.FILTER_REJECT)
            });

            let node = walker.currentNode;
            while (node) {
                const el = node;
                if (!el.getAttribute("data-uiedit-key")) {
                    el.setAttribute("data-uiedit-key", String(__uieditNextKey++));
                }
                node = walker.nextNode();
            }
        } catch { }
    }

    function isEditMode() {
        try { return new URLSearchParams(location.search).get("__uiedit") === "1"; }
        catch { return false; }
    }

    // Assign deterministic data-ui-table keys to tables based on their order on the page.
    // This avoids changing the target pages (Artifacts, Users, etc.).
    function ensureTableKeys() {
        const tables = Array.from(document.querySelectorAll("table"));
        tables.forEach((t, i) => {
            if (!t.getAttribute("data-ui-table")) {
                t.setAttribute("data-ui-table", "t" + (i + 1));
            }
        });
    }

    function getReturnPath() {
        try { return new URLSearchParams(location.search).get("path") || ""; }
        catch { return ""; }
    }

    function tableSelectorOf(table) {
        if (!table) return "";
        if (table.id) return "#" + table.id;
        const k = table.getAttribute("data-ui-table");
        if (k) return `table[data-ui-table="${k}"]`;
        return "table";
    }

    function cssPath(el) {
        if (!el || el.nodeType !== 1) return "";
        if (el.id) return "#" + el.id;

        function escAttr(v) {
            return String(v ?? "")
                .replace(/\\/g, "\\\\")
                .replace(/\"/g, "\\\"")
                .replace(/\n/g, "\\n")
                .replace(/\r/g, "\\r")
                .replace(/\t/g, "\\t");
        }

        function isUniqueSelector(sel) {
            try {
                return !!sel && document.querySelectorAll(sel).length === 1;
            } catch {
                return false;
            }
        }

        function scopePrefixFor(element) {
            try {
                // Prefer narrowing to the nearest nav (desktop/mobile) and then to the topbar.
                const nav = element.closest && element.closest("nav[aria-label]");
                if (nav) {
                    const label = nav.getAttribute("aria-label");
                    if (label) return `nav[aria-label=\"${escAttr(label)}\"] `;
                }

                const topbar = element.closest && element.closest("header[data-topbar]");
                if (topbar) return "header[data-topbar] ";
            } catch { }
            return "";
        }

        // Prefer stable, unique selectors based on semantic attributes.
        // This fixes a common case where data-uiedit-key is re-assigned differently after reload
        // (e.g., TopBar menu changes depending on auth state / responsive layout).
        try {
            const tag = String(el.tagName || "").toLowerCase();
            const prefix = scopePrefixFor(el);

            if (tag === "a") {
                const href = (el.getAttribute && el.getAttribute("href")) ? String(el.getAttribute("href") || "").trim() : "";
                if (href) {
                    const sel = prefix + `a[href=\"${escAttr(href)}\"]`;
                    if (isUniqueSelector(sel)) return sel;
                }
            }

            // Common stable attributes (unique in typical layouts).
            const attrCandidates = ["aria-label", "title", "name", "data-testid", "data-test", "role"];
            for (const attr of attrCandidates) {
                const v = (el.getAttribute && el.getAttribute(attr)) ? String(el.getAttribute(attr) || "").trim() : "";
                if (!v) continue;
                const sel = prefix + `${tag}[${attr}=\"${escAttr(v)}\"]`;
                if (isUniqueSelector(sel)) return sel;
            }

            // Images: src is often stable (logo/icons).
            if (tag === "img") {
                const src = (el.getAttribute && el.getAttribute("src")) ? String(el.getAttribute("src") || "").trim() : "";
                if (src) {
                    const sel = prefix + `img[src=\"${escAttr(src)}\"]`;
                    if (isUniqueSelector(sel)) return sel;
                }
            }
        } catch { }

        // Prefer deterministic keys (stable across moves + reloads).
        const k = el.getAttribute && el.getAttribute("data-uiedit-key");
        if (k) return `[data-uiedit-key="${String(k).replace(/\\"/g, "")}"]`;

        // Short DOM path: stable enough for editing sessions
        const parts = [];
        let cur = el;
        for (let i = 0; cur && cur.nodeType === 1 && i < 6; i++) {
            const tag = cur.tagName.toLowerCase();
            if (!tag) break;

            let nth = 1;
            let sib = cur;
            while ((sib = sib.previousElementSibling)) {
                if (sib.tagName === cur.tagName) nth++;
            }
            parts.unshift(`${tag}:nth-of-type(${nth})`);
            cur = cur.parentElement;
        }
        return parts.join(" > ");
    }

    function findTableContext(target) {
        const cell = target.closest ? target.closest("td,th") : null;
        if (!cell) return { tableSelector: "", colIndex: null };

        const table = cell.closest("table");
        if (!table) return { tableSelector: "", colIndex: null };

        const tableSelector = tableSelectorOf(table);

        // cellIndex is 0-based and works for both TD and TH
        const colIndex = typeof cell.cellIndex === "number" ? cell.cellIndex : null;

        return { tableSelector, colIndex };
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
                t.content.querySelectorAll("script").forEach(s => s.remove());
                t.content.querySelectorAll("base,meta,object,embed").forEach(el => el.remove());
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
        }

        const table = document.querySelector(selector);

        actions.forEach(a => {
            if (!a || !a.type) return;

            // Hide a table column by index
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

            // Generic element actions (selector targets any element)
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

            if (a.type === "move") {
                try {
                    const source = document.querySelector(selector);
                    if (!source) return;

                    const pos = String(a.position || "after").toLowerCase();
                    const refSel = String(a.referenceSelector || "").trim();
                    const containerSel = String(a.containerSelector || "").trim();

                    let container = null;
                    try { container = containerSel ? document.querySelector(containerSel) : null; } catch { container = null; }

                    let ref = null;
                    if (refSel) {
                        // If a container is specified, only resolve reference inside it.
                        // This prevents accidentally matching the wrong element elsewhere.
                        if (container) {
                            try { ref = container.querySelector(refSel); } catch { ref = null; }
                            // If refSel is an absolute selector (e.g. "nav ... a[href=...]") it will not match
                            // when scoped to the container. Fall back to a global match, but only accept it
                            // if it is actually within the container.
                            if (!ref) {
                                try {
                                    const globalRef = document.querySelector(refSel);
                                    if (globalRef && container.contains(globalRef)) ref = globalRef;
                                } catch { }
                            }
                        } else {
                            try { ref = document.querySelector(refSel); } catch { ref = null; }
                        }
                    }

                    // Enforce required inputs by position.
                    if (pos === "before" || pos === "after") {
                        if (!ref) return;
                        const refParent = ref.parentElement;
                        if (!refParent) return;
                        container = refParent;
                    } else {
                        if (!container) container = source.parentElement;
                    }

                    if (!container) return;
                    if (container === source) return;
                    if (source.contains && source.contains(container)) return;
                    if (ref && (ref === source || (source.contains && source.contains(ref)))) return;

                    if (pos === "append") {
                        container.appendChild(source);
                    } else if (pos === "prepend") {
                        container.insertBefore(source, container.firstChild);
                    } else if (pos === "before") {
                        container.insertBefore(source, ref);
                    } else {
                        container.insertBefore(source, ref.nextSibling);
                    }

                    assignKeysInSubtree(container || document.body);
                } catch { }
            }

            if (a.type === "moveAbs") {
                try {
                    const source = document.querySelector(selector);
                    if (!source) return;

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

                    // Prevent shrink/grow when element is removed from normal layout flow.
                    freezeSizeForAbs(source);

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

                    assignKeysInSubtree(container || document.body);
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
                document.querySelectorAll(selector).forEach(el => {
                    try {
                        if (!el || String(el.tagName || "").toLowerCase() !== "select") return;
                        const opts = Array.isArray(a.options) ? a.options : [];
                        el.replaceChildren();
                        opts.forEach(o => {
                            const opt = document.createElement("option");
                            opt.value = String(o && o.value !== undefined ? o.value : "");
                            opt.textContent = String(o && o.text !== undefined ? o.text : opt.value);
                            if (o && o.disabled) opt.disabled = true;
                            if (o && o.selected) opt.selected = true;
                            el.appendChild(opt);
                        });
                    } catch { }
                });
            }
            if (a.type === "selectValue") {
                document.querySelectorAll(selector).forEach(el => {
                    try {
                        if (!el || String(el.tagName || "").toLowerCase() !== "select") return;
                        const v = a.value;
                        if (Array.isArray(v)) {
                            Array.from(el.options || []).forEach(o => { o.selected = v.includes(String(o.value)); });
                        } else {
                            el.value = String(v ?? "");
                        }
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
            if (a.type === "classAdd") {
                const cls = String(a.value ?? "").trim();
                if (!cls) return;
                document.querySelectorAll(selector).forEach(el => cls.split(/\s+/).forEach(c => el.classList.add(c)));
            }
            if (a.type === "classRemove") {
                const cls = String(a.value ?? "").trim();
                if (!cls) return;
                const parts = cls.split(/\s+/).filter(Boolean);
                if (!parts.length) return;
                document.querySelectorAll(selector).forEach(el => parts.forEach(c => el.classList.remove(c)));
            }
        });
    }

    if (!isEditMode()) return;

    initUiEditKeysOnce();

    ensureTableKeys();

    function collectIconClassStrings(limit) {
        try {
            const max = Number.isFinite(limit) ? limit : 250;
            const out = [];
            const seen = new Set();

            const nodes = document.querySelectorAll("i[class],span[class],a[class],svg[class]");
            for (const el of nodes) {
                if (!el || el.nodeType !== 1) continue;
                const cls = String(safeClassName(el) || "").trim().replace(/\s+/g, " ");
                if (!cls) continue;
                if (!(/\bfa[srbld]?\b/.test(cls) || /\bfa-\S+/.test(cls) || /\bmdi\b/.test(cls) || /\bmdi-\S+/.test(cls))) continue;
                if (seen.has(cls)) continue;
                seen.add(cls);
                out.push(cls);
                if (out.length >= max) break;
            }
            return out;
        } catch {
            return [];
        }
    }

    function collectImageSrcStrings(limit) {
        try {
            const max = Number.isFinite(limit) ? limit : 250;
            const out = [];
            const seen = new Set();
            const nodes = document.querySelectorAll("img[src]");
            for (const el of nodes) {
                if (!el || el.nodeType !== 1) continue;
                const src = String(el.getAttribute("src") || "").trim();
                if (!src) continue;
                if (seen.has(src)) continue;
                seen.add(src);
                out.push(src);
                if (out.length >= max) break;
            }
            return out;
        } catch {
            return [];
        }
    }

    // Provide an icon "picker" list to the parent editor (used for datalist autocomplete).
    try {
        const send = () => {
            try {
                window.parent.postMessage({
                    type: "uiedit:iconCatalog",
                    classes: collectIconClassStrings(250)
                }, "*");
            } catch { }

            try {
                window.parent.postMessage({
                    type: "uiedit:imageCatalog",
                    srcs: collectImageSrcStrings(250)
                }, "*");
            } catch { }
        };

        if (typeof requestIdleCallback === "function") requestIdleCallback(send, { timeout: 800 });
        else setTimeout(send, 50);
    } catch { }

    // Incremental key assignment for dynamically added nodes.
    try {
        const obs = new MutationObserver((mutations) => {
            try {
                for (const m of (mutations || [])) {
                    if (!m || !m.addedNodes || !m.addedNodes.length) continue;
                    for (const n of m.addedNodes) {
                        if (!n) continue;
                        if (n.nodeType === 1) assignKeysInSubtree(n);
                    }
                }
            } catch { }
        });
        if (document.body) obs.observe(document.body, { childList: true, subtree: true });
    } catch { }

    // DnD mode state
    let __uieditMode = "text";
    let __uieditDragSource = null;
    let __uieditDropTarget = null;
    let __uieditDropPos = null;
    let __uieditDragGrabDx = 0;
    let __uieditDragGrabDy = 0;

    // Tell the parent Editor what page we are editing + where to return after save.
    try {
        window.parent.postMessage({
            type: "uiedit:init",
            returnPath: getReturnPath(),
            href: String(location.href || "")
        }, "*");
    } catch { }

    // Keep a persistent highlight for the currently selected element.
    // We use a CSS class to avoid fighting inline styles from the target page.
    let __uieditSelectedEl = null;
    (function ensureHighlightStyle() {
        try {
            const id = "__uiedit_highlight_style";
            if (document.getElementById(id)) return;
            const style = document.createElement("style");
            style.id = id;
            style.textContent = `
                .__uiedit-selected {
                    outline: 2px solid #ef4444 !important;
                    outline-offset: 2px !important;
                }
                .__uiedit-drop-before {
                    outline: 2px solid #22c55e !important;
                    outline-offset: 2px !important;
                }
                .__uiedit-drop-after {
                    outline: 2px dashed #22c55e !important;
                    outline-offset: 2px !important;
                }
                .__uiedit-img-handle {
                    position: fixed;
                    width: 14px;
                    height: 14px;
                    box-sizing: border-box;
                    border: 2px solid #ef4444;
                    background: rgba(255, 255, 255, 0.92);
                    border-radius: 3px;
                    z-index: 2147483647;
                    cursor: nwse-resize;
                    display: none;
                }
            `;
            document.head.appendChild(style);
        } catch { }
    })();

    // IMAGE mode: allow moving/resizing a selected <img> via mouse.
    // - Drag the image to move it (uses position:relative + left/top).
    // - Drag the bottom-right handle to resize (sets width/height in px).
    let __uieditImgHandle = null;
    let __uieditImgOp = null; // { kind, el, startX, startY, startLeft, startTop, startW, startH, prevUserSelect }

    function ensureImgHandle() {
        try {
            if (__uieditImgHandle) return;
            const h = document.createElement("div");
            h.className = "__uiedit-img-handle";
            h.setAttribute("title", "Drag to resize");
            h.addEventListener("mousedown", (e) => {
                try {
                    if (__uieditMode !== "image") return;
                    if (!__uieditSelectedEl) return;
                    const tag = String(__uieditSelectedEl.tagName || "").toLowerCase();
                    if (tag !== "img") return;
                    if (!e || e.button !== 0) return;

                    e.preventDefault();
                    e.stopPropagation();
                    startImgResize(__uieditSelectedEl, e.clientX, e.clientY);
                } catch { }
            }, true);

            document.body.appendChild(h);
            __uieditImgHandle = h;

            window.addEventListener("scroll", () => updateImgHandlePosition(), true);
            window.addEventListener("resize", () => updateImgHandlePosition(), true);
        } catch { }
    }

    function hideImgHandle() {
        try {
            if (__uieditImgHandle) __uieditImgHandle.style.display = "none";
        } catch { }
    }

    function updateImgHandlePosition() {
        try {
            ensureImgHandle();
            if (__uieditMode !== "image") { hideImgHandle(); return; }
            if (!__uieditSelectedEl) { hideImgHandle(); return; }
            const tag = String(__uieditSelectedEl.tagName || "").toLowerCase();
            if (tag !== "img") { hideImgHandle(); return; }
            if (!__uieditImgHandle) return;

            const r = __uieditSelectedEl.getBoundingClientRect();
            if (!(r.width > 0 && r.height > 0)) { hideImgHandle(); return; }

            const size = 14;
            const x = Math.round(r.right - (size / 2));
            const y = Math.round(r.bottom - (size / 2));

            __uieditImgHandle.style.left = x + "px";
            __uieditImgHandle.style.top = y + "px";
            __uieditImgHandle.style.display = "block";
        } catch { }
    }

    function readCssPx(el, prop) {
        try {
            const cs = getComputedStyle(el);
            const v = String(cs && cs[prop] !== undefined ? cs[prop] : "").trim().toLowerCase();
            if (!v || v === "auto") return 0;
            const n = parseFloat(v);
            return Number.isFinite(n) ? n : 0;
        } catch {
            return 0;
        }
    }

    function cancelImgOp() {
        try {
            if (!__uieditImgOp) return;
            document.removeEventListener("mousemove", onImgOpMove, true);
            document.removeEventListener("mouseup", onImgOpUp, true);
            try {
                if (document.body) document.body.style.userSelect = __uieditImgOp.prevUserSelect || "";
            } catch { }
            __uieditImgOp = null;
        } catch { }
    }

    function startImgMove(el, startX, startY) {
        try {
            cancelImgOp();
            if (!el || el.nodeType !== 1) return;
            const tag = String(el.tagName || "").toLowerCase();
            if (tag !== "img") return;

            try {
                const cs = getComputedStyle(el);
                const pos = String(cs.position || "").toLowerCase();
                if (!pos || pos === "static") el.style.position = "relative";
            } catch { el.style.position = "relative"; }

            __uieditImgOp = {
                kind: "move",
                el,
                startX: Number(startX) || 0,
                startY: Number(startY) || 0,
                startLeft: readCssPx(el, "left"),
                startTop: readCssPx(el, "top"),
                startW: 0,
                startH: 0,
                prevUserSelect: (document.body && document.body.style && document.body.style.userSelect) ? document.body.style.userSelect : ""
            };

            try { if (document.body) document.body.style.userSelect = "none"; } catch { }
            document.addEventListener("mousemove", onImgOpMove, true);
            document.addEventListener("mouseup", onImgOpUp, true);
        } catch { }
    }

    function startImgResize(el, startX, startY) {
        try {
            cancelImgOp();
            if (!el || el.nodeType !== 1) return;
            const tag = String(el.tagName || "").toLowerCase();
            if (tag !== "img") return;

            const r = el.getBoundingClientRect();
            const w = (r.width > 0) ? r.width : 0;
            const h = (r.height > 0) ? r.height : 0;

            __uieditImgOp = {
                kind: "resize",
                el,
                startX: Number(startX) || 0,
                startY: Number(startY) || 0,
                startLeft: 0,
                startTop: 0,
                startW: w,
                startH: h,
                prevUserSelect: (document.body && document.body.style && document.body.style.userSelect) ? document.body.style.userSelect : ""
            };

            try { if (document.body) document.body.style.userSelect = "none"; } catch { }
            document.addEventListener("mousemove", onImgOpMove, true);
            document.addEventListener("mouseup", onImgOpUp, true);
        } catch { }
    }

    function onImgOpMove(e) {
        try {
            if (!__uieditImgOp) return;
            const el = __uieditImgOp.el;
            if (!el || el.nodeType !== 1) return;

            const dx = (Number.isFinite(e.clientX) ? (e.clientX - __uieditImgOp.startX) : 0);
            const dy = (Number.isFinite(e.clientY) ? (e.clientY - __uieditImgOp.startY) : 0);

            if (__uieditImgOp.kind === "move") {
                const left = (__uieditImgOp.startLeft || 0) + dx;
                const top = (__uieditImgOp.startTop || 0) + dy;
                el.style.left = Math.round(left) + "px";
                el.style.top = Math.round(top) + "px";
            } else if (__uieditImgOp.kind === "resize") {
                const w = Math.max(8, (__uieditImgOp.startW || 0) + dx);
                const h = Math.max(8, (__uieditImgOp.startH || 0) + dy);
                el.style.width = Math.round(w) + "px";
                el.style.height = Math.round(h) + "px";
            }

            updateImgHandlePosition();
            try { e.preventDefault(); e.stopPropagation(); } catch { }
        } catch { }
    }

    function onImgOpUp(e) {
        try {
            if (!__uieditImgOp) return;
            const op = __uieditImgOp;
            const el = op.el;

            cancelImgOp();
            updateImgHandlePosition();

            if (!el || el.nodeType !== 1) return;
            initUiEditKeysOnce();

            const selector = cssPath(el);
            if (!selector) return;

            const styles = {};
            if (op.kind === "move") {
                styles.position = String(el.style.position || "relative");
                styles.left = String(el.style.left || "0px");
                styles.top = String(el.style.top || "0px");
            } else if (op.kind === "resize") {
                styles.width = String(el.style.width || "");
                styles.height = String(el.style.height || "");
            }

            window.parent.postMessage({
                type: "uiedit:imageTransformed",
                selector,
                styles
            }, "*");

            try { e && e.preventDefault && e.preventDefault(); } catch { }
        } catch { }
    }

    function setSelectedHighlight(el) {
        try {
            if (__uieditSelectedEl && __uieditSelectedEl !== el) {
                __uieditSelectedEl.classList.remove("__uiedit-selected");

                // Only the selected element is draggable in MOVE mode.
                try { __uieditSelectedEl.removeAttribute("draggable"); } catch { }
            }
            __uieditSelectedEl = el || null;
            if (__uieditSelectedEl) {
                __uieditSelectedEl.classList.add("__uiedit-selected");

                if (__uieditMode === "move") {
                    try { __uieditSelectedEl.setAttribute("draggable", "true"); } catch { }
                }
            }

            // Keep IMAGE resize handle in sync with selection.
            try { updateImgHandlePosition(); } catch { }
        } catch { }
    }

    function safeClassName(el) {
        try {
            const c = el && el.className;
            if (typeof c === "string") return c;
            if (c && typeof c.baseVal === "string") return c.baseVal; // SVGAnimatedString
        } catch { }
        return "";
    }

    function snapshotSizeForAbs(el) {
        try {
            if (!el || el.nodeType !== 1) return null;
            const r = el.getBoundingClientRect();
            if (!Number.isFinite(r.width) || !Number.isFinite(r.height)) return null;

            const tag = String(el.tagName || "").toLowerCase();
            const cls = String(safeClassName(el) || "");
            const iconLike = tag === "i" || /\bfa[srbld]?\b/.test(cls) || /\bfa-\S+/.test(cls) || /\bmdi\b/.test(cls) || /\bmdi-\S+/.test(cls);

            let fontSize = "";
            let lineHeight = "";
            try {
                const cs = getComputedStyle(el);
                fontSize = String(cs.fontSize || "").trim();
                lineHeight = String(cs.lineHeight || "").trim();
            } catch { }

            return {
                width: r.width,
                height: r.height,
                iconLike,
                fontSize,
                lineHeight
            };
        } catch { }
        return null;
    }

    function freezeSizeForAbs(el, snap) {
        try {
            if (!el || el.nodeType !== 1) return;
            const s = snap || snapshotSizeForAbs(el);
            if (!s) return;

            // Only set if not explicitly set already.
            if (!s.iconLike) {
                if (!el.style.width) el.style.width = s.width + "px";
                if (!el.style.height) el.style.height = s.height + "px";
            }

            // Font-based icons can change size when moved between containers with different font-size.
            try {
                if (s.iconLike) {
                    if (!el.style.fontSize && s.fontSize) el.style.fontSize = s.fontSize;
                    if (!el.style.lineHeight && s.lineHeight) el.style.lineHeight = s.lineHeight;
                }
            } catch { }
        } catch { }
    }

    function effectiveBackgroundColor(el) {
        try {
            let cur = el;
            for (let i = 0; cur && i < 20; i++) {
                const bg = getComputedStyle(cur).backgroundColor;
                if (bg && bg !== "transparent" && bg !== "rgba(0, 0, 0, 0)") return bg;
                cur = cur.parentElement;
            }
        } catch { }
        return "transparent";
    }

    // Receive live preview apply messages from the parent Editor page
    window.addEventListener("message", (ev) => {
        const msg = ev && ev.data;
        if (!msg || typeof msg !== "object") return;

        if (msg.type === "uiedit:apply") {
            applyActions(msg.selector, msg.actions);
            return;
        }

        if (msg.type === "uiedit:applyBatch") {
            try {
                const items = Array.isArray(msg.items) ? msg.items : [];
                items.forEach(it => {
                    if (!it || !it.selector) return;
                    applyActions(it.selector, Array.isArray(it.actions) ? it.actions : []);
                });
            } catch { }
            return;
        }

        if (msg.type === "uiedit:setMode") {
            const m = String(msg.mode || "").toLowerCase().trim();
            __uieditMode = m || "text";
            try {
                document.body.style.cursor = (__uieditMode === "move") ? "move" : "";
            } catch { }

            // Cancel image operation when switching modes.
            try { if (__uieditMode !== "image") cancelImgOp(); } catch { }
            try { updateImgHandlePosition(); } catch { }

            // Keep draggable only on the selected element.
            try {
                if (__uieditSelectedEl) {
                    if (__uieditMode === "move") __uieditSelectedEl.setAttribute("draggable", "true");
                    else __uieditSelectedEl.removeAttribute("draggable");
                }
            } catch { }
        }

        if (msg.type === "uiedit:moveStep") {
            try {
                if (__uieditMode !== "move") return;
                if (!__uieditSelectedEl) return;

                const dir = String(msg.direction || "").toLowerCase();
                const el = __uieditSelectedEl;
                const parent = el.parentElement;
                if (!parent) return;

                let ref = null;
                let position = "after";

                if (dir === "up") {
                    ref = el.previousElementSibling;
                    if (!ref) return;
                    parent.insertBefore(el, ref);
                    position = "before";
                } else if (dir === "down") {
                    ref = el.nextElementSibling;
                    if (!ref) return;
                    parent.insertBefore(el, ref.nextElementSibling);
                    position = "after";
                } else {
                    return;
                }

                initUiEditKeysOnce();
                try { setSelectedHighlight(el); } catch { }

                window.parent.postMessage({
                    type: "uiedit:moved",
                    sourceSelector: cssPath(el),
                    containerSelector: cssPath(parent),
                    referenceSelector: cssPath(ref),
                    position
                }, "*");
            } catch { }
        }

        if (msg.type === "uiedit:selectBySelector") {
            try {
                const sel = String(msg.selector || "").trim();
                if (!sel) return;
                const el = document.querySelector(sel);
                if (!el) return;
                try { setSelectedHighlight(el); } catch { }
                try { el.scrollIntoView({ block: "center", inline: "nearest" }); } catch { }
                try { postSelected(el); } catch { }
            } catch { }
        }
    });

    function postSelected(target) {
        if (!target || target.nodeType !== 1) return;

        let kind = "text";
        try {
            const tag = (target.tagName || "").toLowerCase();
            const role = (target.getAttribute && target.getAttribute("role")) ? String(target.getAttribute("role")).toLowerCase() : "";
            if (tag === "img") kind = "image";
            else if (tag === "i" || tag === "svg") kind = "icon";
            else if (tag === "select") kind = "select";
            else if (tag === "input" || tag === "textarea") kind = "input";
            else if (tag === "label") kind = "label";
            else if (role === "tab") kind = "tab";
            else if (target.closest && (target.closest("table") || target.closest("td,th"))) kind = "table";
        } catch { }

        const selector = cssPath(target);
        let insertContainerSelector = selector;
        try {
            const tag = (target.tagName || "").toLowerCase();
            const voidTags = new Set(["area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"]);
            if (voidTags.has(tag)) {
                const p = target.parentElement;
                if (p) insertContainerSelector = cssPath(p) || selector;
            }
        } catch { }
        const ctx = findTableContext(target);

        let className = "";
        let bg = "";
        let bgEffective = "";
        let color = "";
        try {
            className = safeClassName(target);
            const cs = getComputedStyle(target);
            bg = cs.backgroundColor || "";
            color = cs.color || "";
            bgEffective = effectiveBackgroundColor(target);
        } catch { }

        // Heuristic: icon libraries often render as <span>/<a> with fa-* classes.
        try {
            if (kind === "text") {
                const c = String(className || "");
                if (/\bfa[srbld]?\b/.test(c) || /\bfa-\S+/.test(c) || /\bmdi\b/.test(c) || /\bmdi-\S+/.test(c)) {
                    kind = "icon";
                }
            }
        } catch { }

        // If a wrapper is selected, try to find the actual icon element for editing.
        let iconTargetSelector = "";
        try {
            let iconEl = null;
            const tag = String(target.tagName || "").toLowerCase();
            if (tag === "i" || tag === "svg") {
                iconEl = target;
            } else {
                const first = target.querySelector ? target.querySelector("i,svg") : null;
                if (first) {
                    const cls = String(safeClassName(first) || "");
                    if (/\bfa[srbld]?\b/.test(cls) || /\bfa-\S+/.test(cls) || /\bmdi\b/.test(cls) || /\bmdi-\S+/.test(cls) || String(first.tagName || "").toLowerCase() === "svg") {
                        iconEl = first;
                    }
                }
            }

            if (!iconEl && target.closest) {
                const p = target.closest("button,a,th,td,span,div");
                const cand = p && p.querySelector ? p.querySelector("i,svg") : null;
                if (cand) iconEl = cand;
            }

            if (iconEl) iconTargetSelector = cssPath(iconEl);
        } catch { }

        let src = "";
        let alt = "";
        try {
            if ((target.tagName || "").toLowerCase() === "img") {
                src = target.getAttribute("src") || "";
                alt = target.getAttribute("alt") || "";
            }
        } catch { }

        let inputType = "";
        let placeholder = "";
        let value = "";
        let required = false;
        let selectOptions = [];
        let selectValue = "";
        try {
            const tag = (target.tagName || "").toLowerCase();
            if (tag === "input") {
                inputType = String(target.getAttribute("type") || target.type || "text").toLowerCase();
            } else if (tag === "textarea") {
                inputType = "textarea";
            } else if (tag === "select") {
                inputType = "select";
                try {
                    selectOptions = Array.from(target.options || []).map(o => ({
                        value: String(o.value ?? ""),
                        text: String(o.text ?? ""),
                        selected: !!o.selected,
                        disabled: !!o.disabled
                    }));
                } catch { }
                selectValue = ("value" in target) ? String(target.value || "") : "";
            }

            placeholder = String(target.getAttribute("placeholder") || "");
            if ("value" in target) value = String(target.value || "");
            required = !!target.required;
        } catch { }

        const textForInspector = (kind === "input") ? value : (target.textContent || "");

        try {
            window.parent.postMessage({
                type: "uiedit:selected",
                selector,
                kind,
                iconTargetSelector,
                text: String(textForInspector || "").trim().slice(0, 2000),
                insertContainerSelector,
                className,
                bg,
                bgEffective,
                color,
                tableSelector: ctx.tableSelector,
                colIndex: ctx.colIndex,
                src,
                alt,
                inputType,
                placeholder,
                value,
                required,
                selectOptions,
                selectValue
            }, "*");
        } catch { }
    }

    function clearDropHint() {
        try {
            if (__uieditDropTarget) {
                __uieditDropTarget.classList.remove("__uiedit-drop-before");
                __uieditDropTarget.classList.remove("__uiedit-drop-after");
            }
        } catch { }
        __uieditDropTarget = null;
        __uieditDropPos = null;
    }

    function setDropHint(target, pos) {
        clearDropHint();
        if (!target) return;
        __uieditDropTarget = target;
        __uieditDropPos = pos;
        try {
            if (pos === "before") target.classList.add("__uiedit-drop-before");
            else if (pos === "after") target.classList.add("__uiedit-drop-after");
        } catch { }
    }

    function isTableCell(el) {
        try {
            if (!el || el.nodeType !== 1) return false;
            const tag = String(el.tagName || "").toLowerCase();
            return tag === "td" || tag === "th";
        } catch {
            return false;
        }
    }

    function pickNearestChildPlacement(container, clientX, clientY, excludeEl) {
        try {
            if (!container || container.nodeType !== 1) return { ref: null, position: "append" };
            const kids = Array.from(container.children || []).filter((k) => {
                if (!k) return false;
                if (excludeEl && k === excludeEl) return false;
                return true;
            });
            if (!kids.length) return { ref: null, position: "append" };

            // Choose an insertion point based on cursor position among siblings.
            // This lands closer to where the user releases than “nearest child midpoint”.
            let axis = "y";
            try {
                const cs = getComputedStyle(container);
                const isFlex = (cs.display || "").includes("flex");
                const isColumn = isFlex && String(cs.flexDirection || "").toLowerCase().startsWith("column");
                axis = isColumn ? "y" : "x";
            } catch { }

            // If not flex, infer axis by which direction children are laid out.
            // Toolbars/buttons typically vary along X, lists/forms vary along Y.
            try {
                const cs = getComputedStyle(container);
                const disp = String(cs.display || "").toLowerCase();
                const isFlex = disp.includes("flex");
                if (!isFlex) {
                    const sample = [];
                    for (const c of kids) {
                        const r = c.getBoundingClientRect();
                        if (!(r.width > 0 && r.height > 0)) continue;
                        sample.push(r);
                        if (sample.length >= 8) break;
                    }
                    if (sample.length >= 2) {
                        let minLeft = Number.POSITIVE_INFINITY, maxLeft = Number.NEGATIVE_INFINITY;
                        let minTop = Number.POSITIVE_INFINITY, maxTop = Number.NEGATIVE_INFINITY;
                        for (const r of sample) {
                            if (r.left < minLeft) minLeft = r.left;
                            if (r.left > maxLeft) maxLeft = r.left;
                            if (r.top < minTop) minTop = r.top;
                            if (r.top > maxTop) maxTop = r.top;
                        }
                        const rangeX = maxLeft - minLeft;
                        const rangeY = maxTop - minTop;
                        if (Number.isFinite(rangeX) && Number.isFinite(rangeY)) {
                            axis = (rangeX > rangeY) ? "x" : "y";
                        }
                    }
                }
            } catch { }

            const boxes = [];
            for (const c of kids) {
                const r = c.getBoundingClientRect();
                if (!(r.width > 0 && r.height > 0)) continue;
                const start = (axis === "y") ? r.top : r.left;
                const end = (axis === "y") ? r.bottom : r.right;
                const mid = (start + end) / 2;
                boxes.push({ el: c, start, end, mid });
            }

            if (!boxes.length) return { ref: null, position: "append" };
            boxes.sort((a, b) => a.start - b.start);

            const p = (axis === "y") ? clientY : clientX;

            // If pointer is before the first element's midpoint, insert before it.
            if (p < boxes[0].mid) return { ref: boxes[0].el, position: "before" };

            // Compute boundaries between siblings based on the gap between their edges.
            // This makes insertion correspond to the cursor position (especially when elements
            // have different widths or there are gaps/padding).
            for (let i = 0; i < boxes.length - 1; i++) {
                const a = boxes[i];
                const b = boxes[i + 1];
                const boundary = (a.end + b.start) / 2;
                if (p < boundary) return { ref: b.el, position: "before" };
            }

            // After the last boundary: append.
            return { ref: null, position: "append" };
        } catch {
            return { ref: null, position: "append" };
        }
    }

    function isBadMoveContainerTag(tag) {
        const t = String(tag || "").toLowerCase();
        return t === "html" || t === "body" || t === "head" || t === "script" || t === "style";
    }

    function isTableStructureTag(tag) {
        const t = String(tag || "").toLowerCase();
        return t === "tr" || t === "thead" || t === "tbody" || t === "table";
    }

    function isInteractiveTag(tag) {
        const t = String(tag || "").toLowerCase();
        return t === "button" || t === "a" || t === "input" || t === "select" || t === "textarea" || t === "label" || t === "option";
    }

    function countVisibleElementChildren(container, excludeEl) {
        try {
            if (!container || container.nodeType !== 1) return 0;
            let n = 0;
            for (const c of Array.from(container.children || [])) {
                if (!c || c.nodeType !== 1) continue;
                if (excludeEl && c === excludeEl) continue;
                const r = c.getBoundingClientRect();
                if (!(r.width > 0 && r.height > 0)) continue;
                n++;
                if (n >= 2) return n;
            }
            return n;
        } catch {
            return 0;
        }
    }

    function findPeerMoveContainer(startEl, source) {
        try {
            let cur = startEl;
            for (let i = 0; cur && cur.nodeType === 1 && i < 8; i++) {
                const p = cur.parentElement;
                if (!p || p.nodeType !== 1) break;

                const pTag = String(p.tagName || "").toLowerCase();
                if (isBadMoveContainerTag(pTag)) break;

                // Never insert into internal table structure nodes.
                if (isTableStructureTag(pTag)) {
                    cur = p;
                    continue;
                }

                // Avoid inserting into interactive controls.
                if (isInteractiveTag(pTag)) {
                    cur = p;
                    continue;
                }

                // Choose the first ancestor parent that has multiple visible element children.
                const cnt = countVisibleElementChildren(p, source);
                if (cnt >= 2) return p;

                cur = p;
            }
        } catch { }
        return startEl && startEl.parentElement ? startEl.parentElement : null;
    }

    function computeMovePlacement(source, rawTarget, clientX, clientY) {
        try {
            if (!source || source.nodeType !== 1) return null;
            if (!rawTarget || rawTarget.nodeType !== 1) return null;
            if (rawTarget === source) return null;
            if (source.contains && source.contains(rawTarget)) return null;

            const srcIsCell = isTableCell(source);

            // If dropping a non-cell element onto a TD/TH, place it inside the cell.
            if (!srcIsCell) {
                const cell = isTableCell(rawTarget) ? rawTarget : (rawTarget.closest ? rawTarget.closest("td,th") : null);
                if (cell) {
                    const cellTag = String(cell.tagName || "").toLowerCase();

                    function looksLikeSortIndicator(el) {
                        try {
                            if (!el || el.nodeType !== 1) return false;
                            const t = String(el.tagName || "").toLowerCase();
                            const txt = String(el.textContent || "").trim();
                            const cls = String(el.className || "").toLowerCase();
                            if (!txt && (t === "i" || t === "svg" || t === "path" || t === "use")) return true;
                            if (cls.includes("sort") || cls.includes("sorting") || cls.includes("order")) return !txt;
                            const ariaHidden = el.getAttribute ? String(el.getAttribute("aria-hidden") || "") : "";
                            if (ariaHidden === "true" && !txt) return true;
                            return false;
                        } catch {
                            return false;
                        }
                    }

                    function findHeaderTitleContainer(startEl) {
                        try {
                            if (cellTag !== "th") return null;
                            if (startEl && startEl.closest) {
                                const closest = startEl.closest(".dt-column-title");
                                if (closest && cell.contains(closest)) return closest;
                            }
                            const direct = cell.querySelector ? cell.querySelector(".dt-column-title") : null;
                            if (direct) return direct;

                            // Heuristic: pick the closest text-bearing element under the cursor.
                            if (!cell.querySelectorAll) return null;
                            const candidates = Array.from(cell.querySelectorAll("span,a,label,strong,em,div"))
                                .filter((el) => {
                                    if (!el || el.nodeType !== 1) return false;
                                    if (looksLikeSortIndicator(el)) return false;
                                    const txt = String(el.textContent || "").trim();
                                    if (!txt) return false;
                                    const tag = String(el.tagName || "").toLowerCase();
                                    if (tag === "div") {
                                        // Avoid picking large container divs unless they are actually the label.
                                        if (el.querySelector && el.querySelector("input,select,textarea,button")) return false;
                                    }
                                    return true;
                                });

                            if (!candidates.length) return null;
                            let best = null;
                            let bestScore = Number.POSITIVE_INFINITY;
                            for (const el of candidates) {
                                const r = el.getBoundingClientRect();
                                if (!(r.width > 0 && r.height > 0)) continue;
                                const inside = (clientX >= r.left && clientX <= r.right && clientY >= r.top && clientY <= r.bottom);
                                const cx = r.left + (r.width / 2);
                                const cy = r.top + (r.height / 2);
                                const dx = clientX - cx;
                                const dy = clientY - cy;
                                const d = (dx * dx) + (dy * dy);
                                const score = (inside ? 0 : 1e6) + d;
                                if (score < bestScore) { bestScore = score; best = el; }
                            }
                            return best;
                        } catch {
                            return null;
                        }
                    }

                    function ensureInlineTextWrapper(containerEl) {
                        try {
                            if (!containerEl || containerEl.nodeType !== 1) return null;
                            if (containerEl.childElementCount > 0) return null;

                            const hasText = Array.from(containerEl.childNodes || []).some((n) => n && n.nodeType === 3 && String(n.nodeValue || "").trim().length > 0);
                            if (!hasText) return null;

                            const wrap = document.createElement("span");
                            wrap.setAttribute("data-uiedit-textwrap", "1");
                            // Move ALL child nodes (text/comments) into wrapper.
                            while (containerEl.firstChild) wrap.appendChild(containerEl.firstChild);
                            containerEl.appendChild(wrap);
                            return wrap;
                        } catch {
                            return null;
                        }
                    }

                    // Prefer precision: if the pointer is over a specific element inside the cell,
                    // insert relative to that element within its immediate parent.
                    let innerRef = null;
                    try {
                        // Prefer the actual element under the cursor (drop targets can be unreliable).
                        const stack = (document.elementsFromPoint && Number.isFinite(clientX) && Number.isFinite(clientY))
                            ? document.elementsFromPoint(clientX, clientY)
                            : null;
                        if (Array.isArray(stack) && stack.length) {
                            for (const el of stack) {
                                if (!el || el.nodeType !== 1) continue;
                                if (el === source) continue;
                                if (source.contains && source.contains(el)) continue;
                                if (el === cell) continue;
                                if (cell.contains(el)) { innerRef = el; break; }
                            }
                        }
                        if (!innerRef && rawTarget !== cell && cell.contains(rawTarget)) innerRef = rawTarget;
                    } catch { }

                    // Table header handling: prefer the actual title container over sort glyphs/wrappers.
                    try {
                        if (cellTag === "th") {
                            const title = findHeaderTitleContainer(innerRef || cell);
                            if (title) innerRef = title;
                            if (innerRef && looksLikeSortIndicator(innerRef)) {
                                const title2 = findHeaderTitleContainer(innerRef);
                                if (title2) innerRef = title2;
                            }
                        }
                        if (innerRef === cell) {
                            const title3 = findHeaderTitleContainer(cell);
                            if (title3) innerRef = title3;
                        }
                    } catch { }

                    if (innerRef && innerRef.parentElement) {
                        // Special case: dropping over a label/title container should insert INSIDE it.
                        // DataTables headers often use spans for the column title; inserting as a sibling
                        // makes the icon jump to the cell edge.
                        try {
                            const t = String(innerRef.tagName || "").toLowerCase();
                            const textish = (t === "span" || t === "a" || t === "label" || t === "strong" || t === "em" || t === "div");
                            const hasText = String(innerRef.textContent || "").trim().length > 0;
                            if (textish && hasText) {
                                // If this is pure text (no element children), wrap text so we can insert
                                // before/after that wrapper using the same sibling-placement logic.
                                if (innerRef.childElementCount === 0) ensureInlineTextWrapper(innerRef);

                                const picked = pickNearestChildPlacement(innerRef, clientX, clientY, source);
                                if (!picked.ref) {
                                    const rr = innerRef.getBoundingClientRect();
                                    const midX = rr.left + (rr.width / 2);
                                    const position = (clientX < midX) ? "prepend" : "append";
                                    return { container: innerRef, ref: null, position, hintEl: innerRef, hintPos: (position === "prepend") ? "before" : "after" };
                                }
                                return { container: innerRef, ref: picked.ref, position: picked.position, hintEl: picked.ref, hintPos: picked.position };
                            }
                        } catch { }

                        const innerContainer = innerRef.parentElement;
                        const tag = String(innerContainer.tagName || "").toLowerCase();
                        // Never insert non-cell nodes directly into a table row/section.
                        if (tag !== "tr" && tag !== "thead" && tag !== "tbody" && tag !== "table" && cell.contains(innerContainer)) {
                            if (!(innerContainer === source) && !(source.contains && source.contains(innerContainer))) {
                                let position = "after";
                                try {
                                    const r = innerRef.getBoundingClientRect();
                                    const cs = getComputedStyle(innerContainer);
                                    const isFlex = (cs.display || "").includes("flex");
                                    const isColumn = isFlex && String(cs.flexDirection || "").toLowerCase().startsWith("column");
                                    if (isColumn) {
                                        const midY = r.top + (r.height / 2);
                                        position = (clientY < midY) ? "before" : "after";
                                    } else {
                                        const midX = r.left + (r.width / 2);
                                        position = (clientX < midX) ? "before" : "after";
                                    }
                                } catch { }
                                return { container: innerContainer, ref: innerRef, position, hintEl: innerRef, hintPos: position };
                            }
                        }
                    }

                    // Fallback: choose placement among direct children of the cell.
                    const picked = pickNearestChildPlacement(cell, clientX, clientY, source);
                    if (!picked.ref) {
                        // No children: decide append/prepend by X.
                        const cr = cell.getBoundingClientRect();
                        const midX = cr.left + (cr.width / 2);
                        const position = (clientX < midX) ? "prepend" : "append";
                        return { container: cell, ref: null, position, hintEl: cell, hintPos: (position === "prepend") ? "before" : "after" };
                    }
                    return { container: cell, ref: picked.ref, position: picked.position, hintEl: picked.ref, hintPos: picked.position };
                }
            }

            // Default behavior (non-table): choose insertion based on cursor position among
            // siblings in the target's parent container. This makes horizontal toolbars,
            // header button rows, etc. behave like “drop where released”.
            let target = rawTarget;
            try {
                const stack = (document.elementsFromPoint && Number.isFinite(clientX) && Number.isFinite(clientY))
                    ? document.elementsFromPoint(clientX, clientY)
                    : null;
                if (Array.isArray(stack) && stack.length) {
                    for (const el of stack) {
                        if (!el || el.nodeType !== 1) continue;
                        if (el === source) continue;
                        if (source.contains && source.contains(el)) continue;
                        const tag = String(el.tagName || "").toLowerCase();
                        if (tag === "html" || tag === "body") continue;
                        target = el;
                        break;
                    }
                }
            } catch { }

            const container = findPeerMoveContainer(target, source);
            if (!container || container.nodeType !== 1) return null;

            // Avoid inserting directly into table structure nodes.
            try {
                const tag = String(container.tagName || "").toLowerCase();
                if (tag === "tr" || tag === "thead" || tag === "tbody" || tag === "table") {
                    const up = container.parentElement;
                    if (up && up.nodeType === 1) {
                        const picked2 = pickNearestChildPlacement(up, clientX, clientY, source);
                        return { container: up, ref: picked2.ref, position: picked2.position, hintEl: picked2.ref || up, hintPos: picked2.ref ? picked2.position : "after" };
                    }
                }
            } catch { }

            const picked = pickNearestChildPlacement(container, clientX, clientY, source);
            return { container, ref: picked.ref, position: picked.position, hintEl: picked.ref || container, hintPos: picked.ref ? picked.position : "after" };
        } catch {
            return null;
        }
    }

    // MOVE mode: drag selected element and drop before/after a target.
    document.addEventListener("dragstart", (e) => {
        if (__uieditMode !== "move") return;
        const t = e && e.target;
        if (!t || t.nodeType !== 1) return;
        if (!__uieditSelectedEl || t !== __uieditSelectedEl) return;

        initUiEditKeysOnce();
        __uieditDragSource = __uieditSelectedEl;

        try {
            const r = __uieditDragSource.getBoundingClientRect();
            __uieditDragGrabDx = (Number.isFinite(e.clientX) ? (e.clientX - r.left) : (r.width / 2));
            __uieditDragGrabDy = (Number.isFinite(e.clientY) ? (e.clientY - r.top) : (r.height / 2));
        } catch {
            __uieditDragGrabDx = 0;
            __uieditDragGrabDy = 0;
        }

        try {
            e.dataTransfer.effectAllowed = "move";
            e.dataTransfer.setData("text/plain", cssPath(__uieditSelectedEl));
        } catch { }
    }, true);

    document.addEventListener("dragover", (e) => {
        if (__uieditMode !== "move") return;
        if (!__uieditDragSource) return;

        const t = e && e.target;
        if (!t || t.nodeType !== 1) return;

        const placement = computeMovePlacement(__uieditDragSource, t, e.clientX, e.clientY);
        if (!placement) return;

        e.preventDefault();

        try { setDropHint(placement.hintEl || t, placement.hintPos || "after"); } catch { }
    }, true);

    document.addEventListener("dragleave", (e) => {
        if (__uieditMode !== "move") return;
        if (!__uieditDragSource) return;
        const t = e && e.target;
        if (__uieditDropTarget && t === __uieditDropTarget) clearDropHint();
    }, true);

    document.addEventListener("drop", (e) => {
        if (__uieditMode !== "move") return;
        if (!__uieditDragSource) return;

        const t = e && e.target;
        if (!t || t.nodeType !== 1) {
            __uieditDragSource = null;
            clearDropHint();
            return;
        }

        e.preventDefault();

        const source = __uieditDragSource;
        __uieditDragSource = null;

        // Resolve the actual element under the cursor. e.target is often unreliable
        // when dragging across sections (icons/overlays can become the drop target).
        let dropEl = t;
        try {
            const x = e.clientX;
            const y = e.clientY;
            const stack = (document.elementsFromPoint && Number.isFinite(x) && Number.isFinite(y))
                ? document.elementsFromPoint(x, y)
                : null;
            if (Array.isArray(stack) && stack.length) {
                for (const el of stack) {
                    if (!el || el.nodeType !== 1) continue;
                    if (el === source) continue;
                    if (source.contains && source.contains(el)) continue;
                    const tag = String(el.tagName || "").toLowerCase();
                    if (tag === "html" || tag === "body") continue;
                    dropEl = el;
                    break;
                }
            }
        } catch { }

        try {
            function isTableStructure(el) {
                try {
                    if (!el || el.nodeType !== 1) return false;
                    const tag = String(el.tagName || "").toLowerCase();
                    return tag === "table" || tag === "thead" || tag === "tbody" || tag === "tfoot" || tag === "tr" || tag === "td" || tag === "th";
                } catch { }
                return false;
            }

            function canContain(el) {
                try {
                    if (!el || el.nodeType !== 1) return false;
                    const tag = String(el.tagName || "").toLowerCase();
                    if (!tag) return false;
                    if (tag === "html" || tag === "body") return false;
                    const voidTags = new Set(["area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"]);
                    if (voidTags.has(tag)) return false;
                    return true;
                } catch { }
                return false;
            }

            // Pixel-accurate drop for table cells/headers.
            const cell = (isTableCell(dropEl) ? dropEl : (dropEl.closest ? dropEl.closest("td,th") : null));
            if (cell && Number.isFinite(e.clientX) && Number.isFinite(e.clientY)) {
                try {
                    const cs = getComputedStyle(cell);
                    if (String(cs.position || "").toLowerCase() === "static") {
                        cell.style.position = "relative";
                    }
                } catch { }

                const cr = cell.getBoundingClientRect();
                const snap = snapshotSizeForAbs(source);
                try { cell.appendChild(source); } catch { }

                const left = (e.clientX - cr.left - (__uieditDragGrabDx || 0));
                const top = (e.clientY - cr.top - (__uieditDragGrabDy || 0));

                freezeSizeForAbs(source, snap);
                source.style.position = "absolute";
                source.style.left = left + "px";
                source.style.top = top + "px";
                try {
                    source.style.right = "";
                    source.style.bottom = "";
                } catch { }

                initUiEditKeysOnce();

                try {
                    window.parent.postMessage({
                        type: "uiedit:movedAbs",
                        sourceSelector: cssPath(source),
                        containerSelector: cssPath(cell),
                        left,
                        top
                    }, "*");
                } catch { }

                try { setSelectedHighlight(source); } catch { }
                return;
            }

            // Free placement: move the element into a container and absolutely position it.
            // We skip table-structure nodes to avoid breaking table layout; those fall back to the reorder behavior below.
            if (!isTableStructure(source) && Number.isFinite(e.clientX) && Number.isFinite(e.clientY)) {
                let container = null;
                try {
                    const placementForAbs = computeMovePlacement(source, dropEl, e.clientX, e.clientY);
                    if (placementForAbs && placementForAbs.container && canContain(placementForAbs.container)) {
                        container = placementForAbs.container;
                    }
                } catch { }

                if (!container) {
                    try {
                        // Walk up from the actual drop element to find a reasonable container.
                        let cur = dropEl;
                        for (let i = 0; cur && i < 12; i++) {
                            if (canContain(cur)) { container = cur; break; }
                            cur = cur.parentElement;
                        }
                    } catch { }
                }

                if (!container) {
                    try {
                        const p = source.parentElement;
                        if (p && canContain(p)) container = p;
                    } catch { }
                }

                if (container && container.nodeType === 1) {
                    try {
                        const cs = getComputedStyle(container);
                        if (String(cs.position || "").toLowerCase() === "static") {
                            container.style.position = "relative";
                        }
                    } catch { }

                    const cr = container.getBoundingClientRect();
                    const snap = snapshotSizeForAbs(source);
                    try { container.appendChild(source); } catch { }

                    const left = (e.clientX - cr.left - (__uieditDragGrabDx || 0));
                    const top = (e.clientY - cr.top - (__uieditDragGrabDy || 0));

                    freezeSizeForAbs(source, snap);
                    source.style.position = "absolute";
                    source.style.left = left + "px";
                    source.style.top = top + "px";
                    try {
                        source.style.right = "";
                        source.style.bottom = "";
                    } catch { }

                    initUiEditKeysOnce();

                    try {
                        window.parent.postMessage({
                            type: "uiedit:movedAbs",
                            sourceSelector: cssPath(source),
                            containerSelector: cssPath(container),
                            left,
                            top
                        }, "*");
                    } catch { }

                    try { setSelectedHighlight(source); } catch { }
                    return;
                }
            }

            const placement = computeMovePlacement(source, dropEl, e.clientX, e.clientY);
            if (!placement) return;

            const container = placement.container;
            const ref = placement.ref;
            const pos = placement.position;
            if (!container) return;

            if (pos === "append") {
                container.appendChild(source);
            } else if (pos === "prepend") {
                container.insertBefore(source, container.firstChild);
            } else if (pos === "before") {
                if (!ref) return;
                container.insertBefore(source, ref);
            } else {
                if (!ref) return;
                container.insertBefore(source, ref.nextSibling);
            }

            initUiEditKeysOnce();

            try {
                window.parent.postMessage({
                    type: "uiedit:moved",
                    sourceSelector: cssPath(source),
                    containerSelector: cssPath(container),
                    referenceSelector: ref ? cssPath(ref) : "",
                    position: pos
                }, "*");
            } catch { }

            // Keep selection on moved element.
            try { setSelectedHighlight(source); } catch { }
        } finally {
            clearDropHint();
        }
    }, true);

    document.addEventListener("dragend", () => {
        if (__uieditMode !== "move") return;
        __uieditDragSource = null;
        clearDropHint();
    }, true);

    // IMAGE mode: drag selected image to move it.
    document.addEventListener("mousedown", (e) => {
        try {
            if (__uieditMode !== "image") return;
            if (!e || e.button !== 0) return;
            if (!__uieditSelectedEl) return;

            const t = e.target;
            if (!t || t.nodeType !== 1) return;
            if (t !== __uieditSelectedEl) return;

            const tag = String(__uieditSelectedEl.tagName || "").toLowerCase();
            if (tag !== "img") return;

            e.preventDefault();
            e.stopPropagation();
            startImgMove(__uieditSelectedEl, e.clientX, e.clientY);
        } catch { }
    }, true);

    document.addEventListener("click", (e) => {
        // Persistently highlight the clicked element while it is selected.
        try { setSelectedHighlight(e.target); } catch { }

        e.preventDefault();
        e.stopPropagation();


        const target = e.target;
        if (!target || target.nodeType !== 1) return;

        initUiEditKeysOnce();

        try { postSelected(target); } catch { }
    }, true);
})();
