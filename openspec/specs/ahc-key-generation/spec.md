# AHC Key Generation

## Purpose

Defines how the AHC parser derives localization keys from XML elements, including ancestor chain walking, Gem/Tag/Context1Description skip rules, and empty-value key recording.

## Requirements

### Requirement: Key derivation walks full ancestor chain
The parser SHALL derive the key for each `LanguageValue` by walking up the XML ancestor tree until finding the first element with a `Name` attribute. Elements without a `Name` attribute (e.g., `LanguageValues`, `Value`, `Title`, `GsLocalizedString`) SHALL be skipped during the walk.

#### Scenario: Text element produces correct key
- **WHEN** a `<Text Name="txtLimits">` element contains `<Title><Value><LanguageValues><LanguageValue>`
- **THEN** the derived key SHALL be `"txtLimits"`

#### Scenario: CheckBox element produces correct key
- **WHEN** a `<CheckBox Name="chkboxEnable">` element contains `<Label><Value><LanguageValues><LanguageValue>`
- **THEN** the derived key SHALL be `"chkboxEnable"`

#### Scenario: Top-level Title produces correct key
- **WHEN** a `<Title>` element (direct child of `<ContextualDisplay>`) contains `<LanguageValues><LanguageValue>`
- **THEN** the derived key SHALL be `"Title"`

#### Scenario: Top-level Description produces correct key
- **WHEN** a `<Description>` element (direct child of `<ContextualDisplay>`) contains `<LanguageValues><LanguageValue>`
- **THEN** the derived key SHALL be `"Description"`

### Requirement: Gem element descendants are skipped
The parser SHALL NOT produce entries for `LanguageValue` elements that are descendants of a `<Gem>` element.

#### Scenario: Gem EU variable is not parsed
- **WHEN** a `<Gem>` element contains a `GraphicVariable` with `GsLocalizedString > LanguageValues > LanguageValue` containing non-empty content
- **THEN** no entry SHALL be produced for that `LanguageValue`

### Requirement: Top-level Tag and Context1Description are skipped
The parser SHALL NOT produce entries for `LanguageValue` elements inside `<Tag>` or `<Context1Description>` elements at the root of `<ContextualDisplay>`.

#### Scenario: Tag GsLocalizedString is not parsed
- **WHEN** the root `<ContextualDisplay>` has a `<Tag><GsLocalizedString><LanguageValues><LanguageValue>` structure
- **THEN** no entry SHALL be produced for that `LanguageValue`

#### Scenario: Context1Description is not parsed
- **WHEN** the root `<ContextualDisplay>` has a `<Context1Description><GsLocalizedString><LanguageValues><LanguageValue>` structure
- **THEN** no entry SHALL be produced for that `LanguageValue`

### Requirement: Empty-value keys are recorded for Text/CheckBox without LanguageValues
The parser SHALL produce entries for `<Text Name="...">` and `<CheckBox Name="...">` elements under `<ContainedElements>` even when they have no `LanguageValue` descendants. These entries SHALL have empty values, `IsTranslated = false`, and `Comment = "no language value provided"`.

#### Scenario: Text with no LanguageValues produces empty entry
- **WHEN** a `<Text Name="txtEnab">` element has no `LanguageValue` descendants
- **THEN** the parser SHALL produce entries (one per locale in the file) with empty value, `IsTranslated = false`, and comment `"no language value provided"`

#### Scenario: CheckBox with empty LanguageValues
- **WHEN** a `<CheckBox Name="chkboxEnable">` element has `LanguageValue` descendants but all `Content` values are empty
- **THEN** the entries SHALL have empty values, `IsTranslated = false`, and comment `"no language value provided"`
