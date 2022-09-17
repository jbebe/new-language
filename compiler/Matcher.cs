using System.Text.RegularExpressions;

namespace NewLanguage.Engine.Matcher;

public record EvalResult(string Code, Lexer.Node Node);

//
// Grammar:
//  number := <decimal number regex>
//  binary_operator := '+' | '-' | '*' | '/'
//  binary_subexpr := (expression) | number
//  variable := <alphanumeric string starting with [a-z]>
//  declaration := variable: expression
//  fn_declaration := variable [param1[, param2, ...]: expression
//  function_expr := variable [param1[, param2, ...]
//  expression := binary_subexpr binary_operator binary_subexpr | number | variable | function_expr | (expression)
//  command := declaration | expression
//  code := command | command , command [, command....]
//
public abstract class Expr
{
  public abstract EvalResult Eval(string code);
  public static string SkipWhitespace(string code) =>
    new Regex(@"[\ \t\r\n]").Replace(code, "");

  protected static EvalResult EvalGroup(Expr[] groups, string code)
  {
    var originalCode = code;
    code = SkipWhitespace(code);
    foreach (var group in groups)
    {
      var (newCode, root) = group.Eval(code);
      if (root != null) return new(newCode, root);
    }

    return new(originalCode, null);
  }
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
    new VariableExpr(),
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

    return new(code[match.Length..], new Lexer.ValueExpr(result));
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

public class BaseExpr : Expr
{
  private readonly Expr[] Groups = new Expr[] {
    new BinaryExpr(),
    new ValueExpr(),
    new VariableExpr(),
    new BracketExpr(),
  };

  public override EvalResult Eval(string code) => EvalGroup(Groups, code);
}

public class CommandExpr : Expr
{
  private readonly Expr[] Groups = new Expr[] {
    new DeclarationExpr(),
    new BaseExpr(),
  };

  public override EvalResult Eval(string code) => EvalGroup(Groups, code);
}

public class Code : Expr
{
  public override EvalResult Eval(string code)
  {
    var noMatchResult = new EvalResult(code, null);
    var commandExpr = new CommandExpr();
    var codeExpr = new Lexer.CodeExpr();

    Lexer.Node root;
    (code, root) = commandExpr.Eval(code);
    if (root == null) return noMatchResult;
    codeExpr.Expressions.Add(root);

    while (true)
    {
      code = SkipWhitespace(code);
      // parse comma
      if (code.Length == 0 || code[0] != ',') return new(code, codeExpr);
      code = code[1..];


      code = SkipWhitespace(code);
      // parse new expr
      (code, root) = commandExpr.Eval(code);
      if (root == null) return noMatchResult;
      codeExpr.Expressions.Add(root);
    }
  }
}

public class VariableExpr : Expr
{
  public static readonly Regex VariableRx = new(@"[a-zA-Z_][a-zA-Z_0-9]*");

  public static (string Variable, string Code) GetVariableName(string code)
  {
    var match = VariableRx.Match(code);
    if (!match.Success || match.Index > 0) return (null, null);
    return (match.Value, code[match.Value.Length..]);
  }

  public override EvalResult Eval(string code)
  {
    var noMatchResult = new EvalResult(code, null);
    var (match, newCode) = GetVariableName(code);
    if (match == null) return noMatchResult;

    return new(newCode, new Lexer.Variable(match));
  }
}

public class DeclarationExpr : Expr
{
  public override EvalResult Eval(string code)
  {
    var noMatchResult = new EvalResult(code, null);
    var decl = new Lexer.Declaration();

    code = SkipWhitespace(code);
    // declaration
    {
      var (variable, newCode) = VariableExpr.GetVariableName(code);
      if (variable == null) return noMatchResult;
      decl.Variable = variable;
      code = newCode;
    }

    // assignment operator
    {
      code = SkipWhitespace(code);
      if (code.Length == 0 || code[0] != ':') return noMatchResult;
      code = code[1..];
    }

    // expression
    {
      var expr = new BaseExpr();
      var (newCode, exprNode) = expr.Eval(code);
      if (exprNode == null) return noMatchResult;
      decl.Assignee = exprNode;
      code = newCode;
    }

    return new(code, decl);
  }
}