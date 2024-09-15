using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Reflection;
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
            CliRootCommand,
            CliCommand,
            CliArgument,
            CliOption
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
        private DescriptorAttribute() { }

        protected DescriptorAttribute(DescKind kind, string? name = null, string? helpName = null, string[]? aliases = null, string? description = null, bool hidden = false, bool required = false)
        {
            Kind = kind;
            Name = name;
            Description = description;
            IsHidden = hidden;
            IsRequired = required;
            HelpName = helpName;
            Aliases = aliases;
        }

        protected DescriptorAttribute(DescKind kind, int arityMin, int arityMax, string? name = null, string? helpName = null, string[]? aliases = null, string? description = null, bool hidden = false, bool required = false)
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
    /// <remarks>
    /// <para>Applies to command handler method arguments.</para>
    /// <para>Can also be used on static fields and properies to declare global options.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// 
    ///     [CliCommand]
    ///     public static void Hello(
    ///         [CliOption(name:"Name", description:"Person's name")]
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
    ///     [CliOption(name:"config", description:"Specifies configuration file path)]
    ///     public static string g_configFile = "config.ini";
    /// 
    /// </code>
    /// </example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
    public class OptionAttribute : DescriptorAttribute
    {
        /// <summary>
        /// Declares CLI <b>option</b> definition.
        /// </summary>
        /// <param name="name">CliOption name</param>
        /// <param name="helpName">CliOption value name</param>
        /// <param name="aliases">Aliases for the option</param>
        /// <param name="description">CliOption description</param>
        /// <param name="hidden">Hidden options are not shown in help but still can be used</param>
        /// <param name="required">Required options must be always specified in command line</param>
        public OptionAttribute(string? name = null, string? helpName = null, string[]? aliases = null, string? description = null, bool hidden = false, bool required = false)
            : base(DescKind.CliOption, name, helpName, aliases, description, hidden, required) { }

        /// <summary>
        /// Declares CLI <b>option</b> definition.
        /// </summary>
        /// <param name="arityMin">Minimum number of values an option receives</param>
        /// <param name="arityMax">Maximum number of values an option receives</param>
        /// <param name="name">CliOption name</param>
        /// <param name="helpName">CliOption value name</param>
        /// <param name="aliases">Aliases for the option</param>
        /// <param name="description">CliOption description</param>
        /// <param name="hidden">Hidden options are not shown in help but still can be used</param>
        /// <param name="required">Required options must be always specified in command line</param>
        public OptionAttribute(int arityMin, int arityMax, string? name = null, string? helpName = null, string[]? aliases = null, string? description = null, bool hidden = false, bool required = false)
            : base(DescKind.CliOption, arityMin, arityMax, name, helpName, aliases, description, hidden, required) { }
    }

    /// <summary>
    /// Declares <b>argument</b> definition for CLI command.
    /// <remarks>
    /// <para>Can be applied to method argument</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// 
    ///     [CliCommand]
    ///     static public void Read(
    ///         [CliArgument(name:"path", description:"Input file path")] 
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
        /// Declares <b>argument</b> definition for CLI command.
        /// </summary>
        /// <param name="name">CliArgument name</param>
        /// <param name="helpName">CliArgument name in help</param>
        /// <param name="description">CliArgument description</param>
        /// <param name="hidden">Hidden arguments are not shown in help but still can be used</param>
        public ArgumentAttribute(string? name = null, string? helpName = null, string? description = null, bool hidden = false)
            : base(DescKind.CliArgument, name, helpName, aliases: null, description, hidden) { }

        /// <summary>
        /// Declares <b>argument</b> definition for CLI command.
        /// </summary>
        /// <param name="arityMin">Minimum number of values an argument receives</param>
        /// <param name="arityMax">Maximum number of values an argument receives</param>
        /// <param name="name">CliArgument name</param>
        /// <param name="helpName">CliArgument name in help</param>
        /// <param name="description">CliArgument description</param>
        /// <param name="hidden">Hidden arguments are not shown in help but still can be used</param>
        public ArgumentAttribute(int arityMin, int arityMax, string? name = null, string? helpName = null, string? description = null, bool hidden = false)
        : base(DescKind.CliArgument, arityMin, arityMax, name, helpName, aliases: null, description, hidden) { }
    }

    /// <summary>
    /// Declares handler for <see cref="CliRootCommand"/>, i.e. command that executed when no subcommands are present on the command line. Only one method may be declared with this attribute.
    /// <remarks>
    /// <para>If program has only one method declared with <see cref="CommandAttribute"/> and command name not explicitly specified in <c>name</c> attribute, this command is automatically treated as root command.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// 
    ///     [CliRootCommand]
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
        /// Declares handler for <see cref="CliRootCommand"/>, i.e. command that executed when no subcommands are present on the command line. Only one method may be declared with this attribute.
        /// </summary>
        /// <param name="description">Root command description, also serving as programs general description when help is shown.</param>

        public RootCommandAttribute(string? description = null) : base(DescKind.CliRootCommand, description: description)
        {
        }
    }

    /// <summary>
    /// Declares handler for CLI <see cref="CliCommand"/>. 
    /// <remarks>
    /// <para>Can be applied to any static public method</para>
    /// <para>If program has only one method declared with <see cref="CommandAttribute"/> and command name not explicitly specified in <code>name</code> parameter, this command is automatically treated as root command.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// 
    ///     [CliCommand]
    ///     static public void Hello() 
    ///     {
    ///       ... 
    ///     }
    ///     
    /// </code></example>
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>If name not specified and program has only one method declared with <see cref="CommandAttribute"/> and command name not explicitly specified in its <c>name</c> parameter, this command is automatically treated as <see cref="CliRootCommand"/>.</description></item>
    /// <item><description>If name not specified, method name converted to lower case is used as command name. For example method <c>Hello()</c> will handle <c>hello</c> command.</description></item>
    /// <item><description>If method name is used and it contains underscore <c>_</c> char, it describes subcommand - for example "list_orders()" method is subcommand <b>orders</b> of <b>list</b> command.</description></item>
    /// <item><description>If name specified and contains spaces, it describes subcommand - for example "list orders" is subcommand <b>orders</b> of <b>list</b> command.</description></item>
    /// </list>  
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method|AttributeTargets.Class, AllowMultiple = true)]
    public class CommandAttribute : DescriptorAttribute {

        /// <summary>
        /// Declares handler for CLI <see cref="CliCommand"/>. 
        /// </summary>
        /// <param name="name">CliCommand name</param>
        /// <param name="aliases">CliCommand aliases</param>
        /// <param name="description">CliCommand description</param>
        /// <param name="hidden">Hidden commands are not shown in help but still can be used</param>
        public CommandAttribute(string? name = null, string[]? aliases = null, string? description = null, bool hidden = false) : base(DescKind.CliCommand, name: name, aliases: aliases, description: description, hidden: hidden)
        {
        }
    }

    /// <summary>
    /// CliCommand Line Interface class implementation. Provides simple interface to create CLI programs using attributes declarations.
    /// </summary>
    public static class CLI
    {
        /// <summary>
        /// Helper method to run CLI application. Should be called from program Main() entry point.
        /// </summary>
        /// <param name="args">CliCommand line arguments passed from Main()</param>
        /// <returns></returns>
        public static int Run(string[]? args = null) => BuildCommands().Invoke(args ?? Environment.GetCommandLineArgs().Skip(1).ToArray());

        /// <summary>
        /// Helper asynchronous method to run CLI application. Should be called from program async Main() entry point.
        /// </summary>
        /// <param name="args">CliCommand line arguments passed from Main()</param>
        /// <returns></returns>
        public static async Task<int> RunAsync(string[]? args = null) => await BuildCommands().InvokeAsync(args ?? Environment.GetCommandLineArgs().Skip(1).ToArray());

        /// <summary>
        /// Provides access to commands hierarchy and their options and arguments.
        /// </summary>
        public static CliRootCommand CliRootCommand => rootCommand ?? throw new InvalidOperationException($"{nameof(BuildCommands)}() must be invoked before accessing {nameof(CliRootCommand)} property");
        private static CliRootCommand? rootCommand = null;

        /// <summary>
        /// Provides access to currently executing command definition.
        /// </summary>
        public static CliCommand CurrentCommand { 
            get => _currentCommand ?? throw new InvalidOperationException($"Cannot access {nameof(CurrentCommand)} from outside of CLI command handler method"); 
            private set => _currentCommand = value; 
        }
        private static CliCommand? _currentCommand = null;

        private class CommandMethodDesc
        {
            public string CommandName;
            public DescriptorAttribute Desc;
            public MethodInfo Method;

            public CommandMethodDesc(MethodInfo method, DescriptorAttribute desc)
            {
                Method = method;
                Desc = desc;
                CommandName = desc.Name ?? method.Name.ToLower().Replace('_', ' ');
            }
        }

        /// <summary>
        /// Builds commands hierarchy based on attributes.
        /// </summary>
        /// <returns>Returns <see cref="CliRootCommand"></see></returns>
        /// <exception cref="InvalidOperationException">Commands hierarchy already built or there are attributes usage errors detected.</exception>
        public static CliConfiguration BuildCommands()
        {
            if (rootCommand != null)
                throw new InvalidOperationException("BuildCommands() was already invoked and commands hierarchy built");

            Assembly assembly = (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly());

            BindingFlags bindingFlags = BindingFlags.Public |
                                 BindingFlags.NonPublic |
                                 BindingFlags.Static |
                                 BindingFlags.Instance |
                                 BindingFlags.DeclaredOnly;

            var globalDescriptors = GetGlobalDescriptors(assembly, bindingFlags);
            var commandMethods = GetCommandMethods(assembly, bindingFlags);

            if (commandMethods.Count == 0)
                throw new InvalidOperationException("The CLI program must declare at least one method with [CliCommand] or [CliRootCommand] attribute, see documentation https://github.com/mikepal2/snap-cli/blob/main/README.md");

            // create root command

            rootCommand = CreateRootCommand(assembly, globalDescriptors, commandMethods, out var rootMethod);

            // add commands without handler methods, i.e. those declared with [CliCommand] on class level

            var parentCommands = globalDescriptors.Where(d => d.Kind == DescriptorAttribute.DescKind.CliCommand)
                .Select(desc => CreateAndAddCommand(rootCommand, desc.Name!, desc))
                .ToArray();

            // add properties and fields described with [CliOption]

            var globalOptionsInitializersList = new List<Action<ParseResult>>();

            foreach (var (prop, desc) in assembly.GetTypes()
                .SelectMany(t => t.GetProperties(bindingFlags))
                .Select(x => new { prop = x, desc = x.GetCustomAttribute<DescriptorAttribute>() })
                .Where(x => x.desc != null && x.desc.Kind == DescriptorAttribute.DescKind.CliOption)
                .Select(x => (x.prop, x.desc!)))
            {
                if (!prop.CanWrite)
                    throw new InvalidOperationException($"Property {prop.Name} declared as [CliOption] must be writable");
                if (!prop.SetMethod?.IsStatic == null)
                    throw new InvalidOperationException($"Property {prop.Name} declared as [CliOption] must be static");
                var opt = CreateOption(desc, prop.Name, prop.PropertyType, () => prop.GetValue(null));
                opt.Recursive = true;
                rootCommand.Add(opt);
                globalOptionsInitializersList.Add((parseResult) => prop.SetValue(null, parseResult.GetValue((dynamic)opt)));
            }

            foreach (var (field, desc) in assembly.GetTypes()
                .SelectMany(t => t.GetFields(bindingFlags))
                .Select(x => new { field = x, desc = x.GetCustomAttribute<DescriptorAttribute>() })
                .Where(x => x.desc != null && x.desc.Kind == DescriptorAttribute.DescKind.CliOption)
                .Select(x => (x.field, x.desc!)))
            {
                if (field.IsInitOnly)
                    throw new InvalidOperationException($"Field {field.Name} declared as [CliOption] must be writable");
                if (!field.IsStatic)
                    throw new InvalidOperationException($"Field {field.Name} declared as [CliOption] must be static");
                var opt = CreateOption(desc, field.Name, field.FieldType, () => field.GetValue(null));
                opt.Recursive = true;
                rootCommand.Add(opt);
                globalOptionsInitializersList.Add((parseResult) => field.SetValue(null, parseResult.GetValue((dynamic)opt)));
            }

            var globalOptionsInitializers = globalOptionsInitializersList.ToArray();

            // add method handlers

            if (rootMethod != null)
                AddCommandHandler(rootCommand, rootMethod.Method, globalOptionsInitializers);

            foreach (var m in commandMethods
                .Where(m => m.Desc.Kind == DescriptorAttribute.DescKind.CliCommand && m != rootMethod)
                .OrderBy(m => m.CommandName.Length)) // sort by name length to ensure parent commands created before subcommands
            {
                var command = CreateAndAddCommand(rootCommand, m.CommandName, m.Desc);
                AddCommandHandler(command, m.Method, globalOptionsInitializers);
            }

            // validate parent commands

            foreach (var command in parentCommands)
                if (command.Subcommands.Count == 0 && command.Action == null && command.Hidden == false)
                    throw new InvalidOperationException($"CliCommand '{command.Name}' has no subcommands nor handler methods");

            var config = new CliConfiguration(rootCommand);

            return config;
        }

        // find [CliRootCommand] and [CliCommand] attributes declared on class
        private static List<DescriptorAttribute> GetGlobalDescriptors(Assembly assembly, BindingFlags bindingFlags)
        {

            return assembly.GetTypes().SelectMany(t => t.GetCustomAttributes<DescriptorAttribute>()).ToList();
        }

        // find methods declared with [CliRootCommand] or [CliCommand] attributes
        private static List<CommandMethodDesc> GetCommandMethods(Assembly assembly, BindingFlags bindingFlags)
        {
            return assembly.GetTypes()
                                  .SelectMany(t => t.GetMethods(bindingFlags))
                                  .Select(m =>
                                  {
                                      if (m.GetCustomAttributes<DescriptorAttribute>().Count() > 1)
                                          throw new InvalidOperationException($"Method {m.Name} has multiple [CliCommand] attributes declared");
                                      return new { method = m, desc = m.GetCustomAttribute<DescriptorAttribute>() };
                                  })
                                  .Where(m => m.desc != null)
                                  .Select(m => new CommandMethodDesc(m.method, m.desc!))
                                  .ToList();
        }

        private static CliRootCommand CreateRootCommand(Assembly assembly, List<DescriptorAttribute> globalDescriptors, List<CommandMethodDesc> commandMethods, out CommandMethodDesc? rootMethod)
        {
            var globalRootDescriptors = globalDescriptors.Where(d => d.Kind == DescriptorAttribute.DescKind.CliRootCommand).ToList();
            var rootMethods = commandMethods.Where(m => m.Desc.Kind == DescriptorAttribute.DescKind.CliRootCommand).ToList();
            var rootDescriptorsCount = globalRootDescriptors.Count + rootMethods.Count;

            rootMethod = null;
            DescriptorAttribute? rootDescriptor = null;

            if (rootDescriptorsCount > 1)
                throw new InvalidOperationException($"Only one [CliRootCommand] attribute may be declared, found {rootDescriptorsCount}");

            if (globalRootDescriptors.Any())
                rootDescriptor = globalDescriptors.First();
            else if (rootMethods.Any())
                rootMethod = rootMethods.First();
            else if (commandMethods.Count == 1 && string.IsNullOrEmpty(commandMethods.First().Desc.Name))
                rootMethod = commandMethods.First();

            var rootCommandDescription = rootMethod?.Desc.Description ??
                rootDescriptor?.Description ??
                assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ??
                "";

            return new CliRootCommand(rootCommandDescription);
        }

        private static CliCommand CreateAndAddCommand(CliRootCommand rootCommand, string name, DescriptorAttribute desc)
        {
            if (desc.Kind != DescriptorAttribute.DescKind.CliCommand)
                throw new InvalidOperationException($"Unexpected descriptor type {desc.Kind} for the command");

            var subcommandNames = name.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            CliCommand parentCommand = rootCommand;
            CliCommand? command = null;
            bool created = false;
            foreach (var (subName, i) in subcommandNames.Select((n, i) => (n, i)))
            {
                command = parentCommand.Subcommands.FirstOrDefault(c => string.Compare(subName, c.Name, StringComparison.Ordinal) == 0 || c.Aliases.Contains(subName));
                if (command == null)
                {
                    bool isLast = (i == subcommandNames.Length - 1);
                    command = new CliCommand(subName, isLast ? desc.Description : null);
                    created = true;
                    parentCommand.Add(command);
                }
                parentCommand = command;
            }
            if (command == null)
                throw new InvalidOperationException();
            if (!created)
                throw new InvalidOperationException($"CliCommand '{name}' has multiple [CliCommand] definitions");
            if (desc.Aliases != null)
                foreach (var alias in desc.Aliases)
                    command.Aliases.Add(alias);
            command.Hidden = desc.IsHidden;
            return command;
        }

        private static readonly Type[] SupportedReturnTypes = new[] { typeof(void), typeof(int), typeof(Task<int>), typeof(Task)
#if NETCOREAPP2_0_OR_GREATER
            , typeof(ValueTask<int>), typeof(ValueTask)
#endif
        };
        private static void AddCommandHandler(CliCommand command, MethodInfo method, Action<ParseResult>[] globalOptionsInitializers)
        {
            if (command.Action != null)
                throw new InvalidOperationException($"CliCommand '{command.Name}' has multiple handler methods");

            if (!method.IsStatic)
                throw new InvalidOperationException($"Method {method.Name} declared as [CliCommand] must be static");

            // FIXME: generic type name is shown as Task`1 instead of Task<int>
            if (!SupportedReturnTypes.Any(t => t.IsAssignableFrom(method.ReturnType)))
                throw new InvalidOperationException($"Method {method.Name} should return any of {string.Join(",", SupportedReturnTypes.Select(t => t.Name))}");

            var paramInfo = new List<CliSymbol>();

            foreach (var param in method.GetParameters())
            {
                var info = param.GetCustomAttribute<DescriptorAttribute>() ?? new OptionAttribute();
                Func<object?>? getDefaultValue = null;
                if (param.HasDefaultValue && param.DefaultValue != null)
                    getDefaultValue = () => param.DefaultValue;
                switch (info.Kind)
                {
                    case DescriptorAttribute.DescKind.CliOption:
                        var option = CreateOption(info, param.Name, param.ParameterType, getDefaultValue);
                        command.Add(option);
                        paramInfo.Add(option);
                        break;
                    case DescriptorAttribute.DescKind.CliArgument:
                        var argument = CreateArgument(info, param);
                        command.Add(argument);
                        paramInfo.Add(argument);
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }

            command.SetAction(async (parseResult, cancellationToken) =>
            {
                _currentCommand = command;

                foreach (var initializer in globalOptionsInitializers)
                    initializer.Invoke(parseResult);

                var _params = paramInfo.Select(param => parseResult.GetValue((dynamic)param)).ToArray();

                var awatable = method.Invoke(null, _params)!;

                var exitCode = 0;

                if (awatable != null)
                { 
                    switch (awatable)
                    {
                        case Task<int> t:
                            exitCode = await t;
                            break;
                        case Task t:
                            await t;
                            exitCode = 0;
                            break;
#if NETCOREAPP2_0_OR_GREATER
                        case ValueTask<int> t:
                            exitCode = await t;
                            break;
                        case ValueTask t:
                            await t;
                            exitCode = 0;
                            break;
#endif
                        case int i:
                            exitCode = i;
                            break;
                        default:
                            // should not be here because of SupportedReturnTypes check above
                            throw new InvalidOperationException();
                    }
                }

                return exitCode;
            });
        }

        private static CliOption CreateOption(DescriptorAttribute info, string? memberName, Type valueType, Func<object?>? defaultValueFactory = null)
        {
            var name = info.Name ?? memberName ?? throw new NotSupportedException($"CliOption name cannot be deduced from parameter [{info}], specify name explicitly");
            name = AddPrefix(name);

            CliOption instance = defaultValueFactory == null || info.IsRequired ?
                OptionBuilder.CreateOption(name, valueType, info.Description) :
                OptionBuilder.CreateOption(name, valueType, info.Description, defaultValueFactory);

            if (info.Arity.HasValue)
                instance.Arity = info.Arity.Value;
            if (info.HelpName != null)
                instance.HelpName = info.HelpName;
            instance.Hidden = info.IsHidden;
            if (info.Aliases != null)
                foreach (var alias in info.Aliases)
                    instance.Aliases.Add(AddPrefix(alias));
            instance.Required = info.IsRequired;
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

        private static CliArgument CreateArgument(DescriptorAttribute info, ParameterInfo parameterInfo)
        {
            if (info.Name is null && parameterInfo.Name is null) 
                throw new NotSupportedException($"CliArgument name cannot be deduced from parameter [{info}], specify name explicitly");
            CliArgument instance = ArgumentBuilder.CreateArgument(parameterInfo);
            if (info.Arity.HasValue)
                instance.Arity = info.Arity.Value;
            if (info.HelpName != null)
                instance.HelpName = info.HelpName;
            instance.Hidden = info.IsHidden;
            return instance;
        }

    }
}
