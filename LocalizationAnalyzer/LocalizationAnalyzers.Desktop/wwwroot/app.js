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
let rulesMap = {};
let activeFilters = new Set(['LOC001', 'LOC002', 'LOC003', 'LOC004', 'LOC005', 'LOC006', 'LOC007', 'LOC010', 'LOC011', 'LOC012', 'LOC013', 'LOC014', 'LOC015']);
let expandedRowId = null;

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
            rulesMap = data.rules || {};
            updateSummary(data.summary);
            resultsSection.classList.remove('hidden');
            expandedRowId = null;
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

    for (let i = 0; i < filtered.length; i++) {
        const r = filtered[i];
        const rowId = `row-${i}`;
        const row = document.createElement('tr');
        const shortPath = r.filePath.replace(/^.*[\\/]/, '');
        const severityClass = `severity-${r.level}`;
        const isCa = r.ruleId.startsWith('CA');
        const badgeClass = isCa ? 'badge-ca' : 'badge-loc';
        const badgeLabel = isCa ? 'CA' : 'LOC';
        const isExpanded = expandedRowId === rowId;
        row.className = isExpanded ? 'row-expanded' : '';
        row.dataset.rowId = rowId;
        row.dataset.index = i;
        row.innerHTML = `
            <td><span class="${badgeClass}">${badgeLabel}</span> <strong>${r.ruleId}</strong></td>
            <td class="${severityClass}">${r.level}</td>
            <td class="file-path" title="${r.filePath}">${shortPath}</td>
            <td class="line-num">${r.startLine}</td>
            <td class="message">${escapeHtml(r.message)}</td>
        `;
        row.addEventListener('click', () => toggleExpandedRow(rowId, r, i));
        resultsBody.appendChild(row);

        if (isExpanded) {
            const detailRow = document.createElement('tr');
            detailRow.className = 'expanded-detail';
            detailRow.innerHTML = `<td colspan="5">${renderExpandedContent(r)}</td>`;
            resultsBody.appendChild(detailRow);
        }
    }
}

function toggleExpandedRow(rowId, result, index) {
    if (expandedRowId === rowId) {
        expandedRowId = null;
    } else {
        expandedRowId = rowId;
    }
    renderResults();
}

function renderExpandedContent(r) {
    const rule = rulesMap[r.ruleId] || {};
    const sections = [];

    sections.push(`<div class="expanded-panel">`);

    sections.push(`<div class="expanded-section">`);
    sections.push(`<h4 class="expanded-heading">Metadata</h4>`);
    sections.push(`<div class="expanded-grid">`);
    sections.push(`<div class="expanded-field"><span class="expanded-label">Rule:</span> <span class="expanded-value">${escapeHtml(r.ruleId)}</span></div>`);
    sections.push(`<div class="expanded-field"><span class="expanded-label">Severity:</span> <span class="expanded-value severity-${r.level}">${escapeHtml(r.level)}</span></div>`);
    if (r.classification) {
        sections.push(`<div class="expanded-field"><span class="expanded-label">Classification:</span> <span class="expanded-value"><span class="classification-badge">${escapeHtml(r.classification)}</span></span></div>`);
    }
    sections.push(`<div class="expanded-field"><span class="expanded-label">Location:</span> <span class="expanded-value file-path">${escapeHtml(r.filePath)}:${r.startLine}</span></div>`);
    sections.push(`</div></div>`);

    if (r.sourceSnippet) {
        sections.push(`<div class="expanded-section">`);
        sections.push(`<h4 class="expanded-heading">Source Code</h4>`);
        sections.push(`<div class="source-snippet">`);
        sections.push(`<div class="source-line highlighted"><span class="line-num">${r.startLine}</span><span class="line-content">${escapeHtml(r.sourceSnippet)}</span></div>`);
        sections.push(`</div>`);
        sections.push(`</div>`);
    }

    if (r.stringLiteral) {
        sections.push(`<div class="expanded-section">`);
        sections.push(`<h4 class="expanded-heading">String Literal</h4>`);
        sections.push(`<pre class="source-snippet"><code>${escapeHtml(r.stringLiteral)}</code></pre>`);
        sections.push(`</div>`);
    }

    sections.push(`<div class="expanded-section">`);
    sections.push(`<h4 class="expanded-heading">Rule Details</h4>`);
    if (rule.shortDescription) {
        sections.push(`<p class="rule-description">${escapeHtml(rule.shortDescription)}</p>`);
    }
    if (rule.fullDescription && rule.fullDescription !== rule.shortDescription) {
        sections.push(`<p class="rule-description-full">${escapeHtml(rule.fullDescription)}</p>`);
    }
    if (rule.helpUri) {
        sections.push(`<p class="rule-link"><a href="${escapeHtml(rule.helpUri)}" target="_blank">View documentation</a></p>`);
    }
    if (rule.tags && rule.tags.length > 0) {
        sections.push(`<div class="rule-tags">${rule.tags.map(t => `<span class="tag-badge">${escapeHtml(t)}</span>`).join(' ')}</div>`);
    }
    if (rule.relatedRules && rule.relatedRules.length > 0) {
        const relatedLinks = rule.relatedRules.map(rr => {
            const isCa = rr.startsWith('CA');
            const uri = isCa
                ? `https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/${rr.toLowerCase()}`
                : `https://github.com/your-org/LocalizationAnalyzers/blob/main/docs/${rr}.md`;
            return `<a href="${uri}" target="_blank" class="related-rule-link">${rr}</a>`;
        }).join(' ');
        sections.push(`<p class="related-rules"><span class="expanded-label">Related rules:</span> ${relatedLinks}</p>`);
    }
    sections.push(`</div>`);

    if (rule.exampleBad || rule.exampleGood) {
        sections.push(`<div class="expanded-section">`);
        sections.push(`<h4 class="expanded-heading">Code Example</h4>`);
        sections.push(`<div class="example-container">`);
        if (rule.exampleBad) {
            sections.push(`<div class="example-block example-bad"><span class="example-label">Bad (current)</span><pre><code>${escapeHtml(rule.exampleBad)}</code></pre></div>`);
        }
        if (rule.exampleGood) {
            sections.push(`<div class="example-block example-good"><span class="example-label">Good (fix)</span><pre><code>${escapeHtml(rule.exampleGood)}</code></pre></div>`);
        }
        sections.push(`</div></div>`);
    }

    sections.push(`</div>`);
    return sections.join('');
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}
