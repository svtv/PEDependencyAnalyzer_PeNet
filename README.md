# PE Dependency Analyzer

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Platform](https://img.shields.io/badge/platform-windows-lightgrey.svg)](https://github.com/yourusername/PEDependencyAnalyzer)
[![en](https://img.shields.io/badge/lang-en-green.svg)](https://github.com/yourusername/PEDependencyAnalyzer/blob/main/README.md)

*Read this in other languages: [Русский](README.ru.md)*

A command-line tool for analyzing and publishing dependencies of Windows PE files (executables and DLLs).

## Features

- Analyzes dependencies of PE files (both native and managed)
- Classifies dependencies into categories:
  - System Dependencies (Windows system DLLs)
  - Runtime Dependencies (MSVC, MFC, and .NET runtime DLLs)
  - Virtual Dependencies (Windows API Sets)
  - Other Dependencies (application-specific DLLs)
- Supports recursive dependency analysis
- Can publish the application with its dependencies

## Requirements

- .NET 8.0 or higher
- Windows operating system

## Installation

1. Clone the repository
2. Build the project:

## Build Options

The analyzer can be built in several ways depending on your needs:

### 1. Native AOT Build (Maximum Performance)
- Fastest startup time
- Best runtime performance
- Smallest memory footprint
- Platform-specific (needs separate builds for different platforms)
- Limited reflection capabilities
```bash
dotnet publish --configuration Release /p:PublishProfile=NativeAot
```
Output: `bin/Release/publish-native/PEDependencyAnalyzer.exe`

### 2. Single-file Build (Easy Distribution)
- Single executable file
- Includes all dependencies
- No installation required
- Larger file size
- Slightly slower startup
```bash
dotnet publish --configuration Release /p:PublishProfile=SingleFile
```
Output: `bin/Release/publish-single-file/PEDependencyAnalyzer.exe`

### 3. Framework-dependent Build (Maximum Compatibility)
- Requires .NET Runtime
- Smallest distribution size
- Full runtime features
- Platform independent
- Best for development
```bash
dotnet publish --configuration Release /p:PublishProfile=Framework
```
Output: `bin/Release/publish-framework/PEDependencyAnalyzer.exe`

Choose the appropriate build based on your requirements:
- Use Native AOT for best performance in production
- Use Single-file for easy distribution to end users
- Use Framework-dependent for development or when .NET Runtime is already installed

## Usage

Basic analysis:
```bash
PEDependencyAnalyzer <file_or_directory_path>
```

Publishing with dependencies:
```bash
PEDependencyAnalyzer <file_path> --publish[=directory_name]
```

### Command Line Options

- `--publish[=directory_name]` - Enable publish mode (default directory: 'publish')
- `--no-runtime` - Exclude runtime DLLs from publish
- `--no-virtual` - Exclude virtual DLLs from publish

### Examples

Analyze a single file:
```bash
PEDependencyAnalyzer app.exe
```

Analyze all executables in a directory:
```bash
PEDependencyAnalyzer C:\MyApp
```

Publish with all dependencies:
```bash
PEDependencyAnalyzer app.exe --publish
```

Publish to a specific directory without runtime DLLs:
```bash
PEDependencyAnalyzer app.exe --publish=dist --no-runtime
```

## Dependency Classification

The tool classifies dependencies in the following order:

1. **Virtual Dependencies**: DLLs starting with "api-ms-win-" or "ext-ms-win-"
2. **Runtime Dependencies**: MSVC runtime, MFC, and .NET runtime DLLs
3. **System Dependencies**: DLLs located in Windows system directories
4. **Other Dependencies**: All remaining DLLs

## Output Example

```
System Dependencies:
-------------------
KERNEL32.dll                             C:\Windows\system32\KERNEL32.dll
USER32.dll                               C:\Windows\system32\USER32.dll
...

Runtime Dependencies:
--------------------
msvcrt.dll                               C:\Windows\system32\msvcrt.dll
msvcp_win.dll                            C:\Windows\system32\msvcp_win.dll
...

Virtual Dependencies (API Sets):
------------------------------
api-ms-win-core-console-l1-1-0.dll       ...
api-ms-win-core-debug-l1-1-0.dll         ...
...

Other Dependencies:
------------------
MyApp.Core.dll                           C:\MyApp\MyApp.Core.dll
...

Summary:
--------
System Dependencies: 17
Runtime Dependencies: 2
Virtual Dependencies: 35
Other Dependencies: 1
Total Dependencies: 55
```

## License

MIT License
