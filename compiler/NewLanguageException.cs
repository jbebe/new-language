namespace NewLanguage.Engine;

public class NewLanguageException: Exception
{
  public object Context { get; set; }

  public NewLanguageException(string message, object context = null): base(message)
  {
    Context = context ?? new { };
  }
}
