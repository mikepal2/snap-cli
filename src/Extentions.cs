//#define BEFORE_AFTER_COMMAND_ATTRIBUTE

using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;

namespace SnapCLI
{
    /// <summary>
    /// SnapCLI extentions
    /// </summary>
    public static class Extentions
    {
        /// <summary>
        /// Validates that only one of specified options appear on command line.
        /// </summary>
        /// <param name="parseResult">Command line parse result.</param>
        /// <param name="options">List of mutually exclusive options.</param>
        /// <param name="commands">Optional list of commands this validation applies. If not specified, validation applies to all commands.</param>
        /// <exception cref="ArgumentException">When there are mutually exclusive options found on command line.</exception>
        /// <exception cref="ArgumentNullException">Option name or command name is <code>null</code>.</exception>
        public static void ValidateMutuallyExclusiveOptions(this ParseResult parseResult, string[] options, string[]? commands = null)
        {
            if (options.Length < 2)
                throw new ArgumentException($"At least two options must be specified for {nameof(ValidateMutuallyExclusiveOptions)} method");

            if (commands != null)
            {
                if (!commands.Any(commandName => parseResult.RootCommandResult.Command.GetCommand(commandName ?? throw new ArgumentNullException(nameof(commandName))) == parseResult.CommandResult.Command))
                {
                    return;
                }
            }

            var optionsPresentInCommandLine = parseResult.RootCommandResult.Children // cannot access option.IsGlobal(), assume all options of root command are global options
                    .Concat(parseResult.CommandResult.Children) // command options
                    .OfType<OptionResult>()
                    .Where(optResult => optResult.IsImplicit == false && options.Any(optionName => optResult.Option.NameEquals(optionName ?? throw new ArgumentNullException(nameof(optionName)))))
                    .Select(optResult => optResult.Option)
                    .ToArray();

            if (optionsPresentInCommandLine.Length > 1)
                throw new ArgumentException($"Options {optionsPresentInCommandLine[0]!.Name} and {optionsPresentInCommandLine[1]!.Name} are mutually exclusive for command '{parseResult.CommandResult.Command.FullName()}'");
        }

        /// <summary>
        /// Find option by specified name or <code>null</code> if command has no such option.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="name">Option name or alias to find.</param>
        /// <returns>Option matching specified name or <code>null</code> if command has no such option.</returns>
        public static Option? GetOption(this Command command, string name)
        {
            return command.Options.FirstOrDefault(opt => opt.NameEquals(name));
        }
        
        /// <summary>
        /// Find command by name.
        /// </summary>
        /// <param name="command">Root command to start search, usually RootCommand.</param>
        /// <param name="name">Full name of command to find. Subcommands are separated by spaces.</param>
        /// <returns>Command matching the <paramref name="name"/></returns>
        public static Command? GetCommand(this Command? command, string name)
        {
            foreach (var subcommandName in name.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (command == null)
                    break;
                command = command.Subcommands.FirstOrDefault(c => c.Name == subcommandName || c.Aliases.Any(alias => alias == subcommandName));
            }
            return command;
        }

        /// <summary>
        /// Return command full name including parents.
        /// </summary>
        /// <param name="command"></param>
        /// <returns>Command full name including parents.</returns>
        public static string FullName(this Command command)
        {
            var name = command.Name;
            Symbol? parent = command;
            while (parent != null) 
            {
                parent = parent.Parents.OfType<Command>().FirstOrDefault();
                if (parent is RootCommand)
                    break;
                if (parent != null)
                    name = parent.Name + " " + name;
            }
            return name;
        }

        /// <summary>
        /// Compares symbol name taking into account any aliases and full names.
        /// </summary>
        /// <param name="symbol"><see cref="Command"/>, <see cref="Option"/> or <see cref="Argument"/>.</param>
        /// <param name="name">Name to match.</param>
        /// <returns>True if symbol has matching name or alias.</returns>
        public static bool NameEquals(this Symbol symbol, string name)
        {
            switch (symbol)
            {
                case Option opt:
                    name = name.TrimStart('-');
                    return opt.Aliases.Any(alias => alias.TrimStart('-') == name);
                case Command command:
                    if (command.Aliases.Any(alias => alias == name))
                        return true;
                    return command.FullName() == name;
                default:
                    return symbol.Name == name;
            }
        }
    }
}
