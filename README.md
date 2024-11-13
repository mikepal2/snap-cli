# SnapCLI

Quickly create POSIX-like Command Line Interface (CLI) applications with a simple metadata API, built on top of the [System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/) library.

## NuGet Package

The library is available as a NuGet package:

- [SnapCLI](https://www.nuget.org/packages/SnapCLI/)

## Project Goal

While Microsoft's `System.CommandLine` library provides all the necessary APIs to parse command-line arguments, it requires significant effort to set up the code responsible for command-line handling before the program is ready to run. Additionally, this code can be difficult to maintain. For more context, see the [Motivation](https://github.com/mikepal2/snap-cli/blob/main/docs/Documentation.md) page.

The goal of this project is to address these issues by providing developers with easy-to-use mechanisms, while retaining the core functionality and features of `System.CommandLine`.

This library enables developers to quickly create POSIX-like CLI applications by automatically managing command-line commands and parameters using the provided metadata. This simplifies the development process and allows developers to focus on their application logic.

Additionally, it streamlines the creation of the application's help system, ensuring that all necessary information is easily accessible to end users.

The inspiration for this project came from the [DragonFruit](https://github.com/dotnet/command-line-api/blob/main/docs/DragonFruit-overview.md) project, which was a step in the right direction to simplify the usage of `System.CommandLine` but has significant limitations.

## Documentation

Visit the [Documentation](https://github.com/mikepal2/snap-cli/blob/main/docs/Documentation.md) page to get started with SnapCLI’s APIs.

## Examples

There are several [samples](https://github.com/mikepal2/snap-cli/blob/main/samples/readme.md) provided to demonstrate various ways to use the library.

## .NET Framework Support

Supported frameworks can be found on the [SnapCLI NuGet page](https://www.nuget.org/packages/SnapCLI#supportedframeworks-body-tab). The goal is to maintain the same level of support as the System.CommandLine library.

## License

This project is licensed under the [MIT License](https://github.com/mikepal2/snap-cli/blob/main/LICENSE.md). Some parts of this project are borrowed with modifications from [DragonFruit](https://github.com/dotnet/command-line-api/tree/main/src/System.CommandLine.DragonFruit/targets) under the [MIT License](https://github.com/mikepal2/snap-cli/blob/main/LICENSE-command-line-api.md).
