namespace Codegen
{
  public class Extensions
  {
    Global Model;
    Helper Helper;
    public Extensions(Helper helper) {
      Helper = helper;
      Model = Helper.GetModel();
    }
    public bool IsObjectType(Property p) {
      return p.List || ToType(p.Type) == "T" + p.Type;
    }

    public bool HasObjectTypeOrList(Object o)
    {
      foreach (var p in o.Properties)
      {
        if (IsObjectType(p))
        {
          return true;
        }
      }
      return false;
    }

    public bool NeedsDestructor(Property p)
    {
      if (p.List) {
        return true;
      }
      foreach (var o in Model.Objects) {
        if (p.Type.ToLower() == o.Name.ToLower() && Helper.IsSet(o.Attributes, "interfaced")) {
          return false;
        }
      }
      foreach (var t in Model.ExternalTypes) {
        if (p.Type.ToLower() == t.Name.ToLower() && Helper.IsSet(t.Attributes, "interfaced")) {
          return false;
        }
      }
      return true;
    }

    public string ToType(string type, string prefix = "T")
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
        case "int32":
          result = "Int32";
          break;
        case "double":
        case "float":
          result = "Double";
          break;
        default:
          result = prefix + type;
          break;
      }
      return result;
    }
  }

  public class Template<T> : CustomizedTemplate<T>
  {
    public Template() : base()
    {
      Extensions = new Extensions(Helper);
    }

    public Extensions Extensions { get; set; }
  }
}