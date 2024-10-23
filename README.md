# SnapCLI

Quickly create POSIX-like Command Line Interface (CLI) applications with a simple metadata API, built on top of the [System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/) library.

## NuGet Package

The library is available as a NuGet package:

- [SnapCLI](https://www.nuget.org/packages/SnapCLI/)

## Motivation

The goal of this project is to enable developers to create POSIX-like CLI applications without the hassle of manually parsing command line inputs. 

While the Microsoft's `System.CommandLine` library provides all the necessary APIs to parse command line arguments, it requires significant effort to set up the code before the program is ready to run. It also involves repetitive work in referencing the same objects from multiple places, making code maintenance difficult and error-prone - see [details and examples](./docs/SystemCommandLineExample.md).

The inspiration for this project came from the [DragonFruit](https://github.com/dotnet/command-line-api/blob/main/docs/DragonFruit-overview.md) project, which was a step in the right direction to simplify the usage of `System.CommandLine` but has significant limitations.

This library automatically manages command-line commands and parameters using the provided metadata, simplifying the development process and allowing developers to focus on their application logic. It also streamlines the creation of the application's help system, ensuring that all necessary information is easily accessible to end users. There’s even [no need](#main-method) to write a `Main()` method for the application. This means developers can skip writing any startup boilerplate code for their CLI applications and dive straight into implementing the application logic, i.e., commands.


## API Paradigm

This project employs an API paradigm that utilizes [attributes](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/) to declare and describe CLI commands, options, and arguments through metadata.

Any public static method can be designated as a CLI command handler using the `[Command]` attribute, effectively serving as an entry point for that command within the CLI application. Each parameter of the command handler method automatically becomes a command option. Refer to the [usage](#usage) section and examples below for further details.

### What About Classes?

Many CLI frameworks require separate class implementations for each command. However, creating individual classes for each command can add unnecessary complexity with minimal benefit. Using attributes simplifies maintenance and enhances readability, as they are declared close to the entities they describe, keeping related information in one place. Additionally, attributes can provide extra details, such as descriptions and aliases.

To minimize complexity, the decision was made to avoid using classes. While this approach may not offer the same flexibility as some alternatives, it effectively meets the needs of most CLI applications. Additional customizations are also possible, see [Advanced Usage](#advanced-usage) section.

## Command Line Syntax

Since this project is based on the [System.CommandLine🗗](https://learn.microsoft.com/en-us/dotnet/standard/commandline/) library, the parsing rules align with those established by that package. Microsoft provides detailed explanations of the [command-line syntax🗗](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax) recognized by `System.CommandLine`. 

It is recommended to follow the System.CommandLine [design guidance🗗](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#design-guidance) when designing your CLI.

> **Note**: Additional links to Microsoft documentation, marked with 🗗 symbol, will be included throughout the text below.


# Basic Usage
The main goal of the library is to simplify the development of POSIX-like CLI applications. It takes responsibility for the initialization and execution of the application, eliminating the need for any startup boilerplate code. There’s even [no need](#main-method) to write a `Main()` method for the application.

For more complex CLI applications, the library allows for fine control over initialization, execution, and exception handling - see [Advanced Usage](#advanced-usage).

Here, we will explore basic constructs to define [commands](#commands), [options](#options) and [arguments](#arguments).

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
[Command(Name = "hello", Aliases = "hi,hola,bonjour", Description = "Hello example", Hidden = false)]
public static void Hello() 
{
    Console.WriteLine("Hello World!");
}
```

Async handler methods are also supported. 

The library supports handler methods with the following return types: `void`, `int`, `Task<int>`, `Task`, `ValueTask<int>`, and `ValueTask`. The result from handlers returning `int`, `Task<int>`, and `ValueTask<int>` is used as the program's exit code.

```csharp
[Command(Name = "sleep", Description = "Async sleep example")]
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
  - If there are multiple commands declared, the method name, converted to [kebab case](#kebab-case), is used as the command name. For example, the method `Hello()` will handle the `hello` command, while method `HelloWorld()` will handle `hello-world` command.
  - If the method name contains underscores (`_`), it declares a [subcommand](#subcommands). For example, a method named "order_create()" will define a subcommand `create` under the `order` command.
- If the name specified in the `[Command]` attribute explicitly contains spaces, it declares a [subcommand](#subcommands). For example, `[Command(Name = "order create")]` defines `create` as a subcommand of the `order` command.
- Commands may have [aliases🗗](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#aliases). These are usually short forms that are easier to type or alternate spellings of a word.
- Command names and aliases are [case-sensitive🗗](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#case-sensitivity). If you want your CLI to be case insensitive, define aliases for the various casing alternatives.

## Options
An [option🗗](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#options) is a named parameter that can be passed to a command.

Any parameter of command handler method automatically becomes a command option. In the next example `name` becomes option for command `hello`:

```csharp
[Command(Name = "hello", Aliases = "hi,hola,bonjour", Description = "Hello example", Hidden = false)]
public static void Hello(string name = "World") 
{
    Console.WriteLine($"Hello {name}!");
}
```

Additional information about an option can be provided using the `[Option]` attribute, including an explicit name, aliases, a description, and whether the option is required.

```csharp
[Command(Name = "hello", Aliases = "hi,hola,bonjour", Description = "Hello example", Hidden = false)]
public static void Hello(
    [Option(Name = "name", Description = "The name we should use for the greeting")]
    string name = "World"
) 
{
    Console.WriteLine($"Hello {name}!");
}
```

**Required options**

Required options must be specified on the command line; otherwise, the program will show an error and display the command help. Method parameters that have default values (as in the examples above) are, by default, translated into options that are not required, while those without default values are always translated into required options. You may force option to be required using `Required` property of the attribute.

**Option name convention**

- The option name is automatically prepended with a single dash (`-`) if it consists of a single letter, or with two dashes (`--`) if it is longer, unless it already starts with a dash.
- If option name is not explicitly specified in the attribute, or attribute is omitted, the  name of the parameter, converted to [kebab case](#kebab-case), will be used implicitly. For example, for the parameter `userId` the default option name will be `--user-id`.
- Options may have [aliases🗗](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#aliases). These are usually short forms that are easier to type or alternate spellings of a word.
- Option names and aliases are [case-sensitive🗗](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#case-sensitivity). If you want your CLI to be case insensitive, define aliases for the various casing alternatives.

**What do we have so far?**

With the full program source code consisting of just a few lines:
```csharp
using SnapCLI;

class Program
{
    [Command(Name = "hello", Description = "Hello example")]
    public static void Hello(
        [Option(Name = "name", Description = "The name we should use for the greeting")]
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
An [argument🗗](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#arguments) is a value passed to an option or command without specifying an option name; it is also referred to as a positional argument.

You can declare that a parameter is an argument using the `[Argument]` attribute. Let's change "Option" to "Argument" in our example:

```csharp
[Command(Name = "hello", Description = "Hello example")]
public static void Hello(
    [Argument(Name = "name", Description = "The name we should use for the greeting")]
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
- If argument name is not explicitly specified in the attribute, the name of the parameter, converted to [kebab case](#kebab-case), will be used implicitly.

You can provide options before arguments or arguments before options on the command line. See [System.CommandLine documentation🗗](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#order-of-options-and-arguments) for details.

# Advanced Usage
Besides basic constructs for [commands](#commands), [options](#options), and [arguments](#arguments), the library allows for fine control over application initialization, execution, and exception handling.


## Application Lifecycle

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

The command handler is the only *required* component that must be provided by the application developer, all other steps are automatic or optional.

## Root command
The [root command🗗](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#root-commands) is executed if program invoked without any known commands on the command line. If no handler is assigned for the root command, the CLI will indicate that the required command is not provided and display the help message. To assign a handler method for the root command, use the `[RootCommand]` attribute. Its usage is similar to the `[Command]` attribute, except that you cannot specify a command name. There can be only one method declared with `[RootCommand]` attribute.

The description for the root command essentially serves as the program description in the help output, as shown when program is invoked with the `--help` parameter. If the root command is not declared, **SnapCLI** library will use the assembly description as the root command description.

```csharp
[RootCommand(Description = "This command greets the world!")]
public static void Hello()
{
    Console.WriteLine("Hello World!");
}
```

> **Note**: If a program has only one command handler method declared with the [Command] attribute, and the command name is not explicitly specified in the `Name` property of the attribute, the SnapCLI library will automatically set this command as the root command.

## Subcommands
Any command may have multiple subcommands. As mentioned earlier, if command name includes spaces or if the name is not specified and the method name contains underscores, it will describe a [subcommand🗗](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#subcommands). 

In the following example we have a subcommand `world` of the command `hello`:

```csharp
[Command(Name = "hello world", Description = "This command greets the world!")]
public static void Hello() 
{
    Console.WriteLine("Hello World!");
}
```

Or equivalent using just method name:

```csharp
[Command(Description = "This command greets the world!")]
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

[assembly: RootCommand(Description = "This is a sample program")] // or [assembly: AssemblyDescription(Description = "This is sample program")]
[assembly: Command(Name = "hello", Description = "This command greets someone", Aliases= "hi,hola,bonjour")]

class Program
{
    [Command(Description = "This command greets the world!")]
    public static void hello_world() 
    {
        Console.WriteLine("Hello World!");
    }
}
```

## Global options
Any public static property or field can be declared as global option using the `[Option]` attribute.

By default, global options are not required because properties and fields always have default values, either implicitly or explicitly. You can make a global option required by using the `Required` property of the attribute.

```csharp
class Program
{
    // This global option is not required and have explicit default value of "config.json"
    [Option(Name = "config", Description = "Configuration file name", Aliases = "c,cfg")]
    public static string ConfigFile = "config.json";

    // This global option is not required and have implicit default value of (null)
    [Option(Name = "profile", Description = "User profile")]
    public static string Profile;

    // This global option is always required
    [Option(Name = "user", Description = "User name", Required = true)]
    public static string User { get; set; }

    ...
}
```

## Kebab case
When the command, option, or argument name is not specified in the attribute, an implicit name is generated based on the method name for commands or the parameter name for options and arguments. If the name is in [Camel case🗗](https://en.wikipedia.org/wiki/Letter_case#Camel_case), a hyphen is inserted between words. Finally, the name is converted to lowercase. 

Below are examples of method/parameter names and their resulting kebab-case names.

| Name               | Kebab-case            |
|--------------------|-----------------------|
| `Hello`            | `hello`               |
| `HelloWorld`       | `hello-world`         |
| `user`             | `user`                |
| `userId`           | `user-id`             |
| `noLaunchProfile`  | `no-launch-profile`   |

## Arity
The [arity🗗](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#argument-arity) of an option or command's argument is the number of values that can be passed if that option or command is specified. Arity is expressed with a minimum value and a maximum value.

```csharp
[Command(Name = "print", Description = "Arity example")]
public static void Print(
    [Argument(arityMin:1, arityMax:2, Name = "numbers", Description = "Takes 1 or 2 numbers")]
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

## Main method  
Typically, the `Main` method serves as the entry point of a C# application. However, to simplify startup code and usage, this library overrides the program's entry point and uses command handler methods as the entry points instead. 

It’s important to note that since the library overrides the application entry point, if you include your own `Main` function in the program, it will **not** be invoked.

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

The startup method may have a parameter of type `CommandLineBuilder`. If you choose this alternative, you must configure `CommandLineBuilder` yourself, typically using the [.UseDefaults()🗗](https://learn.microsoft.com/en-us/dotnet/api/system.commandline.builder.commandlinebuilderextensions.usedefaults?view=system-commandline#system-commandline-builder-commandlinebuilderextensions-usedefaults(system-commandline-builder-commandlinebuilder)) extension method.

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

> **Important:** When the startup method is invoked, the command line has not been parsed yet; therefore properties and fields declared as global options still have their default values and not the values from the command line, and `CLI.ParseResult` property is not accessible.


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

For global options validation may be implemented in property setter.

```csharp
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

    ...
}
```

## Mutually Exclusive Options and Arguments
The library provides mechanisms to check for mutually exclusive options and arguments

* The `MutuallyExclusiveOptionsArguments` property of the `[Command]` attribute can be used to declare a comma-separated list of mutually exclusive option/argument names. If there are multiple groups of mutually exclusive options/arguments, they must be enclosed in parentheses.
  ```csharp
  [Command(MutuallyExclusiveOptionsArguments = "(opt1,opt2)(arg1,opt2)")]
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
