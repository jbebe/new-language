using NewLanguage.Engine;

namespace NewLanguage.Test;

[TestClass]
public class UnitTest
{
  [TestMethod]
  public void HandleNumber()
  {
    var source = @"1234";
    var engine = new Engine.Engine(source);
    Assert.AreEqual(1234.0, engine.Run());
  }

  [TestMethod]
  public void HandleBinaryOperator()
  {
    var source = @"1234 + 5678.9";
    var engine = new Engine.Engine(source);
    Assert.AreEqual(6912.9, engine.Run());
  }
  
  [TestMethod]
  public void HandleBrackets()
  {
    var source = @"(5 + 2) + 7";
    var engine = new Engine.Engine(source);
    Assert.AreEqual((double)((5 + 2) + 7), engine.Run());
  }

  [TestMethod]
  public void HandleRepeatedBinaryOperator()
  {
    var source = @"1 + 10 / 2 * 2";
    var engine = new Engine.Engine(source);
    Assert.AreEqual((double)11, engine.Run());
  }

  [TestMethod]
  public void HandleMultipleCommands()
  {
    var source = @"
      5,
      6,
      7";
    var engine = new Engine.Engine(source);
    Assert.AreEqual((double)7, engine.Run());
  }

  [TestMethod]
  public void HandleVariableDeclaration()
  {
    var source = @"
      foo: 5,
      foo";
    var engine = new Engine.Engine(source);
    Assert.AreEqual((double)5, engine.Run());
  }

  [TestMethod]
  public void HandleComplexVariableDeclaration()
  {
    var source = @"
      foo: 5,
      bar: 2 * foo,
      bar + foo";
    var engine = new Engine.Engine(source);
    Assert.AreEqual((double)15, engine.Run());
  }

  [TestMethod]
  public void HandleSimpleFunctions()
  {
    var source = @"
      foo x y: x * y,
      bar x: 2 * x,
      baz: foo 2 (bar 1.5) + 2,
      baz";
    var engine = new Engine.Engine(source);
    Assert.AreEqual((double)8, engine.Run());
  }
}
