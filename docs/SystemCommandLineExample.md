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

There are several problems with `System.CommandLine` API related to creating and maintaining the code:
1. The largest portion of the code in this sample is dedicated to setting up the command-line parsing rules. This must be written manually, with careful attention to the relationships between different objects, names, and the order of the parameters, ensuring that no bindings are missed. Only the final part of the code is dedicated to the actual application logic.
2. The command line entities' definition, their binding, and their use are often located in completely different parts of the code, making the code difficult to understand and maintain. For example, the method `ReadFile()` has a `file` parameter. While `ReadFile()` is associated with the `read` command, which in turn is subcommand of `quotes` command, these commands don't have a file option. The `file` parameter is actually linked to the global (recursive) parameter `--file` of the RootCommand. However, you must read through all the code to identify these relationships.
3. Options, arguments, and commands are created and initialized in multiple stages. First, multiple options are created; then, multiple commands are defined. After that, commands are configured with options and arranged into hierarchy, followed by configuring command handlers where bindings to particular options and arguments are added. To fully understand the configuration of a single command, you often need to read through all the code and isolate the sections related to that command, skipping over everything else.
4. It is not practical to mix configuration code with implementation handler methods. As a result, the `SetHandler` method have to use lambda solely to call the corresponding handler method. 
   >  ```csharp
   > addCommand.SetHandler((file, quote, byline) => { AddToFile(file!, quote, byline); },
   >     fileOption, quoteArgument, bylineArgument);
   > 
   > internal static void AddToFile(FileInfo file, string quote, string byline)
   > { 
   >    ...
   > }  
   > ```
   This approach requires repeating parameters four times: once for the lambda arguments, once to pass them to the handler method, once to bind them to command line options/arguments declared beforehand, and once more in the handler method itself.
   
5. Another problem here is that the `SetHandler` method supports binding of up to 8 parameters. If you add one more, you will need to completely rewrite the handler method code, as automatic binding will no longer be available, and you will have to access options and arguments manually from the handler method.

For large CLI application (think of `dotnet` CLI) the amount and complexity of above code may grow enormously.

For comparison, here is the complete functionally equivalent code using the SnapCLI library.


<details>
<summary>SnapCLI sample code</summary>

```csharp
using SnapCLI;

// these commands have no associated handler methods, and therefore declared at assembly level
[assembly: RootCommand(Description = "Sample app for SnapCLI")]
[assembly: Command(Name = "quotes", Description = "Work with a file that contains quotes.")]

class Program
{
    // global option with validation
    [Option(Name = "file", Description = "An option whose argument is parsed as a FileInfo")]
    public static FileInfo file  {
        get { return _file; }
        set {
            if (!value.Exists)
                throw new FileNotFoundException($"Specified file not found", value.FullName);
            _file = value; 
        }
    }
    private static FileInfo _file = new FileInfo("sampleQuotes.txt");

    [Command(Name = "quotes read", Description = "Read and display the file.")]
    public static async Task ReadFile(
        [Option(Description = "Delay between lines, specified as milliseconds per character in a line.")]   
        int delay = 42,
        
        [Option(Name = "fgcolor", Description = "Foreground color of text displayed on the console.")]
        ConsoleColor fgColor = ConsoleColor.White,
        
        [Option(Description = "Background color of text displayed on the console: default is black, light mode is white.")]
        bool lightMode = false)
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

    [Command(Name = "quotes delete", Description = "Delete lines from the file.")]
    internal static void DeleteFromFile(
        [Option(Description = "Strings to search for when deleting entries.")] string[] searchTerms
        )
    {
        Console.WriteLine("Deleting from file");
        File.WriteAllLines(
            file.FullName, File.ReadLines(file.FullName)
                .Where(line => searchTerms.All(s => !line.Contains(s))).ToList());
    }

    [Command(Name = "quotes add", Description = "Add an entry to the file.", Aliases = "insert")]
    internal static void AddToFile(
        [Argument(Description = "Text of quote.")]   string quote,
        [Argument(Description = "Byline of quote.")] string byline
        )
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

Note that in this example:
* There is no code needed for command-line configuration - only metadata directly connected to the entities it describes. 
* Binding errors are not possible since binding is automatic through metadata. Should any changes to the handler method parameters be made, they will be automatically reflected to command line configuration. 
* The option default values are specified as parameters default values, where they are logically expected.





