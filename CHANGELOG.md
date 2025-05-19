# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [2.2.0.0] - 2025-05-10

### Fixed

- Importer broke due to plans endpoint deprecating a parameter.
- Crash when opening the cookies editor before doing the initial setup.

### Changed

- Refactored error handling.
- Refactored logging to limit space occupied on disk.


## [2.1.0.0] - 2024-07-20

### Added

- Cloudflare detection.
- Fields for User-Agent and cf\_clearance cookie required to bypass cloudflare.


## [2.0.1.1] - 2023-08-04

### Added

- Basic logging for importing errors.

### Changed

- Consider Fanbox API errors as a cookie problem to report to the user.
