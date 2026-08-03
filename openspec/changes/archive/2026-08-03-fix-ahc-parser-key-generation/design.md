## Context

The AHC parser (`AhcParser.cs`) extracts localized strings from AHC XML files. The `GenerateKey` method derives a key for each `LanguageValue` element by walking up the XML ancestor tree. Currently, it returns `parent.Name.LocalName` at the first ancestor without a `Name` attribute, which produces wrong keys for nested elements like `<Text>` and `<CheckBox>`.

AHC file structure:

```
ContextualDisplay
├── Title > LanguageValues > LanguageValue        → key = "Title"
├── Description > LanguageValues > LanguageValue  → key = "Description"
├── Tag > GsLocalizedString > ...                 → (should skip)
├── Context1Description > GsLocalizedString > ... → (should skip)
├── ContainedElements
│   ├── Text(Name="txtLimits") > Title > Value > LanguageValues > LanguageValue → key = "txtLimits"
│   ├── Text(Name="txtEnab") > (no LanguageValues) → key = "txtEnab", empty value
│   ├── CheckBox(Name="chkboxEnable") > Label > Value > LanguageValues > LanguageValue → key = "chkboxEnable"
│   └── Gem(Name="...") > ... > LanguageValue → (should skip)
```

## Goals / Non-Goals

**Goals:**
- Fix key derivation to return the correct named ancestor for `<Text>` and `<CheckBox>` elements
- Skip `<Gem>` element descendants (no valid key)
- Skip top-level `<Tag>` and `<Context1Description>` wrappers (not element keys)
- Record keys for `<Text>`/`<CheckBox>` elements even without `LanguageValue` children, flagged as untranslated

**Non-Goals:**
- Changing how other parsers (FHX, RC, RESX) work
- Modifying the data-bank schema
- Parsing Gem variable contents

## Decisions

### Decision 1: Walk full ancestor chain in `GenerateKey`

**Choice**: Continue walking past elements without `Name` attributes until finding one with a `Name` or reaching the root.

**Rationale**: The current early-return at the first unnamed element is the root cause of wrong keys. Walking the full chain gives us the correct named ancestor (e.g., `Text` with `Name="txtLimits"`).

**Alternative considered**: Use a lookup table of known element names. Rejected because it's brittle and doesn't scale to new element types.

### Decision 2: Skip Gem descendants

**Choice**: In `Parse`, check if a `LanguageValue`'s ancestor chain contains a `<Gem>` element. If so, skip it.

**Rationale**: Gem elements contain internal graphic variables (EU, Scale, etc.) that are not user-facing translatable keys. The user confirmed these should not be parsed.

**Alternative**: Let `GenerateKey` return a sentinel value. Rejected because it still produces unwanted entries.

### Decision 3: Record empty-value keys for Text/CheckBox without LanguageValues

**Choice**: After processing `LanguageValue` elements, scan for `<Text Name="...">` and `<CheckBox Name="...">` under `<ContainedElements>` that have no `LanguageValue` descendants. Emit one `RawLocalizedEntry` per locale with empty value, `IsTranslated = false`, and `Comment = "no language value provided"`.

**Rationale**: The user wants these keys recorded for coverage tracking, even when there's nothing to translate yet.

## Risks / Trade-offs

- **Data change**: Existing data-bank.json output will change (correct keys replace wrong ones). This is expected and desired.
- **Test updates**: Unit tests assert on entry counts and key names; these will need updating.
