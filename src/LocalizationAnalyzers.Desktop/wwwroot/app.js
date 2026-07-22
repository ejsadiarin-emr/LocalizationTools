const host = window.chrome?.webview || {
    postMessage: (msg) => console.warn('WebView2 not available', msg)
};

const projectPathInput = document.getElementById('projectPath');
const browseBtn = document.getElementById('browseBtn');
const runBtn = document.getElementById('runBtn');
const statusEl = document.getElementById('status');
const resultsSection = document.getElementById('resultsSection');
const resultsBody = document.getElementById('resultsBody');
const exportBtn = document.getElementById('exportBtn');
const filtersPanel = document.getElementById('filtersPanel');

const caRulesToggle = document.getElementById('ca-rules-toggle');

let allResults = [];
let activeFilters = new Set(['LOC001', 'LOC002', 'LOC003', 'LOC004', 'LOC005', 'LOC006', 'LOC007', 'LOC010']);

projectPathInput.addEventListener('input', () => {
    runBtn.disabled = !projectPathInput.value.trim();
});

browseBtn.addEventListener('click', () => {
    host.postMessage({ action: 'browseFolder' });
});

runBtn.addEventListener('click', () => {
    const projectPath = projectPathInput.value.trim();
    if (!projectPath) return;

    setStatus('running', 'Analyzing project...');
    runBtn.disabled = true;
    resultsSection.classList.add('hidden');

    host.postMessage({ action: 'runAnalysis', projectPath, includeCaRules: caRulesToggle.checked });
});

exportBtn.addEventListener('click', () => {
    host.postMessage({ action: 'exportSarif', outputPath: 'results.sarif' });
});

filtersPanel.addEventListener('change', (e) => {
    if (e.target.type === 'checkbox') {
        const rule = e.target.dataset.rule;
        if (rule) {
            if (e.target.checked) {
                activeFilters.add(rule);
            } else {
                activeFilters.delete(rule);
            }
            renderResults();
        }
    }
});

caRulesToggle.addEventListener('change', () => {
    const projectPath = projectPathInput.value.trim();
    if (!projectPath) return;

    setStatus('running', 'Analyzing project...');
    runBtn.disabled = true;
    resultsSection.classList.add('hidden');

    host.postMessage({ action: 'runAnalysis', projectPath, includeCaRules: caRulesToggle.checked });
});

host.addEventListener('message', (event) => {
    const data = event.data;

    if (data.action === 'browseResult') {
        if (data.path) {
            projectPathInput.value = data.path;
            runBtn.disabled = false;
        }
        return;
    }

    if (data.action === 'analysisResult') {
        runBtn.disabled = false;
        if (data.success) {
            setStatus('success', `Analysis complete: ${data.results.length} diagnostics found`);
            allResults = data.results;
            updateSummary(data.summary);
            resultsSection.classList.remove('hidden');
            renderResults();
        } else {
            setStatus('error', `Analysis failed: ${data.error}`);
        }
        return;
    }

    if (data.action === 'exportResult') {
        if (data.success) {
            setStatus('success', `SARIF exported to: ${data.path}`);
        } else {
            setStatus('error', `Export failed: ${data.error}`);
        }
    }
});

function setStatus(type, message) {
    statusEl.className = `status ${type}`;
    statusEl.textContent = message;
}

function updateSummary(summary) {
    document.getElementById('totalFiles').textContent = summary.totalFileCount;
    document.getElementById('totalLines').textContent = formatNumber(summary.totalLineCount);
    document.getElementById('totalDiagnostics').textContent = summary.totalDiagnostics;
    document.getElementById('totalDuration').textContent = `${summary.totalDurationMs}ms`;
}

function formatNumber(n) {
    return n.toLocaleString();
}

function renderResults() {
    const filtered = allResults.filter(r => {
        if (r.ruleId.startsWith('CA')) return caRulesToggle.checked;
        return activeFilters.has(r.ruleId);
    });
    resultsBody.innerHTML = '';

    if (filtered.length === 0) {
        const row = document.createElement('tr');
        row.innerHTML = `<td colspan="5" style="text-align: center; color: #888; padding: 24px;">No diagnostics to display</td>`;
        resultsBody.appendChild(row);
        return;
    }

    for (const r of filtered) {
        const row = document.createElement('tr');
        const shortPath = r.filePath.replace(/^.*[\\/]/, '');
        const severityClass = `severity-${r.level}`;
        const isCa = r.ruleId.startsWith('CA');
        const badgeClass = isCa ? 'badge-ca' : 'badge-loc';
        const badgeLabel = isCa ? 'CA' : 'LOC';
        row.innerHTML = `
            <td><span class="${badgeClass}">${badgeLabel}</span> <strong>${r.ruleId}</strong></td>
            <td class="${severityClass}">${r.level}</td>
            <td class="file-path" title="${r.filePath}">${shortPath}</td>
            <td class="line-num">${r.startLine}</td>
            <td class="message">${escapeHtml(r.message)}</td>
        `;
        resultsBody.appendChild(row);
    }
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}
