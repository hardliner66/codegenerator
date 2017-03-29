using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codegen
{

    public class ObjectWithAttributes
    {
        public ImmutableDictionary<string, string> Attributes { get; }
        public ObjectWithAttributes(ImmutableDictionary<string, string> attributes)
        {
            Attributes = attributes;
        }
    }

    public class Property : ObjectWithAttributes
    {
        public string Name { get; }
        public string Type { get; }
        public bool List { get; }
        public string Default { get; }

        public Property(string name, string type, bool list, ImmutableDictionary<string, string> attributes, string default_value) : base(attributes)
        {
            Name = name;
            Type = type;
            List = list;
            Default = default_value;
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
