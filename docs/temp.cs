using SnapCLI;
using System;

class Program
{
    [Command]
    public static void Encode([Argument] string input)
    {
        var bytes = Text.Encoding.UTF8.GetBytes(input);
        var output = Convert.ToBase64String(bytes);
        Console.WriteLine(output);
    }

    [Command]
    public static void Decode([Argument] string input)
    {
        var bytes = Convert.FromBase64String(input);
        var output = Text.Encoding.UTF8.GetString(bytes);
        Console.WriteLine(output);
    }
}