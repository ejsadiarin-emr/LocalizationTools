# Localization Analyzers

Roslyn analyzers for detecting behavioral strings and unlocalized display strings in C# codebases.

## Overview

This analyzer package helps prevent localization issues by detecting two categories of hardcoded strings:

- **Behavioral strings** (LOC001-LOC003): Strings that drive control flow, database lookups, or equality comparisons. These break when translated and must use resource keys or constants.
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
dotnet_diagnostic.LOC010.severity = suggestion
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
        "rules": [...]
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
      }]
    }]
  }]
}
```

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

## License

MIT