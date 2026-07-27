## 1. Model Changes

- [ ] 1.1 Create `TranslationStatus` enum in `DatabankTool/DataBank.Cli/Models/TranslationStatus.cs` with values: `Translated`, `Untranslated`, `DoNotTranslate`, `NeedsReview`
- [ ] 1.2 Add `IsTranslated` (bool, default `false`) property to `EntryMetadata` in `DatabankTool/DataBank.Cli/Models/EntryMetadata.cs`
- [ ] 1.3 Add `TranslationStatus` (enum, default `Untranslated`) property to `EntryMetadata` in `DatabankTool/DataBank.Cli/Models/EntryMetadata.cs`
- [ ] 1.4 Create `TranslationSummary` class in `DatabankTool/DataBank.Cli/Models/TranslationSummary.cs` with `Overall` counts (Translated, Untranslated, DoNotTranslate, NeedsReview) and `ByLocale` list of per-locale breakdowns
- [ ] 1.5 Create `LocaleTranslationCounts` class with `Locale` string and per-status counts
- [ ] 1.6 Add nullable `TranslationSummary? TranslationSummary` property to `DataBankOutput` in `DatabankTool/DataBank.Cli/Models/DataBankOutput.cs`

## 2. Translation Status Analyzer

- [ ] 2.1 Create `DatabankTool/DataBank.Cli/TranslationStatusAnalyzer.cs` with static `Analyze(List<LocalizedStringEntry> entries)` method returning `TranslationSummary`
- [ ] 2.2 Implement EN-key set construction: build `HashSet<string>` from all entries where `Locale` equals `"en"` (case-insensitive)
- [ ] 2.3 Implement per-entry status assignment loop: check `DoNotTranslate` first, then locale is `en`, then key-in-EN-set, else `NeedsReview`
- [ ] 2.4 Implement summary aggregation: count entries by status overall and per locale
- [ ] 2.5 Implement untranslated EN key counting: for each target locale, count EN keys with no matching entry in that locale, add to summary as `Untranslated`

## 3. CLI Integration

- [ ] 3.1 Add `--flag-untranslated` boolean flag parsing in `Program.Main` argument loop in `DatabankTool/DataBank.Cli/Program.cs`
- [ ] 3.2 Add post-parse call to `TranslationStatusAnalyzer.Analyze(entries)` when flag is set, assign returned summary to `output.TranslationSummary`
- [ ] 3.3 Ensure `TranslationSummary` is `null` when flag is not set (default behavior unchanged)
- [ ] 3.4 Add `--flag-untranslated` to `PrintUsage()` help text

## 4. Serialization

- [ ] 4.1 Add `JsonStringEnumConverter` to `JsonSerializerOptions` in `Program.Main` so `TranslationStatus` serializes as string
- [ ] 4.2 Verify `data-bank.json` output includes `isTranslated`, `translationStatus` in each entry's `metadata` and `translationSummary` at root when flag is active

## 5. Testing

- [ ] 5.1 Write unit test for `TranslationStatusAnalyzer.Analyze` with mixed EN and target locale entries, verify correct status assignment
- [ ] 5.2 Write unit test verifying `DoNotTranslate` entries receive `DoNotTranslate` status regardless of EN key match
- [ ] 5.3 Write unit test verifying EN entries always receive `Translated` status
- [ ] 5.4 Write unit test verifying summary counts: Translated, Untranslated, DoNotTranslate, NeedsReview per locale and overall
- [ ] 5.5 Write integration test running CLI with `--flag-untranslated` and verifying JSON output contains new fields
- [ ] 5.6 Run `dotnet build` and `dotnet test` to confirm all tests pass
