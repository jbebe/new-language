namespace NewLanguage.Engine;

public class Engine
{
  private string Code { get; set; }

  public Engine(string code)
  {
    Code = code;
  }

  public static Engine FromSourcePath(string sourcePath)
  {
    // validate input
    if (!File.Exists(sourcePath))
      throw new NewLanguageException("Input file is invalid", context: new { Path = sourcePath });

    // load input into string
    var code = File.ReadAllText(sourcePath);
    code.ReplaceLineEndings("\n");

    return new(code);
  }

  public double Run()
  {
    // parse code
    var entry = Parse();

    // create state
    var state = new Lexer.State(new());

    // Evaluate symbols a.k.a. run code
    return entry.Value(state);
  }

  private Lexer.Node Parse()
  {
    var codeExpr = new Matcher.Code();
    var (_, root) = codeExpr.Eval(Code);
    if (root == null) throw new NewLanguageException("No parseable expression found in code");

    return root;
  }
}
