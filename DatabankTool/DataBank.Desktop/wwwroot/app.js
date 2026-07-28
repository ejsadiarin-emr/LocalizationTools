(function () {
    'use strict';

    // State
    let allEntries = [];
    let filteredEntries = [];
    let currentPage = 1;
    let sortColumn = '';
    let sortDirection = 'asc';
    const PAGE_SIZE = 50;

    // DOM refs
    const dashboardSection = document.getElementById('dashboard-section');
    const tableSection = document.getElementById('table-section');
    const tableBody = document.getElementById('table-body');
    const pagination = document.getElementById('pagination');
    const noDataMessage = document.getElementById('no-data-message');
    const searchInput = document.getElementById('search-input');
    const localeFilter = document.getElementById('locale-filter');
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

    function renderGrfFiles(files) {
        var container = document.getElementById('grf-file-list');
        var noGrfMessage = document.getElementById('no-grf-message');
        container.innerHTML = '';

        if (files.length === 0) {
            noGrfMessage.classList.remove('hidden');
            return;
        }

        noGrfMessage.classList.add('hidden');
        files.forEach(function (file) {
            var item = document.createElement('div');
            item.className = 'grf-file-item';
            item.innerHTML =
                '<span class="grf-file-name">' + escapeHtml(file.fileName) + '</span>' +
                '<span class="grf-folder-badge">' + escapeHtml(file.folder) + '</span>';
            container.appendChild(item);
        });
    }

    function renderGrfTab() {
        var grfEntries = allEntries.filter(function (e) {
            return e.source && e.source.format === 'grf';
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
            item.innerHTML =
                '<span class="grf-file-name">' + escapeHtml(entry.key) + '.grf</span>' +
                '<span class="grf-folder-badge">' + escapeHtml(entry.locale) + '</span>' +
                (comment ? '<span class="grf-comment">' + escapeHtml(comment) + '</span>' : '');
            container.appendChild(item);
        });
    }

    function onDataLoaded() {
        populateFilters();
        applyFilters();
        updateDashboard();
        renderGrfTab();
        noDataMessage.classList.add('hidden');
    }

    // --- Dashboard ---
    function updateDashboard() {
        var total = allEntries.length;
        var locales = new Set(allEntries.map(function (e) { return e.locale; }));
        var formats = new Set(allEntries.map(function (e) { return e.source ? e.source.format : ''; }));
        var translated = allEntries.filter(function (e) { return getStatus(e) === 'translated'; }).length;
        var untranslated = allEntries.filter(function (e) { return getStatus(e) === 'untranslated'; }).length;

        document.getElementById('stat-total').textContent = total;
        document.getElementById('stat-locales').textContent = locales.size;
        document.getElementById('stat-formats').textContent = formats.size;
        document.getElementById('stat-translated').textContent = translated;
        document.getElementById('stat-untranslated').textContent = untranslated;

        renderLocaleStats(locales, total);
    }

    function renderLocaleStats(locales, total) {
        var container = document.getElementById('locale-stats');
        container.innerHTML = '';
        var localeArray = Array.from(locales).sort();

        localeArray.forEach(function (locale) {
            var count = allEntries.filter(function (e) { return e.locale === locale; }).length;
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
        if (!entry.value || entry.value.trim() === '') {
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
        allEntries.forEach(function (e) {
            if (e.locale) locales[e.locale] = true;
            if (e.source && e.source.format) formats[e.source.format] = true;
        });

        populateDropdown(localeFilter, Object.keys(locales).sort(), 'All Locales');
        populateDropdown(formatFilter, Object.keys(formats).sort(), 'All Formats');
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
        var locale = localeFilter.value;
        var format = formatFilter.value;
        var status = statusFilter.value;
        var search = searchInput.value.toLowerCase().trim();

        filteredEntries = allEntries.filter(function (e) {
            if (locale && e.locale !== locale) return false;
            if (format && (!e.source || e.source.format !== format)) return false;
            if (status && getStatus(e) !== status) return false;
            if (search) {
                var keyMatch = e.key && e.key.toLowerCase().indexOf(search) !== -1;
                var valueMatch = e.value && e.value.toLowerCase().indexOf(search) !== -1;
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
            case 'sourceFile': return (entry.source && entry.source.file) || '';
            case 'value': return entry.value || '';
            case 'locale': return entry.locale || '';
            case 'format': return (entry.source && entry.source.format) || '';
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

        pageEntries.forEach(function (entry) {
            var status = getStatus(entry);
            var tr = document.createElement('tr');
            tr.className = 'row-' + status;
            tr.setAttribute('data-id', entry.id);

            tr.innerHTML =
                '<td title="' + escapeAttr(entry.key) + '">' + escapeHtml(truncate(entry.key, 40)) + '</td>' +
                '<td title="' + escapeAttr((entry.source && entry.source.file) || '') + '">' + escapeHtml(truncate((entry.source && entry.source.file) || '', 30)) + '</td>' +
                '<td title="' + escapeAttr(entry.value) + '">' + escapeHtml(truncate(entry.value, 50)) + '</td>' +
                '<td>' + escapeHtml(entry.locale) + '</td>' +
                '<td>' + escapeHtml((entry.source && entry.source.format) || '') + '</td>' +
                '<td><span class="status-badge status-' + status + '">' + getStatusLabel(status) + '</span></td>';

            tr.addEventListener('click', function () {
                showDetail(entry);
            });

            tableBody.appendChild(tr);
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
        detailContent.innerHTML =
            detailField('ID', entry.id, true) +
            detailField('Key', entry.key, true) +
            detailField('Value', entry.value) +
            detailField('Locale', entry.locale) +
            detailField('Status', '<span class="status-badge status-' + status + '">' + getStatusLabel(status) + '</span>') +
            '<hr style="border-color:#3c3c3c;margin:16px 0">' +
            detailField('Source Format', (entry.source && entry.source.format) || 'N/A') +
            detailField('Source File', (entry.source && entry.source.file) || 'N/A', true) +
            detailField('Source Path', (entry.source && entry.source.path) || 'N/A', true) +
            '<hr style="border-color:#3c3c3c;margin:16px 0">' +
            detailField('Comment', (entry.metadata && entry.metadata.comment) || 'N/A') +
            detailField('RC ID', (entry.metadata && entry.metadata.rcId != null) ? String(entry.metadata.rcId) : 'N/A') +
            detailField('RC Define', (entry.metadata && entry.metadata.rcDefine) || 'N/A') +
            detailField('Is Behavioral', entry.metadata && entry.metadata.isBehavioral ? 'Yes' : 'No') +
            detailField('Do Not Translate', entry.metadata && entry.metadata.doNotTranslate ? 'Yes' : 'No') +
            detailField('Format Specifiers', (entry.metadata && entry.metadata.formatSpecifiers && entry.metadata.formatSpecifiers.length > 0) ? entry.metadata.formatSpecifiers.join(', ') : 'None');

        detailPanel.classList.remove('hidden');
    }

    function detailField(label, value, isMono) {
        return '<div class="detail-field">' +
            '<div class="detail-label">' + label + '</div>' +
            '<div class="detail-value' + (isMono ? ' mono' : '') + '">' + (value != null ? value : 'N/A') + '</div>' +
            '</div>';
    }

    // --- Event Listeners ---
    searchInput.addEventListener('input', debounce(function () {
        applyFilters();
    }, 300));

    localeFilter.addEventListener('change', function () { applyFilters(); });
    formatFilter.addEventListener('change', function () { applyFilters(); });
    statusFilter.addEventListener('change', function () { applyFilters(); });

    detailClose.addEventListener('click', function () {
        detailPanel.classList.add('hidden');
    });

    // Column sorting
    document.querySelectorAll('#entries-table th[data-sort]').forEach(function (th) {
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

    // Expose function for C# ExecuteScriptAsync to call
    window.receiveDataFromCSharp = function (data) {
        handleMessage(data);
    };
})();
