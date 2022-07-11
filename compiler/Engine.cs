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

    // Evaluate symbols a.k.a. run code
    return entry.Value;
  }

  private Lexer.Node Parse()
  {
    var baseExpression = new Matcher.BaseExpr();
    var (_, root) = baseExpression.Eval(Code);
    if (root == null) throw new NewLanguageException("No parseable expression found in code");

    return root;
  }
}
