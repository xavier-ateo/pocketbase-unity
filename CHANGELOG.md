# PocketBase Unity - Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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