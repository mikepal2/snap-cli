# SnapCLI.DataAnnotations

This extension for the [SnapCLI🗗](https://www.nuget.org/packages/SnapCLI/) library enables validation of command-line arguments using [DataAnnotations🗗](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel/dataannotations) validation attributes. It allows you to ensure that command-line input meets specified criteria before executing the command.

## Usage

To enable validation, add the following code to your CLI application:

```csharp
using SnapCLI;

[Startup]
public static void Startup()
{
    CLI.BeforeCommand += (args) => args.ParseResult.ValidateDataAnnotations();
}
```

With this code, any argument or option declared with the SnapCLI API and annotated with validation attributes will be validated before the command is executed.

If validation fails, a [ValidationException🗗](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.validationexception) will be thrown, detailing the failed option or argument. Note that the CLI application may implement a custom exception handler to process exceptions according to the application's needs.

## Validation Attributes

The [`System.ComponentModel.DataAnnotations`🗗](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations) library offers a variety of validation attributes, such as `[Range]`, `[StringLength]`, and `[FileExtensions]`, that can employed for argument validation. Note that the `System.ComponentModel.DataAnnotations` NuGet package must be installed in your project to use these attributes.

The `SnapCLI.DataAnnotations` namespace provides several additional validation attributes useful for command-line argument validation:

- `[FileExists]`
- `[FileNotExists]`
- `[DirectoryExists]`
- `[DirectoryNotExists]`

## Example

The sample code below demonstrates the usage of various validation attributes, including custom validation via the [`[CustomValidation]`🗗](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.customvalidationattribute) attribute.

```csharp
using SnapCLI;
using SnapCLI.DataAnnotations;
using System.ComponentModel.DataAnnotations;

[Startup]
public static void Startup()
{
    // enable validation
    CLI.BeforeCommand += (args) => args.ParseResult.ValidateDataAnnotations();
}

[Command(name: "quotes read", description: "Read and display the file.")]
public static async Task ReadQuotesFile(
    [Argument(name: "file", description: "File containing quotes")]
    [FileExtensions(Extensions = "txt,quotes")]
    [FileExists]
    [CustomValidation(typeof(FileCustomValidation), "FileNotEmpty")]
    FileInfo file,

    [Option(description: "Delay between lines, specified as milliseconds per character in a line.")]
    [Range(0, 1000)]
    int delay = 42,

    [Option(name: "fgcolor", description: "Foreground color of text displayed on the console.")]
    [AllowedValues(ConsoleColor.White, ConsoleColor.Red, ConsoleColor.Blue, ConsoleColor.Green)]
    ConsoleColor fgColor = ConsoleColor.White)
{
    Console.ForegroundColor = fgColor;
    foreach (string line in File.ReadLines(file.FullName))
    {
        Console.WriteLine(line);
        await Task.Delay(delay * line.Length);
    };
}

public static class FileCustomValidation
{
    public static ValidationResult FileNotEmpty(object? obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        if (obj is string s)
            obj = new FileInfo(s);
        if (obj is not FileInfo fileInfo)
            throw new NotSupportedException($"FileNotEmpty validation doesn't support {obj.GetType()}");
        if (!fileInfo.Exists)
            return new ValidationResult($"File {fileInfo.FullName} doesn't exist");
        if (fileInfo.Length == 0)
            return new ValidationResult($"File {fileInfo.FullName} is empty");
        return ValidationResult.Success!;
    }
}
```

## NuGet Package

The library is available as a NuGet package:

- [SnapCLI.DataAnnotationsValidation🗗](https://www.nuget.org/packages/SnapCLI.DataAnnotationsValidation/)

