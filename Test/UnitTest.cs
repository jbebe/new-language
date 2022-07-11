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

}
