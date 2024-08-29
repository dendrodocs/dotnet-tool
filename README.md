# DendroDocs.Tool

[![Nuget][NUGET_BADGE]][NUGET_FEED]

**DendroDocs.Tool** is the successor to the [Living Documentation](https://github.com/eNeRGy164/LivingDocumentation) Analyzer, providing a solution for analyzing dotnet projects and generating detailed JSON outputs.
This tool is a key component of the DendroDocs ecosystem, designed to bridge the gap between evolving code and up-to-date documentation.

## Features

A command-line tool that analyzes dotnet projects or solutions and produces a JSON file that follows the schema defined in the [DendroDocs Schema](https://github.com/dendrodocs/schema) repository.

## Shared Code

**DendroDocs.Tool** uses the shared library from the [DendroDocs.Shared](https://github.com/dendrodocs/dotnet-shared-lib) repository, ensuring reusability across different parts of the DendroDocs dotnet ecosystem.

## Prerequisites

* .NET 8.0 SDK or newer.

## Installation

Install **DendroDocs.Tool** as a dotnet global tool:

```shell
dotnet tool install --global DendroDocs.Tool
```

Example usage:

```shell
dendrodocs-analyze --solution G:\DendroDocs\dotnet-shared-lib\DendroDocs.Shared.sln --output shared.json --pretty --verbose --exclude G:\DendroDocs\dotnet-shared-lib\build\_build.csproj
```

## Output

The output of **DendroDocs.Tool** is a comprehensive JSON file that conforms to the schema defined in the [DendroDocs Schema](https://github.com/dendrodocs/schema).
This JSON file provides a representation of your source code, which can be used to generate various types of documentation or integrate with other tools in your development pipeline.

## The DendroDocs Ecosystem

**DendroDocs.Tool** is part of the broader DendroDocs ecosystem.
Explore [DendroDocs](https://github.com/dendrodocs) to find more tools, libraries, and documentation resources that help you bridge the gap between your code and its documentation.

## Contributing

Contributions are welcome! Please feel free to create [issues](https://github.com/dendrodocs/dotnet-tool/issues) or [pull requests](https://github.com/dendrodocs/dotnet-tool/pulls).

## License

This project is licensed under the MIT License.

[NUGET_BADGE]: https://img.shields.io/nuget/v/DendroDocs.Tool.svg?style=plastic
[NUGET_FEED]: https://www.nuget.org/packages/DendroDocs.Tool/
