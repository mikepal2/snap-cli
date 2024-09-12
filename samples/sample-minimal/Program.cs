using SnapCLI;

// Minimal CLI example

internal class Program
{
    // By using the [Command] attribute, we designate the Hello() method as the command handler.
    // Since this is the only method in the program with the [Command] attribute and the command name is not explicitly set in the attribute parameter,
    // it automatically becomes the "Root" command. This means the method is executed by default, and no command name needs to be specified on the command line.
    // Additionally, any parameters of the method are treated as options by default, even if they are not explicitly declared with the [Option] attribute.
    [Command]
    public static void Hello(string name = "World")
    {
        Console.WriteLine($"Hello {name}!");
    }
}

/* This program will produce output:

>sample-1-minimal.exe --help
Description:

Usage:
  sample-minimal [options]

Options:
  --name <name>   [default: World]
  --version       Show version information
  -?, -h, --help  Show help and usage information


>sample-1-minimal.exe
Hello World!

>sample-1-minimal.exe --name Joe
Hello Joe!
  
*/