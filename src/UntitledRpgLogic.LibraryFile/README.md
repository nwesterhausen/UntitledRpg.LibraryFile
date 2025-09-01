# Untitled RPG Library File Format (`.urpglib`)

## 1. Introduction

The `.urpglib` file format is a custom binary container designed to package game data modules for the "Untitled RPG" project. It bundles multiple configuration files (e.g., `.toml` files) into a single, distributable package.

This format is designed to be robust, extensible, and efficient, allowing for quick validation of metadata without needing to decompress the entire file. It supports versioning for different parts of its structure, ensuring backward compatibility and a clear path for future upgrades.

---

## 2. File Structure Overview

An `.urpglib` file is composed of three main sections, laid out in the following order:

1.  **Header:** A small, fixed-size binary section containing critical validation data, version information, and pointers to the other sections.
2.  **Manifest:** A variable-size section containing human-readable metadata as a UTF-8 encoded JSON string.
3.  **Payload:** A variable-size section containing the actual game data files, compressed into a Tape Archive (`.tar`).

```
+--------------------------------+
|          HEADER                | (Binary Data)
+--------------------------------+
|          MANIFEST              | (JSON String)
+--------------------------------+
|          PAYLOAD               | (Compressed .tar Archive)
|                                |
|                                |
+--------------------------------+
```

---

## 3. Header Section Details

The header is a compact binary structure that allows an application to quickly identify and validate the file.

| Offset (Bytes) | Length (Bytes) | Data Type | Description                                                                                                                              |
| :------------- | :------------- | :-------- | :--------------------------------------------------------------------------------------------------------------------------------------- |
| 0              | 8              | `byte[8]` | **Magic Bytes**: A constant signature to identify the file as `.urpglib`. The value is `BA 4E 57 7E 52 50 47 1A` (ASCII: `ºNW~RPG.`). |
| 8              | 1              | `byte`    | **Header Schema Version**: The version of the binary header layout itself. Allows for adding/removing fields from the header in the future.  |
| 9              | 2              | `ushort`  | **Manifest Schema Version**: The version of the JSON `PackageManifest` structure. Incremented when fields are added, removed, or changed. |
| 11             | 1              | `byte`    | **Payload Compression**: An enum value indicating the compression algorithm used for the payload. `0x01` = Gzip, `0x00` = None.             |
| 12             | 2              | `ushort`  | **Payload Schema Version**: The version of the data *inside* the payload. Used to signal changes in the TOML file structures.             |
| 14             | 4              | `uint`    | **Manifest Length**: The total size, in bytes, of the JSON Manifest section that immediately follows the header.                           |


---

## 4. Manifest Section Details

The manifest provides detailed, human-readable metadata about the package. It is a UTF-8 encoded JSON object whose structure is defined by the `PackageManifest` record.    

### `PackageManifest` JSON Fields

The manifest fields provide easy access to key information about the package. This is very handy for exposing package details in a user interface or for quick validation checks.

| Field Name     | Data Type         | Description                                                                                                        |
| :------------- | :---------------- | :----------------------------------------------------------------------------------------------------------------- |
| `id`           | `string` (GUID)   | The unique identifier for this package.                                                                            |
| `name`         | `string`          | The human-readable name of the package.                                                                            |
| `description`  | `string`          | A detailed description of the package's contents and purpose.                                                      |
| `version`      | `string`          | The semantic version string (e.g., "1.2.3"). Setting this automatically populates the Major/Minor/Patch versions. |
| `authorName`   | `string`          | The name of the package author or studio.                                                                          |
| `authorId`     | `string` (GUID)   | The unique identifier for the author.                                                                              |
| `dependencies` | `string[]` (GUIDs)| An array of `id`s from other `.urpglib` packages that this package depends on.                                      |

The following fields are available after parsing the JSON into the `PackageManifest` object, but they are not present in the JSON itself:

| Field Name     | Data Type         | Description                                                                                                        |
| :------------- | :---------------- | :----------------------------------------------------------------------------------------------------------------- |
| `majorVersion` | `number`          | The major version number, parsed from the `version` string.                                                        |
| `minorVersion` | `number`          | The minor version number, parsed from the `version` string.                                                        |
| `patchVersion` | `number`          | The patch version number, parsed from the `version` string.                                                        |

---

## 5. Payload Section Details

The payload section contains the actual game data. It is a **Tape Archive (`.tar`)** that has been compressed using the algorithm specified in the `PayloadCompression` header field.

This design allows for multiple files and directory structures to be stored within a single package, preserving their original paths and names.

The payload version is unlikely to change ever, but if it does, changes will be documented in the version history below. The TOML files themselves contain metadata that allows the game engine to interpret their contents correctly.

---

## 6. Versioning Strategy

The file format uses a decoupled versioning system to maximize flexibility:

- **Header Schema Version:** Changed only when the binary layout of the header is modified. This should be very rare.
- **Manifest Schema Version:** Changed when the JSON structure of the manifest is modified (e.g., adding a new metadata field). This allows applications to handle different manifest formats.
- **Payload Schema Version:** Changed when the content or structure of the files *within* the payload is modified. This tells the game's data-loading logic how to interpret the TOML files it finds.

This approach allows, for example, the manifest to be updated with new descriptive fields without requiring any changes to the payload data or the core header structure.

### Version History

A reference for the version history is included here for easy tracking of changes.

### Header Schema Version History

If any new versions of the header schema are introduced, they will be documented here. Each version will include a brief description of the changes made and a model of the binary layout.

| Version | Description                                                                                     |
| :------ | :---------------------------------------------------------------------------------------------- |
| 0x01       | Initial version of the header format.                   |

#### Header Version 0x01

| Offset (Bytes) | Length (Bytes) | Data Type | Description                                                                                                                              |
| :------------- | :------------- | :-------- | :--------------------------------------------------------------------------------------------------------------------------------------- |
| 0              | 8              | `byte[8]` | **Magic Bytes**: A constant signature to identify the file as `.urpglib`. The value is `BA 4E 57 7E 52 50 47 1A` (ASCII: `ºNW~RPG.`). |
| 8              | 1              | `byte`    | **Header Schema Version**: The version of the binary header layout itself. Allows for adding/removing fields from the header in the future.  |
| 9              | 2              | `ushort`  | **Manifest Schema Version**: The version of the JSON `PackageManifest` structure. Incremented when fields are added, removed, or changed. |
| 11             | 1              | `byte`    | **Payload Compression**: An enum value indicating the compression algorithm used for the payload. `0x01` = Gzip, `0x00` = None.             |
| 12             | 2              | `ushort`  | **Payload Schema Version**: The version of the data *inside* the payload. Used to signal changes in the TOML file structures.             |
| 14             | 4              | `uint`    | **Manifest Length**: The total size, in bytes, of the JSON Manifest section that immediately follows the header.                           |

### Manifest Schema Versions

If any new versions of the manifest schema are introduced, they will be documented here. Each version will include a brief description of the changes made and a model of the JSON structure.

| Version | Description                                                                                     |
| :------ | :---------------------------------------------------------------------------------------------- |
| 0x01       | Initial version of the manifest format.                   |

#### Manifest Version 0x01

| Field Name     | Data Type         | Description                                                                                                        |
| :------------- | :---------------- | :----------------------------------------------------------------------------------------------------------------- |
| `id`           | `string` (GUID)   | The unique identifier for this package.                                                                            |
| `name`         | `string`          | The human-readable name of the package.                                                                            |
| `description`  | `string`          | A detailed description of the package's contents and purpose.                                                      |
| `version`      | `string`          | The semantic version string (e.g., "1.2.3"). Setting this automatically populates the Major/Minor/Patch versions. |
| `authorName`   | `string`          | The name of the package author or studio.                                                                          |
| `authorId`     | `string` (GUID)   | The unique identifier for the author.                                                                              |
| `dependencies` | `string[]` (GUIDs)| An array of `id`s from other `.urpglib` packages that this package depends on.                                      |

The following fields are available after parsing the JSON into the `PackageManifest` object, but they are not present in the JSON itself:

| Field Name     | Data Type         | Description                                                                                                        |
| :------------- | :---------------- | :----------------------------------------------------------------------------------------------------------------- |
| `majorVersion` | `number`          | The major version number, parsed from the `version` string.                                                        |
| `minorVersion` | `number`          | The minor version number, parsed from the `version` string.                                                        |
| `patchVersion` | `number`          | The patch version number, parsed from the `version` string.                                                        |
