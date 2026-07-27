const browseBtn = document.getElementById('browseBtn');
const runBtn = document.getElementById('runBtn');
const selectedPath = document.getElementById('selectedPath');
const output = document.getElementById('output');

let currentPath = '';

browseBtn.addEventListener('click', () => {
    window.chrome.webview.postMessage({ action: 'browseFolder' });
});

runBtn.addEventListener('click', () => {
    if (currentPath) {
        output.textContent = 'Running extraction...';
        runBtn.disabled = true;
        window.chrome.webview.postMessage({
            action: 'runExtraction',
            projectPath: currentPath
        });
    }
});

window.chrome.webview.addEventListener('message', (event) => {
    const data = event.data;
    switch (data.action) {
        case 'browseResult':
            currentPath = data.path;
            selectedPath.textContent = data.path;
            runBtn.disabled = false;
            break;
        case 'extractionResult':
            runBtn.disabled = false;
            if (data.success) {
                output.textContent = JSON.stringify(data.results, null, 2);
            } else {
                output.textContent = 'Error: ' + data.error;
            }
            break;
    }
});
