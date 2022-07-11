namespace NewLanguage.Engine.Lexer;

public enum BinaryOperatorType
{
  Plus, Minus, Multiply, Divide
}

public abstract record Node
{
  public abstract double Value { get; }
}

public record ValueExpr(double NumericValue) : Node
{ 
  public override double Value => NumericValue;
}

public record BinaryExpr: Node
{
  public Node ValueA { get; set; }

  public BinaryOperatorType Operator { get; set; }

  public Node ValueB { get; set; }

  public override double Value => Operator switch
  {
    BinaryOperatorType.Plus => ValueA.Value + ValueB.Value,
    BinaryOperatorType.Minus => ValueA.Value - ValueB.Value,
    BinaryOperatorType.Multiply => ValueA.Value * ValueB.Value,
    BinaryOperatorType.Divide => ValueA.Value / ValueB.Value,
    _ => throw new NewLanguageException("Invalid operator")
  };
}
