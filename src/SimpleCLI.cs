using System.CommandLine.Invocation;
using System.Reflection;

namespace System.CommandLine.SimpleCLI
{

// CLIDescriptorAttribute is only used as base for other CLI attributes and should not be used by clients,
// therefore we don't generate XML documentation for this class
#pragma warning disable 1591

    [AttributeUsage(AttributeTargets.All)]
    public class CLIDescriptorAttribute : Attribute
    {
        public enum DescKind
        {
            Program,
            RootCommand,
            Command,
            Argument,
            Option
        }

        public DescKind Kind { get; }
        public string? Name { get; }
        public string? HelpName { get; }
        public string[]? Aliases { get; }
        public string? Description { get; }
        public bool IsHidden { get; }
        public bool IsRequired { get; }
        public ArgumentArity? Arity { get; }

        // only allow to use this attribute in subclasses
        private CLIDescriptorAttribute() { }

        protected CLIDescriptorAttribute(DescKind kind, string? name = null, string? helpName = null, string[]? aliases = null, string? description = null, bool hidden = false, bool required = false)
        {
            Kind = kind;
            Name = name;
            Description = description;
            IsHidden = hidden;
            IsRequired = required;
            HelpName = helpName;
            Aliases = aliases;
        }

        protected CLIDescriptorAttribute(DescKind kind, int arityMin, int arityMax, string? name = null, string? helpName = null, string[]? aliases = null, string? description = null, bool hidden = false, bool required = false)
            : this(kind, name, helpName, aliases, description, hidden, required)
        {
            Arity = new ArgumentArity(arityMin, arityMax);
        }

        public override string ToString()
        {
            return $"{Kind}: name:{Name ?? HelpName ?? Aliases?.FirstOrDefault()}, desc:{Description}";
        }
    }
#pragma warning restore 1591

    /// <summary>
    /// Declares <b>option</b> definition for CLI command.
    /// <remark>Can be also used on static fields and properies to declare global options.</remark>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
    public class CLIOptionAttribute : CLIDescriptorAttribute
    {
        /// <summary>
        /// Declares CLI <b>option</b> definition.
        /// </summary>
        /// <param name="name">Option name</param>
        /// <param name="helpName">Option value name</param>
        /// <param name="aliases">Aliases for the option</param>
        /// <param name="description">Option description</param>
        /// <param name="hidden">Hidden options are not shown in help but still can be used</param>
        /// <param name="required">Required options must be always specified in command line</param>
        public CLIOptionAttribute(string? name = null, string? helpName = null, string[]? aliases = null, string? description = null, bool hidden = false, bool required = false)
            : base(DescKind.Option, name, helpName, aliases, description, hidden, required) { }

        /// <summary>
        /// Declares CLI <b>option</b> definition.
        /// </summary>
        /// <param name="arityMin">Minimum number of values an option receives</param>
        /// <param name="arityMax">Maximum number of values an option receives</param>
        /// <param name="name">Option name</param>
        /// <param name="helpName">Option value name</param>
        /// <param name="aliases">Aliases for the option</param>
        /// <param name="description">Option description</param>
        /// <param name="hidden">Hidden options are not shown in help but still can be used</param>
        /// <param name="required">Required options must be always specified in command line</param>
        public CLIOptionAttribute(int arityMin, int arityMax, string? name = null, string? helpName = null, string[]? aliases = null, string? description = null, bool hidden = false, bool required = false)
            : base(DescKind.Option, arityMin, arityMax, name, helpName, aliases, description, hidden, required) { }
    }

    /// <summary>
    /// Declares <b>argument</b> definition for CLI command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class CLIArgumentAttribute : CLIDescriptorAttribute
    {
        /// <summary>
        /// Declares <b>argument</b> definition for CLI command.
        /// </summary>
        /// <param name="name">Argument name</param>
        /// <param name="helpName">Argument name in help</param>
        /// <param name="description">Argument description</param>
        /// <param name="hidden">Hidden arguments are not shown in help but still can be used</param>
        public CLIArgumentAttribute(string? name = null, string? helpName = null, string? description = null, bool hidden = false)
            : base(DescKind.Argument, name, helpName, aliases: null, description, hidden) { }

        /// <summary>
        /// Declares <b>argument</b> definition for CLI command.
        /// </summary>
        /// <param name="arityMin">Minimum number of values an argument receives</param>
        /// <param name="arityMax">Maximum number of values an argument receives</param>
        /// <param name="name">Argument name</param>
        /// <param name="helpName">Argument name in help</param>
        /// <param name="description">Argument description</param>
        /// <param name="hidden">Hidden arguments are not shown in help but still can be used</param>
        public CLIArgumentAttribute(int arityMin, int arityMax, string? name = null, string? helpName = null, string? description = null, bool hidden = false)
        : base(DescKind.Argument, arityMin, arityMax, name, helpName, aliases: null, description, hidden) { }
    }

    /// <summary>
    /// Declares handler for <see cref="RootCommand"/>, i.e. command that executed when no subcommands are present on the command line. Only one method may be declared with this attribute.
    /// <remarks>If program has only one method declared with <see cref="CLICommandAttribute"/> and command name not explicitly specified in <c>name</c> attribute, this command is automatically treated as root command.</remarks>
    /// </summary>
    /// <param name="description">Root command description, also serving as programs general description when help is shown.</param>
    [AttributeUsage(AttributeTargets.Method)]
    public class CLIRootCommandAttribute(string? description = null)
        : CLIDescriptorAttribute(DescKind.RootCommand, description: description)  { }

    /// <summary>
    /// Declares handler for CLI <see cref="Command"/>. 
    /// </summary>
    /// <param name="name">Command name</param>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>If name contains spaces, it describes subcommand - for example "list orders" is subcommand <b>orders</b> of <b>list</b> command.</description></item>
    /// <item><description>If name not specified and program has only one method declared with <see cref="CLICommandAttribute"/> and command name not explicitly specified in its <c>name</c> parameter, this command is automatically treated as <see cref="RootCommand"/>.</description></item>
    /// <item><description>If name not specified, method name converted to lower case is used as command name. For example method <c>Hello()</c> will handle <c>hello</c> command.</description></item>
    /// <item><description>If method name is used and it contains underscore <c>_</c> char, it describes subcommand - for example "list_orders()" method is subcommand <b>orders</b> of <b>list</b> command.</description></item>
    /// </list>  
    /// </remarks>
    /// <param name="aliases">Command aliases</param>
    /// <param name="description">Command description</param>
    /// <param name="hidden">Hidden commands are not shown in help but still can be used</param>
    [AttributeUsage(AttributeTargets.Method)]
    public class CLICommandAttribute(string? name = null, string[]? aliases = null, string? description = null, bool hidden = false)
        : CLIDescriptorAttribute(DescKind.Command, name: name, aliases: aliases, description: description, hidden: hidden) { }

    /// <summary>
    /// Declares command without handler.
    /// <remarks>Can be used to declare parent command that always expects subcommand to be invoked and has no it's own method handler</remarks>
    /// </summary>
    /// <param name="name">Command name</param>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>If name contains spaces, it describes subcommand - for example "list orders" is subcommand <b>orders</b> of <b>list</b> command.</description></item>
    /// <item><description>If name not specified and program has multiple commands then method name is used as command name.</description></item>
    /// <item><description>If method name is used and it contains underscore <c>_</c> char, it describes subcommand. For example, "list_orders()" method is subcommand <b>orders</b> of <b>list</b> command.</description></item>
    /// </list>  
    /// </remarks>
    /// <param name="aliases">Command aliases</param>
    /// <param name="description">Command description</param>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class CLIParentCommandAttribute(string name, string description, string[]? aliases = null)
    : CLIDescriptorAttribute(DescKind.Command, name: name, aliases: aliases, description: description) { }

    /// <summary>
    /// Provides program description to me show on main help.
    /// <remarks>
    /// If there is root command method declared with <see cref="CLIRootCommandAttribute"/> then root command description will be shown on main help. 
    /// <para>You may also use <see cref="AssemblyDescriptionAttribute"/> to provide program description</para>
    /// </remarks>
    /// </summary>
    /// <param name="description">Program description</param>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class CLIProgramAttribute(string description) : Attribute
    {
        /// <summary>
        /// Program description to me show on main help.
        /// </summary>
        public string Description { get; } = description;
    }

    /// <summary>
    /// Command Line Interface class implementation. Provides simple interface to create CLI programs using attributes declarations.
    /// </summary>
    public static class CLI
    {
        /// <summary>
        /// Helper method to run CLI application. Should be called from program Main() entry point.
        /// </summary>
        /// <param name="args">Command line arguments passed from Main()</param>
        /// <param name="console">Optional <see cref="IConsole"></see> interface to pass to the commands.</param>
        /// <returns></returns>
        public static int Run(string[]? args = null, IConsole? console = null) => BuildCommands().Invoke(args ?? Environment.GetCommandLineArgs()[1..], console);

        /// <summary>
        /// Helper asynchronous method to run CLI application. Should be called from program async Main() entry point.
        /// </summary>
        /// <param name="args">Command line arguments passed from Main()</param>
        /// <param name="console">Optional <see cref="IConsole"></see> interface to pass to the commands.</param>
        /// <returns></returns>
        public static async Task<int> RunAsync(string[]? args = null, IConsole? console = null) => await BuildCommands().InvokeAsync(args ?? Environment.GetCommandLineArgs()[1..], console);

        /// <summary>
        /// Provides access to commands hierarchy and their options and arguments.
        /// </summary>
        public static RootCommand RootCommand => rootCommand ?? throw new InvalidOperationException($"{nameof(BuildCommands)}() must be invoked before accessing {nameof(RootCommand)} property");
        private static RootCommand? rootCommand = null;

        /// <summary>
        /// Provides access to currently executing command definition.
        /// </summary>
        public static Command CurrentCommand { 
            get => _currentCommand ?? throw new InvalidOperationException($"Cannot access {nameof(CurrentCommand)} from outside of CLI command handler method"); 
            private set => _currentCommand = value; 
        }
        private static Command? _currentCommand = null;

        /// <summary>
        /// Current command invocation context provides access to parsed command line, CancellationToken, ExitCode and other properties.
        /// </summary>
        public static InvocationContext CurrentContext => _currentContext ?? throw new InvalidOperationException($"Cannot access {nameof(CurrentContext)} from outside of command handler method");
        private static InvocationContext? _currentContext = null;

        private class CommandMethodDesc
        {
            public string CommandName;
            public CLIDescriptorAttribute Desc;
            public MethodInfo Method;

            public CommandMethodDesc(MethodInfo method, CLIDescriptorAttribute desc)
            {
                Method = method;
                Desc = desc;
                CommandName = desc.Name ?? method.Name.ToLower().Replace('_', ' ');
            }
        }

        /// <summary>
        /// Builds commands hierarchy based on attributes.
        /// </summary>
        /// <returns>Returns <see cref="RootCommand"></see></returns>
        /// <exception cref="InvalidOperationException">Commands hierarchy already built or there are attributes usage errors detected.</exception>
        public static RootCommand BuildCommands()
        {
            if (rootCommand != null)
                throw new InvalidOperationException("BuildCommands() was already invoked and commands hierarchy built");

            Assembly assembly = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly());

            BindingFlags bindingFlags = BindingFlags.Public |
                                 BindingFlags.NonPublic |
                                 BindingFlags.Static |
                                 BindingFlags.Instance |
                                 BindingFlags.DeclaredOnly;
            
            var methods = GetCommandMethods(assembly, bindingFlags);

            // create root command

            CommandMethodDesc? rootMethod = GetRootCommandMethod(methods);
            rootCommand = new(rootMethod?.Desc.Description ?? GetAssemblyDescription(assembly) ?? "");

            // add parent commands described with [assembly:CLIParentCommand(...)] 

            var parentCommands = assembly.GetCustomAttributes<CLIParentCommandAttribute>()
                .Select(desc => CreateAndAddCommand(rootCommand, desc.Name!, desc))
                .ToArray();

            // add properties and fields described with [CLIOption]

            var globalOptionsInitializersList = new List<Action<InvocationContext>>();

            foreach (var (prop, desc) in assembly.GetTypes()
                .SelectMany(t => t.GetProperties(bindingFlags))
                .Select(x => new { prop = x, desc = x.GetCustomAttribute<CLIDescriptorAttribute>() })
                .Where(x => x.desc != null && x.desc.Kind == CLIDescriptorAttribute.DescKind.Option)
                .Select(x => (x.prop, x.desc!)))
            {
                if (!prop.CanWrite)
                    throw new InvalidOperationException($"Property {prop.Name} marked as [CLIOption] must be writable");
                if (!prop.SetMethod?.IsStatic == null)
                    throw new InvalidOperationException($"Property {prop.Name} marked as [CLIOption] must be static");
                var opt = CreateOption(desc, prop.Name, prop.PropertyType, () => prop.GetValue(null));
                rootCommand.AddGlobalOption(opt);
                globalOptionsInitializersList.Add((ctx) => prop.SetValue(null, ctx.ParseResult.GetValueForOption(opt)));
            }

            foreach (var (field, desc) in assembly.GetTypes()
                .SelectMany(t => t.GetFields(bindingFlags))
                .Select(x => new { field = x, desc = x.GetCustomAttribute<CLIDescriptorAttribute>() })
                .Where(x => x.desc != null && x.desc.Kind == CLIDescriptorAttribute.DescKind.Option)
                .Select(x => (x.field, x.desc!)))
            {
                if (field.IsInitOnly)
                    throw new InvalidOperationException($"Field {field.Name} marked as [CLIOption] must be writable");
                if (!field.IsStatic)
                    throw new InvalidOperationException($"Field {field.Name} marked as [CLIOption] must be static");
                var opt = CreateOption(desc, field.Name, field.FieldType, () => field.GetValue(null));
                rootCommand.AddGlobalOption(opt);
                globalOptionsInitializersList.Add((ctx) => field.SetValue(null, ctx.ParseResult.GetValueForOption(opt)));
            }

            var globalOptionsInitializers = globalOptionsInitializersList.ToArray();

            // add method handlers

            if (rootMethod != null)
                AddCommandHandler(rootCommand, rootMethod.Method, globalOptionsInitializers);

            foreach (var m in methods
                .Where(m => m.Desc.Kind == CLIDescriptorAttribute.DescKind.Command && m != rootMethod)
                .OrderBy(m => m.CommandName.Length)) // sort by name length to ensure parent commands created before subcommands
            {
                var command = CreateAndAddCommand(rootCommand, m.CommandName, m.Desc);
                AddCommandHandler(command, m.Method, globalOptionsInitializers);
            }

            // validate parent commands

            foreach (var command in parentCommands)
                if (command.Subcommands.Count == 0 && command.Handler == null && command.IsHidden == false)
                    throw new InvalidOperationException($"Command '{command.Name}' has no subcommands nor handler methods");

            return rootCommand;
        }

        private static List<CommandMethodDesc> GetCommandMethods(Assembly assembly, BindingFlags bindingFlags)
        {
            // find method tagged with CLIRootCommand or CLICommand attributes

            return assembly.GetTypes()
                                  .SelectMany(t => t.GetMethods(bindingFlags))
                                  .Select(m => new { method = m, desc = m.GetCustomAttribute<CLIDescriptorAttribute>() })
                                  .Where(m => m.desc != null)
                                  .Select(m => new CommandMethodDesc(m.method, m.desc!))
                                  .ToList();
        }

        private static CommandMethodDesc? GetRootCommandMethod(List<CommandMethodDesc> methods)
        {
            CommandMethodDesc? rootMethod = null;

            // TODO: add documentation link
            if (methods.Count == 0)
                throw new InvalidOperationException("Cannot find methods with [CLICommand] attribute, see documentation");

            // if we have only one method not named explicitly - use it as root
            if (methods.Count == 1 && string.IsNullOrEmpty(methods[0].Desc.Name))
                return methods[0];

            foreach (var m in methods.Where(m => m.Desc.Kind == CLIDescriptorAttribute.DescKind.RootCommand))
            {
                if (rootMethod != null)
                    throw new InvalidOperationException("Only one method can be marked as [CLIRootCommand]");
                rootMethod = m;
            }

            return rootMethod;
        }

        private static string? GetAssemblyDescription(Assembly assembly)
        {
            return assembly.GetCustomAttribute<CLIProgramAttribute>()?.Description ??
                   assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;
        }

        private static Command CreateAndAddCommand(RootCommand rootCommand, string name, CLIDescriptorAttribute desc)
        {
            var subcommandNames = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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
            if (command == null)
                throw new InvalidOperationException();
            if (!created)
                throw new InvalidOperationException($"Command '{name}' has multiple [CLICommand] definitions");
            if (desc.Aliases != null)
                foreach (var alias in desc.Aliases)
                    command.AddAlias(alias);
            command.IsHidden = desc.IsHidden;
            return command;
        }

        private static Type[] SupportedReturnTypes = [typeof(void), typeof(int), typeof(Task<int>), typeof(Task), typeof(ValueTask<int>), typeof(ValueTask)];
        private static void AddCommandHandler(Command command, MethodInfo method, Action<InvocationContext>[] globalOptionsInitializers)
        {
            if (command.Handler != null)
                throw new InvalidOperationException($"Command '{command.Name}' has multiple handler methods");

            if (!method.IsStatic)
                throw new InvalidOperationException($"Method {method.Name} marked as [CLICommand] must be static");

            // FIXME: generic type name is shown as Task`1 instead of Task<int>
            if (!SupportedReturnTypes.Any(t => t.IsAssignableFrom(method.ReturnType)))
                throw new InvalidOperationException($"Method {method.Name} should return any of {string.Join(",", SupportedReturnTypes.Select(t => t.Name))}");

            List<Symbol> paramInfo = [];

            foreach (var param in method.GetParameters())
            {
                var info = param.GetCustomAttribute<CLIDescriptorAttribute>() ?? new CLIOptionAttribute();
                switch (info.Kind)
                {
                    case CLIDescriptorAttribute.DescKind.Option:
                        var option = CreateOption(info, param.Name, param.ParameterType, param.HasDefaultValue && param.DefaultValue != null ? () => param.DefaultValue : null);
                        command.AddOption(option);
                        paramInfo.Add(option);
                        break;
                    case CLIDescriptorAttribute.DescKind.Argument:
                        var argument = CreateArgument(info, param.Name, param.ParameterType, param.HasDefaultValue && param.DefaultValue != null ? () => param.DefaultValue : null);
                        command.AddArgument(argument);
                        paramInfo.Add(argument);
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }

            command.SetHandler(async (ctx) =>
            {
                _currentCommand = command;
                _currentContext = ctx;

                foreach (var initializer in globalOptionsInitializers)
                    initializer.Invoke(ctx);

                var _params = paramInfo.Select(param =>
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

                var awatable = method.Invoke(null, _params)!;

                if (awatable == null)
                {
                    ctx.ExitCode = 0;
                }
                else
                {
                    switch (awatable)
                    {
                        case Task<int> t:
                            ctx.ExitCode = await t;
                            break;
                        case Task t:
                            await t;
                            ctx.ExitCode = 0;
                            break;
                        case ValueTask<int> t:
                            ctx.ExitCode = await t;
                            break;
                        case ValueTask t:
                            await t;
                            ctx.ExitCode = 0;
                            break;
                        case int i:
                            ctx.ExitCode = i;
                            break;
                        default:
                            // should not be here because of SupportedReturnTypes check above
                            throw new InvalidOperationException();
                    }
                }
            });
        }

        private static Option CreateOption(CLIDescriptorAttribute info, string? memberName, Type valueType, Func<object?>? getDefaultValue = null)
        {
            var genericType = typeof(Option<>).MakeGenericType([valueType]);
            var name = info.Name ?? memberName ?? throw new NotSupportedException($"Option name cannot be deduced from parameter [{info}], specify name explicitly");
            name = AddPrefix(name);
            Option instance = (Option)Activator.CreateInstance(genericType, [name, info.Description])!;
            if (info.Arity.HasValue)
                instance.Arity = info.Arity.Value;
            if (info.HelpName != null)
                instance.ArgumentHelpName = info.HelpName;
            instance.IsHidden = info.IsHidden;
            if (info.Aliases != null)
                foreach (var alias in info.Aliases)
                    instance.AddAlias(AddPrefix(alias));
            instance.IsRequired = info.IsRequired;
            if (getDefaultValue != null && instance.IsRequired == false)
                instance.SetDefaultValueFactory(getDefaultValue);
            return instance;

            static string AddPrefix(string name)
            {
                if (name.StartsWith('-'))
                    return name;
                if (name.Length == 1)
                    return "-" + name;
                return "--" + name;
            }
        }

        private static Argument CreateArgument(CLIDescriptorAttribute info, string? memberName, Type valueType, Func<object?>? getDefaultValue = null)
        {
            var genericType = typeof(Argument<>).MakeGenericType([valueType]);
            var name = info.Name ?? memberName ?? throw new NotSupportedException($"Argument name cannot be deduced from parameter [{info}], specify name explicitly");
            Argument instance = (Argument)Activator.CreateInstance(genericType, [name, info.Description])!;
            if (info.Arity.HasValue)
                instance.Arity = info.Arity.Value;
            if (info.HelpName != null)
                instance.HelpName = info.HelpName;
            instance.IsHidden = info.IsHidden;
            if (getDefaultValue != null)
                instance.SetDefaultValueFactory(getDefaultValue);
            return instance;
        }

    }
}
