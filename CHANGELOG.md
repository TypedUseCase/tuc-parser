# Changelog

<!-- There is always Unreleased section on the top. Subsections (Add, Changed, Fix, Removed) should be Add as needed. -->
## Unreleased
- Update to net5.0 and use F# 5.0

## 3.0.0 - 2020-11-13
- Fix `Alias.Value` to have a surrounding `"`
- Add a `Component option` to `ParsedParticipant`
- Add an `Operator` type and module
- Add fields for `Data` type
    - Domain
    - Type
    - Cases
- Add `KeyWord` `Do` and parse it as a `Parsed.KeyWordOnly`
- Add a field with `DomainType` to `ParticipantComponent`
- Add more functions to the `Data` module
    - `case`
    - `casesFor`
    - `iterCases`
- Add more data for `ParseError`

## 2.0.1 - 2020-11-06
- Add missing `KeyWord` value to `KeyWordIf`

## 2.0.0 - 2020-11-06
- [**BC**] Add `alias` keyWord location to Parsed Participant Definition
- [**BC**] Add `KeyWord` type and a module

## 1.0.0 - 2020-11-05
- Initial implementation
