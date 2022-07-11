using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLanguage.Engine;

public class CommandLineOptions
{
  [Value(0, MetaName = "input", HelpText = "Path to the source file")]
  public string InputPath { get; set; }
}
