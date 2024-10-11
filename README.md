# SnapCLI

Quickly create POSIX-like Command Line Interface (CLI) applications with a simple metadata API, built on top of the [System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/) library.

## NuGet Package

The library is available as a NuGet package:

- [SnapCLI](https://www.nuget.org/packages/SnapCLI/)

## Motivation

The goal of this project is to enable developers to create POSIX-like CLI applications without the hassle of manually parsing command line inputs. 

While the `System.CommandLine` library provides all the necessary APIs to parse command line arguments, it requires significant effort to set up the code before the program is ready to run. It also involves repetitive work in referencing the same objects from multiple places, making code maintenance difficult and error-prone. 

<details><summary>See details and examples</summary>&nbsp;

Let's take a look at the example from [System.CommandLine documentation](https://learn.microsoft.com/en-us/dotnet/standard/commandline/get-started-tutorial#add-subcommands-and-custom-validation).

<details>
<summary>System.CommandLine sample code</summary>

```csharp
using System.CommandLine;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var fileOption = new Option<FileInfo?>(
            name: "--file",
            description: "An option whose argument is parsed as a FileInfo",
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
            name: "--delay",
            description: "Delay between lines, specified as milliseconds per character in a line.",
            getDefaultValue: () => 42);

        var fgcolorOption = new Option<ConsoleColor>(
            name: "--fgcolor",
            description: "Foreground color of text displayed on the console.",
            getDefaultValue: () => ConsoleColor.White);

        var lightModeOption = new Option<bool>(
            name: "--light-mode",
            description: "Background color of text displayed on the console: default is black, light mode is white.");

        var searchTermsOption = new Option<string[]>(
            name: "--search-terms",
            description: "Strings to search for when deleting entries.")
        { IsRequired = true, AllowMultipleArgumentsPerToken = true };

        var quoteArgument = new Argument<string>(
            name: "quote",
            description: "Text of quote.");

        var bylineArgument = new Argument<string>(
            name: "byline",
            description: "Byline of quote.");

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

There are several problems with `System.CommandLine` API:
1. The biggest part of the code in this sample is dedicated to setting up the command line parsing rules. The code must be written manually with careful attention to the relationships between different objects, names, and the order of the parameters, ensuring that no bindings are missed. Only the last part is dedicated to the actual application logic.
2. The command line entities' definition, their binding, and their use are often located in completely different parts of the code, making the code difficult to understand and maintain. For example, the method `ReadFile()` has a `file` parameter. While `ReadFile()` is associated with the `read` command, which in turn is subcommand of `quotes` command, these commands don't have a file option. The `file` parameter is actually linked to the global (recursive) parameter `--file` of the RootCommand. However, you must read through all the code to identify these relationships.
3. Options, arguments, and commands are created and initialized in multiple stages. First, multiple options are created; then, multiple commands are defined. After that, commands are configured with options and arranged into hierarchy, followed by configuring command handlers where bindings to particular options and arguments are added. To fully understand the configuration of a single command, you often need to read through all the code and isolate the sections related to that command, skipping over everything else.
4. It is not practical to mix configuration code with implementation methods. As a result, the `SetHandler` method have to use lambda solely to call the corresponding handler method. 
   >  ```csharp
   > addCommand.SetHandler((file, quote, byline) => { AddToFile(file!, quote, byline); },
   >     fileOption, quoteArgument, bylineArgument);
   > ```
   This approach requires repeating parameters four times: once for the lambda arguments, once to pass them to the handler method, once to bind them to command line options/arguments declared beforehand, and once more in the handler method itself.
   
5. Another problem here is that the `SetHandler` method supports binding of up to 8 parameters. If you add one more, you will need to completely rewrite the handler method code, as automatic binding will no longer be available, and you will have to access options and arguments manually from the handler method.

For large CLI application (think of `dotnet` CLI) the amount and complexity of above code may grow enormously.

For comparison, here is the complete equivalent code using SnapCLI library. 

<details>
<summary>SnapCLI code sample</summary>

```csharp
using SnapCLI;

// these commands have no associated handler methods, and therefore declared at assembly level
[assembly: RootCommand(description: "Sample app for SnapCLI")]
[assembly: Command(name: "quotes", description: "Work with a file that contains quotes.")]

class Program
{
    // global option with validation
    [Option(name: "file", description: "An option whose argument is parsed as a FileInfo")]
    public static FileInfo file  {
        get { return _file; }
        set {
            if (!value.Exists)
                throw new FileNotFoundException($"Specified file not found", value.FullName);
            _file = value; 
        }
    }
    private static FileInfo _file = new FileInfo("sampleQuotes.txt");

    [Command(name: "quotes read", description: "Read and display the file.")]
    public static async Task ReadFile(
        [Option(description: "Delay between lines, specified as milliseconds per character in a line.")]   
        int delay = 42,
        
        [Option(name: "fgcolor", description: "Foreground color of text displayed on the console.")]
        ConsoleColor fgColor = ConsoleColor.White,
        
        [Option(description: "Background color of text displayed on the console: default is black, light mode is white.")]
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

    [Command(name: "quotes delete", description: "Delete lines from the file.")]
    internal static void DeleteFromFile(
        [Option(description: "Strings to search for when deleting entries.")] string[] searchTerms
        )
    {
        Console.WriteLine("Deleting from file");
        File.WriteAllLines(
            file.FullName, File.ReadLines(file.FullName)
                .Where(line => searchTerms.All(s => !line.Contains(s))).ToList());
    }

    [Command(name: "quotes add", description: "Add an entry to the file.", aliases: "insert")]
    internal static void AddToFile(
        [Argument(description: "Text of quote.")]   string quote,
        [Argument(description: "Byline of quote.")] string byline
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

In this example, there is no code related to command-line configuration — only metadata that is directly connected to the entity it describes. The default values are exactly where they are expected to be. The binding is automatic, and there is no way to create binding errors.

</details>&nbsp;

The inspiration for this project came from the [DragonFruit](https://github.com/dotnet/command-line-api/blob/main/docs/DragonFruit-overview.md) project, which was a step in the right direction to simplify the usage of `System.CommandLine` but has significant limitations.


This library automatically manages command-line commands and parameters using the provided metadata, simplifying the development process and allowing developers to focus on their application logic. It also streamlines the creation of the application's help system, ensuring that all necessary information is easily accessible to end users. 

You don't even need to write a [Main()](#main-method) method for the application (though you can if you wish). This means you can skip writing any startup boilerplate code for your CLI application and dive straight into implementing the application logic, i.e., commands.


## API Paradigm

This project employs an API paradigm that utilizes [attributes](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/) to declare and describe CLI commands, options, and arguments through metadata.

Any public static method can be designated as a CLI command handler using the `[Command]` attribute, effectively serving as an entry point for that command within the CLI application. Each parameter of the command handler method automatically becomes a command option. Refer to the [usage](#usage) section and examples below for further details.

### What About Classes?

Many CLI frameworks require separate class implementations for each command. However, creating individual classes for each command can add unnecessary complexity with minimal benefit. Using attributes simplifies maintenance and enhances readability, as they are declared close to the entities they describe, keeping related information in one place. Additionally, attributes can provide extra details, such as descriptions and aliases.

To minimize complexity, I opted to avoid using classes. While this approach may not offer the same flexibility as some alternatives, it effectively meets the needs of most CLI applications. Additional customizations are also possible, see [Advanced Usage](#advanced-usage) section.

## Command Line Syntax

Since this project is based on the [System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/) library, the parsing rules align with those established by that package. Microsoft provides detailed explanations of the [command-line syntax](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax) recognized by `System.CommandLine`. I will include additional links to this documentation throughout the text below.

It is recommended to follow the System.CommandLine [design guidance](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#design-guidance) when designing your CLI.

# Usage

## Commands
A [command](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#commands) in command-line input is a token that specifies an action or defines a group of related actions. Sometimes commands may be referred as *verbs*. 

Any public static method can be declared as a CLI command handler using the `[Command]` attribute. 

```csharp
[Command]
public static void Hello() 
{
    Console.WriteLine("Hello World!");
}
```

Additional information can be provided in attribute parameters to enhance command-line parsing and the help system, such as the command's explicit name, aliases, description, and whether the command is hidden

```csharp
[Command(name:"hello", aliases:"hi,hola,bonjour", description:"Hello example", hidden:false)]
public static void Hello() 
{
    Console.WriteLine("Hello World!");
}
```

Async handler methods are also supported. 

The library supports handler methods with the following return types: `void`, `int`, `Task<int>`, `Task`, `ValueTask<int>`, and `ValueTask`. The result from handlers returning `int`, `Task<int>`, and `ValueTask<int>` is used as the program's exit code.

```csharp
[Command(name:"sleep", description:"Async sleep example")]
public static async Task<int> Sleep(int milliseconds = 1000)
{
    Console.WriteLine("Sleeping...");
    await Task.Delay(milliseconds);
    Console.WriteLine("OK");
    return 0; // exit code
}
```

**Command name convention**

- If the `[Command]` attribute does not specify a command name:
  - If this is the only command in the program, it is automatically treated as the [root command](#root-command).
  - If there are multiple commands declared, the method name, converted to [kebab case](https://en.wikipedia.org/wiki/Letter_case#Kebab_case), is used as the command name. For example, the method `Hello()` will handle the `hello` command, while method `HelloWorld()` will handle `hello-world` commmand.
  - If the method name constains underscores (`_`), it declares a [subcommand](#subcommands). For example, a method named "order_create()" will define a subcommand `create` under the `order` command.
- If the name specified in the `[Command]` attribute explicitly contains spaces, it declares a [subcommand](#subcommands). For example, `[Command(name:"order create")]` defines `create` as a subcommand of the `order` command.
- Commands may have [aliases](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#aliases). These are usually short forms that are easier to type or alternate spellings of a word.
- Command names and aliases are [case-sensitive](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#case-sensitivity). If you want your CLI to be case insensitive, define aliases for the various casing alternatives.

## Options
An [option](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#options) is a named parameter that can be passed to a command.

Any parameter of command handler method automatically becomes a command option. In the next example `name` becomes option for command `hello`:

```csharp
[Command(name:"hello", aliases:"hi,hola,bonjour", description:"Hello example", hidden:false)]
public static void Hello(string name = "World") 
{
    Console.WriteLine($"Hello {name}!");
}
```

Additional information about an option can be provided using the `[Option]` attribute, including an explicit name, aliases, a description, and whether the option is required.

```csharp
[Command(name:"hello", aliases:"hi,hola,bonjour", description:"Hello example", hidden:false)]
public static void Hello(
    [Option(name:"name", description:"The name we should use for the greeting")]
    string name = "World"
) 
{
    Console.WriteLine($"Hello {name}!");
}
```

**Required options**

Required options must be specified on the command line; otherwise, the program will show an error and display the command help. Method parameters that have default values (as in the examples above) are, by default, translated into options that are not required, while those without default values are always translated into required options. You may force option to be required using `required` parameter of the attribute.

**Option name convention**

- The option name is automatically prepended with a single dash (`-`) if it consists of a single letter, or with two dashes (`--`) if it is longer, unless it already starts with a dash.
- If option name is not explicitly specified in the attribute, or attribute is ommitted, the  name of the parameter, converted to [kebab case](https://en.wikipedia.org/wiki/Letter_case#Kebab_case), will be used implicitly. For example, for the parameter `userId` the default option name will be `--user-id`.
- Options may have [aliases](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#aliases). These are usually short forms that are easier to type or alternate spellings of a word.
- Option names and aliases are [case-sensitive](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#case-sensitivity). If you want your CLI to be case insensitive, define aliases for the various casing alternatives.

**What do we have so far?**

With the full program source code consisting of just a few lines:
```csharp
using SnapCLI;

class Program
{
    [Command(name:"hello", description:"Hello example")]
    public static void Hello(
        [Option(name:"name", description:"The name we should use for the greeting")]
        string name = "World"
    ) 
    {
        Console.WriteLine($"Hello {name}!");
    }
}
```

We get complete help output:
```text
> sample hello -?
Description:
  Hello example

Usage:
  sample hello [options]

Options:
  --name <name>   The name we should use for the greeting [default: World]
  -?, -h, --help  Show help and usage information
```

We may run the command without a parameter (default name value `World` is used):
```text
> sample hello
Hello World!
```

And we may may run command with the a parameter:
```text
> sample hello --name Michael
Hello Michael!
```

## Arguments
An [argument](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#arguments) is a value passed to an option or command without specifying an option name; it is also referred to as a positional argument.

You can declare that a parameter is an argument using the `[Argument]` attribute. Let's change "Option" to "Argument" in our example:

```csharp
[Command(name:"hello", description:"Hello example")]
public static void Hello(
    [Argument(name:"name", description:"The name we should use for the greeting")]
    string name = "World"
) 
{
    Console.WriteLine($"Hello {name}!");
}
```

Now we don't need to specify `--name` option name.
```text
> sample hello Michael
Hello Michael!
```

Also, note how the help message has changed:
```text
> sample hello -?
Description:
  Hello example

Usage:
  sample hello [name] [options]

Arguments:
  [name]  The name we should use for the greeting [default: World]

Options:
  -?, -h, --help  Show help and usage information
```

**Argument name convention**
- Argument name is used only for help, it cannot be specified on command line.
- If argument name is not explicitly specified in the attribute, the name of the parameter, converted to [kebab case](https://en.wikipedia.org/wiki/Letter_case#Kebab_case), will be used implicitly.

You can provide options before arguments or arguments before options on the command line. See [documentation](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#order-of-options-and-arguments) for details.

## Arity
The [arity](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#argument-arity) of an option or command's argument is the number of values that can be passed if that option or command is specified. Arity is expressed with a minimum value and a maximum value.

```csharp
[Command(name: "print", description: "Arity example")]
public static void Print(
    [Argument(name:"numbers", arityMin:1, arityMax:2, description:"Takes 1 or 2 numbers")]
    int[] nums
)
{
    Console.WriteLine($"Numbers are: {string.Join(",", nums)}!");
}
```

<details>
<summary>Sample output</summary>

```text
> sample print -?
Description:
  Arity example

Usage:
  sample print [numbers]... [options]

Arguments:
  [numbers]  Takes 1 or 2 numbers

Options:
  -?, -h, --help  Show help and usage information
```
```text
> sample print 12
Numbers are: 12!
```
```text
> sample print 12 76
Numbers are: 12,76!
```
</details>

## Global options
Any public static propety or field can be declared as global option using the `[Option]` attribute.
By default, global options are not required because properties and fields always have default values, either implicitly or explicitly. You can make a global option required by using the `required` parameter of the attribute.

```csharp
class Program
{
    // This global option is not required and have explicit default value of "config.ini"
    [Option(name:"config", description:"Configuration file name", aliases:"c,cfg")]
    public static string ConfigFile = "config.ini";

    // This global option is not required and have implicit default value of (null)
    [Option(name:"profile", description:"User profile")]
    public static string Profile;

    // This global option is always required
    [Option(name:"user", description:"User name", required:true)]
    public static string User;

    ...
}
```

## Root command
The [root command](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#root-commands) is executed if program invoked without any known commands on the command line. If no handler is assigned for the root command, the CLI will indicate that the required command is not provided and display the help message. To assign a handler method for the root command, use the `[RootCommand]` attribute. Its usage is similar to the `[Command]` attribute, except that you cannot specify a command name. There can be only one method declared with `[RootCommand]` attribute.

The description for the root command essentially serves as the program description in the help output, as shown when program is invoked with the `--help` parameter. If the root command is not declared, **SnapCLI** library will use the assembly description as the root command description.

```csharp
[RootCommand(description: "This command greets the world!")]
public static void Hello()
{
    Console.WriteLine("Hello World!");
}
```

> **Note**: If a program has only one command handler method declared with the [Command] attribute and the command name is not explicitly specified in the *name parameter, the SnapCLI library will automatically set this command as the root command.

## Subcommands
Any command may have multiple subcommands. As mentioned earlier, if command name includes spaces or if the name is not specified and the method name contains underscores, it will describe a [subcommand](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#subcommands). 

In the following example we have a subcommand `world` of the command `hello`:

```csharp
[Command(name:"hello world", description:"This command greets the world!")]
public static void Hello() 
{
    Console.WriteLine("Hello World!");
}
```

Or equivalent using just method name:

```csharp
[Command(description:"This command greets the world!")]
public static void hello_world() 
{
    Console.WriteLine("Hello World!");
}
```

The usage output will be as follows:

```text
> sample -?
Description:

Usage:
  sample [command] [options]

Options:
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  hello
```
```text
> sample hello -?
Description:

Usage:
  sample hello [command] [options]

Options:
  -?, -h, --help  Show help and usage information

Commands:
  world
```
```text
> sample hello world -?
Description:
  This command greets the world!

Usage:
  sample hello world [options]

Options:
  -?, -h, --help  Show help and usage information

```
```text
> sample hello world
Hello World!
```

## Commands without handlers
In the output above we have description for the `hello world` command, but not for the `hello`. To describe the `hello` command without assigning a handler method you may use `[assembly: Command()]` attribute at the top of the program source.

Similarly, you can provide description for the root command (the first description in the output above) using `[assembly: RootCommand()]` attribute.

With descriptions provided as shown in the following example, the help output will be complete.

```csharp
using SnapCLI;

[assembly: RootCommand(description: "This is a sample program")] // or [assembly: AssemblyDescription(description: "This is sample program")]
[assembly: Command(name: "hello", description: "This command greets someone", aliases: "hi,hola,bonjour")]

class Program
{
    [Command(description:"This command greets the world!")]
    public static void hello_world() 
    {
        Console.WriteLine("Hello World!");
    }
}
```
# Advanced Usage

The main goal of the library is to simplify the development of POSIX-like CLI applications. The library takes responsibility for the initialization and execution of the application, eliminating the need for any startup boilerplate code. You don't even need to write a [Main](#main-method) method for the application.

However, for more complex CLI applications, the library allows for fine control over initialization, execution, and exception handling.

Here are the steps of the CLI application initialization and execution lifecycle with the `SnapCLI` library:

1. The `SnapCLI` library entry point is executed (see notes about [Main](#main-method) method).
1. The library scans the assembly for `[RootCommand]`, `[Command]`, `[Option]`, `[Argument]`, and `[Startup]` attributes.
2. The `System.CommandLine` parser is initialized, and the commands hierarchy is built based on the found attributes.
3. The [Startup](#startup) method(s) are executed, if present.
4. The command line is parsed.
5. [Global options](#global-options) are set according to the command line parameters.
6. The [BeforeCommand](#beforecommand) event is invoked.
7. The command handler corresponding to the command specified on the command line is executed. 
8. The [AfterCommand](#aftercommand) event is invoked.
9. The process exits.

The command handler is the only required component that must be provided by the application developer, all other steps are automatic or optional.

## Main method  
Typically, the `Main` method serves as the entry point of a C# application. However, to simplify startup code and usage, this library overrides the program's entry point and uses command handler methods as the entry points instead. 

It’s important to note that since the library overrides the entry point, if you include your own `Main` function in the program, it will **not** be invoked.

If you need some initialization code to run before command, it can be placed in [Startup](#startup) method or [BeforeCommand](#beforecommand) event handler. 

If you still really need to use your own `Main`, you can do so with following steps:
1. Add `<AutoGenerateEntryPoint>false</AutoGenerateEntryPoint>` property into your program .csproj file
2. Call `SnapCLI` from your `Main()` method as follows
    ```csharp
    public static async Task<int> Main(string[] args)
    {
          // your initialization here
        ...
    
        return await SnapCLI.CLI.RunAsync(args);
    }
    ```

## Startup
The public static method can be declared to perform additional initialization using `[Startup]` attribute. There could be multiple startup methods in the assembly. These methods will be executed *before* command line is parsed. 

The startup method is recognized by its attribute rather than its name; in other words, you can name it anything you like.

```csharp
[Startup]
public static void MyStartupCode()
{
    // additional initialization for your code
    ...
}
```

The startup method may have a parameter of type `CommandLineBuilder`. If you choose this alternative, you must configure `CommandLineBuilder` yourself, typically using the [.UseDefaults()](https://learn.microsoft.com/en-us/dotnet/api/system.commandline.builder.commandlinebuilderextensions.usedefaults?view=system-commandline#system-commandline-builder-commandlinebuilderextensions-usedefaults(system-commandline-builder-commandlinebuilder)) extension method.

```csharp
[Startup]
public static void Startup(CommandLineBuilder commandLineBuilder)
{
    // additional initialization for your code
    ...

    // disable posix option bundling
    commandLineBuilder
      .UseDefaults()
      .EnablePosixBundling(false);
}
```

The `CLI.RootCommand` property, which provides access to the `System.CommandLine` commands hierarchy along with their options and arguments, is available in the startup code for further customization.

> **Important:** When the startup method is invoked, the command line has not been parsed yet; therefore global parameters still have their default values and not the values from the command line, and `CLI.ParseResult` property is not accessible.


## BeforeCommand
The `BeforeCommand` event is invoked after the command line is parsed and before the command handler is executed. It allows for any additional common initialization, validation of preprocessing the program may need before executing any command. With the command line parameters already parsed, global options reflecting the values specified on the command line and the `CLI.ParseResult` property is accessible for validation or to access parsed options and arguments.

The `BeforeCommand` event handler receives a `BeforeCommandEventArguments` parameter with the following member:
* `ParseResult` - The command line parse result.

To register a `BeforeCommand` event handler, use the following code in startup method:

```csharp
[Startup]
public static void Startup()
{
    CLI.BeforeCommand += (args) => {
        // common initialization or validation before executing any command
        ...
    }
}
```

## AfterCommand

The `AfterCommand` event is invoked after the command handler is executed. It allows for any common deinitialization or post-processing the program may need after executing a command. The event handler receives an `AfterCommandEventArguments` parameter with the following members:

* `ParseResult` - The command line parse result.
* `ExitCode` - The exit code to return from the CLI program. The handler may change the exit code to reflect specific execution results.

To register an `AfterCommand` event handler, use the following code:

```csharp
[Startup]
public static void Startup()
{
    CLI.AfterCommand += (args) => {
        // Common deinitialization or post-processing after executing any command
        ...
    };
}
```

## Validation
There are multiple strategies to validate command line input. 

Input values can be validated at the beginning of the command handler method in any manner required by the command syntax.

```csharp
[Command]
public static void command([Argument] int arg1 = 1, int opt1 = 1, int opt2 = 2) 
{
    if (arg1 < 0 || arg1 > 100)
        throw new ArgumentException($"The valid range for the <arg1> value is 0-100");
    ...
}
```

For global options validation may be implemented in setter.

```csharp
class Program
{
    // global option with validation
    [Option(name: "file", description: "An option whose argument is parsed as a FileInfo")]
    public static FileInfo file  {
        get { return _file; }
        set {
            if (!value.Exists)
                throw new FileNotFoundException($"Specified file not found", value.FullName);
            _file = value; 
        }
    }
    private static FileInfo _file = new FileInfo("sampleQuotes.txt");

    ...
}
```

## Mutually Exclusive Options and Arguments
The library provides mechanisms to check for mutually exclusive options and arguments

* The `mutuallyExclusiveOptionsArguments` parameter of the `[Command]` attribute can be used to declare a list of mutually exclusive option/argument names separated by spaces, commas, semicolons, or pipe characters. If there are multiple groups of mutually exclusive options/arguments, they must be enclosed in parentheses.
  ```csharp
  [Command(mutuallyExclusiveOptionsArguments="(opt1,opt2)(arg1,opt2)")]
  public static void command([Argument] int arg1 = 1, int opt1 = 1, int opt2 = 2) 
  {
      ...
  }
  ``` 
* The `ParseResult.ValidateMutuallyExclusiveOptionsArguments()` method can be used from within the command handler method.
  ```csharp
  [Command]
  public static void command([Argument] int arg1 = 1, int opt1 = 1, int opt2 = 2) 
  {
      CLI.ParseResult.ValidateMutuallyExclusiveOptionsArguments("(opt1,opt2)(arg1,opt2)");
      ...
  }
  ``` 
* Alternatively, the `ParseResult.ValidateMutuallyExclusiveOptionsArguments()` method can be used from the [BeforeCommand](#beforecommand) event handler.
  ```csharp
  [Startup]
  public static void Startup()
  {
      CLI.BeforeCommand += (args) => {
          args.ParseResult.ValidateMutuallyExclusiveOptionsArguments("global-opt1,global-opt2");
          args.ParseResult.ValidateMutuallyExclusiveOptionsArguments("(global-opt1,opt2)(opt3,opt4)");
          ...
      };
  }
  ``` 

## Exception handling
To catch unhandled exceptions during command execution you may set exception handler in [Startup](#startup) method. The handler is intended to provide exception diagnostics according to the need of your application before exiting. The return value from handler will be used as program's exit code. For example:

```csharp
[Startup]
public static void Startup()
{
    CLI.ExceptionHandler = (exception) => {
        var color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        if (exception is OperationCanceledException)
        {   // special case
            Console.Error.WriteLine("Operation cancelled!");
        }
        else if (g_debugMode)
        {   // show detailed exception info in debug mode
            Console.Error.WriteLine(exception.ToString());
        }
        else 
        {   // show short error message during normal run
            Console.Error.WriteLine($"Error: {exception.Message}");
        }
        Console.ForegroundColor = color;
        return 1; // exit code
    };
}
```

# .Net framework support
Supported frameworks can be found on the [SnapCLI NuGet page](https://www.nuget.org/packages/SnapCLI#supportedframeworks-body-tab). The goal is to maintain the same level of support as the System.CommandLine library.

# License
This project is licensed under the [MIT License](LICENSE.md).
Some parts of this project are borrowed with modifications from [DragonFruit](https://github.com/dotnet/command-line-api/tree/main/src/System.CommandLine.DragonFruit/targets) under the [MIT License](LICENSE-command-line-api.md).
