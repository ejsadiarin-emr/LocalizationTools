## 1. Directory Structure Setup

- [ ] 1.1 Create `DatabankTool/` directory at repository root
- [ ] 1.2 Move `src/DataBank.Cli/` to `DatabankTool/DataBank.Cli/`
- [ ] 1.3 Move `src/DataBank.Cli.Tests/` to `DatabankTool/DataBank.Cli.Tests/`

## 2. Project File Updates

- [ ] 2.1 Update `DatabankTool/DataBank.Cli/DataBank.Cli.csproj` helper references to use `../../src/Helpers/` paths
- [ ] 2.2 Verify `DatabankTool/DataBank.Cli.Tests/DataBank.Cli.Tests.csproj` project reference still works (should be `../DataBank.Cli/DataBank.Cli.csproj`)

## 3. Desktop Project Setup

- [ ] 3.1 Create `DatabankTool/DataBank.Desktop/` directory structure (root, wwwroot/, Services/)
- [ ] 3.2 Create `DatabankTool/DataBank.Desktop/DataBank.Desktop.csproj` targeting `net10.0-windows` with `<UseWPF>true</UseWPF>` and `Microsoft.Web.WebView2` reference
- [ ] 3.3 Create `DatabankTool/DataBank.Desktop/App.xaml` and `App.xaml.cs`
- [ ] 3.4 Create `DatabankTool/DataBank.Desktop/MainWindow.xaml` with WebView2 control and `MainWindow.xaml.cs`
- [ ] 3.5 Create `DatabankTool/DataBank.Desktop/wwwroot/` with `index.html`, `app.js`, `styles.css` (WebView2 loads this page)
- [ ] 3.6 Verify `DataBank.Desktop` builds successfully with `dotnet build`

## 4. Build Verification

- [ ] 4.1 Run `dotnet build` on `DatabankTool/DataBank.Cli/DataBank.Cli.csproj` and verify success
- [ ] 4.2 Run `dotnet build` on `DatabankTool/DataBank.Cli.Tests/DataBank.Cli.Tests.csproj` and verify success

## 5. Test Verification

- [ ] 5.1 Run `dotnet test` on `DatabankTool/DataBank.Cli.Tests/DataBank.Cli.Tests.csproj` and verify all tests pass

## 6. Documentation Updates

- [ ] 6.1 Update any documentation referencing old paths to `src/DataBank.Cli/` or `src/DataBank.Cli.Tests/`
- [ ] 6.2 Consider adding README to `DatabankTool/` explaining the directory structure
