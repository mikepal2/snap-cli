using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SnapCLI
{
    /// <summary>
    /// DescriptorAttribute is only used as base for other attributes and should not be used by clients 
    /// </summary>
    public class DescriptorAttribute : Attribute
    {
// don't generate XML documentation for internals of this class
#pragma warning disable 1591
        public enum DescKind
        {
            RootCommand,
            Command,
            Argument,
            Option
        }

        public DescKind Kind { get; }
        public string? Name { get; }
        public string? Description { get; }
        public string[]? Aliases { get; }
        public string? HelpName { get; }
        public bool IsHidden { get; }
        public bool IsRequired { get; }
        public string? MutuallyExclusuveOptionsArguments { get; }
        public ArgumentArity? Arity { get; }

        // only allow to use this attribute in subclasses
        private DescriptorAttribute() { }

        protected DescriptorAttribute(DescKind kind, string? name = null, string? description = null, string? aliases = null, string? helpName = null, bool hidden = false, bool required = false, string? mutuallyExclusuveOptionsArguments = null)
        {
            Kind = kind;
            Name = name;
            Description = description;
            IsHidden = hidden;
            IsRequired = required;
            MutuallyExclusuveOptionsArguments = mutuallyExclusuveOptionsArguments;
            HelpName = helpName;
            Aliases = SplitAliases(aliases);
        }

        protected DescriptorAttribute(DescKind kind, int arityMin, int arityMax, string? name = null, string? description = null, string? aliases = null, string? helpName = null, bool hidden = false, bool required = false, string? mutuallyExclusuveOptionsArguments = null)
            : this(kind, name: name, description: description, aliases: aliases, helpName: helpName, hidden: hidden, required: required, mutuallyExclusuveOptionsArguments: mutuallyExclusuveOptionsArguments)
        {
            Arity = new ArgumentArity(arityMin, arityMax);
        }

        public override string ToString()
        {
            return $"{Kind}: name:{Name ?? HelpName ?? Aliases?.FirstOrDefault()}, desc:{Description}";
        }

        internal static readonly char[] NameListDelimiters = " ,;".ToCharArray();
        private static string[]? SplitAliases(string? aliases)
        {
            if (aliases == null)
                return null;
            return aliases.Split(DescriptorAttribute.NameListDelimiters, StringSplitOptions.RemoveEmptyEntries)
                .Where(alias => !string.IsNullOrWhiteSpace(alias))
                .ToArray();
        }

    }
#pragma warning restore 1591

    /// <summary>
    /// Declares <b>option</b> definition for CLI command.
    /// <remarks>
    /// <para>Applies to command handler method arguments.</para>
    /// <para>Can also be used on static fields and properies to declare global options.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// 
    ///     [Command]
    ///     public static void Hello(
    ///         [Option(name:"Name", description:"Person's name")]
    ///         string name = "everyone"
    ///     ) 
    ///     {
    ///       Console.WriteLine($"Hello {name}!");
    ///     }
    /// 
    /// </code>
    /// Global option:
    /// <code>
    /// 
    ///     [Option(name:"config", description:"Specifies configuration file path)]
    ///     public static string g_configFile = "config.json";
    /// 
    /// </code>
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
    public class OptionAttribute : DescriptorAttribute
    {
        /// <summary>
        /// Declares the CLI <b>option</b> definition.
        /// </summary>
        /// <param name="name">The name of the option.</param>
        /// <param name="description">A description of the option.</param>
        /// <param name="aliases">A list of aliases for the option, separated by spaces, commas, semicolons, or pipe characters.</param>
        /// <param name="helpName">The name of the option value.</param>
        /// <param name="hidden">Hidden options are not shown in help, but they can still be used on the command line.</param>
        /// <param name="required">Required options must always be specified on the command line.</param>
        public OptionAttribute(string? name = null, string? description = null, string? aliases = null, string? helpName = null, bool hidden = false, bool required = false)
            : base(DescKind.Option, name: name, description: description, aliases: aliases, helpName: helpName, hidden: hidden, required: required) { }

        /// <summary>
        /// Declares the CLI <b>option</b> definition.
        /// </summary>
        /// <param name="arityMin">The minimum number of values an option can receive.</param>
        /// <param name="arityMax">The maximum number of values an option can receive.</param>
        /// <param name="name">The name of the option.</param>
        /// <param name="description">A description of the option.</param>
        /// <param name="aliases">A list of aliases for the option, separated by spaces, commas, semicolons, or pipe characters.</param>
        /// <param name="helpName">The name of the option value.</param>
        /// <param name="hidden">Hidden options are not shown in help, but they can still be used on the command line.</param>
        /// <param name="required">Required options must always be specified on the command line.</param>
        public OptionAttribute(int arityMin, int arityMax, string? name = null, string? description = null, string? aliases = null, string? helpName = null,  bool hidden = false, bool required = false)
            : base(DescKind.Option, arityMin: arityMin, arityMax: arityMax, name: name, description: description, aliases: aliases, helpName: helpName, hidden: hidden, required: required) { }
    }

    /// <summary>
    /// Declares <b>argument</b> definition for CLI command.
    /// <remarks>
    /// <para>Can be applied to method argument</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// 
    ///     [Command]
    ///     static public void Read(
    ///         [Argument(name:"path", description:"Input file path")] 
    ///         string filepath
    ///     ) 
    ///     {
    ///       ... 
    ///     }
    ///     
    /// </code>
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ArgumentAttribute : DescriptorAttribute
    {
        /// <summary>
        /// Declares the <b>argument</b> definition for a CLI command.
        /// </summary>
        /// <param name="name">The name of the argument.</param>
        /// <param name="description">A description of the argument.</param>
        /// <param name="helpName">The name of the argument as displayed in help.</param>
        /// <param name="hidden">Hidden arguments are not shown in help but can still be used.</param>
        public ArgumentAttribute(string? name = null, string? description = null, string? helpName = null, bool hidden = false)
            : base(DescKind.Argument, name: name, description: description, aliases: null, helpName: helpName, hidden: hidden) { }

        /// <summary>
        /// Declares the <b>argument</b> definition for a CLI command.
        /// </summary>
        /// <param name="arityMin">The minimum number of values an argument can receive.</param>
        /// <param name="arityMax">The maximum number of values an argument can receive.</param>
        /// <param name="name">The name of the argument.</param>
        /// <param name="description">A description of the argument.</param>
        /// <param name="helpName">The name of the argument as displayed in help.</param>
        /// <param name="hidden">Hidden arguments are not shown in help but can still be used.</param>
        public ArgumentAttribute(int arityMin, int arityMax, string? name = null, string? description = null, string? helpName = null, bool hidden = false)
        : base(DescKind.Argument, arityMin, arityMax, name: name, description: description, aliases: null, helpName: helpName, hidden: hidden) { }
    }

    /// <summary>
    /// Declares handler for <see cref="RootCommand"/>, i.e. command that executed when no subcommands are present on the command line. Only one method may be declared with this attribute.
    /// <remarks>
    /// <para>If program has only one method declared with <see cref="CommandAttribute"/> and command name not explicitly specified in <c>name</c> attribute, this command is automatically treated as root command.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// 
    ///     [RootCommand]
    ///     static public void Hello() 
    ///     {
    ///       ... 
    ///     }
    ///     
    /// </code>
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method|AttributeTargets.Class)]
    public class RootCommandAttribute : DescriptorAttribute  {
        /// <summary>
        /// Declares handler for <see cref="RootCommand"/>, i.e. command that executed when no subcommands are present on the command line. Only one method may be declared with this attribute.
        /// </summary>
        /// <param name="description">Root command description, also serving as programs general description when help is shown.</param>
        /// <param name="mutuallyExclusuveOptionsArguments">List of mutually exclusive options/arguments names separated by spaces, commas, semicolons, or pipe characters. If there are multiple groups of mutually exclusive options/arguments, they must be enclosed in parentheses. Example: (option1,option2)(option3,arg1)</param>

        public RootCommandAttribute(string? description = null, string? mutuallyExclusuveOptionsArguments = null) :
            base(DescKind.RootCommand, description: description, mutuallyExclusuveOptionsArguments: mutuallyExclusuveOptionsArguments)
        {
        }
    }

    /// <summary>
    /// Declares handler for CLI <see cref="Command"/>. 
    /// <remarks>
    /// <para>Can be applied to any static public method</para>
    /// <para>If program has only one method declared with <see cref="CommandAttribute"/> and command name not explicitly specified in <code>name</code> parameter, this command is automatically treated as root command.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// 
    ///     [Command]
    ///     static public void Hello() 
    ///     {
    ///       ... 
    ///     }
    ///     
    /// </code></example>
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>If the name is not specified and the program has only one method declared with <see cref="CommandAttribute"/>, this command is automatically treated as <see cref="RootCommand"/>.</description></item>
    /// <item><description>If the name is not specified, the method name converted to kebab-case is used as the command name. For example, the method <c>HelloWorld()</c> will handle the <c>hello-world</c> command.</description></item>
    /// <item><description>If the method name is used and contains the underscore <c>_</c> character, it describes a subcommand. For example, the method <c>order_create()</c> is the subcommand <b>crate</b> of the <b>oreder</b> command.</description></item>
    /// <item><description>If the name is specified and contains spaces, it describes a subcommand. For example, <c>order list</c> is the subcommand <b>list</b> of the <b>order</b> command.</description></item>
    /// </list>  
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method|AttributeTargets.Class, AllowMultiple = true)]
    public class CommandAttribute : DescriptorAttribute {
        /// <summary>
        /// Declares a handler for the CLI <see cref="Command"/>.
        /// </summary>
        /// <param name="name">The name of the command.</param>
        /// <param name="description">A description of the command.</param>
        /// <param name="aliases">A list of aliases for the command. This can be a string where aliases are separated by spaces, commas, semicolons, or pipe characters.</param>
        /// <param name="hidden">Hidden commands are not shown in help but can still be used.</param>
        /// <param name="mutuallyExclusuveOptionsArguments">List of mutually exclusive options/arguments names separated by spaces, commas, semicolons, or pipe characters. If there are multiple groups of mutually exclusive options/arguments, they must be enclosed in parentheses. Example: (option1,option2)(option3,arg1)</param>
        public CommandAttribute(string? name = null, string? description = null, string? aliases = null, bool hidden = false, string? mutuallyExclusuveOptionsArguments = null) 
            : base(DescKind.Command, name: name, description: description, aliases: aliases, hidden: hidden, mutuallyExclusuveOptionsArguments: mutuallyExclusuveOptionsArguments)
        {
        }
    }

    /// <summary>
    /// This exception describes incorrect usage of CLI attributes. It is thrown during CLI initialization if any errors in attribute usage are detected.
    /// </summary>
    public class AttributeUsageException : Exception {
        /// <summary>
        /// Initializes a new instance of the SnapCLI.AttributeUsageException class.
        /// </summary>
        public AttributeUsageException() : base() { }
        /// <summary>
        /// Initializes a new instance of the SnapCLI.AttributeUsageException class with a specified error message.
        /// </summary>
        public AttributeUsageException(string message) : base(message) { }
    }

    /// <summary>
    /// Declares startup method for CLI. The method must be public static and may have no parameters or one parameter of type <see cref="CommandLineBuilder"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class StartupAttribute : Attribute
    {
    }

    /// <summary>
    /// Command Line Interface class implementation. Provides simple interface to create CLI programs using attributes declarations.
    /// </summary>
    public static class CLI
    {
        private class ConsoleHelper : IConsole
        {
            private class StandardStreamWriter : IStandardStreamWriter
            {
                public StandardStreamWriter(TextWriter stream)
                {
                    Stream = stream;
                }

                public TextWriter Stream { get; }

                public void Write(string? value)
                {
                    Stream.Write(value);
                }
            }

            private ConsoleHelper(TextWriter? output, TextWriter? error)
            {
                Out = new StandardStreamWriter(output ?? Console.Out);
                IsOutputRedirected = output != null;
                Error = new StandardStreamWriter(error ?? Console.Error);
                IsErrorRedirected = error != null;
            }

            public static IConsole? CreateOrDefault(TextWriter? output = null, TextWriter? error = null)
            {
                if (output == null && error == null)
                    return null;
                return new ConsoleHelper(output, error);
            }

            IStandardStreamWriter Out;
            IStandardStreamWriter Error;

            public bool IsOutputRedirected;

            public bool IsErrorRedirected;

            IStandardStreamWriter IStandardOut.Out => Out;

            IStandardStreamWriter IStandardError.Error => Error;

            bool IStandardOut.IsOutputRedirected => IsOutputRedirected;

            bool IStandardError.IsErrorRedirected => IsErrorRedirected;

            bool IStandardIn.IsInputRedirected => false;
        }

        /// <summary>
        /// Arguments for the BeforeCommand event.
        /// </summary>
        public class BeforeCommandEventArguments
        {
            /// <summary>
            /// Command line parse result. 
            /// </summary>
            public ParseResult ParseResult;

            /// <summary>
            /// BeforeCommandEventArguments constructor.
            /// </summary>
            /// <param name="parseResult">Command line parse result.</param>
            public BeforeCommandEventArguments(ParseResult parseResult) => ParseResult = parseResult;
        }

        /// <summary>
        /// Event invoked immediately before command is executed. Can be used for custom initialization.
        /// </summary>
        public static event Action<BeforeCommandEventArguments>? BeforeCommand;

        /// <summary>
        /// Arguments for AfterCommandEvent.
        /// </summary>
        public class AfterCommandEventArguments
        {
            /// <summary>
            /// Command line parse result.
            /// </summary>
            public ParseResult ParseResult;
            
            /// <summary>
            /// Exit code to return from CLI program. The handler may change the exit code to reflect specific execution results.
            /// </summary>
            public int ExitCode;

            /// <summary>
            /// AfterCommandEventArguments constructor.
            /// </summary>
            /// <param name="parseResult">Command line parse result.</param>
            /// <param name="exitCode">Exit code to return from CLI program.</param>
            public AfterCommandEventArguments(ParseResult parseResult, int exitCode)
            {
                ParseResult = parseResult;
                ExitCode = exitCode;
            }

        }

        /// <summary>
        /// Event invoked immediately after command was executed. Can be used for deinitialization.
        /// </summary>
        public static event Action<AfterCommandEventArguments>? AfterCommand;

        private static Parser Parser { get; }

        /// <summary>
        /// Provides access to commands hierarchy and their options and arguments.
        /// </summary>
        public static RootCommand RootCommand { get; }

        /// <summary>
        /// Handler to use when exception is occured during command execution. Set <code>null</code> to suppress exception handling.
        /// </summary>
        /// <returns>Returns exit code to return from program. It is strongly recommanded to return non-zero exit code on error.</returns>
        public static Func<Exception, int>? ExceptionHandler { get; set; } = DefaultExceptionHandler;

        /// <summary>
        /// Command line parse result.
        /// </summary>
        public static ParseResult ParseResult {
            get { return _parseResult ?? throw new InvalidOperationException($"The {nameof(ParseResult)} property is not available before command line is parsed."); }
            private set { _parseResult = value; }
        }
        private static ParseResult? _parseResult;

        /// <summary>
        /// Cancellation token for asynchronous CLI commands.
        /// </summary>
        public static CancellationToken CancellationToken { get; private set; } = CancellationToken.None;

        private static TextWriter Error
        {
            get => _error ?? Console.Error;
            set => _error = value;
        }
        private static TextWriter? _error;

        private static int DefaultExceptionHandler(Exception exception)
        {
            switch (exception)
            {
                case OperationCanceledException _:
                    break;

                default:
                    if (Error == Console.Error)
                    {
                        var color = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Error.WriteLine(exception.ToString());
                        Console.ForegroundColor = color;
                    }
                    else
                    {
                        Error.WriteLine(exception.ToString());
                    }
                    break;
            }

            return 1;
        }

        private class CommandMethodDesc
        {
            public string CommandName;
            public DescriptorAttribute Desc;
            public MethodInfo Method;

            public CommandMethodDesc(MethodInfo method, DescriptorAttribute desc)
            {
                Method = method;
                Desc = desc;
                CommandName = desc.Name ?? method.Name.Replace('_', ' ').ToKebabCase();
            }
        }

        /// <summary>
        /// Helper method to run CLI application. Should be called from program Main() entry point.
        /// </summary>
        /// <param name="args">Command line arguments passed from Main()</param>
        /// <param name="output">Redirect output stream</param>
        /// <param name="error">Redirect error stream</param>
        /// <returns></returns>
        public static int Run(string[]? args = null, TextWriter? output = null, TextWriter? error = null) 
        {
            try
            {
                _error = error;
                var parseResult = Parser.Parse(args ?? Environment.GetCommandLineArgs().Skip(1).ToArray());
                return parseResult.Invoke(ConsoleHelper.CreateOrDefault(output, error));
            }
            catch (Exception ex)
            {
                if (ExceptionHandler != null)
                    return ExceptionHandler(ex);
                ExceptionDispatchInfo.Capture(ex).Throw();
                return 1;
            }
        }

        /// <summary>
        /// Helper asynchronous method to run CLI application. Should be called from program async Main() entry point.
        /// </summary>
        /// <param name="args">Command line arguments passed from Main()</param>
        /// <param name="output">Redirect output stream</param>
        /// <param name="error">Redirect error stream</param>
        /// <returns></returns>
        public static async Task<int> RunAsync(string[]? args = null, TextWriter? output = null, TextWriter? error = null)
        {
            try
            {
                _error = error;
                var parseResult = Parser.Parse(args ?? Environment.GetCommandLineArgs().Skip(1).ToArray());
                return await parseResult.InvokeAsync(ConsoleHelper.CreateOrDefault(output, error));
            }
            catch (Exception ex)
            {
                if (ExceptionHandler != null)
                    return ExceptionHandler(ex);
                ExceptionDispatchInfo.Capture(ex).Throw();
                return 1;
            }
        }

        private static Dictionary<object, object> _bindings = new Dictionary<object, object>();

        /// <summary>
        /// The library using attributes on methods, properies, fields and parameters to create CommandLine parser commands, options and arguments. 
        /// This method returns corresponding entity info (method, propery, field ot parameter) binded to specified CommandLine object.
        /// </summary>
        /// <param name="commandLineObject">One of CommandLine parser objects: <see cref="Command"/>, <see cref="Option"/> or <see cref="Argument"/>.</param>
        /// <returns>The entity binded to CommandLine parser object <see cref="MethodInfo"/>, <see cref="PropertyInfo"/>, <see cref="FieldInfo"/> or <see cref="ParameterInfo"/>. Returns <code>null</code> if binding not found.</returns>
        public static object? GetBinding(object commandLineObject)
        {
            if (_bindings.TryGetValue(commandLineObject, out var binding))
                return binding;
            else
                return null;
        }

        /// <summary>
        /// The library using attributes on methods, properies, fields and parameters to create CommandLine parser commands, options and arguments. 
        /// This method returns <see cref="ICustomAttributeProvider"/> for corresponding entity (method, propery, field ot parameter) binded to specified CommandLine object.
        /// Return binding CommandLine parser object to 
        /// </summary>
        /// <param name="commandLineObject">One of CommandLine parser objects: <see cref="Command"/>, <see cref="Option"/> or <see cref="Argument"/>.</param>
        /// <returns>The entity binded to CommandLine parser object <see cref="MethodInfo"/>, <see cref="PropertyInfo"/>, <see cref="FieldInfo"/> or <see cref="ParameterInfo"/>. Returns <code>null</code> if binding not found.</returns>
        public static ICustomAttributeProvider? GetBindingCustomAttributeProvider(object commandLineObject)
        {
            return GetBinding(commandLineObject) as ICustomAttributeProvider;
        }

        /// <summary>
        /// Static constructor, initializes commands hierarchy from attributes.
        /// </summary>
        /// <exception cref="AttributeUsageException">Attribute usage error detected.</exception>
        static CLI()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                if (ExceptionHandler != null && args.ExceptionObject is Exception ex)
                    Environment.ExitCode = ExceptionHandler.Invoke(ex);
            };

            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            Assembly assembly = Assembly.GetEntryAssembly() ?? executingAssembly;

            BindingFlags bindingFlags = BindingFlags.Public |
                                 BindingFlags.NonPublic |
                                 BindingFlags.Static |
                                 BindingFlags.Instance |
                                 BindingFlags.DeclaredOnly;

            var commandMethods = GetCommandMethods(assembly, bindingFlags);

            // test project calling CLI.Run() directly and it's commands are in calling assembly
            if (commandMethods.Count == 0)
            {
                var callingAssembly = new StackTrace(1, false).GetFrames().Select(f => f.GetMethod()?.Module?.Assembly).FirstOrDefault(a => a != null && a != executingAssembly);
                if (callingAssembly != null)
                {
                    assembly = callingAssembly;
                    commandMethods = GetCommandMethods(callingAssembly, bindingFlags);
                }
            }

            if (commandMethods.Count == 0)
                throw new AttributeUsageException("The CLI program must declare at least one method with [Command] or [RootCommand] attribute, see documentation https://github.com/mikepal2/snap-cli/blob/main/README.md");

            // create root command
            var globalDescriptors = GetGlobalDescriptors(assembly, bindingFlags);
            RootCommand = CreateRootCommand(assembly, globalDescriptors, commandMethods, out var rootMethod);
            if (rootMethod != null)
                _bindings.Add(RootCommand, rootMethod.Method);

            // add commands without handler methods, i.e. those declared with [Command] on class level

            var parentCommands = globalDescriptors.Where(d => d.Kind == DescriptorAttribute.DescKind.Command)
                .Select(desc => CreateAndAddCommand(RootCommand, desc.Name!, desc))
                .ToArray();

            // add properties and fields described with [Option]

            var globalOptionsInitializersList = new List<Action<InvocationContext>>();

            foreach (var (prop, desc) in assembly.GetTypes()
                .SelectMany(t => t.GetProperties(bindingFlags))
                .Where(x => x.IsDefined(typeof(DescriptorAttribute)))
                .Select(x => new { prop = x, desc = GetCustomAttribute<PropertyInfo, DescriptorAttribute>(x) })
                .Where(x => x.desc != null && x.desc.Kind == DescriptorAttribute.DescKind.Option)
                .Select(x => (x.prop, x.desc!)))
            {
                if (!prop.CanWrite)
                    throw new AttributeUsageException($"Property {prop.Name} declared as [Option] must be writable");
                if (!prop.SetMethod?.IsStatic == null)
                    throw new AttributeUsageException($"Property {prop.Name} declared as [Option] must be static");
                var opt = CreateOption(desc, prop.Name, prop.PropertyType, () => prop.GetValue(null));
                RootCommand.AddGlobalOption(opt);
                globalOptionsInitializersList.Add((ctx) => prop.SetValue(null, ctx.ParseResult.GetValueForOption(opt)));
                _bindings.Add(opt, prop);
            }

            foreach (var (field, desc) in assembly.GetTypes()
                .SelectMany(t => t.GetFields(bindingFlags))
                .Select(x => new { field = x, desc = GetCustomAttribute<FieldInfo,DescriptorAttribute>(x) })
                .Where(x => x.desc != null && x.desc.Kind == DescriptorAttribute.DescKind.Option)
                .Select(x => (x.field, x.desc!)))
            {
                if (field.IsInitOnly)
                    throw new AttributeUsageException($"Field {field.Name} declared as [Option] must be writable");
                if (!field.IsStatic)
                    throw new AttributeUsageException($"Field {field.Name} declared as [Option] must be static");
                var opt = CreateOption(desc, field.Name, field.FieldType, () => field.GetValue(null));
                RootCommand.AddGlobalOption(opt);
                globalOptionsInitializersList.Add((ctx) => field.SetValue(null, ctx.ParseResult.GetValueForOption(opt)));
                _bindings.Add(opt, field);
            }

            var globalOptionsInitializers = globalOptionsInitializersList.ToArray();

            // add method handlers

            if (rootMethod != null)
                AddCommandHandler(RootCommand, rootMethod, globalOptionsInitializers);

            foreach (var m in commandMethods
                .Where(m => m.Desc.Kind == DescriptorAttribute.DescKind.Command && m != rootMethod)
                .OrderBy(m => m.CommandName.Length)) // sort by name length to ensure parent commands created before subcommands
            {
                var command = CreateAndAddCommand(RootCommand, m.CommandName, m.Desc);
                AddCommandHandler(command, m, globalOptionsInitializers);
            }

            // validate parent commands

            foreach (var command in parentCommands)
                if (command.Subcommands.Count == 0 && command.Handler == null && command.IsHidden == false)
                    throw new AttributeUsageException($"Command '{command.Name}' has no subcommands nor handler methods");

            var builder = new CommandLineBuilder(RootCommand);

            // call [Startup] methods

            var startupMethods = GetCallbackMethodsByAttribute<StartupAttribute>(assembly, bindingFlags, paramTypes: new Type[] { typeof(CommandLineBuilder) }, paramsAreOptional: true);

            bool useDefaults = true;
            if (startupMethods.Any())
            {
                var _params = new object[] { builder };
                foreach (var method in startupMethods)
                {
                    try
                    {
                        if (method.GetParameters().Length > 0)
                        {
                            useDefaults = false; // this startup method is responsible for builder configuration
                            method.Invoke(null, new object[] { builder });
                        }
                        else
                        {
                            method.Invoke(null, null);
                        }
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException != null)
                    {
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    }
                }
            }

            if (useDefaults)
            {
                // use all from .UseDefaults() except .UseExceptionHandler()
                builder.UseVersionOption()
                       .UseHelp()
                       .UseEnvironmentVariableDirective()
                       .UseParseDirective()
                       .UseSuggestDirective()
                       .RegisterWithDotnetSuggest()
                       .UseTypoCorrections()
                       .UseParseErrorReporting()
                       .CancelOnProcessTermination();
            }

            Parser = builder.Build();
        }

        // find [RootCommand] and [Command] attributes declared on class
        private static List<DescriptorAttribute> GetGlobalDescriptors(Assembly assembly, BindingFlags bindingFlags)
        {
            return assembly.GetTypes().SelectMany(t => GetCustomAttributes<Type, DescriptorAttribute>(t)).ToList();
        }

        // find methods declared with [RootCommand] or [Command] attributes
        private static List<CommandMethodDesc> GetCommandMethods(Assembly assembly, BindingFlags bindingFlags)
        {
            return assembly.GetTypes()
                                  .SelectMany(t => t.GetMethods(bindingFlags))
                                  .Select(m =>
                                  {
                                      if (GetCustomAttributes<MethodInfo, DescriptorAttribute>(m).Count() > 1)
                                          throw new AttributeUsageException($"Method {m.Name} has multiple [Command] attributes declared");
                                      return new { method = m, desc = GetCustomAttribute<MethodInfo, DescriptorAttribute>(m) };
                                  })
                                  .Where(m => m.desc != null)
                                  .Select(m => new CommandMethodDesc(m.method, m.desc!))
                                  .ToList();
        }

        // Helper method to provide meaningful diagnostics on attribute instantiation.
        private static AttrType? GetCustomAttribute<SourceType, AttrType>(SourceType source) 
            where SourceType : ICustomAttributeProvider 
            where AttrType : Attribute
        {
            try
            {
                return source.GetCustomAttributes(typeof(AttrType), false).OfType<AttrType>().FirstOrDefault();
            }
            catch (Exception ex) 
            {
                throw new Exception($"Failed to process attributes for {source}", ex);
            }
        }

        // Helper method to provide meaningful diagnostics on attribute instantiation.
        private static IEnumerable<AttrType> GetCustomAttributes<SourceType, AttrType>(SourceType source)
            where SourceType : ICustomAttributeProvider
            where AttrType : Attribute
        {
            try
            {
                return source.GetCustomAttributes(typeof(AttrType), false).OfType<AttrType>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to process attributes for {source}", ex);
            }
        }

        // find methods declared with attribute
        private static MethodInfo[] GetCallbackMethodsByAttribute<T>(Assembly assembly, BindingFlags bindingFlags, Type[] paramTypes, bool paramsAreOptional = false) where T : Attribute 
        {
            var methods = assembly.GetTypes()
                .SelectMany(t => t.GetMethods(bindingFlags))
                .Where(m => GetCustomAttribute<MethodInfo,T>(m) != null)
                .ToArray();

            foreach (var method in methods)
            {
                var attributeName = typeof(T).Name.Replace("Attrubute", "");
                if (!method.IsStatic)
                    throw new AttributeUsageException($"Method {method.Name} declared as [{attributeName}] must be static");

                if (!ValidateParams(method, paramTypes, paramsAreOptional))
                {
                    var methodDefinition = $"public static void {method.Name}(";
                    methodDefinition += string.Join(", ", paramTypes.Select(t => (paramsAreOptional ? "[" : "") + t.Name));
                    methodDefinition += paramsAreOptional ? new string(']', paramTypes.Length) : "";
                    methodDefinition += " { ... }";
                    throw new AttributeUsageException($"Method {method.Name} declared as [{attributeName}] must be of type: {methodDefinition}");
                }
            }

            return methods;

            static bool ValidateParams(MethodInfo method, Type[] paramTypes, bool paramsAreOptional)
            {
                var _params = method.GetParameters();
                if (!paramsAreOptional && _params.Length != paramTypes.Length)
                    return false;
                if (_params.Length > paramTypes.Length)
                    return false;
                for (int i = 0; i < _params.Length; i++)
                    if (_params[i].ParameterType != paramTypes[i])
                        return false;
                return true;
            }
        }
        private static RootCommand CreateRootCommand(Assembly assembly, List<DescriptorAttribute> globalDescriptors, List<CommandMethodDesc> commandMethods, out CommandMethodDesc? rootMethod)
        {
            var globalRootDescriptors = globalDescriptors.Where(d => d.Kind == DescriptorAttribute.DescKind.RootCommand).ToList();
            var rootMethods = commandMethods.Where(m => m.Desc.Kind == DescriptorAttribute.DescKind.RootCommand).ToList();
            var rootDescriptorsCount = globalRootDescriptors.Count + rootMethods.Count;

            rootMethod = null;
            DescriptorAttribute? rootDescriptor = null;

            if (rootDescriptorsCount > 1)
                throw new AttributeUsageException($"Only one [RootCommand] attribute may be declared, found {rootDescriptorsCount}");

            if (globalRootDescriptors.Any())
                rootDescriptor = globalDescriptors.First();
            else if (rootMethods.Any())
                rootMethod = rootMethods.First();
            else if (commandMethods.Count == 1 && string.IsNullOrEmpty(commandMethods.First().Desc.Name))
                rootMethod = commandMethods.First();

            var rootCommandDescription = rootMethod?.Desc.Description ??
                rootDescriptor?.Description ??
                GetCustomAttribute<Assembly,AssemblyDescriptionAttribute>(assembly)?.Description ??
                "";

            return new RootCommand(rootCommandDescription);
        }

        private static Command CreateAndAddCommand(RootCommand rootCommand, string name, DescriptorAttribute desc)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Valid command name is required", nameof(name));

            if (desc.Kind != DescriptorAttribute.DescKind.Command)
                throw new ArgumentException($"Unexpected descriptor type {desc.Kind} for the command");

            var subcommandNames = name.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Command parentCommand = rootCommand;
            Command? command = null;
            bool created = false;
            foreach (var (subName, i) in subcommandNames.Select((n, i) => (n, i)))
            {
                command = parentCommand.Subcommands.FirstOrDefault(c => string.Compare(subName, c.Name, StringComparison.Ordinal) == 0 || c.HasAlias(subName));
                if (command == null)
                {
                    bool isLast = (i == subcommandNames.Length - 1);
                    command = new Command(subName, isLast ? desc.Description : null);
                    created = true;
                    parentCommand.Add(command);
                }
                parentCommand = command;
            }
            if (command == null) // this is to satisfy compiler, in fact command cannot be null here because we already checked that name is not empty and we either found existing or created new command
                throw new InvalidOperationException(); 
            if (!created)
                throw new AttributeUsageException($"Command '{name}' has multiple [Command] definitions");
            if (desc.Aliases != null)
                foreach (var alias in desc.Aliases)
                    command.AddAlias(alias);
            command.IsHidden = desc.IsHidden;
            return command;
        }

        private static readonly Type[] SupportedReturnTypes = new[] { typeof(void), typeof(int), typeof(Task<int>), typeof(Task)
#if NETCOREAPP2_0_OR_GREATER
            , typeof(ValueTask<int>), typeof(ValueTask)
#endif
        };
        private static void AddCommandHandler(Command command, CommandMethodDesc methodDescriptor, Action<InvocationContext>[] globalOptionsInitializers)
        {
            if (command.Handler != null)
                throw new AttributeUsageException($"Command '{command.Name}' has multiple handler methods");

            var method = methodDescriptor.Method;

            if (!method.IsStatic)
                throw new AttributeUsageException($"Method {method.Name} declared as [Command] must be static");

            if (!SupportedReturnTypes.Any(t => t.IsAssignableFrom(method.ReturnType)))
                throw new AttributeUsageException($"Method {method.Name} return type must be one of {string.Join(", ", SupportedReturnTypes.Select(t => GetFullTypeName(t)))}.");

            var paramInfo = new List<Symbol>();

            foreach (var param in method.GetParameters())
            {
                var info = GetCustomAttribute<ParameterInfo, DescriptorAttribute>(param) ?? new OptionAttribute();
                Func<object?>? getDefaultValue = null;
                if (param.HasDefaultValue)
                    getDefaultValue = () => param.DefaultValue;
                switch (info.Kind)
                {
                    case DescriptorAttribute.DescKind.Option:
                        var option = CreateOption(info, param.Name, param.ParameterType, getDefaultValue);
                        command.AddOption(option);
                        paramInfo.Add(option);
                        _bindings.Add(option, param);
                        break;
                    case DescriptorAttribute.DescKind.Argument:
                        var argument = CreateArgument(info, param.Name, param.ParameterType, getDefaultValue);
                        command.AddArgument(argument);
                        paramInfo.Add(argument);
                        _bindings.Add(argument, param);
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }

            command.SetHandler(async (ctx) =>
            {
                ParseResult = ctx.ParseResult;
                CancellationToken = ctx.GetCancellationToken();

                foreach (var initializer in globalOptionsInitializers)
                    initializer.Invoke(ctx);

                var methodParams = paramInfo.Select(param =>
                {
                    switch (param)
                    {
                        case Option opt:
                            return ctx.ParseResult.GetValueForOption(opt);
                        case Argument arg:
                            return ctx.ParseResult.GetValueForArgument(arg);
                        default:
                            throw new InvalidOperationException();
                    }
                }).ToArray();

                if (methodDescriptor.Desc.MutuallyExclusuveOptionsArguments != null)
                    ParseResult.ValidateMutuallyExclusiveOptionsArguments(methodDescriptor.Desc.MutuallyExclusuveOptionsArguments);

                try
                {
                    var beforeCommandEventArguments = new BeforeCommandEventArguments(ctx.ParseResult);
                    BeforeCommand?.Invoke(beforeCommandEventArguments);

                    var awaitable = method.Invoke(null, methodParams)!;

                    if (awaitable == null)
                    {
                        ctx.ExitCode = 0;
                    }
                    else
                    {
                        switch (awaitable)
                        {
                            case Task<int> t:
                                ctx.ExitCode = await t;
                                break;
                            case Task t:
                                await t;
                                ctx.ExitCode = 0;
                                break;
#if NETCOREAPP2_0_OR_GREATER
                            case ValueTask<int> t:
                                ctx.ExitCode = await t;
                                break;
                            case ValueTask t:
                                await t;
                                ctx.ExitCode = 0;
                                break;
#endif
                            case int i:
                                ctx.ExitCode = i;
                                break;
                            default:
                                // should not be here because of SupportedReturnTypes check above
                                throw new InvalidOperationException();
                        }
                    }

                    var afterCommandEventArguments = new AfterCommandEventArguments(ctx.ParseResult, ctx.ExitCode);
                    AfterCommand?.Invoke(afterCommandEventArguments);

                    // AfterCommand event handler(s) may change exit code
                    ctx.ExitCode = afterCommandEventArguments.ExitCode;
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }
            });
        }

        private static string GetFullTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                string genericArguments = type.GetGenericArguments()
                                    .Select(x => x.Name)
                                    .Aggregate((x1, x2) => $"{x1}, {x2}");
                return $"{type.Name.Substring(0, type.Name.IndexOf("`"))}"
                     + $"<{genericArguments}>";
            }
            return type.Name;
        }
        private static Option CreateOption(DescriptorAttribute info, string? memberName, Type valueType, Func<object?>? getDefaultValue = null)
        {
            var genericType = typeof(Option<>).MakeGenericType(new[] { valueType });
            var name = info.Name ?? memberName?.ToKebabCase() ?? throw new NotSupportedException($"Option name cannot be deduced from parameter [{info}], specify name explicitly");
            name = AddPrefix(name);
            Option instance = (Option)Activator.CreateInstance(genericType, new[] { name, info.Description })!;
            if (info.Arity.HasValue)
                instance.Arity = info.Arity.Value;
            if (info.HelpName != null)
                instance.ArgumentHelpName = info.HelpName;
            instance.IsHidden = info.IsHidden;
            if (info.Aliases != null)
                foreach (var alias in info.Aliases)
                    instance.AddAlias(AddPrefix(alias));
            if (info.IsRequired == false && getDefaultValue != null)
                instance.SetDefaultValueFactory(getDefaultValue);
            else
                instance.IsRequired = true;
            return instance;

            static string AddPrefix(string name)
            {
                if (name.StartsWith("-"))
                    return name;
                if (name.Length == 1)
                    return "-" + name;
                return "--" + name;
            }
        }

        private static Argument CreateArgument(DescriptorAttribute info, string? memberName, Type valueType, Func<object?>? getDefaultValue = null)
        {
            var genericType = typeof(Argument<>).MakeGenericType(new[] { valueType });
            var name = info.Name ?? memberName?.ToKebabCase() ?? throw new NotSupportedException($"Argument name cannot be deduced from parameter [{info}], specify name explicitly");
            Argument instance = (Argument)Activator.CreateInstance(genericType, new[] { name, info.Description })!;
            if (info.Arity.HasValue)
                instance.Arity = info.Arity.Value;
            if (info.HelpName != null)
                instance.HelpName = info.HelpName;
            instance.IsHidden = info.IsHidden;
            if (getDefaultValue != null)
                instance.SetDefaultValueFactory(getDefaultValue);
            return instance;
        }

        private static string ToKebabCase(this string str)
        {
            return Regex.Replace(str, @"([a-z])([A-Z][a-z])", "$1-$2").ToLower();
        }
    }
}
