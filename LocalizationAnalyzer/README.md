# Localization Analyzers

Roslyn analyzers for detecting behavioral strings and unlocalized display strings in C# codebases.

## Overview

This analyzer package helps prevent localization issues by detecting two categories of hardcoded strings:

- **Behavioral strings** (LOC001-LOC003): Strings that drive control flow, database lookups, or equality comparisons. These break when translated and must use resource keys or constants.
- **Localization code smells** (LOC004-LOC007): Patterns that make text hard to translate: string concatenation in output contexts, hardcoded date/number formats, missing StringComparison, and hardcoded pluralization logic.
- **Display strings** (LOC010): UI text, labels, and messages that should be routed through `Localize()` for translation.

Additional rules (LOC011-LOC015) cover string interpolation in localizable contexts, hardcoded `DateTime` formats without `CultureInfo`, dynamic resource keys, English-only pluralization, and punctuation concatenated outside translatable strings.

## Use Case

The analyzer classifies every string literal in a C# codebase as either **behavioral**
(drives control flow, DB lookups, equality checks — translated strings break the program)
or **display** (UI text that should go through `Localize()`). The goal: stop
locale-specific string literals from driving business logic, so one build can serve all
languages with locale data loaded at runtime.

- Run it on **any directory** of C# files — no project compilation required (works on
  plain folders; `bin`, `obj`, `Test`, `TestResults` are skipped).
- Results are emitted as **SARIF 2.1.0**, consumable by GitHub Code Scanning, SonarQube,
  and Azure DevOps.
- Includes a **code fix** (lightbulb) that extracts a LOC010 display string into a
  `Localize("suggested.key")` call.

## Quick Run

### CLI (analyze a directory → SARIF)

```bash
# make targets
make build-cli          # build the CLI (net10.0)
make analyze            # analyze LocalizationAnalyzer/ → results.sarif
make analyze-test       # analyze test-codebase/ → prints SARIF to stdout
make analyze-test-ca    # same, plus built-in CA globalization rules (CA1303-CA1311)

# raw dotnet run — point at any folder containing .cs files:
dotnet run --project LocalizationAnalyzer/LocalizationAnalyzers.csproj --no-build -c Release -f net10.0 -- <directory> [output.sarif] [--with-ca-rules]
```

Examples:

```bash
# Analyze a specific directory and write the SARIF to a file
dotnet run --project LocalizationAnalyzer/LocalizationAnalyzers.csproj --no-build -c Release -f net10.0 -- test-codebase/ results.sarif

# Analyze and print SARIF to stdout (no output file argument)
dotnet run --project LocalizationAnalyzer/LocalizationAnalyzers.csproj --no-build -c Release -f net10.0 -- test-codebase/

# Include Microsoft's built-in CA globalization rules
make analyze-test-ca
```

### Desktop app

```bash
make run-desktop        # builds + runs

# raw:
dotnet run --project LocalizationAnalyzer/LocalizationAnalyzers.Desktop/LocalizationAnalyzers.Desktop.csproj -c Release
```

See [Desktop App](#desktop-app) for what you can do in the GUI.

### Checking the SARIF file

The CLI output (whether written to a file or printed) is SARIF 2.1.0 JSON. Each result
carries enriched properties — `classification` (hardcoded / concatenated / interpolated /
format-string / plural-form / display-string / dynamic-key / culture-default),
`sourceSnippet` (the offending line), and `stringLiteral` — plus per-file metrics in
`runs[].properties.fileMetrics[]` and rule metadata (helpUri, tags, relatedRules, bad/good
examples) in `runs[].tool.driver.rules[]`.

To view it:

```bash
# Pretty-print / query it like any JSON
Get-Content results.sarif | ConvertFrom-Json

# Or open it in VS Code with the SARIF Viewer extension
code results.sarif
```

### Where to use the SARIF file

- **GitHub Code Scanning** — the repo's `.github/workflows/analyze.yml` already runs the
  CLI on `LocalizationAnalyzer/` and uploads the result to Code Scanning
  (`github/codeql-action/upload-sarif@v4`).
- **SonarQube** — import the SARIF file in the project settings.
- **Azure DevOps** — `dotnet build /p:ErrorLog=results.sarif` produces SARIF natively;
  publish it as a build artifact (see [CI Integration](#ci-integration)).
- **Local review** — the desktop app's **Export SARIF** button or the CLI file output,
  opened in the VS Code SARIF viewer.

## Installation

### NuGet Package

```bash
dotnet add package LocalizationAnalyzers
```

### Manual Installation

1. Build the project: `dotnet build`
2. Reference the analyzer DLL in your project:
   ```xml
   <ItemGroup>
     <Analyzer Include="path\to\LocalizationAnalyzers.dll" />
   </ItemGroup>
   ```

## Diagnostic Rules

### LOC001 - String in Conditional (Warning)

Detects string literals used in if/switch/ternary conditions.

**Why it's bad:** Translated strings may not match the expected values, breaking control flow.

```csharp
// Bad
if (status == "Running") { ... }
switch (mode) { case "Manual": ... }

// Good
if (status == StatusConstants.Running) { ... }
switch (mode) { case ModeConstants.Manual: ... }
```

### LOC002 - String in Data Access (Warning)

Detects string literals passed to Find/Get/Query/Lookup methods.

**Why it's bad:** Database lookups with translated strings will fail to find records.

```csharp
// Bad
var obj = db.Find("Start Pump");

// Good
var obj = db.Find(ObjectKeys.StartPump);
```

### LOC003 - String in Equality Comparison (Warning)

Detects string literals in == or .Equals() comparisons outside conditionals.

**Why it's bad:** Equality checks with translated strings will fail.

```csharp
// Bad
bool match = status == "Running";

// Good
bool match = status == StatusConstants.Running;
```

### LOC004 - String Concatenation in Output (Warning)

Detects string concatenation (`+` operator) or interpolation in output contexts (Console, Debug, logging, UI properties).

**Why it's bad:** Concatenated strings can't be reordered by translators. Different languages have different word orders.

```csharp
// Bad
Console.WriteLine("Hello " + name);
Debug.Log("Count: " + count);

// Good
Console.WriteLine($"Hello {name}");
Console.WriteLine(Localize("greeting.hello", name));
```

### LOC005 - Hardcoded Date/Number Format (Warning)

Detects `ToString("format")` calls with hardcoded date or number format specifiers.

**Why it's bad:** Date `3/5/2026` means March 5 in US but May 3 in Europe. Number formats vary by locale.

```csharp
// Bad
string formatted = now.ToString("dd/MM/yyyy");
string price = amount.ToString("#,##0.00");

// Good
string formatted = now.ToString("d", CultureInfo.CurrentCulture);
string price = amount.ToString("C", CultureInfo.CurrentCulture);
```

### LOC006 - Missing String Comparison (Info)

Detects string methods called without `StringComparison` parameter.

**Why it's bad:** Turkish 'I' problem — `"I".ToLower()` produces different results depending on culture.

```csharp
// Bad
bool found = text.Contains("hello");
string lower = text.ToLower();

// Good
bool found = text.Contains("hello", StringComparison.Ordinal);
string lower = text.ToLower(CultureInfo.InvariantCulture);
```

### LOC007 - Hardcoded Plural Logic (Warning)

Detects ternary expressions comparing `.Count`/`.Length` to 0 or 1 with string literal branches.

**Why it's bad:** English has 2 plural forms; Russian has 3; Arabic has 6; Japanese has none.

```csharp
// Bad
string label = count == 1 ? "1 item" : count + " items";

// Good — use ICU MessageFormat or resource-based plural rules
string label = Pluralize(count, "item", "items");
```

### LOC010 - Display String Not Localized (Info)

Detects display strings not routed through `Localize()`.

**Why it's bad:** UI text won't be translated for other languages.

```csharp
// Bad
label.Text = "Hello World";

// Good
label.Text = Localize("greeting.hello");
```

## Configuration

### .editorconfig

Add to your `.editorconfig` file:

```ini
# Localization Rules
dotnet_diagnostic.LOC001.severity = warning
dotnet_diagnostic.LOC002.severity = warning
dotnet_diagnostic.LOC003.severity = warning
dotnet_diagnostic.LOC004.severity = warning
dotnet_diagnostic.LOC005.severity = warning
dotnet_diagnostic.LOC006.severity = suggestion
dotnet_diagnostic.LOC007.severity = warning
dotnet_diagnostic.LOC010.severity = suggestion
dotnet_diagnostic.LOC011.severity = warning
dotnet_diagnostic.LOC012.severity = warning
dotnet_diagnostic.LOC013.severity = suggestion
dotnet_diagnostic.LOC014.severity = warning
dotnet_diagnostic.LOC015.severity = suggestion
```

### Suppression

To suppress a specific diagnostic:

```csharp
#pragma warning disable LOC001
if (status == "Running") { } // Known behavioral string
#pragma warning restore LOC001
```

## Code Fixes

The analyzer provides a code fix for LOC010 that:

1. Generates a resource key from the type, method, and string value
2. Replaces the string with `Localize("generated.key")`
3. Optionally updates `Resources/en.json`

**Example:**

```csharp
// Before
label.Text = "Start Pump";

// After code fix
label.Text = Localize("TestClass.DoWork.startpump");
```

## SARIF Output

The analyzer generates SARIF **2.1.0** output compatible with:

- **Azure DevOps**: Use `dotnet build /p:ErrorLog=results.sarif`
- **SonarQube**: Import SARIF file in project settings

Both platforms require SARIF version 2.1.0. The `dotnet build /p:ErrorLog=` command produces SARIF 2.1.0 natively on modern .NET.

### Sample SARIF Structure

```json
{
  "version": "2.1.0",
  "runs": [{
    "tool": {
      "driver": {
        "name": "LocalizationAnalyzers",
        "version": "1.0.0",
        "rules": [{
          "id": "LOC001",
          "name": "LOC001",
          "shortDescription": { "text": "String literal in conditional expression" },
          "fullDescription": { "text": "..." },
          "helpUri": "https://github.com/.../LOC001",
          "defaultConfiguration": { "level": "warning" },
          "properties": { "category": "Localization" },
          "tags": ["behavioral"],
          "relatedRules": ["CA1303"],
          "example": {
            "bad": "if (lang == \"en\") { ... }",
            "good": "if (culture == Culture.En) { ... }"
          }
        }]
      }
    },
    "results": [{
      "ruleId": "LOC001",
      "level": "warning",
      "message": { "text": "String literal 'Running' used in conditional..." },
      "locations": [{
        "physicalLocation": {
          "artifactLocation": { "uri": "file:///path/to/File.cs" },
          "region": { "startLine": 10, "startColumn": 15 }
        }
      }],
      "properties": {
        "classification": "hardcoded",
        "sourceSnippet": "if (lang == \"Running\") {",
        "stringLiteral": "Running"
      }
    }]
  }]
}
```

## CLI Tool

The project includes a command-line tool for running analyzers and generating SARIF output with metrics. It is compiled only for `net10.0` (the analyzer itself also targets `netstandard2.0` for the NuGet package).

### Build and Run

```bash
# Build the CLI
dotnet build LocalizationAnalyzer/LocalizationAnalyzers.csproj -c Release -f net10.0
# (make build-cli does the same)

# Run analysis on a directory of C# files
dotnet run --project LocalizationAnalyzer/LocalizationAnalyzers.csproj --no-build -c Release -f net10.0 -- test-codebase results.sarif

# With the built-in CA globalization rules (CA1303-CA1311)
dotnet run --project LocalizationAnalyzer/LocalizationAnalyzers.csproj --no-build -c Release -f net10.0 -- test-codebase/ --with-ca-rules

# Print SARIF to stdout instead of a file
dotnet run --project LocalizationAnalyzer/LocalizationAnalyzers.csproj --no-build -c Release -f net10.0 -- test-codebase/

# Or publish as self-contained
dotnet publish LocalizationAnalyzer/LocalizationAnalyzers.csproj -c Release -f net10.0 --self-contained
./publish/LocalizationAnalyzers src results.sarif
```

Arguments:

- `<directory>` — any folder containing `*.cs` files (a `.csproj` path also works; its
  containing directory is used). `bin`, `obj`, `Test`, `TestResults` subfolders are
  excluded.
- `[output-file]` — optional SARIF output path; omitted → prints to stdout.
- `--with-ca-rules` — include Microsoft's built-in globalization analyzers.

### Metrics Output

The CLI includes per-file and aggregate metrics in the SARIF output:

- **Per-file:** Start/end time, file size (bytes), line count, diagnostic count
- **Aggregate:** Total files, total lines, total duration (ms)
- **Invocation:** Standard SARIF `invocations[]` array with timing and arguments

## Desktop App

A WPF + WebView2 desktop application provides a GUI for running the analyzer on any directory of C# files.

### Build and Run

```bash
make run-desktop        # builds + runs

# raw:
dotnet build LocalizationAnalyzer/LocalizationAnalyzers.Desktop/LocalizationAnalyzers.Desktop.csproj -c Release
dotnet run --project LocalizationAnalyzer/LocalizationAnalyzers.Desktop/LocalizationAnalyzers.Desktop.csproj -c Release --no-build
```

### Common Workflow

1. **Select a folder** — type a path or click **Browse Folder** (directory containing C# files).
2. **Configure rules** — toggle individual LOC rule checkboxes (LOC001–LOC015) and
   optionally **Include CA Rules** to add Microsoft's built-in globalization analyzers
   (CA1303–CA1311).
3. **Run Analysis** — the summary panel shows total files, lines, diagnostics, and duration.
4. **Inspect results** — sortable table with rule, severity, file, line, and message.
   Click any row to **expand details**: classification badge (hardcoded/concatenated/
   interpolated/etc.), source snippet, string literal, rule description with help link,
   related rules, tags, and bad/good code examples side-by-side.
5. **Export SARIF** — save the current run to a `.sarif` file for GitHub Code Scanning,
   SonarQube, or Azure DevOps.

### Features

- Browse to select a directory of C# files
- Run analysis with a single click (LOC001-LOC015, plus optional CA rules)
- Sortable, filterable results table
- **Expandable row details**:
  - **Metadata**: Rule ID, severity, classification badge, file location
  - **Source Code**: Offending source line snippet
  - **String Literal**: The problematic string value
  - **Rule Details**: Description, help link, related rules, tags
  - **Code Example**: Bad/good code side-by-side (when available)
- Summary panel with total files, diagnostics, and execution time
- Rule filter toggles for all LOC rules (LOC001-LOC015)
- Export results to a SARIF file

## CI Integration

### Azure DevOps Pipeline

```yaml
- task: DotNetCoreCLI@2
  inputs:
    command: build
    arguments: '/p:ErrorLog=$(Build.SourcesDirectory)/results.sarif'
  
- task: PublishBuildArtifacts@1
  inputs:
    pathToPublish: '$(Build.SourcesDirectory)/results.sarif'
    artifactName: 'CodeAnalysisLogs'
```

### GitHub Actions

The repo ships `.github/workflows/analyze.yml`, which runs the CLI on `LocalizationAnalyzer/` and uploads
the SARIF to GitHub Code Scanning:

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '10.0.x'

- name: Build CLI tool (net10.0)
  run: dotnet build LocalizationAnalyzer/LocalizationAnalyzers.csproj --no-restore -c Release -f net10.0

- name: Run analyzers and generate SARIF
  run: |
    dotnet run --project LocalizationAnalyzer/LocalizationAnalyzers.csproj --no-build -c Release -f net10.0 -- src string_classification_results.sarif

- name: Upload SARIF to GitHub Code Scanning
  uses: github/codeql-action/upload-sarif@v4
  if: always()
  with:
    sarif_file: string_classification_results.sarif
    category: localization-analyzers
```

For build-time SARIF (e.g., when the analyzer is referenced as a NuGet package), the
`dotnet build /p:ErrorLog=results.sarif` approach works too.

## Testing

Run the unit tests:

```bash
dotnet test
```

## Building

```bash
dotnet build
dotnet pack
```

The NuGet package will be generated in the `bin/Release` directory.

## Future Enhancements

### Rule Metadata (Pending Localization Data)

The following per-rule metadata properties are planned but deferred until localization sample data (RC, RESX files) is analyzed:

- **impact**: localization impact rating (high/medium/low) — requires understanding of real-world string usage patterns
- **fixability**: whether a rule has an automatic code fix (automatic/manual/none)
- **ciGate**: whether a rule is enforced in CI pipelines (true/false)

These properties will be added to the SARIF `rules[]` array once we have sufficient data to make informed decisions about their values.

### Binary Format Parsers (GRF & iFix DLL)

Two file formats in the l10n-files test set require specialized binary parsers that are not yet implemented:

- **GRF (`.grf`)**: GE iFix graphic files — OLE Compound Documents with embedded VBA and proprietary stream formats. Would require OpenMcdf (NuGet) for OLE container access and reverse-engineering of iFix-specific stream layouts (CONTROLSAVESTREAM, TabStripStorage, etc.). Best-effort string extraction is feasible but low-confidence.

- **iFix DLLs (`.dll`)**: Compiled .NET satellite assemblies containing localized error strings. Would require AsmResolver (NuGet) or System.Resources.ResourceManager for runtime extraction. Only Translated versions exist in the test set — no EN source counterpart means coverage analysis is impossible without also providing the EN assembly.

These parsers are deferred pending prioritization and availability of EN/Translated file pairs for coverage analysis.

## License

MIT
