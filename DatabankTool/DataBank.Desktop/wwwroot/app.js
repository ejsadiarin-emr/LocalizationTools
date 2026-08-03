(function () {
    'use strict';

    // State
    let allEntries = [];
    let filteredEntries = [];
    let currentPage = 1;
    let sortColumn = '';
    let sortDirection = 'asc';
    const PAGE_SIZE = 50;
    const DEFAULT_LOCALES = ['en', 'zh-CN', 'ru', 'ja'];
    let selectedLocales = new Set();

    // DOM refs
    const dashboardSection = document.getElementById('dashboard-section');
    const tableSection = document.getElementById('table-section');
    const tableBody = document.getElementById('table-body');
    const pagination = document.getElementById('pagination');
    const noDataMessage = document.getElementById('no-data-message');
    const searchInput = document.getElementById('search-input');
    const localeTrigger = document.getElementById('locale-trigger');
    const localeDropdown = document.getElementById('locale-dropdown');
    const localeOptions = document.getElementById('locale-options');
    const localeSelectAll = document.getElementById('locale-select-all');
    const localeClearAll = document.getElementById('locale-clear-all');
    const formatFilter = document.getElementById('format-filter');
    const statusFilter = document.getElementById('status-filter');
    const detailPanel = document.getElementById('detail-panel');
    const detailContent = document.getElementById('detail-content');
    const detailClose = document.getElementById('detail-close');

    // --- Tab Navigation ---
    var tabBtns = document.querySelectorAll('.tab-btn');
    var tabContents = document.querySelectorAll('.tab-content');

    tabBtns.forEach(function (btn) {
        btn.addEventListener('click', function () {
            var targetTab = btn.getAttribute('data-tab');
            tabBtns.forEach(function (b) { b.classList.remove('active'); });
            tabContents.forEach(function (c) { c.classList.remove('active'); });
            btn.classList.add('active');
            document.getElementById('tab-' + targetTab).classList.add('active');
        });
    });

    // --- WebView2 Message Handler ---
    function handleMessage(data) {
        if (data && data.action === 'loadData') {
            allEntries = data.entries || [];
            currentPage = 1;
            onDataLoaded();
        }
    }

    // --- Exclude GRF entries from main table ---
    function isGrfEntry(e) {
        return getEntryFormat(e) === 'grf';
    }

    function getTableEntries() {
        return allEntries.filter(function (e) { return !isGrfEntry(e); });
    }

    function onDataLoaded() {
        populateFilters();
        applyFilters();
        updateDashboard();
        renderGrfTab();
        noDataMessage.classList.add('hidden');
    }

    // --- Helper: Get locale value from entry ---
    function getLocaleValue(entry, locale) {
        if (!entry.values) return '';
        var found = entry.values.find(function (v) { return v.locale === locale; });
        return found ? found.value : '';
    }

    // --- Helper: Get all locales present in data (excluding GRF) ---
    function getAllLocales() {
        var localeSet = new Set();
        getTableEntries().forEach(function (e) {
            if (e.values) {
                e.values.forEach(function (v) { localeSet.add(v.locale); });
            }
        });
        return Array.from(localeSet).sort();
    }

    // --- Helper: Get format from entry sources ---
    function getEntryFormat(entry) {
        if (!entry.sources) return '';
        var keys = Object.keys(entry.sources);
        return keys.length > 0 ? entry.sources[keys[0]].format : '';
    }

    // --- Dashboard ---
    function updateDashboard() {
        var tableEntries = getTableEntries();
        var total = tableEntries.length;
        var allLocales = getAllLocales();
        var formats = new Set();
        tableEntries.forEach(function (e) {
            var fmt = getEntryFormat(e);
            if (fmt) formats.add(fmt);
        });

        var translated = tableEntries.filter(function (e) { return getStatus(e) === 'translated'; }).length;
        var untranslated = tableEntries.filter(function (e) { return getStatus(e) === 'untranslated'; }).length;

        document.getElementById('stat-total').textContent = total;
        document.getElementById('stat-locales').textContent = allLocales.length;
        document.getElementById('stat-formats').textContent = formats.size;
        document.getElementById('stat-translated').textContent = translated;
        document.getElementById('stat-untranslated').textContent = untranslated;

        renderLocaleStats(allLocales, total);
    }

    function renderLocaleStats(locales, total) {
        var container = document.getElementById('locale-stats');
        container.innerHTML = '';

        locales.forEach(function (locale) {
            var count = getTableEntries().filter(function (e) {
                return getLocaleValue(e, locale) !== '';
            }).length;
            var pct = total > 0 ? Math.round((count / total) * 100) : 0;

            var row = document.createElement('div');
            row.className = 'locale-row';
            row.innerHTML =
                '<span class="locale-name">' + escapeHtml(locale) + '</span>' +
                '<div class="locale-bar-wrapper">' +
                '<div class="locale-bar" style="width:' + pct + '%"></div>' +
                '</div>' +
                '<span class="locale-count">' + count + ' (' + pct + '%)</span>';
            container.appendChild(row);
        });
    }

    // --- Status Logic ---
    function getStatus(entry) {
        if (entry.metadata && entry.metadata.doNotTranslate) {
            return 'do-not-translate';
        }
        if (!entry.metadata || !entry.metadata.isTranslated) {
            return 'untranslated';
        }
        return 'translated';
    }

    function getStatusLabel(status) {
        switch (status) {
            case 'translated': return 'Translated';
            case 'untranslated': return 'Untranslated';
            case 'do-not-translate': return 'Do Not Translate';
            default: return status;
        }
    }

    // --- Filters ---
    function populateFilters() {
        var locales = {};
        var formats = {};
        getTableEntries().forEach(function (e) {
            if (e.values) {
                e.values.forEach(function (v) { locales[v.locale] = true; });
            }
            var fmt = getEntryFormat(e);
            if (fmt && fmt !== 'grf') formats[fmt] = true;
        });

        populateLocaleCheckboxes(Object.keys(locales).sort());
        populateDropdown(formatFilter, Object.keys(formats).sort(), 'All Formats');
    }

    function populateLocaleCheckboxes(locales) {
        localeOptions.innerHTML = '';
        locales.forEach(function (loc) {
            var label = document.createElement('label');
            label.className = 'locale-multiselect-option';
            var checkbox = document.createElement('input');
            checkbox.type = 'checkbox';
            checkbox.value = loc;
            checkbox.checked = selectedLocales.has(loc);
            checkbox.addEventListener('change', function () {
                toggleLocale(loc);
            });
            label.appendChild(checkbox);
            label.appendChild(document.createTextNode(' ' + loc));
            localeOptions.appendChild(label);
        });
        updateTriggerDisplay();
    }

    // --- Locale Multi-Select Dropdown ---
    function toggleLocaleDropdown() {
        localeDropdown.classList.toggle('hidden');
    }

    function toggleLocale(locale) {
        if (selectedLocales.has(locale)) {
            selectedLocales.delete(locale);
        } else {
            selectedLocales.add(locale);
        }
        updateTriggerDisplay();
        updateCheckboxStates();
        updateStatusFilterOptions();
        applyFilters();
    }

    function selectAllLocales() {
        var checkboxes = localeOptions.querySelectorAll('input[type="checkbox"]');
        checkboxes.forEach(function (cb) {
            selectedLocales.add(cb.value);
            cb.checked = true;
        });
        updateTriggerDisplay();
        updateStatusFilterOptions();
        applyFilters();
    }

    function clearAllLocales() {
        selectedLocales.clear();
        var checkboxes = localeOptions.querySelectorAll('input[type="checkbox"]');
        checkboxes.forEach(function (cb) { cb.checked = false; });
        updateTriggerDisplay();
        updateStatusFilterOptions();
        applyFilters();
    }

    function updateTriggerDisplay() {
        if (selectedLocales.size === 0) {
            localeTrigger.textContent = 'All Locales';
        } else {
            var sorted = Array.from(selectedLocales).sort();
            localeTrigger.textContent = sorted.join(', ');
        }
    }

    function updateCheckboxStates() {
        var checkboxes = localeOptions.querySelectorAll('input[type="checkbox"]');
        checkboxes.forEach(function (cb) {
            cb.checked = selectedLocales.has(cb.value);
        });
    }

    function updateStatusFilterOptions() {
        var isSpecificLocale = selectedLocales.size > 0;
        var untranslatedOption = statusFilter.querySelector('option[value="untranslated"]');

        if (isSpecificLocale && untranslatedOption) {
            statusFilter.removeChild(untranslatedOption);
            if (statusFilter.value === 'untranslated') {
                statusFilter.value = '';
            }
        } else if (!isSpecificLocale && !untranslatedOption) {
            var allStatusOption = statusFilter.querySelector('option[value=""]');
            var option = document.createElement('option');
            option.value = 'untranslated';
            option.textContent = 'Untranslated';
            if (allStatusOption) {
                allStatusOption.insertAdjacentElement('afterend', option);
            } else {
                statusFilter.appendChild(option);
            }
        }
    }

    function populateDropdown(select, options, defaultLabel) {
        select.innerHTML = '<option value="">' + defaultLabel + '</option>';
        options.forEach(function (opt) {
            var option = document.createElement('option');
            option.value = opt;
            option.textContent = opt;
            select.appendChild(option);
        });
    }

    function applyFilters() {
        var format = formatFilter.value;
        var status = statusFilter.value;
        var search = searchInput.value.toLowerCase().trim();

        filteredEntries = getTableEntries().filter(function (e) {
            // Filter by locale: show entries that have a non-empty value for ANY selected locale (OR logic)
            if (selectedLocales.size > 0) {
                var hasAnyLocale = false;
                for (var loc of selectedLocales) {
                    if (getLocaleValue(e, loc) !== '') {
                        hasAnyLocale = true;
                        break;
                    }
                }
                if (!hasAnyLocale) return false;
            }

            // Filter by format
            if (format && getEntryFormat(e) !== format) return false;

            // Filter by status
            if (status && getStatus(e) !== status) return false;

            // Search across key and all locale values
            if (search) {
                var keyMatch = e.key && e.key.toLowerCase().indexOf(search) !== -1;
                var valueMatch = false;
                if (e.values) {
                    valueMatch = e.values.some(function (v) {
                        return v.value && v.value.toLowerCase().indexOf(search) !== -1;
                    });
                }
                if (!keyMatch && !valueMatch) return false;
            }
            return true;
        });

        if (sortColumn) {
            sortEntries();
        }

        currentPage = 1;
        renderTable();
        renderPagination();
        updateExportButtonState();
    }

    function sortEntries() {
        filteredEntries.sort(function (a, b) {
            var aVal = getSortValue(a, sortColumn);
            var bVal = getSortValue(b, sortColumn);
            if (aVal < bVal) return sortDirection === 'asc' ? -1 : 1;
            if (aVal > bVal) return sortDirection === 'asc' ? 1 : -1;
            return 0;
        });
    }

    function getSortValue(entry, column) {
        switch (column) {
            case 'key': return entry.key || '';
            case 'format': return getEntryFormat(entry);
            case 'status': return getStatus(entry);
            default: return '';
        }
    }

    // --- Table Rendering ---
    function renderTable() {
        tableBody.innerHTML = '';
        var start = (currentPage - 1) * PAGE_SIZE;
        var end = Math.min(start + PAGE_SIZE, filteredEntries.length);
        var pageEntries = filteredEntries.slice(start, end);

        // Determine which locales to show as columns
        var displayLocales;
        if (selectedLocales.size > 0) {
            // Show only selected locales
            displayLocales = Array.from(selectedLocales).sort();
        } else {
            // No selection: show all locales (backwards compatible default)
            displayLocales = DEFAULT_LOCALES.slice();
            var allLocales = getAllLocales();
            allLocales.forEach(function (loc) {
                if (displayLocales.indexOf(loc) === -1) {
                    displayLocales.push(loc);
                }
            });
        }

        // Update table header
        var thead = document.querySelector('#entries-table thead tr');
        if (thead) {
            var headerHtml = '<th data-sort="key" class="col-key">Key <span class="sort-arrow"></span></th>';
            headerHtml += '<th data-sort="format" class="col-format">Format <span class="sort-arrow"></span></th>';
            headerHtml += '<th data-sort="status" class="col-status">Status <span class="sort-arrow"></span></th>';
            displayLocales.forEach(function (loc) {
                headerHtml += '<th class="col-locale">' + escapeHtml(loc) + '</th>';
            });
            thead.innerHTML = headerHtml;

            // Re-attach sort listeners
            thead.querySelectorAll('th[data-sort]').forEach(function (th) {
                th.addEventListener('click', function () {
                    var col = th.getAttribute('data-sort');
                    if (sortColumn === col) {
                        sortDirection = sortDirection === 'asc' ? 'desc' : 'asc';
                    } else {
                        sortColumn = col;
                        sortDirection = 'asc';
                    }
                    updateSortIndicators();
                    applyFilters();
                });
            });
        }

        // Render rows
        pageEntries.forEach(function (entry) {
            var status = getStatus(entry);
            var tr = document.createElement('tr');
            tr.className = 'row-' + status;
            tr.setAttribute('data-key', entry.key);

            var rowHtml = '<td class="col-key" title="' + escapeAttr(entry.key) + '">' + escapeHtml(truncate(entry.key, 40)) + '</td>';

            var format = getEntryFormat(entry);
            rowHtml += '<td class="col-format"><span class="format-badge">' + escapeHtml(format) + '</span></td>';
            rowHtml += '<td class="col-status"><span class="status-badge status-' + status + '">' + getStatusLabel(status) + '</span></td>';

            displayLocales.forEach(function (loc) {
                var val = getLocaleValue(entry, loc);
                var titleAttr = escapeAttr(val);
                var displayVal = escapeHtml(truncate(val, 50));
                rowHtml += '<td class="locale-cell" data-locale="' + escapeAttr(loc) + '" data-key="' + escapeAttr(entry.key) + '" title="' + titleAttr + '">' + (displayVal || '<span class="empty-value">\u2014</span>') + '</td>';
            });
            tr.innerHTML = rowHtml;

            // Click to show detail
            tr.addEventListener('click', function (e) {
                // Don't show detail if clicking on editable cell
                if (e.target.classList.contains('locale-cell') && e.target.getAttribute('contenteditable') === 'true') {
                    return;
                }
                showDetail(entry);
            });

            tableBody.appendChild(tr);
        });

        // Add inline editing to locale cells
        addInlineEditing();
    }

    // --- Inline Editing ---
    function addInlineEditing() {
        document.querySelectorAll('.locale-cell').forEach(function (cell) {
            cell.addEventListener('dblclick', function () {
                if (cell.getAttribute('contenteditable') === 'true') return;

                var key = cell.getAttribute('data-key');
                var locale = cell.getAttribute('data-locale');
                var entry = getTableEntries().find(function (e) { return e.key === key; });
                if (!entry) return;

                var currentValue = getLocaleValue(entry, locale);
                cell.setAttribute('contenteditable', 'true');
                cell.textContent = currentValue;
                cell.focus();

                // Select all text
                var range = document.createRange();
                range.selectNodeContents(cell);
                var sel = window.getSelection();
                sel.removeAllRanges();
                sel.addRange(range);

                function saveEdit() {
                    cell.removeAttribute('contenteditable');
                    var newValue = cell.textContent.trim();

                    // Update local state
                    if (!entry.values) entry.values = [];
                    var existing = entry.values.find(function (v) { return v.locale === locale; });
                    if (existing) {
                        existing.value = newValue;
                    } else {
                        entry.values.push({ locale: locale, value: newValue });
                    }

                    // Update metadata.isTranslated: true if any non-en locale has a non-empty value
                    var enVal = getLocaleValue(entry, 'en');
                    entry.metadata.isTranslated = entry.values.some(function (v) {
                        return v.locale !== 'en' && v.value !== '' && v.value !== enVal;
                    });

                    // Re-render cell
                    cell.textContent = newValue || '';
                    if (!newValue) {
                        cell.innerHTML = '<span class="empty-value">—</span>';
                    }

                    // Update dashboard
                    updateDashboard();
                }

                cell.addEventListener('blur', saveEdit, { once: true });
                cell.addEventListener('keydown', function (e) {
                    if (e.key === 'Enter') {
                        e.preventDefault();
                        cell.blur();
                    } else if (e.key === 'Escape') {
                        cell.removeAttribute('contenteditable');
                        cell.textContent = currentValue || '';
                        if (!currentValue) {
                            cell.innerHTML = '<span class="empty-value">—</span>';
                        }
                    }
                });
            });
        });
    }

    // --- Pagination ---
    function renderPagination() {
        pagination.innerHTML = '';
        var totalPages = Math.ceil(filteredEntries.length / PAGE_SIZE);

        if (totalPages <= 1) {
            if (filteredEntries.length > 0) {
                pagination.innerHTML = '<span class="pagination-info">Showing ' + filteredEntries.length + ' entries</span>';
            }
            return;
        }

        var start = (currentPage - 1) * PAGE_SIZE + 1;
        var end = Math.min(currentPage * PAGE_SIZE, filteredEntries.length);

        var prevBtn = document.createElement('button');
        prevBtn.textContent = 'Prev';
        prevBtn.disabled = currentPage === 1;
        prevBtn.addEventListener('click', function () {
            if (currentPage > 1) {
                currentPage--;
                renderTable();
                renderPagination();
            }
        });
        pagination.appendChild(prevBtn);

        var info = document.createElement('span');
        info.className = 'pagination-info';
        info.textContent = start + '-' + end + ' of ' + filteredEntries.length;
        pagination.appendChild(info);

        var nextBtn = document.createElement('button');
        nextBtn.textContent = 'Next';
        nextBtn.disabled = currentPage >= totalPages;
        nextBtn.addEventListener('click', function () {
            if (currentPage < totalPages) {
                currentPage++;
                renderTable();
                renderPagination();
            }
        });
        pagination.appendChild(nextBtn);
    }

    // --- Detail Panel ---
    function showDetail(entry) {
        var status = getStatus(entry);
        var html = '';

        html += detailField('Key', entry.key, true);

        // Show all locale values
        html += '<hr style="border-color:#3c3c3c;margin:16px 0">';
        html += '<div class="detail-label">Locale Values</div>';
        if (entry.values && entry.values.length > 0) {
            entry.values.forEach(function (v) {
                html += detailField(v.locale, v.value, true);
            });
        } else {
            html += detailField('Values', 'None');
        }

        // Show sources
        html += '<hr style="border-color:#3c3c3c;margin:16px 0">';
        html += '<div class="detail-label">Sources</div>';
        if (entry.sources) {
            Object.keys(entry.sources).forEach(function (locale) {
                var src = entry.sources[locale];
                html += detailField(locale + ' Format', src.format || 'N/A');
                html += detailField(locale + ' File', src.file || 'N/A', true);
                if (src.line) {
                    html += detailField(locale + ' Line', src.line.toString());
                }
                if (src.file) {
                    html += '<div class="detail-field"><button class="open-source-btn" data-file="' + escapeAttr(src.file) + '" data-line="' + (src.line || '') + '">Open Source File</button></div>';
                }
            });
        }

        // Metadata
        html += '<hr style="border-color:#3c3c3c;margin:16px 0">';
        html += detailField('Status', '<span class="status-badge status-' + status + '">' + getStatusLabel(status) + '</span>');
        html += detailField('Comment', (entry.metadata && entry.metadata.comment) || 'N/A');
        html += detailField('Do Not Translate', entry.metadata && entry.metadata.doNotTranslate ? 'Yes' : 'No');
        html += detailField('Is Translated', entry.metadata && entry.metadata.isTranslated ? 'Yes' : 'No');
        html += detailField('Format Specifiers', (entry.metadata && entry.metadata.formatSpecifiers && entry.metadata.formatSpecifiers.length > 0) ? entry.metadata.formatSpecifiers.join(', ') : 'None');

        detailContent.innerHTML = html;
        detailPanel.classList.remove('hidden');

        // Attach click handlers for "Open Source File" buttons
        detailContent.querySelectorAll('.open-source-btn').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var filePath = btn.getAttribute('data-file');
                var line = btn.getAttribute('data-line');
                var message = { action: 'openSourceFile', filePath: filePath };
                if (line) {
                    message.line = parseInt(line, 10);
                }
                window.chrome.webview.postMessage(message);
            });
        });
    }

    function detailField(label, value, isMono) {
        return '<div class="detail-field">' +
            '<div class="detail-label">' + label + '</div>' +
            '<div class="detail-value' + (isMono ? ' mono' : '') + '">' + (value != null ? value : 'N/A') + '</div>' +
            '</div>';
    }

    // --- GRF Tab ---
    function renderGrfTab() {
        var grfEntries = allEntries.filter(function (e) {
            return getEntryFormat(e) === 'grf';
        });
        var container = document.getElementById('grf-file-list');
        var noGrfMessage = document.getElementById('no-grf-message');
        container.innerHTML = '';

        if (grfEntries.length === 0) {
            noGrfMessage.classList.remove('hidden');
            return;
        }

        noGrfMessage.classList.add('hidden');
        grfEntries.forEach(function (entry) {
            var item = document.createElement('div');
            item.className = 'grf-file-item';
            var comment = (entry.metadata && entry.metadata.comment) || '';
            var locale = entry.values && entry.values.length > 0 ? entry.values[0].locale : '';
            item.innerHTML =
                '<span class="grf-file-name">' + escapeHtml(entry.key) + '.grf</span>' +
                '<span class="grf-folder-badge">' + escapeHtml(locale) + '</span>' +
                (comment ? '<span class="grf-comment">' + escapeHtml(comment) + '</span>' : '');
            container.appendChild(item);
        });
    }

    // --- Export Functions ---
    function buildExportJson() {
        var locales = selectedLocales.size > 0
            ? Array.from(selectedLocales)
            : getAllLocales();

        var entries = filteredEntries.map(function (entry) {
            var filteredValues = (entry.values || []).filter(function (v) {
                return locales.indexOf(v.locale) !== -1;
            });

            var filteredSources = {};
            if (entry.sources) {
                Object.keys(entry.sources).forEach(function (locale) {
                    if (locales.indexOf(locale) !== -1) {
                        filteredSources[locale] = entry.sources[locale];
                    }
                });
            }

            return {
                id: entry.id || entry.key,
                key: entry.key,
                values: filteredValues,
                sources: filteredSources,
                metadata: entry.metadata || {}
            };
        });

        return {
            version: 3,
            generated: new Date().toISOString(),
            basePath: '',
            entries: entries
        };
    }

    function generateExportFilename() {
        var now = new Date();
        var timestamp = now.getFullYear() + '-' +
            String(now.getMonth() + 1).padStart(2, '0') + '-' +
            String(now.getDate()).padStart(2, '0') + 'T' +
            String(now.getHours()).padStart(2, '0') + '-' +
            String(now.getMinutes()).padStart(2, '0') + '-' +
            String(now.getSeconds()).padStart(2, '0');

        var locales = selectedLocales.size > 0
            ? Array.from(selectedLocales).sort().join('-')
            : 'all';

        return 'databank-export-' + timestamp + '-' + locales + '.json';
    }

    function exportFilteredData() {
        if (filteredEntries.length === 0) return;

        var exportData = buildExportJson();
        var jsonString = JSON.stringify(exportData, null, 2);
        var defaultFilename = generateExportFilename();

        window.chrome.webview.postMessage({
            action: 'exportJson',
            data: jsonString,
            defaultFilename: defaultFilename
        });
    }

    function updateExportButtonState() {
        var exportBtn = document.getElementById('export-btn');
        if (exportBtn) {
            exportBtn.disabled = filteredEntries.length === 0;
        }
    }

    // --- Event Listeners ---
    searchInput.addEventListener('input', debounce(function () {
        applyFilters();
    }, 300));

    localeTrigger.addEventListener('click', function (e) {
        e.stopPropagation();
        toggleLocaleDropdown();
    });

    localeSelectAll.addEventListener('click', function (e) {
        e.stopPropagation();
        selectAllLocales();
    });

    localeClearAll.addEventListener('click', function (e) {
        e.stopPropagation();
        clearAllLocales();
    });

    document.addEventListener('click', function () {
        localeDropdown.classList.add('hidden');
    });

    localeDropdown.addEventListener('click', function (e) {
        e.stopPropagation();
    });

    formatFilter.addEventListener('change', function () { applyFilters(); });
    statusFilter.addEventListener('change', function () { applyFilters(); });

    detailClose.addEventListener('click', function () {
        detailPanel.classList.add('hidden');
    });

    document.getElementById('export-btn').addEventListener('click', function () {
        exportFilteredData();
    });

    // --- Utilities ---
    function escapeHtml(str) {
        if (!str) return '';
        return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
    }

    function escapeAttr(str) {
        if (!str) return '';
        return str.replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }

    function truncate(str, max) {
        if (!str) return '';
        return str.length > max ? str.substring(0, max) + '...' : str;
    }

    function debounce(fn, delay) {
        var timer;
        return function () {
            clearTimeout(timer);
            timer = setTimeout(fn, delay);
        };
    }

    function updateSortIndicators() {
        document.querySelectorAll('#entries-table th').forEach(function (th) {
            var arrow = th.querySelector('.sort-arrow');
            if (th.getAttribute('data-sort') === sortColumn) {
                arrow.textContent = sortDirection === 'asc' ? ' ▲' : ' ▼';
            } else {
                arrow.textContent = '';
            }
        });
    }

    // Expose function for C# ExecuteScriptAsync to call
    window.receiveDataFromCSharp = function (data) {
        handleMessage(data);
    };
})();
