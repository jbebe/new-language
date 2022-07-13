namespace NewLanguage.Engine.Lexer;

public record State(Dictionary<string, Declaration> Variables)
{
  public State CreateSnapshot() => new(new Dictionary<string, Declaration>(Variables));
}

public enum BinaryOperatorType
{
  Plus, Minus, Multiply, Divide
}

public static class BinaryOperatorTypeHelper
{
  public static int GetPriority(this BinaryOperatorType type)
  {
    switch (type)
    {
      case BinaryOperatorType.Plus:
      case BinaryOperatorType.Minus:
        return 1;
      case BinaryOperatorType.Multiply:
      case BinaryOperatorType.Divide:
        return 2;
      default: throw new NewLanguageException("Uknown operator has no priority");
    }
  }
}

public abstract record Node
{
  public abstract double Value(State state);
}

public record ValueExpr(double NumericValue) : Node
{ 
  public override double Value(State state) => NumericValue;
}

public record BinaryExpr : Node
{
  public class BinaryPartWrapper
  {
    public Node? Node { get; set; }
    public BinaryOperatorType? Operator { get; set; }
    public double? Value { get; set; }

    public BinaryPartWrapper(Node? node = null, BinaryOperatorType? op = null, double? value = null)
    {
      Node = node;
      Operator = op;
      Value = value;
    }

    public int GetOperatorScore()
    {
      if (!Operator.HasValue) return 0;
      return Operator.Value.GetPriority();
    }
  }

  public List<BinaryPartWrapper> Operations { get; set; } = new();

  public override double Value(State state)
  {
    int GetHighPrecendenceIndex()
    {
      var max = Operations
        .Select((x, idx) => (x, idx))
        .OrderByDescending(x => x.x.GetOperatorScore())
        .ThenBy(x => x.idx)
        .First();
      return max.idx;
    }
    double DoOperation(BinaryOperatorType op, double a, double b)
    => op switch
    {
      BinaryOperatorType.Plus => a + b,
      BinaryOperatorType.Minus => a - b,
      BinaryOperatorType.Multiply => a * b,
      BinaryOperatorType.Divide => a / b,
      _ => throw new NewLanguageException("Invalid operator")
    };

    while (Operations.Count > 1)
    {
      var nextOpIdx = GetHighPrecendenceIndex();
      var newVal = new BinaryPartWrapper(value: DoOperation(
        Operations[nextOpIdx].Operator.Value,
        Operations[nextOpIdx - 1].Value ?? Operations[nextOpIdx - 1].Node.Value(state),
        Operations[nextOpIdx + 1].Value ?? Operations[nextOpIdx + 1].Node.Value(state)));
      Operations.Insert(nextOpIdx + 2, newVal);
      Operations.RemoveRange(nextOpIdx - 1, 3);
    }

    return Operations.Single().Value.Value;
  }
}

public record CodeExpr : Node
{
  public List<Node> Expressions { get; set; } = new();
  
  public override double Value(State state)
  {
    var last = Expressions.Last();
    var expressions = Expressions.SkipLast(1);
    foreach (var expr in expressions)
      expr.Value(state);

    return last.Value(state);
  }
}

public record Variable(string Name) : Node
{
  public override double Value(State state) => state.Variables[Name].Assignee.Value(state);
}

public record Declaration() : Node
{
  public string Variable { get; set; }

  public Node Assignee { get; set; }

  public override double Value(State state)
  {
    // create snapshot to avoid recursion
    // if declaration references itself in the assignment
    var oldState = state.CreateSnapshot();
    if (!state.Variables.TryAdd(Variable, this))
      throw new NewLanguageException($"Variable '{Variable}' is already declared");
    
    return Assignee.Value(oldState);
  }
}