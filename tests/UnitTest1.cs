using SnapCLI;
using System.CommandLine;
using System.CommandLine.Builder;
using System.Text;
using System.Text.RegularExpressions;
using SnapCLI.DataAnnotations;
using System.ComponentModel.DataAnnotations;

[assembly: Command(Name = "level1cmd")]

namespace Tests
{
    [TestClass]
    public class UnitTest1 : StaticFieldsCache
    {
        static StringWriter Out = new StringWriter();

        public enum UseExceptionHandler
        {
            Default,
            Custom,
            Null,
            Throwing
        }

        [TestMethod]
        [DataRow("", "[testhost()]")]
        [DataRow("-?", "Root description")]
        [DataRow("test1", "like:[before:test1]*[test1()]*[after:test1]")]
        [DataRow("test2", "'-p' is required")]
        [DataRow("test2 -p 1", "test2(1)")]
        [DataRow("test3", "test3(2)")]
        [DataRow("test3 --param 3", "test3(3)")]
        [DataRow("test4", "test4()")]
        [DataRow("test4 --param foo", "test4(foo)")]
        [DataRow("test-field", "test-field(globalOptionFieldDefaultValue)")]
        [DataRow("test-field --global-option-field 1 ", "test-field(1)")]
        [DataRow("test-property", "test-property(globalOptionPropertyDefaultValue)")]
        [DataRow("test-property --prop 123 ", "test-property(123)")]
        [DataRow("test6", "'--opt1name' is required")]
        [DataRow("test6 --opt1name", "Required argument")]
        [DataRow("test6 --opt1name a", "expected type 'System.Int32'")]
        [DataRow("test6 --opt1name 222", "test6(True,222,1,arg2)")]
        [DataRow("test6 --opt1alias 222", "test6(True,222,1,arg2)")]
        [DataRow("test6 -O 222", "test6(True,222,1,arg2)")]
        [DataRow("test6 --opt1name false 222", "test6(False,222,1,arg2)")]
        [DataRow("test6 -O false 222", "test6(False,222,1,arg2)")]
        [DataRow("test6 --opt1name false --option2", "Required argument missing")]
        [DataRow("test6 --opt1name false 222 --option2 2", "test6(False,222,2,arg2)")]
        [DataRow("test6 --opt1name false 222 --option2 2 arg2_new", "test6(False,222,2,arg2_new)")]
        [DataRow("test6 --opt1name --option2 2 222 arg2_new", "test6(True,222,2,arg2_new)")] // change order - options first
        [DataRow("test6 222 arg2_new --opt1name false --option2 2 ", "test6(False,222,2,arg2_new)")] // change order - arguments first
        [DataRow("test6 -?", "like:Test6 description*test6 <arg1name> [<argument2>] [options]")]
        [DataRow("test6 -?", "<arg1help>  arg1 description")]
        [DataRow("test6 -?", "<arg2help>  arg2 description [default: arg2]")]
        [DataRow("test6 -?", "like: -O, --opt1alias, --opt1name (REQUIRED)*opt1 description")]
        [DataRow("test6 -?", "like:--option2 <opt2help>*opt2 description [default: 1]")]
        [DataRow("test6 -?", "--global-option-field")]
        [DataRow("test6 -?", "[default: globalOptionFieldDefaultValue]")]
        [DataRow("test6 -?", "--prop, --propAlias <propHelpName>")]
        [DataRow("test6 -?", "Prop description [default: globalOptionPropertyDefaultValue]")]
        [DataRow("exception", $"like:[exception()]*\nSystem.ApplicationException: THIS IS TEST-GENERATED EXCEPTION!*[exitCode:1]", UseExceptionHandler.Default)]
        [DataRow("exception", "like:[exception:THIS IS TEST-GENERATED EXCEPTION!]*[exitCode:999]", UseExceptionHandler.Custom)]
        [DataRow("exception", "like:[unhandled:System.ApplicationException: THIS IS TEST-GENERATED EXCEPTION!", UseExceptionHandler.Null)]
        [DataRow("exception", "like:[throw:System.ApplicationException: THIS IS TEST-GENERATED EXCEPTION!", UseExceptionHandler.Throwing)]
        [DataRow("exception", "like:[unhandled:System.Exception: Rethrowing", UseExceptionHandler.Throwing)]
        [DataRow("exit-code", "[exitCode:0]")]
        [DataRow("exit-code --exit-code 1", "[exitCode:1]")]
        [DataRow("exit-code --exit-code -1", "[exitCode:-1]")]
        [DataRow("exit-code-async", "[exitCode:0]")]
        [DataRow("exit-code-async --exit-code 1", "[exitCode:1]")]
        [DataRow("exit-code-async --exit-code -1", "[exitCode:-1]")]
        [DataRow("level1cmd -?", "regex:Commands:\\s*level2cmd")]
        [DataRow("level1cmd level2cmd", "[level1cmd_level2cmd()]")]
        [DataRow("validate-mutually-exclusive-options", "!mutually exclusive")]
        [DataRow("validate-mutually-exclusive-options --opt1 1 --opt2 2", "mutually exclusive")]
        [DataRow("validate-mutually-exclusive-options --opt1 1 --propAlias 2", "mutually exclusive")]
        [DataRow("validate-mutually-exclusive-options --opt2 1 --propAlias 2", "!mutually exclusive")]
        [DataRow("validate-mutually-exclusive-options --opt2 1 --global-option-field 2", "mutually exclusive")]
        [DataRow("validate-mutually-exclusive-options2 --opt1 1 --opt2 2", "mutually exclusive")]
        [DataRow("validate-mutually-exclusive-options2 --opt1 1 --propAlias 2", "mutually exclusive")]
        [DataRow("validate-mutually-exclusive-options2 --opt2 1 --propAlias 2", "!mutually exclusive")]
        [DataRow("validate-mutually-exclusive-options2 --opt2 1 --global-option-field 2", "mutually exclusive")]
        [DataRow("validate-mutually-exclusive-options2 3 --opt2 1", "mutually exclusive")]
        [DataRow("validate-mutually-exclusive-options2 3 --opt1 1", "!mutually exclusive")]
        [DataRow("cmd-r1", "[cmd-r1(11,1,opt2,opt3)]")]
        [DataRow("cmd-r1 --r-opt1 111", "[cmd-r1(11,111,opt2,opt3)]")]
        [DataRow("cmd-r1 --r-opt1 111 --r-opt2 opt222 --r-opt3 opt333", "[cmd-r1(11,111,opt222,opt333)]")]
        [DataRow("cmd-r1 --r-opt1 1111 --r-opt2 opt222s --r-opt3 opt333s sub-cmd-r1 --opt1 101", "[cmd-r1_sub-cmd-r1(101,1111,opt222s,opt333s)]")]
        [DataRow("cmd-r2 --opt1 11 --r-opt1 111 --r-opt2 opt222 --r-opt3 opt333", "[cmd-r2(11,1,opt2,opt3,111,opt222,opt333)]")]
        [DataRow("ann1", "The option 'opt1' must be between 1 and 10")]
        [DataRow("ann1 --opt1 1", "[ann1(1,test)]")]
        [DataRow("ann1 --opt1 10", "[ann1(10,test)]")]
        [DataRow("ann1 --opt1 11", "The option 'opt1' must be between 1 and 10")]
        [DataRow("ann1 --opt1 5 --opt2 aa", "minimum length of '3' and maximum length of '10'")]
        [DataRow("ann1 --opt1 5 --opt2 test!", "[ann1(5,test!)]")]
        public void TestCLI(string commandLine, string pattern, UseExceptionHandler useExceptionHandler = UseExceptionHandler.Default)
        {
            // synchronize tests
            lock (Out)
            {
                var defaultExceptionHandler = CLI.ExceptionHandler;
                try
                {

                    ResetValuesFromCache();

                    switch (useExceptionHandler)
                    {
                        case UseExceptionHandler.Default: break;
                        case UseExceptionHandler.Custom: CLI.ExceptionHandler = CustomExceptionHandler; break;
                        case UseExceptionHandler.Null: CLI.ExceptionHandler = null; break;
                        case UseExceptionHandler.Throwing: CLI.ExceptionHandler = ThrowingExceptionHandler; break;
                        default: throw new ArgumentOutOfRangeException(nameof(useExceptionHandler));
                    }

                    try
                    {
                        int exitCode = CLI.Run(SplitArgs(commandLine).ToArray(), Out, Out);
                        Out.WriteLine($"[exitCode:{exitCode}]");
                    }
                    catch (Exception ex)
                    {
                        Out.WriteLine($"[unhandled:{ex}]");

                    }
                    var output = Out.ToString().Trim(" \r\n".ToCharArray());
                    bool expectedContains = true;

                    if (pattern.StartsWith("!"))
                    {
                        expectedContains = false;
                        pattern = pattern.Substring(1);
                    }
                    bool contains;
                    if (pattern.StartsWith("like:"))
                        contains = output.Like(pattern.Substring("like:".Length), substring: true);
                    else if (pattern.StartsWith("regex:"))
                        contains = Regex.IsMatch(output, pattern.Substring("regex:".Length));
                    else
                        contains = output.Contains(pattern);

                    Assert.IsTrue(contains == expectedContains, $"Output {(expectedContains ? "does not contain" : "contains")} '{pattern}'\n\nOutput:\n{Out}\n");
                }
                finally
                {
                    // cleanup
                    Out.GetStringBuilder().Clear();
                    CLI.ExceptionHandler = defaultExceptionHandler;
                }
            }
        }

        // https://stackoverflow.com/a/64236441
        public static IEnumerable<string> SplitArgs(string commandLine)
        {
            var result = new StringBuilder();

            var quoted = false;
            var escaped = false;
            var started = false;
            var allowcaret = false;
            for (int i = 0; i < commandLine.Length; i++)
            {
                var chr = commandLine[i];

                if (chr == '^' && !quoted)
                {
                    if (allowcaret)
                    {
                        result.Append(chr);
                        started = true;
                        escaped = false;
                        allowcaret = false;
                    }
                    else if (i + 1 < commandLine.Length && commandLine[i + 1] == '^')
                    {
                        allowcaret = true;
                    }
                    else if (i + 1 == commandLine.Length)
                    {
                        result.Append(chr);
                        started = true;
                        escaped = false;
                    }
                }
                else if (escaped)
                {
                    result.Append(chr);
                    started = true;
                    escaped = false;
                }
                else if (chr == '"')
                {
                    quoted = !quoted;
                    started = true;
                }
                else if (chr == '\\' && i + 1 < commandLine.Length && commandLine[i + 1] == '"')
                {
                    escaped = true;
                }
                else if (chr == ' ' && !quoted)
                {
                    if (started) yield return result.ToString();
                    result.Clear();
                    started = false;
                }
                else
                {
                    result.Append(chr);
                    started = true;
                }
            }

            if (started) yield return result.ToString();
        }

        private static void TraceCommand(params object?[] args)
        {
            Out.WriteLine($"[{CLI.ParseResult.CommandResult.Command.FullName().Replace(' ', '_')}({string.Join(",", args)})]");
        }

        private static int CustomExceptionHandler(Exception exception)
        {
            Out.WriteLine($"[exception:{exception.Message}]");
            return 999; // exit code
        }

        private static int ThrowingExceptionHandler(Exception exception)
        {
            Out.WriteLine($"[throw:{exception}]");
            throw new Exception("Rethrowing", exception);
        }


        [Startup]
        public static void Startup(CommandLineBuilder commandLineBuilder)
        {
            Assert.IsNotNull(commandLineBuilder);

            // must configure CommandLineBuilder
            // use all from .UseDefaults() except .UseExceptionHandler()
            commandLineBuilder
                   .UseVersionOption()
                   .UseHelp()
                   .UseEnvironmentVariableDirective()
                   .UseParseDirective()
                   .UseSuggestDirective()
                   .RegisterWithDotnetSuggest()
                   .UseTypoCorrections()
                   .UseParseErrorReporting()
                   //.UseExceptionHandler()
                   .CancelOnProcessTermination();
        }

        [Startup]
        public static void Stratup2()
        {
            CLI.BeforeCommand += (args) => { Out.WriteLine($"[before:{args.ParseResult.CommandResult.Command.Name}]"); };
            CLI.AfterCommand += (args) => { Out.WriteLine($"[after:{args.ParseResult.CommandResult.Command.Name}]"); };

            CLI.ExceptionHandler = (exception) =>
            {
                // 
                if (exception is not OperationCanceledException)
                    Out.WriteLine(exception);

                return 1; // exit code
            };
        }

#if BEFORE_AFTER_COMMAND_ATTRIBUTE
        [BeforeCommand]
        public static void PreCommand(ParseResult parseResult, Command command)
        {
            Out.WriteLine($"[PreCommand:{command.Name}]");
        }


        [AfterCommand]
        public static void PostCommand(ParseResult parseResult, Command command)
        {
            Out.WriteLine($"[PostCommand:{command.Name}]");
        }
#endif

        [Option] // implicit Name= global-option-field
        public static string? globalOptionField = globalOptionFieldDefaultValue;
        private const string globalOptionFieldDefaultValue = "globalOptionFieldDefaultValue";

        [Option(Name = "prop", HelpName = "propHelpName", Aliases = "propAlias", Description = "Prop description")]
        public static string? globalOptionProperty { get; set; } = globalOptionPropertyDefaultValue;
        private const string globalOptionPropertyDefaultValue = "globalOptionPropertyDefaultValue";

        [Command]
        public static void Test1()
        {
            TraceCommand();
        }


        [Command]
        public static void Test2(int p)
        {
            TraceCommand(p);
        }

        [Command]
        public static void Test3(int param = 2)
        {
            TraceCommand(param);
        }

        [Command]
        public static void Test4(string? param = null)
        {
            TraceCommand(param);
        }

        [Command]
        public static void TestField()
        {
            TraceCommand(globalOptionField);
        }

        [Command]
        public static void TestProperty()
        {
            TraceCommand(globalOptionProperty);
        }

        [RootCommand(Description = "Root description")]
        public static void TestRoot()
        {
            TraceCommand();
        }

        [Command(Name = "test6", Aliases = "TEST6", Description = "Test6 description")]
        public static void Test6Handler(
            [Option(Name = "opt1name", HelpName = "opt1help", Aliases = "opt1alias,O", Description = "opt1 description")]
            bool option1,

            [Argument(Name = "arg1name", HelpName = "arg1help", Description = "arg1 description")]
            int argument1,


            [Option(HelpName= "opt2help", Description= "opt2 description")]
            int option2 = 1,

            [Argument(HelpName= "arg2help", Description= "arg2 description")]
            string argument2 = "arg2"
            )
        {
            TraceCommand(option1, argument1, option2, argument2);
        }

        [Command]
        public static void exception()
        {
            TraceCommand();
            throw new ApplicationException("THIS IS TEST-GENERATED EXCEPTION!");
        }

        [Command]
        public static int exitCode(int exitCode = 0)
        {
            return exitCode;
        }

        [Command]
        public static Task<int> exitCodeAsync(int exitCode = 0)
        {
            return Task.FromResult(exitCode);
        }

        [Command(Name = "level1cmd level2cmd")]
        public static void level2cmd()
        {
            TraceCommand();
        }

        [Command]
        public static void ValidateMutuallyExclusiveOptions(
            int opt1 = 1, int opt2 = 2)
        {
            TraceCommand();
            CLI.ParseResult.ValidateMutuallyExclusiveOptionsArguments(["opt1", "opt2"]);
            CLI.ParseResult.ValidateMutuallyExclusiveOptionsArguments(["opt1", "prop"]);
            CLI.ParseResult.ValidateMutuallyExclusiveOptionsArguments(["opt2", "global-option-field"], ["validate-mutually-exclusive-options"]);
        }

        [Command(MutuallyExclusuveOptionsArguments = "(opt1,opt2)(opt1,prop)(opt2,global-option-field)(opt2,arg1)")]
        public static void ValidateMutuallyExclusiveOptions2(
            int opt1 = 1, int opt2 = 2, [Argument] int arg1 = 3)
        {
            TraceCommand();
        }

        class RecursiveOptions1
        {
            [Option] internal static int rOpt1 = 1;
            [Option] internal static string rOpt2 = "opt2";
            [Option] internal static string rOpt3 { get; set; } = "opt3";
        }

        [Command(Name = "cmd-r1", RecursiveOptionsContainingType = typeof(RecursiveOptions1))]
        public static void TestRecursiveOptions(int opt1 = 11)
        { 
            TraceCommand(
                opt1,
                RecursiveOptions1.rOpt1, 
                RecursiveOptions1.rOpt2,
                RecursiveOptions1.rOpt3
                );
        }

        [Command(Name = "cmd-r1 sub-cmd-r1")]
        public static void TestRecursiveOptionsSubcommand(int opt1 = 0)
        {
            TraceCommand(
                opt1,
                // this recursive options must be available from parent command
                RecursiveOptions1.rOpt1,
                RecursiveOptions1.rOpt2,
                RecursiveOptions1.rOpt3
                );
        }

        class RecursiveOptions2
        {
            [Option] internal static int rOpt1 = 11;
            [Option] internal static string rOpt2 = "opt22";
            [Option] internal static string rOpt3 { get; set; } = "opt33";
        }

        [Command(Name = "cmd-r2", RecursiveOptionsContainingType = typeof(RecursiveOptions2))]
        public static void TestRecursiveOptions2(int opt1 = 0)
        {
            TraceCommand(
                opt1,
                // this is invalid access, values will stay default
                RecursiveOptions1.rOpt1,
                RecursiveOptions1.rOpt2,
                RecursiveOptions1.rOpt3,
                // this is valid access, values must be reflecyed
                RecursiveOptions2.rOpt1,
                RecursiveOptions2.rOpt2,
                RecursiveOptions2.rOpt3
                );
        }


        [Startup]
        public static void Startup()
        {
            CLI.BeforeCommand += (args) => args.ParseResult.ValidateDataAnnotations();
        }


        [Command(Name = "ann1")]
        public static void TestAnnotations1(
            [Range(1, 10)]
            int opt1 = 0,

            [Length(3, 10)]
            string opt2 = "test"
            )
        {
            TraceCommand(opt1, opt2);
        }
    }


}
