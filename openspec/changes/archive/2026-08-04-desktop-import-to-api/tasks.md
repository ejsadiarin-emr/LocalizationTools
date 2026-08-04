## 1. UI Button

- [x] 1.1 Add "Import Data to API" button to `MainWindow.xaml` toolbar with `Visibility="Collapsed"`
- [x] 1.2 Add `ImportBtn_Click` event handler to code-behind

## 2. Import Logic

- [x] 2.1 Show `OpenFileDialog` with JSON filter and read selected file
- [x] 2.2 Call `ApiClient.ImportJsonAsync(filePath)` with button disabled and status "Importing..."
- [x] 2.3 On success: show imported entry count in status bar and refresh entries via `LoadEntriesFromApi()`
- [x] 2.4 On failure: show error message in status bar
- [x] 2.5 Re-enable button in `finally` block

## 3. Mode Visibility

- [x] 3.1 Show import button in Remote mode and hide in Local mode within `ModeRadio_Checked`
- [x] 3.2 Reset visibility when switching modes (collapsed on connect/retry failure like other remote-only buttons)

## 4. Verification

- [x] 4.1 Build Desktop project successfully
- [x] 4.2 Verify button visibility toggles with Local/Remote mode switch
