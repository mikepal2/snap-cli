The SnapCLI library provides a simple Command Line Interface (CLI) API in C#, built on top of [System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/).

## NuGet package
The library is available in a NuGet package:
- [SnapCLI](https://www.nuget.org/packages/SnapCLI/)

## Motivation
 The goal of this project is to provide a simple and effective way to handle command-line commands and parameters, allowing developers to create POSIX-like CLI applications with minimal hassle in parsing the command line and enabling them to focus on application logic. Additionally, it facilitates providing all necessary information for the application's help system, making it easily accessible to end users. The [DragonFruit](https://github.com/dotnet/command-line-api/tree/main/src/System.CommandLine.DragonFruit/targets) project was a step in this direction, but is very limited in abilities it provides.


## Command line syntax
Since this project is based on the ```System.CommandLine``` package, the parsing rules are exactly the same as those for that package. The Microsoft [documentation](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax) provides detailed explanations of the command-line syntax recognized by ```System.CommandLine```. We will include more links to this documentation throughout the text below.

## API Paradigm
The API paradigm of this project is to use [attributes](https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/reflection-and-attributes/) to declare and describe CLI commands, options, and arguments.

Any public static method can be declared as a CLI command handler using the ```[Command]``` attribute, and effectively represent an entry point to the CLI application for that command. Any parameter of command handler method automatically becomes a command option. See examples below for details.


## Commands
A [command](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#commands) in command-line input is a token that specifies an action or defines a group of related actions.

Any public static method can be declared as a CLI command handler using the ```[Command]``` attribute. 

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

- If a program has only one command handler method declared with ```[Command]``` attribute and the command name is not explicitly specified in the ```name``` parameter of the attribute, this command is automatically treated as [root command](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#root-commands) (see more [below](#root-command)).
- If the command name not specified in the attribute then the method name, converted to lower case, is implicitly used as the command name. For example method ```Hello()``` will handle ```Hello``` command.
- If method name is used implicitly and contains an underscore (```_```), it declares a [subcommand](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#subcommands). For example, a method named "list_orders()" will define a subcommand ```orders``` under ```list``` command.
- If name is specified explicitly and contains spaces, it declares a subcommand. For example, ```name:"list orders"``` declares ```orders``` as a subcommand of the ```list``` command.

More information on subcommands is provided [below](#subcommands).

## Options
An [option](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#options) is a named parameter that can be passed to a command.

Any parameter of command handler method automatically becomes a command option. In the next example ```name``` becomes option for command ```hello```:

```csharp
[Command(name:"hello", aliases:["hi"], description:"Hello example", hidden:false)]
static public void Hello(string name = "World") 
{
    Console.WriteLine($"Hello {name}!");
}
```

Of course we can provide additional information about option with attribute ```[Options]``` such as explicit name, aliases, description, and whatever option is required.

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

Required options must be specified on the command line; otherwise, the program will show an error and display the command help. Method parameters that have default values (as in the examples above) are, by default, translated into options that are not reuired, while those without default values are always translated into required options.

##### Option name convention
- If option name is not explicitly specified in the attribute, or attribute is ommitted, the  name of the parameter will be implicitly used.
- The option name automatically prepended with one dash ```-``` (if name consists of a single letter) or two dashes ```--```, unless it is already starting with dash.

What do we have so far?

><pre><b>> sample hello -?</b>
>Description:
>  Hello example
>
>Usage:
>  sample hello [options]
>
>Options:
>  --name <name>   The name we should use for the greeting [default: World]
>  -?, -h, --help  Show help and usage information
>
><b>> sample hello</b>
>Hello World!
>
><b>> sample hello --name Michael</b>
>Hello Michael!

## Arguments
An [argument](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#arguments) is a value passed to an option or command without specifying an option name; it is also referred to as a positional argument.

You can declare that parameter is argument with an ```[Argument]``` attribute. Lets change our example a little bit:

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

Now we don't need to specify ```--name``` option name. Also, note how the help message has changed:

><pre><b>> sample hello -?</b>
>Description:
>  Hello example
>
>Usage:
>  sample hello [&lt;name&gt;] [options]
>
>Arguments:
>  &lt;name&gt;  The name we should use for the greeting [default: World]
>
>Options:
>  -?, -h, --help  Show help and usage information
>
><b>>sample hello Michael</b>
>Hello Michael!</pre>

##### Argument name convention
- Argument name is used only for help, it cannot be specified on command line.
- If argument name is not explicitly specified in the attribute, or attribute is ommitted, the  name of the parameter will be implicitly used.

You can provide options before arguments or arguments before options on the command line. See [documentation](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#order-of-options-and-arguments) for details.


## Arity
The [arity](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#argument-arity) of an option or command's argument is the number of values that can be passed if that option or command is specified. Arity is expressed with a minimum value and a maximum value.

```csharp
[Command(name: "print", description: "Arity example")]
static public void Print(
    [Argument(arityMin:1, arityMax:2, name:"numbers", description:"1 or 2 numbers")]
    int[] nums
)
{
    Console.WriteLine($"Numbers are: {string.Join(",", nums)}!");
}
```

><pre><b>> sample print -?</b>
>Description:
>  Arity example
>
>Usage:
>  sample print &lt;numbers&gt;... [options]
>
>Arguments:
>  &lt;numbers&gt;  1 or 2 numbers
>
>Options:
>  -?, -h, --help  Show help and usage information
>
><b>> sample print 12</b>
>Numbers are: 12!
>
><b>> sample print 12 76</b>
>Numbers are: 12,76!</pre>

## Subcommands
As mentioned earlier, if command name has spaces (or name not specified and method name has underscores) it describes a [subcommand](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#subcommands). Any command may have multiple subcommands.

In the following example we have a subcommand ```world``` of the command ```hello```:

```csharp
[Command(name:"hello world", description:"This command greets the world!")]
static public void Hello() 
{
    Console.WriteLine("Hello World!");
}
```

Or equivalent using method name:

```csharp
[Command(description:"This command greets the world!")]
static public void hello_world() 
{
    Console.WriteLine("Hello World!");
}
```

The usage output will be as follows:

><pre>
><b>> sample -?</b>
>Description:
>
>Usage:
>  sample [command] [options]
>
>Options:
>  --version       Show version information
>  -?, -h, --help  Show help and usage information
>
>Commands:
>  hello
>
><b>> sample hello -?</b>
>Description:
>
>Usage:
>  sample hello [command] [options]
>
>Options:
>  -?, -h, --help  Show help and usage information
>
>Commands:
>  world
>
><b>> sample hello world -?</b>
>Description:
>  This command greets the world!
>
>Usage:
>  sample hello world [options]
>
>Options:
>  -?, -h, --help  Show help and usage information
>
>
><b>> sample hello world</b>
>Hello World!
></pre>

In example above we have description for ```hello world``` command, but not for ```hello```. What if we don't need a handler for that command at all? In this case we may use ```[assembly: ParentCommand()]``` attribute at the top of the source file:

```csharp
[assembly: ParentCommand(name: "hello", description: "This command greets someone", aliases: ["hi"])]
```

In a similar way we may provide description to the root command, i.e. to the program itself, using ```[assembly: Program()]``` attribute:

```csharp
[assembly: Program(description: "This is sample program")]
```

Alternatively, we may use standard ```[assembly: AssemblyDescription]``` attribute:

```csharp
[assembly: AssemblyDescription(description: "This is sample program")]
```

## Root command
When we have multiple commands in a CLI program and execute the program without parameters it will show help message. If instead of help we want to perform some actions, we need to assign a handler for the [root command](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#root-commands). For that we use ```[RootCommand]``` attribute and it's usage is similar to one of ```[Command]``` except you cannot specify the command name.

```csharp
[RootCommand(description: "This command greets the world!")]
static public void Hello()
{
    Console.WriteLine("Hello World!");
}
```

Note: There can be only one method declared with ```[RootCommand]``` attribute.

## Global options
Any public static propety or field can be declared as global option with ```[Option]``` attribute.

```csharp
[Option(name:"config", description:"Configuration file name", aliases: ["c","cfg"])]
public static string ConfigFile = "config.ini";
```

## Aliases
Both commands and options may have [aliases](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#aliases).

## Case sensitivity
Command and option names and aliases are [case-sensitive](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax#case-sensitivity). If you want your CLI to be case insensitive, define aliases for the various casing alternatives.

## .Net support
Currently implemeted for .Net 8.0 with plans to support for .Net Standard

## License
This project is licensed under the [MIT license](LICENSE.md).
Parts of this project ([src/build/](src/build/)) borrowed with some modifications from [DragonFruit](https://github.com/dotnet/command-line-api/tree/main/src/System.CommandLine.DragonFruit/targets) under the [MIT license](LICENSE-command-line-api.md).
