# Localization Analyzers

Roslyn analyzers for detecting behavioral strings and unlocalized display strings in C# codebases.

## Overview

This analyzer package helps prevent localization issues by detecting two categories of hardcoded strings:

- **Behavioral strings** (LOC001-LOC003): Strings that drive control flow, database lookups, or equality comparisons. These break when translated and must use resource keys or constants.
- **Localization code smells** (LOC004-LOC007): Patterns that make text hard to translate: string concatenation in output contexts, hardcoded date/number formats, missing StringComparison, and hardcoded pluralization logic.
- **Display strings** (LOC010): UI text, labels, and messages that should be routed through `Localize()` for translation.

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

The project includes a command-line tool for running analyzers and generating SARIF output with metrics.

### Build and Run

```bash
# Build the CLI
dotnet build src/LocalizationAnalyzers.csproj -c Release -f net10.0

# Run analysis on a project
dotnet run --project src/LocalizationAnalyzers.csproj --no-build -c Release -f net10.0 -- src results.sarif

# Or publish as self-contained
dotnet publish src/LocalizationAnalyzers.csproj -c Release -f net10.0 --self-contained
./publish/LocalizationAnalyzers src results.sarif
```

### Metrics Output

The CLI includes per-file and aggregate metrics in the SARIF output:

- **Per-file:** Start/end time, file size (bytes), line count, diagnostic count
- **Aggregate:** Total files, total lines, total duration (ms)
- **Invocation:** Standard SARIF `invocations[]` array with timing and arguments

## Desktop App

A WPF + WebView2 desktop application provides a GUI for running the analyzer.

### Build and Run

```bash
dotnet build src/LocalizationAnalyzers.Desktop/ -c Release -f net10.0-windows
dotnet run --project src/LocalizationAnalyzers.Desktop/ --no-build -c Release -f net10.0-windows
```

### Features

- Browse to select a `.csproj` file or directory
- Run analysis with a single click
- View results in a sortable, filterable table (LOC001-LOC015, plus optional CA rules)
- **Expandable row details**: Click any row to see:
  - **Metadata**: Rule ID, severity, classification badge (hardcoded/concatenated/interpolated/etc.), file location
  - **Source Code**: Offending source line snippet
  - **String Literal**: The problematic string value
  - **Rule Details**: Description, help link, related rules, tags
  - **Code Example**: Bad/good code side-by-side (when available)
- Summary panel with total files, diagnostics, and execution time
- Rule filter toggles for all LOC rules (LOC001-LOC015)
- Export results to SARIF file

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

```yaml
- name: Build with SARIF
  run: dotnet build /p:ErrorLog=results.sarif

- name: Upload SARIF
  uses: github/codeql-action/upload-sarif@v2
  with:
    sarif_file: results.sarif
```

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

## License

MIT
