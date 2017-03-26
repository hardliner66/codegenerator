namespace Codegen
{
  public class Extensions
  {

    public bool HasObjectType(Object o)
    {
      foreach(var p in o.Properties) {
        if (p.List || ToType(p.Type) == "T" + p.Type) {
          return true;
        }
      }
      return false;
    }
    public string ToType(string type)
    {
      string result = type;
      switch (type.ToLower())
      {
        case "string":
          result = "String";
          break;
        case "bool":
          result = "Boolean";
          break;
        case "boolean":
          result = "Boolean";
          break;
        case "int":
          result = "Integer";
          break;
        case "integer":
          result = "Integer";
          break;
        case "int64":
          result = "Int64";
          break;
        case "double":
          result = "Double";
          break;
        default:
          result = "T" + type;
          break;
      }
      return result;
    }
  }

  public class Template<T> : CustomizedTemplate<T>
  {
    public Template() : base()
    {
      Extensions = new Extensions();
    }

    public Extensions Extensions { get; set; }
  }
}