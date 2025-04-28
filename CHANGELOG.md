# PocketBase Unity - Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.23.4] - 2025-04-28

### Fixed

- Fixed task continuation not running on WebGL.

### Changed

- Await all tasks instead of using `ContinueWith()`, as this is not supported in Unity WebGL.

## [0.23.3] - 2025-03-16

### Deprecated

- `RecordModel.Created` and `RecordModel.Updated` are now deprecated in favor of `RecordModel.Get<string>("created")` and `RecordModel.Get<string>("updated")` to be more consistent with the Dart SDK.

### Added

- `RecordModel.Get<T>(string, T)` method to extract a single value from the `RecordModel` by a dot-notation path and try to cast it to the specified generic type.
- `RecordModel.GetListValue<T>(string, List<T>)` alias for `Get<List<T>>(string, List<T>)`.
- `RecordModel.GetStringValue(string, string)` alias for `Get<string>(string, string)`.
- `RecordModel.GetBoolValue(string, bool)` alias for `Get<bool>(string, bool)`.
- `RecordModel.GetIntValue(string, int)` alias for `Get<int>(string, int)`.
- `RecordModel.GetFloatValue(string, float)` alias for `Get<float>(string, float)`.

### Changed

- `RecordModel` design to be more consistent with the Dart SDK.
- `RecordModel.Data` is now a `JObject` instead of a `Dictionary<string, JToken>`.

## [0.23.2] - 2025-03-07

### Changed

- `RecordAuth.Record` is now of type `RecordModel` instead of `BaseAuthModel`. You can still access the user data via the `RecordModel[string]` indexer: `auth.Record["name"]`.

### Removed

- Removed `BaseAuthModel`.

## [0.23.1] - 2025-02-06

### Fixed

- Replaced incorrect using directives (Unity.Plastic.Newtonsoft.Json and Codice.Utils) with the correct ones.

## [0.23.0] - 2025-01-30

### Changed

- The SDK is now compatible with Pocket Base 0.23+.

- Feature parity with the Dart SDK.

## [0.22.3] - 2025-01-28

### Fixed

- Fixed OAuth2 "all-in-one" flow.

### Changed

- Changed the signature of the method `RealtimeService.Subscribe` to not require a generic argument anymore. Now it simply returns a `RecordModel` instance.

## [0.22.2] - 2025-01-24

### Added

- Add `RecordService.AuthWithOAuth2` method.

## [0.22.1] - 2025-01-23

### Added

- Helpers to work with the `RecordService` and `RecordModel` DTO.

### Changed

- The majority of the SDK methods now returns an instance of `RecordModel`.  

## [0.22.0] - 2024-10-16

### Added

- Initial version of the package.