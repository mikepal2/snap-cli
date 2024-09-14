The **SnapCLI** library provides a simple Command Line Interface (CLI) API. 
It is built on top of [System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/) library.

# NuGet package
The library is available in a NuGet package:
- [SnapCLI](https://www.nuget.org/packages/SnapCLI/)

# Motivation
 The goal of this project is to provide a simple and effective way to handle command-line commands and parameters, allowing developers to create POSIX-like CLI applications with minimal hassle in parsing the command line and enabling them to focus on application logic. Additionally, it facilitates providing all necessary information for the application's help system, making it easily accessible to end users. The [DragonFruit](https://github.com/dotnet/command-line-api/tree/main/src/System.CommandLine.DragonFruit/targets) project was a step in this direction, but is very limited in abilities it provides.

# API Paradigm
The API paradigm of this project is to use [attributes](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/) to declare and describe CLI commands, options, and arguments.

Any public static method can be declared as a CLI command handler using the `[Command]` attribute, and effectively represent an entry point to the CLI application for that command. Any parameter of command handler method automatically becomes a command option. See examples below for details.

**Where are the classes?**

There are multiple CLI frameworks that require separate class implementation for each command. In my opinion, creating a per-command classes adds unnecessary bloat to the code with little to no benefit. To provide additional information such as descriptions and aliases, attributes are anyway required on top of the class declaration. Since the goal is to simplify things as much as possible, I decided not to use classes at all in my approach.

While this approach may not be as flexible as some other solutions, it meets the basic needs of most CLI applications. Ultimately, the goal is simplicity.

# Command line syntax
Since this project is based on the [System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/) library, the parsing rules are exactly the same as those for that package. The Microsoft documentation provides detailed explanations of the [command-line syntax](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax) recognized by `System.CommandLine`. We will include more links to this documentation throughout the text below.

# Usage

## Commands
A [command](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#commands) in command-line input is a token that specifies an action or defines a group of related actions.

Any public static method can be declared as a CLI command handler using the `[Command]` attribute. 

```csharp
[Command]
static public void Hello() 
{
    Console.WriteLine("Hello World!");
}
```

Additional information can be provided in attribute parameters to enhance command-line parsing and the help system, such as the command's explicit name, aliases, description, and whether the command is hidden

```csharp
[Command(name:"hello", aliases:["hi"], description:"Hello example", hidden:false)]
static public void Hello() 
{
    Console.WriteLine("Hello World!");
}
```

##### Command name convention

- If a program has only one command handler method declared with `[Command]` attribute and the command name is not explicitly specified in the `name` parameter of the attribute, this command is automatically treated as [root command](#root-command)).
- If the command name not specified in the attribute then the method name, converted to lower case, is implicitly used as the command name. For example method `Hello()` will handle `Hello` command.
- If method name is used implicitly and contains an underscore (`_`), it declares a [subcommand](#subcommands). For example, a method named "list_orders()" will define a subcommand `orders` under `list` command.
- If name is specified explicitly and contains spaces, it declares a subcommand. For example, `name:"list orders"` declares `orders` as a subcommand of the `list` command.

## Options
An [option](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#options) is a named parameter that can be passed to a command.

Any parameter of command handler method automatically becomes a command option. In the next example `name` becomes option for command `hello`:

```csharp
[Command(name:"hello", aliases:["hi"], description:"Hello example", hidden:false)]
static public void Hello(string name = "World") 
{
    Console.WriteLine($"Hello {name}!");
}
```

Of course we can provide additional information about option with attribute `[Options]` such as explicit name, aliases, description, and whatever option is required.

```csharp
[Command(name:"hello", aliases:["hi"], description:"Hello example", hidden:false)]
static public void Hello(
    [Option(name:"name", description:"The name we should use for the greeting")]
    string name = "World"
) 
{
    Console.WriteLine($"Hello {name}!");
}
```

Required options must be specified on the command line; otherwise, the program will show an error and display the command help. Method parameters that have default values (as in the examples above) are, by default, translated into options that are not required, while those without default values are always translated into required options.

**Option name convention**
- If option name is not explicitly specified in the attribute, or attribute is ommitted, the  name of the parameter will be implicitly used.
- The option name is automatically prepended with a single dash (`-`) if it consists of a single letter, or with two dashes (`--`) if it is longer, unless it already starts with a dash.

**What do we have so far?**

```
> sample hello -?
Description:
  Hello example

Usage:
  sample hello [options]

Options:
  --name <name>   The name we should use for the greeting [default: World]
  -?, -h, --help  Show help and usage information

> sample hello
Hello World!

> sample hello --name Michael
Hello Michael!
```

## Arguments
An [argument](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#arguments) is a value passed to an option or command without specifying an option name; it is also referred to as a positional argument.

You can declare that parameter is argument with an `[Argument]` attribute. Lets change our example a little bit:

```csharp
[Command(name:"hello", aliases:["hi"], description:"Hello example", hidden:false)]
static public void Hello(
    [Argument(name:"name", description:"The name we should use for the greeting")]
    string name = "World"
) 
{
    Console.WriteLine($"Hello {name}!");
}
```

Now we don't need to specify `--name` option name. Also, note how the help message has changed:

```
> sample hello -?
Description:
  Hello example

Usage:
  sample hello [name] [options]

Arguments:
  [name]  The name we should use for the greeting [default: World]

Options:
  -?, -h, --help  Show help and usage information

> sample hello Michael
Hello Michael!
```

**Argument name convention**
- Argument name is used only for help, it cannot be specified on command line.
- If argument name is not explicitly specified in the attribute, or attribute is ommitted, the  name of the parameter will be implicitly used.

You can provide options before arguments or arguments before options on the command line. See [documentation](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#order-of-options-and-arguments) for details.


## Arity
The [arity](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#argument-arity) of an option or command's argument is the number of values that can be passed if that option or command is specified. Arity is expressed with a minimum value and a maximum value.

```csharp
[Command(name: "print", description: "Arity example")]
static public void Print(
    [Argument(arityMin:1, arityMax:2, name:"numbers", description:"Takes 1 or 2 numbers")]
    int[] nums
)
{
    Console.WriteLine($"Numbers are: {string.Join(",", nums)}!");
}
```

Output:

```
> sample print -?
Description:
  Arity example

Usage:
  sample print [numbers]... [options]

Arguments:
  [numbers]  Takes 1 or 2 numbers

Options:
  -?, -h, --help  Show help and usage information

> sample print 12
Numbers are: 12!

> sample print 12 76
Numbers are: 12,76!
```

## Root command
The [root command](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#root-commands) is executed if program invoked without any known commands on the command line. If no handler is assigned for the root command, the CLI will indicate that the required command is not provided and display the help message. To assign a handler method for the root command, use the `[RootCommand]` attribute. Its usage is similar to the `[Command]` attribute, except that you cannot specify a command name.

The description for the root command essentially serves as the program description in the help output, as shown when program is invoked with the `--help` parameter. If the root command is not declared, SnapCLI will use the assembly description as the root command description.

As mentioned earlier, if a program has only one command handler method declared with `[Command]` attribute and the command name is not explicitly specified in the `name` parameter of the attribute, SnapCLI will automatically set this command as root command.

```csharp
[RootCommand(description: "This command greets the world!")]
static public void Hello()
{
    Console.WriteLine("Hello World!");
}
```

Note: There can be only one method declared with `[RootCommand]` attribute.

## Subcommands
Any command may have multiple subcommands. As mentioned earlier, if command name includes spaces or if the name is not specified and the method name contains underscores, it will describe a [subcommand](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#subcommands). 

In the following example we have a subcommand `world` of the command `hello`:

```csharp
[Command(name:"hello world", description:"This command greets the world!")]
static public void Hello() 
{
    Console.WriteLine("Hello World!");
}
```

Or equivalent using just method name:

```csharp
[Command(description:"This command greets the world!")]
static public void hello_world() 
{
    Console.WriteLine("Hello World!");
}
```

The usage output will be as follows:

```
> sample -?
Description:

Usage:
  sample [command] [options]

Options:
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  hello

> sample hello -?
Description:

Usage:
  sample hello [command] [options]

Options:
  -?, -h, --help  Show help and usage information

Commands:
  world

> sample hello world -?
Description:
  This command greets the world!

Usage:
  sample hello world [options]

Options:
  -?, -h, --help  Show help and usage information


> sample hello world
Hello World!
```

## Commands without handlers
In the output above we have description for the `hello world` command, but not for the `hello`. To describe the `hello` command without assigning a handler method you may use `[Command()]` attribute at the top of the class containing handler methods.

Similarly, you can provide description for the root command (the first description in the output above) using `[RootCommand()]` attribute at the top of the containing class.

With descriptions provided as shown in the following example, the help output will be complete.

```csharp
[RootCommand(description: "This is a sample program")] // or [assembly: AssemblyDescription(description: "This is sample program")]
[Command(name: "hello", description: "This command greets someone", aliases: ["hi"])]
class Sample
{
    [Command(description:"This command greets the world!")]
    static public void hello_world() 
    {
        Console.WriteLine("Hello World!");
    }
}
```

## Global options
Any public static propety or field can be declared as global option with `[Option]` attribute.

```csharp
[Option(name:"config", description:"Configuration file name", aliases: ["c","cfg"])]
public static string ConfigFile = "config.ini";
```

## Aliases
Both commands and options may have [aliases](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#aliases).

## Case sensitivity
Command and option names and aliases are [case-sensitive](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#case-sensitivity). If you want your CLI to be case insensitive, define aliases for the various casing alternatives.

# .Net support
Currently implemeted for .Net 8.0 with plans to support for .Net Standard

# License
This project is licensed under the [MIT license](LICENSE.md).
Parts of this project ([src/build/](src/build/)) borrowed with some modifications from [DragonFruit](https://github.com/dotnet/command-line-api/tree/main/src/System.CommandLine.DragonFruit/targets) under the [MIT license](LICENSE-command-line-api.md).
