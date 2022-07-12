using System.Text.RegularExpressions;

namespace NewLanguage.Engine.Matcher;

public record EvalResult(string Code, Lexer.Node Node);

public abstract class Expr
{
  public abstract EvalResult Eval(string code);
  public static string SkipWhitespace(string code) =>
    new Regex(@"[\ \t\r\n]").Replace(code, "");
}

public class BinaryExpr: Expr
{
  public BinarySubExpr SubExpr = new();

  private readonly Dictionary<string, Lexer.BinaryOperatorType> OperatorMap = new()
  {
    {"+", Lexer.BinaryOperatorType.Plus},
    {"-", Lexer.BinaryOperatorType.Minus},
    {"*", Lexer.BinaryOperatorType.Multiply},
    {"/", Lexer.BinaryOperatorType.Divide},
  };

  public override EvalResult Eval(string code)
  {
    var root = new Lexer.BinaryExpr();
    var noMatchResult = new EvalResult(code, null);

    // read first value outside of the loop
    {
      var (newCode, node) = SubExpr.Eval(code);
      if (node == null) return noMatchResult;
      root.Operations.Add(new(node: node));
      code = newCode;
    }

    for (var i = 0; true; i++)
    {
      code = SkipWhitespace(code);
      // read operator
      {
        var op = OperatorMap.Keys.FirstOrDefault(op => code.StartsWith(op));
        var isFirstPass = i == 0;
        if (op == null) 
        {
          // if only the first pass happened, no match, return null
          // but if it's the second or third pass and no more operators,
          // the expression is valid and we can return
          if (isFirstPass) return noMatchResult;
          else return new(code, root);
        }
        root.Operations.Add(new(op: OperatorMap[op]));
        code = code[op.Length..];
      }

      code = SkipWhitespace(code);
      // read second, third... value
      {
        var (newCode, node) = SubExpr.Eval(code);
        if (node == null) return noMatchResult;
        root.Operations.Add(new(node: node));
        code = newCode;
      }
    }
  }
}

public class BinarySubExpr : Expr
{
  private readonly Expr[] Groups = new Expr[] {
    new BracketExpr(),
    new ValueExpr(),
  };

  public override EvalResult Eval(string code)
  {
    Lexer.Node root = null;
    foreach (var group in Groups)
    {
      (code, root) = group.Eval(code);
      if (root != null) break;
    }

    return new(code, root);
  }
}

public class ValueExpr : Expr
{
  private readonly Regex DecimalValueRx = new(@"(?:0|[1-9]\d*)?\.\d+|[1-9]\d*|0");
  public override EvalResult Eval(string code)
  {
    var match = DecimalValueRx.Match(code);
    if (!match.Success || match.Index > 0) return new(code, null);
    if (!double.TryParse(match.Value, out var result))
      throw new NewLanguageException("Invalid number", new { match.Value });

    return new(code.Substring(match.Length), new Lexer.ValueExpr(result));
  }
}

public class BracketExpr: Expr
{
  private const string BracketBegin = "(";
  private const string BracketEnd = ")";

  public override EvalResult Eval(string code)
  {
    var noMatchResult = new EvalResult(code, null);
    if (!code.StartsWith(BracketBegin)) return noMatchResult;
    code = code[BracketBegin.Length..];

    var baseExpr = new BaseExpr();
    var (newCode, node) = baseExpr.Eval(code);
    if (node == null) return noMatchResult;
    code = newCode;

    if (!code.StartsWith(BracketEnd)) return noMatchResult;
    code = code[BracketEnd.Length..];

    return new(code, node);
  }
}

//
// Grammar:
//  number := <decimal number regex>
//  binary_operator := '+' | '-' | '*' | '/'
//  binary_subexpr := (expression) | number
//  expression := binary_subexpr binary_operator binary_subexpr | number | (expression)
public class BaseExpr : Expr
{
  private readonly Expr[] Groups = new Expr[] {
    new BinaryExpr(),
    new ValueExpr(),
    new BracketExpr(),
  };

  public override EvalResult Eval(string code)
  {
    Lexer.Node root = null;
    foreach (var group in Groups)
    {
      (code, root) = group.Eval(code);
      if (root != null) break;
    }

    return new(code, root);
  }
}
