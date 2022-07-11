using CommandLine;
using NewLanguage.Engine;

var options = Parser.Default.ParseArguments<CommandLineOptions>(args).Value;
var engine = new Engine(options.InputPath);
engine.Run();