using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codegen
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class Generator : Attribute { }
}

namespace Codegen.Extensions
{
    public static class StringBuilderExtensions
    {
        public static char IndentationChar = ' ';
        public static int IndentationCount = 2;

        public static StringBuilder AppendLine(this StringBuilder sb, string value, int level)
        {
            return sb.AppendLine(value.PadLeft(IndentationCount * level + value.Length, IndentationChar));
        }
        public static StringBuilder Append(this StringBuilder sb, string value, int level)
        {
            return sb.Append(value.PadLeft(IndentationCount * level + value.Length, IndentationChar));
        }
        public static StringBuilder AppendFormat(this StringBuilder sb, string format, int level, params object[] args)
        {
            var str = string.Format(format, args);
            return sb.Append(str.PadLeft(IndentationCount * level + str.Length, IndentationChar));
        }
    }
}

namespace Codegen.DataModel
{
    public class ObjectWithAttributes
    {
        public Dictionary<string, string> Attributes { get; set; }
        public ObjectWithAttributes(Dictionary<string, string> attributes)
        {
            Attributes = attributes;
        }
    }

    public class PropertyType
    {
        public string Name { get; set; }
        public bool IsList { get; set; }
        public bool IsPrimitive { get; set; }
        public PropertyType(string name, bool isList, bool isPrimitive)
        {
            Name = name;
            IsList = isList;
            IsPrimitive = isPrimitive;
        }
    }

    public class Property : ObjectWithAttributes
    {
        public string Name { get; set; }
        public PropertyType Type { get; set; }
        public string Default { get; set; }

        public bool Optional { get; set; }

        public Property(string name, PropertyType type, Dictionary<string, string> attributes, string default_value, bool optional) : base(attributes)
        {
            Name = name;
            Type = type;
            Default = default_value;
            Optional = optional;
        }
    }

    public class Object : TypeDeclaration
    {
        public List<Property> Properties { get; set; }
        public Object(string name, List<Property> properties, Dictionary<string, string> attributes) : base(name, attributes)
        {
            Properties = properties;
        }
    }

    public class TypeDeclaration : ObjectWithAttributes
    {
        public string Name { get; set; }
        public TypeDeclaration(string name, Dictionary<string, string> attributes) : base(attributes)
        {
            Name = name;
        }
    }

    public class Namespace
    {
        public Namespace FromJson(string data)
        {
            return JsonConvert.DeserializeObject<Namespace>(data);
        }
        public string ToJson(bool formatted = false)
        {
            return JsonConvert.SerializeObject(this, formatted ? Formatting.Indented : Formatting.None);
        }
        public List<Object> Objects { get; set; }
        public List<TypeDeclaration> ExternalTypes { get; set; }
        public string Name { get; set; }
        public Namespace(List<Object> objects, string name, List<TypeDeclaration> externalTypes)
        {
            Objects = objects;
            Name = name;
            ExternalTypes = externalTypes;
        }
    }

    public class GenerationResult
    {
        public string FileName { get; set; }
        public string Content { get; set; }
    }
}
