# Motivation

While the Microsoft's `System.CommandLine` library provides all the necessary APIs to parse command line arguments, it requires significant effort to set up the code before the program is ready to run. It also involves repetitive work in referencing the same objects from multiple places, making code maintenance difficult and error-prone.

The goal of this project is to solve the problems highlighted below by providing developers with easy-to-use mechanisms while retaining the core functionality and features of `System.CommandLine`.

## System.CommandLine Example

Let's take a look at the example from [System.CommandLine documentation🗗](https://learn.microsoft.com/en-us/dotnet/standard/commandline/get-started-tutorial#add-subcommands-and-custom-validation).

<details>
<summary>System.CommandLine sample code</summary>

```csharp
using System.CommandLine;

namespace scl;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var fileOption = new Option<FileInfo?>(
            Name = "--file",
            Description = "An option whose argument is parsed as a FileInfo",
            isDefault: true,
            parseArgument: result =>
            {
                if (result.Tokens.Count == 0)
                {
                    return new FileInfo("sampleQuotes.txt");

                }
                string? filePath = result.Tokens.Single().Value;
                if (!File.Exists(filePath))
                {
                    result.ErrorMessage = "File does not exist";
                    return null;
                }
                else
                {
                    return new FileInfo(filePath);
                }
            });

        var delayOption = new Option<int>(
            Name = "--delay",
            Description = "Delay between lines, specified as milliseconds per character in a line.",
            getDefaultValue: () => 42);

        var fgcolorOption = new Option<ConsoleColor>(
            Name = "--fgcolor",
            Description = "Foreground color of text displayed on the console.",
            getDefaultValue: () => ConsoleColor.White);

        var lightModeOption = new Option<bool>(
            Name = "--light-mode",
            Description = "Background color of text displayed on the console: default is black, light mode is white.");

        var searchTermsOption = new Option<string[]>(
            Name = "--search-terms",
            Description = "Strings to search for when deleting entries.")
        { IsRequired = true, AllowMultipleArgumentsPerToken = true };

        var quoteArgument = new Argument<string>(
            Name = "quote",
            Description = "Text of quote.");

        var bylineArgument = new Argument<string>(
            Name = "byline",
            Description = "Byline of quote.");

        var rootCommand = new RootCommand("Sample app for System.CommandLine");
        rootCommand.AddGlobalOption(fileOption);

        var quotesCommand = new Command("quotes", "Work with a file that contains quotes.");
        rootCommand.AddCommand(quotesCommand);

        var readCommand = new Command("read", "Read and display the file.")
            {
                delayOption,
                fgcolorOption,
                lightModeOption
            };
        quotesCommand.AddCommand(readCommand);

        var deleteCommand = new Command("delete", "Delete lines from the file.");
        deleteCommand.AddOption(searchTermsOption);
        quotesCommand.AddCommand(deleteCommand);

        var addCommand = new Command("add", "Add an entry to the file.");
        addCommand.AddArgument(quoteArgument);
        addCommand.AddArgument(bylineArgument);
        addCommand.AddAlias("insert");
        quotesCommand.AddCommand(addCommand);

        readCommand.SetHandler(async (file, delay, fgcolor, lightMode) =>
        {
            await ReadFile(file!, delay, fgcolor, lightMode);
        },
            fileOption, delayOption, fgcolorOption, lightModeOption);

        deleteCommand.SetHandler((file, searchTerms) =>
        {
            DeleteFromFile(file!, searchTerms);
        },
            fileOption, searchTermsOption);

        addCommand.SetHandler((file, quote, byline) =>
        {
            AddToFile(file!, quote, byline);
        },
            fileOption, quoteArgument, bylineArgument);

        return await rootCommand.InvokeAsync(args);
    }

    internal static async Task ReadFile(
                FileInfo file, int delay, ConsoleColor fgColor, bool lightMode)
    {
        Console.BackgroundColor = lightMode ? ConsoleColor.White : ConsoleColor.Black;
        Console.ForegroundColor = fgColor;
        var lines = File.ReadLines(file.FullName).ToList();
        foreach (string line in lines)
        {
            Console.WriteLine(line);
            await Task.Delay(delay * line.Length);
        };

    }
    internal static void DeleteFromFile(FileInfo file, string[] searchTerms)
    {
        Console.WriteLine("Deleting from file");
        File.WriteAllLines(
            file.FullName, File.ReadLines(file.FullName)
                .Where(line => searchTerms.All(s => !line.Contains(s))).ToList());
    }
    internal static void AddToFile(FileInfo file, string quote, string byline)
    {
        Console.WriteLine("Adding to file");
        using StreamWriter? writer = file.AppendText();
        writer.WriteLine($"{Environment.NewLine}{Environment.NewLine}{quote}");
        writer.WriteLine($"{Environment.NewLine}-{byline}");
        writer.Flush();
    }
}
```
</details>&nbsp;

There are several problems with the `System.CommandLine` API related to creating and maintaining the code:

- **Complexity of command-line handling code**: The largest portion of the code in this sample is dedicated to command-line handling. This must be written manually, with careful attention to the relationships between different objects, names, and the order of parameters, ensuring that no bindings are missed. Only the final portion of the code is dedicated to the actual application logic.

- **Separation of entity definitions, bindings, and usage**: The definitions, bindings, and usage of command-line entities are often spread across different parts of the code, making it difficult to understand and maintain. For example, the method `ReadFile()` has a `file` parameter. While `ReadFile()` is associated with the `read` command (which is a subcommand of the `quotes` command), neither of these commands has a `file` option. Instead, the `file` parameter is linked to the global (recursive) `--file` option of the `RootCommand`. These relationships are not immediately obvious and require reading through the entire code.

- **Multi-stage initialization**: Options, arguments, and commands are created and initialized in multiple stages. First, options are defined; then, commands are created. After that, commands are configured with options and arranged into a hierarchy. Finally, handlers are set up, binding the options and arguments. To fully understand the configuration of a single command, you often have to read through the entire code and isolate the sections relevant to that command, skipping over other parts.

- **Repetition**: It is not practical to mix configuration code with implementation handler methods. As a result, the `SetHandler` method uses a lambda to call the corresponding handler method.
  ```csharp
  addCommand.SetHandler((file, quote, byline) => { AddToFile(file!, quote, byline); },
    fileOption, quoteArgument, bylineArgument);

  internal static void AddToFile(FileInfo file, string quote, string byline)
  { 
      ...
  }  
  ```
  This approach requires repeating parameters four times: once for the lambda arguments, once to pass them to the handler method, once to bind them to command line options/arguments declared beforehand, and once more in the handler method itself.

- **Adding an option to a command**: If you need to add another option to a command, you'll have to modify code in at least four different places: create the option, add it to the command, bind it to the handler method in the `SetHandler` call, and add the parameter to the handler method.

- **Limitation of SetHandler method**: The `SetHandler` method only supports binding up to eight parameters. If you need to add more, you'll have to completely rewrite the handler method, as automatic binding will no longer be available. You'll need to manually access options and arguments from the handler method.

- **Growing complexity**: For large CLI applications (e.g., the `dotnet` CLI), the amount and complexity of command-line handling code can grow significantly.


## SnapCLI Example
For comparison, [here](../samples/quotes/Program.cs) you can see the same code adapted to use the SnapCLI library. It is functionally equivalent, providing the same command line interface and producing the same output. Note how in this example:

- There is no code needed for command-line configuration - only metadata directly connected to the entities it describes. As a result, the code is more compact and readable.
- Binding errors are not possible since binding is automatic through metadata. Any changes to the handler method parameters will be automatically reflected in the command-line configuration.
- In most cases, option names are inferred automatically from parameter names.
- Default values for options are specified as parameter default values, where they are logically expected.






