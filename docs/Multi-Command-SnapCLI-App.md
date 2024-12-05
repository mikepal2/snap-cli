# Building Multi-Command CLI

Let's say you need an app that will have multiple commands.

In this walkthrough, we will create an app to encode and decode base64 format using the SnapCLI app model.

## Create Project

```console
> dotnet new console -o base64
> cd base64
> dotnet add package SnapCLI --prerelease
```

## Create Commands

Replace the content of `Program.cs` with the following code.

Here, we define two commands — `encode` and `decode` — both accepting a string argument and printing the result to the console.

```csharp
using SnapCLI;

class Program
{
    [Command]
    public static void Encode([Argument] string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        var base64 = System.Convert.ToBase64String(bytes);
        Console.WriteLine(base64);
    }

    [Command]
    public static void Decode([Argument] string base64)
    {
        var bytes = System.Convert.FromBase64String(base64);
        var text = System.Text.Encoding.UTF8.GetString(bytes);
        Console.WriteLine(text);
    }
}
```

> **Technical note:** In this example, the two methods `Encode()` and `Decode()` are declared with the `[Command]` attribute and represent two entry points for the CLI application, each associated with the corresponding command. The command names are automatically derived from the method names as `encode` and `decode`, respectively.

> Note, that there is no `Main` method as with SnapCLI it doesn't represent application entry point. See more details on Main in [documentation](Documentation#main-method).

automatically recognizes the `Main` method as the [root command](Documentation#root-command) 

The application is ready to run.

```console
> base64 -?
Description:

Usage:
  base64 [command] [options]

Options:
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  encode <text>
  decode <base64>

> base64 encode "Hello World!"
SGVsbG8gV29ybGQh

> base64 decode SGVsbG8gV29ybGQh
Hello World!
```

Now, let's add descriptions to make the help more user-friendly.

```csharp
```

Now, let's make it a bit more complex by adding the ability to read input from a file and write output to a file. For this, we will add `--input` and `--output` options for each command. We will also declare that the string argument and the `--input` option are mutually exclusive, i.e., only one of them can be specified on the command line.


