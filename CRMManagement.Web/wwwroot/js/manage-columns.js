// Generic per-table column manager. Any <table data-cm-table="<id>"> in the page
// gets a "Manage Columns" button that opens a checklist modal. Selection is
// persisted in localStorage under "cm:<id>" so each user's choices stick across
// reloads. Column labels are auto-extracted from the first header row's <th>
// text; pages can override per-th via data-cm-label="...". Empty header cells
// (action columns) are pinned and can't be hidden.
(function () {
    'use strict';

    var MODAL_ID = 'cm-modal-root';
    var DEBUG = true;
    function log() { if (DEBUG && window.console) console.log.apply(console, ['[manage-columns]'].concat([].slice.call(arguments))); }
    function warn() { if (window.console) console.warn.apply(console, ['[manage-columns]'].concat([].slice.call(arguments))); }

    function trim(s) { return (s || '').replace(/\s+/g, ' ').trim(); }

    function labelFor(th) {
        if (th.dataset.cmLabel) return th.dataset.cmLabel.trim();
        var clone = th.cloneNode(true);
        // Strip filter/sort widgets so we get just the column name
        clone.querySelectorAll('input, button, select, textarea, .col-filter, .filter-row, [data-cm-strip]').forEach(function (n) { n.remove(); });
        var text = trim(clone.textContent);
        var first = text.split('\n').map(function (s) { return s.trim(); }).filter(Boolean)[0];
        return first || '';
    }

    function readHidden(key) {
        try {
            var raw = localStorage.getItem(key);
            return raw ? JSON.parse(raw) : null; // null means "no saved prefs"
        } catch (e) { return null; }
    }
    function readHiddenOrDefault(key, defaults) {
        var saved = readHidden(key);
        return saved == null ? defaults.slice() : saved;
    }

    function writeHidden(key, list) {
        try { localStorage.setItem(key, JSON.stringify(list)); } catch (e) { }
    }

    function applyVisibility(table, hidden) {
        // Apply to ALL header rows (so filter rows' cells also hide), and all body cells.
        // We hide by data-col-idx so any cell sharing the index (across header+body) hides.
        function visit(cell, i) {
            var idx = cell.dataset.colIdx != null ? cell.dataset.colIdx : String(i);
            if (hidden.indexOf(idx) >= 0) cell.setAttribute('data-cm-hidden', '');
            else cell.removeAttribute('data-cm-hidden');
        }
        table.querySelectorAll('thead > tr').forEach(function (tr) {
            for (var i = 0; i < tr.children.length; i++) visit(tr.children[i], i);
        });
        table.querySelectorAll('tbody > tr').forEach(function (tr) {
            for (var i = 0; i < tr.children.length; i++) visit(tr.children[i], i);
        });
        table.querySelectorAll('tfoot > tr').forEach(function (tr) {
            for (var i = 0; i < tr.children.length; i++) visit(tr.children[i], i);
        });
        // Re-style the visible last cell (post-hide) so any "last column" border rules
        // applied via :last-child don't keep a phantom right-border on a column that's
        // no longer visually last.
        markLastVisible(table);
        updateTriggerBadge(table, hidden);
    }

    function markLastVisible(table) {
        function reTag(rows) {
            rows.forEach(function (tr) {
                var cells = tr.children;
                var lastVisible = null;
                for (var i = 0; i < cells.length; i++) {
                    cells[i].removeAttribute('data-cm-last-visible');
                    if (!cells[i].hasAttribute('data-cm-hidden')) lastVisible = cells[i];
                }
                if (lastVisible) lastVisible.setAttribute('data-cm-last-visible', '');
            });
        }
        reTag(table.querySelectorAll('thead > tr'));
        reTag(table.querySelectorAll('tbody > tr'));
        reTag(table.querySelectorAll('tfoot > tr'));
    }

    function updateTriggerBadge(table, hidden) {
        var trigger = table.__cmTrigger;
        if (!trigger) return;
        var badge = trigger.querySelector('.cm-trigger-badge');
        var hideable = (hidden || []).filter(function (idx) {
            // pinned columns don't count toward "hidden" since they can't be unhidden
            var th = table.querySelector('thead > tr > th[data-col-idx="' + idx + '"]');
            return th && !(th.dataset.cmPin === '1' || labelFor(th) === '');
        });
        if (hideable.length === 0) {
            if (badge) badge.remove();
        } else {
            if (!badge) {
                badge = document.createElement('span');
                badge.className = 'cm-trigger-badge';
                trigger.appendChild(badge);
            }
            badge.textContent = hideable.length + ' hidden';
        }
    }

    function ensureModalRoot() {
        var root = document.getElementById(MODAL_ID);
        if (root) return root;
        root = document.createElement('div');
        root.id = MODAL_ID;
        root.className = 'cm-modal-back';
        root.innerHTML = ''
            + '<div class="cm-modal" role="dialog" aria-modal="true" aria-labelledby="cm-modal-title">'
            + '  <div class="cm-modal-header">'
            + '    <h3 id="cm-modal-title">Manage Columns</h3>'
            + '    <button type="button" class="cm-close" aria-label="Close">&times;</button>'
            + '  </div>'
            + '  <input type="text" class="cm-search" placeholder="Search…" autocomplete="off" />'
            + '  <div class="cm-list" role="list"></div>'
            + '  <div class="cm-modal-footer">'
            + '    <button type="button" class="cm-btn cm-btn-link" data-cm-reset>Reset to default</button>'
            + '    <button type="button" class="cm-btn cm-btn-primary" data-cm-save>Save</button>'
            + '  </div>'
            + '</div>';
        document.body.appendChild(root);
        return root;
    }

    function openModal(opts) {
        var root = ensureModalRoot();
        var listEl = root.querySelector('.cm-list');
        var searchEl = root.querySelector('.cm-search');
        var saveBtn = root.querySelector('[data-cm-save]');
        var resetBtn = root.querySelector('[data-cm-reset]');
        var closeBtn = root.querySelector('.cm-close');

        var current = opts.hidden.slice();
        listEl.innerHTML = opts.columns.map(function (col) {
            var visible = current.indexOf(col.idx) < 0;
            var disabled = col.pinned ? 'disabled' : '';
            var checked = (col.pinned || visible) ? 'checked' : '';
            return ''
                + '<label class="cm-row" data-cm-label-row>'
                + '  <input type="checkbox" data-col="' + col.idx + '" ' + disabled + ' ' + checked + ' />'
                + '  <span>' + col.label + (col.pinned ? '<span class="cm-pin">required</span>' : '') + '</span>'
                + '</label>';
        }).join('');

        searchEl.value = '';
        searchEl.oninput = function () {
            var q = searchEl.value.toLowerCase();
            listEl.querySelectorAll('[data-cm-label-row]').forEach(function (row) {
                row.style.display = row.textContent.toLowerCase().indexOf(q) >= 0 ? '' : 'none';
            });
        };

        function close() { root.classList.remove('open'); }
        closeBtn.onclick = close;
        root.onclick = function (e) { if (e.target === root) close(); };

        saveBtn.onclick = function () {
            var hidden = [];
            listEl.querySelectorAll('input[type=checkbox][data-col]').forEach(function (cb) {
                if (!cb.checked && !cb.disabled) hidden.push(cb.dataset.col);
            });
            writeHidden(opts.storageKey, hidden);
            applyVisibility(opts.table, hidden);
            opts.onChange(hidden);
            close();
        };

        resetBtn.onclick = function () {
            // Reset clears the user's overrides, restoring the per-table defaults.
            try { localStorage.removeItem(opts.storageKey); } catch (e) { }
            var fallback = opts.defaultHidden || [];
            applyVisibility(opts.table, fallback);
            opts.onChange(fallback);
            close();
        };

        root.classList.add('open');
    }

    function setupTable(table) {
        var tableId = table.dataset.cmTable;
        if (!tableId || table.dataset.cmReady === '1') return;

        // Only the FIRST row of <thead> defines the columns. Other thead rows
        // (filter inputs, secondary headers) just align to the same indices.
        var firstHeader = table.querySelector('thead > tr');
        if (!firstHeader) return;
        var ths = firstHeader.children;
        if (!ths.length) return;

        table.dataset.cmReady = '1';
        log('setup', tableId, 'columns:', ths.length);

        var columns = [];
        var defaultHidden = [];
        for (var i = 0; i < ths.length; i++) {
            var th = ths[i];
            // Auto-assign data-col-idx if missing so the column index is stable
            if (th.dataset.colIdx == null) th.dataset.colIdx = String(i);
            var label = labelFor(th);
            var pinned = !label || th.dataset.cmPin === '1';
            var defaultHide = th.dataset.cmDefaultHide === '1';
            columns.push({
                idx: th.dataset.colIdx,
                label: label || '(actions)',
                pinned: pinned,
                defaultHide: defaultHide
            });
            if (defaultHide && !pinned) defaultHidden.push(th.dataset.colIdx);
        }

        // Mirror data-col-idx onto secondary header rows + body + foot cells by position
        function mirrorRow(tr) {
            for (var j = 0; j < tr.children.length && j < columns.length; j++) {
                var c = tr.children[j];
                if (c.dataset.colIdx == null) c.dataset.colIdx = columns[j].idx;
            }
        }
        var headRows = table.querySelectorAll('thead > tr');
        for (var hi = 1; hi < headRows.length; hi++) mirrorRow(headRows[hi]);
        table.querySelectorAll('tbody > tr').forEach(mirrorRow);
        table.querySelectorAll('tfoot > tr').forEach(mirrorRow);

        var storageKey = 'cm:' + tableId;
        var hidden = readHiddenOrDefault(storageKey, defaultHidden).filter(function (idx) {
            return !columns.some(function (c) { return c.idx === idx && c.pinned; });
        });
        applyVisibility(table, hidden);

        var trigger = document.createElement('button');
        trigger.type = 'button';
        trigger.className = 'cm-trigger';
        trigger.title = 'Manage columns';
        trigger.innerHTML = '<i class="fa-solid fa-table-columns" aria-hidden="true"></i><span>Columns</span>';
        table.__cmTrigger = trigger;
        // Reflect any default-hidden columns in the badge from first paint.
        updateTriggerBadge(table, hidden);
        trigger.addEventListener('click', function () {
            openModal({
                table: table,
                columns: columns,
                hidden: readHiddenOrDefault(storageKey, defaultHidden),
                storageKey: storageKey,
                defaultHidden: defaultHidden,
                onChange: function (next) { hidden = next; }
            });
        });

        // Placement strategy:
        //   1) Saved-views toolbar (.svb-shell) in same card → append there
        //   2) Existing toolbar (.list-toolbar, .crm-toolbar, [data-cm-toolbar]) → append
        //   3) Dashboard panel header (.crm-dash-panel > div) → append next to icon
        //   4) Otherwise, insert a new toolbar above the table
        var card = table.closest('.rounded-2xl, .crm-dash-panel, [data-cm-host]') || table.parentElement;
        var anchor = null;
        if (card) {
            anchor = card.querySelector('.svb-shell, .list-toolbar, .crm-toolbar, [data-cm-toolbar]');
        }
        // Dashboard widgets: tuck the trigger into the header row beside the icon
        if (!anchor && card && card.classList.contains('crm-dash-panel')) {
            var header = card.querySelector(':scope > div.flex.items-center.justify-between');
            if (header) anchor = header;
        }
        if (anchor) {
            trigger.classList.add('cm-trigger-inline');
            anchor.appendChild(trigger);
            log('placed inline for', tableId);
        } else {
            var scrollWrap = table.closest('.overflow-x-auto, .overflow-auto');
            var insertParent = scrollWrap ? scrollWrap.parentElement : table.parentElement;
            var insertBefore = scrollWrap || table;
            var bar = document.createElement('div');
            bar.className = 'cm-trigger-bar';
            bar.appendChild(trigger);
            if (insertParent && insertBefore) {
                insertParent.insertBefore(bar, insertBefore);
                log('placed bar above table for', tableId);
            } else {
                warn('could not place trigger for', tableId);
            }
        }
    }

    function init() {
        var tables = document.querySelectorAll('table[data-cm-table]');
        log('init: found', tables.length, 'tagged tables on', window.location.pathname);
        tables.forEach(function (t) {
            try { setupTable(t); }
            catch (e) { warn('setup failed for', t.dataset.cmTable, e); }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else { init(); }

    // Re-run when AJAX/Razor partials replace tables
    var mo = new MutationObserver(function () { init(); });
    mo.observe(document.body, { childList: true, subtree: true });

    // Expose for debugging from the console
    window.__cm = { init: init, applyVisibility: applyVisibility };
})();
