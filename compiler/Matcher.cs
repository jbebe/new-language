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
  public BinarySubExpr ValueA = new();
  public BinarySubExpr ValueB = new();

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
    {
      var (newCode, node) = ValueA.Eval(code);
      if (node == null) return noMatchResult;
      root.ValueA = node;
      code = newCode;
    }
    code = SkipWhitespace(code);
    {
      var op = OperatorMap.Keys.FirstOrDefault(op => code.StartsWith(op));
      if (op == null) return noMatchResult;
      root.Operator = OperatorMap[op];
      code = code[op.Length..];
    }
    code = SkipWhitespace(code);
    {
      var (newCode, node) = ValueB.Eval(code);
      if (node == null) return noMatchResult;
      root.ValueB = node;
      code = newCode;
    }
    return new(code, root);
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
