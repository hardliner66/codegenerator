using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codegen
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class Generator : Attribute { }
}

namespace Codegen.DataModel
{
    public class ObjectWithAttributes
    {
        public ImmutableDictionary<string, string> Attributes { get; }
        public ObjectWithAttributes(ImmutableDictionary<string, string> attributes)
        {
            Attributes = attributes;
        }
    }

    public class PropertyType
    {
        public string Name { get; }
        public bool IsList { get; }
        public bool IsPrimitive { get; }
        public PropertyType(string name, bool isList, bool isPrimitive)
        {
            Name = name;
            IsList = isList;
            IsPrimitive = isPrimitive;
        }
    }

    public class Property : ObjectWithAttributes
    {
        public string Name { get; }
        public PropertyType Type { get; }
        public string Default { get; }

        public bool Optional { get; }

        public Property(string name, PropertyType type, ImmutableDictionary<string, string> attributes, string default_value, bool optional) : base(attributes)
        {
            Name = name;
            Type = type;
            Default = default_value;
            Optional = optional;
        }
    }

    public class Object : TypeDeclaration
    {
        public ImmutableList<Property> Properties { get; }
        public Object(string name, ImmutableList<Property> properties, ImmutableDictionary<string, string> attributes) : base(name, attributes)
        {
            Properties = properties;
        }
    }

    public class TypeDeclaration : ObjectWithAttributes
    {
        public string Name { get; }
        public TypeDeclaration(string name, ImmutableDictionary<string, string> attributes) : base(attributes)
        {
            Name = name;
        }
    }

    public class Global
    {
        public Global FromJson(string data)
        {
            return JsonConvert.DeserializeObject<Global>(data);
        }
        public string ToJson(bool formatted = false)
        {
            return JsonConvert.SerializeObject(this, formatted ? Formatting.Indented : Formatting.None);
        }
        public ImmutableList<Object> Objects { get; }
        public ImmutableList<TypeDeclaration> ExternalTypes { get; }
        public string Namespace { get; }
        public Global(ImmutableList<Object> objects, string @namespace, ImmutableList<TypeDeclaration> externalTypes)
        {
            Objects = objects;
            Namespace = @namespace;
            ExternalTypes = externalTypes;
        }
    }
}
