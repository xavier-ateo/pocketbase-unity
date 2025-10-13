# PocketBase Unity - Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.23.8] - 2025-10-12

### Fixed

- Fixed stack overflow risk in `BaseCrudService.GetFullList` and `SyncQueue.Dequeue` methods. Converted recursive implementations to iterative approach to eliminate potential stack overflow exceptions.
- Fixed null reference exception in `PocketBase.NormalizeQueryParameters` method. Added null-conditional operator to safely handle null parameter values when converting to string.
- Fixed null reference exception in `AuthStore.IsValid` method during JWT token parsing. Added proper null checking and safe dictionary access for the "exp" key to prevent exceptions during token validation.
- Fixed exception handling in `AuthStore.IsValid` method during Base64 conversion and JSON parsing. Added try-catch block to handle potential exceptions during token validation.
- Fixed null reference exception in `Caster.Extract` method. Added null check before calling ToObject to prevent exceptions when extracting null values.
- Fixed null validation in `RecordModel.Set` method. Added proper null handling to safely set null values using JValue.CreateNull instead of JToken.FromObject.
- Fixed unsafe dictionary access in `RealtimeService.UnsubscribeByTopicAndListener` method. Improved subscription reference handling to prevent potential null reference exceptions during concurrent access.
- Fixed exception handling in `RecordService.ConfirmEmailChange` method. Added try-catch block to handle potential exceptions during token validation.
- Fixed exception handling in `RecordService.ConfirmVerification` method. Added try-catch block to handle potential exceptions during token validation.
- Fixed unsafe access in `SseClient.Close` method. Added null check before disposing UnityWebRequest to prevent exceptions when closing the client before a connection is established.
- Fixed small performance issue in `ExtensionMethods.GetAwaiter` method. Added null check before creating new task to prevent unnecessary task creation when the operation is already completed.
- Fixed inverted logic in `RecordService.Unsubscribe()` method. The method now properly unsubscribes from all topics when `topic` is null or empty.

## [0.23.7] - 2025-10-11

### Fixed

- **CRITICAL**: Fixed stack overflow exception in `RecordModel.Data` property caused by infinite recursion between `Data` property and `ToString()` method. The `Data` property now directly constructs a `JObject` from the underlying `_data` field instead of parsing the serialized string representation.

## [0.23.6] - 2025-10-11

### Fixed

- Fixed `RealtimeService.Unsubscribe` not working properly because of wrong topic matching logic in `GetSubscriptionsByTopic` method.

## [0.23.5] - 2025-04-29

### Removed

- Task extension methods `ContinueWithOnMainThread` are no longer part of the SDK.

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