using Irony.Parsing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Codegen.DataModel;
using System.Threading.Tasks;

namespace Codegen
{
    public class DataParser
    {
        public static Global Parse(string path, bool shouldValidate, string config, string generator)
        {
            if (File.Exists(Path.Combine(Path.GetDirectoryName(path), config)))
            {
                parserConfig = readParserConfig(Path.Combine(Path.GetDirectoryName(path), config));
            }
            else if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"config.{generator}.json")))
            {
                parserConfig = readParserConfig(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"config.{generator}.json"));
            }
            else if (File.Exists(config))
            {
                parserConfig = readParserConfig(config);
            }

            var grammar = new ObjectGrammar();
            Console.Clear();
            //var language = new LanguageData(grammar);
            var parser = new Parser(grammar);

            var text = File.ReadAllText(path);

            var parseTree = parser.Parse(text);

            if (parseTree.Status == ParseTreeStatus.Error)
            {
                var sb = new StringBuilder();
                foreach (var msg in parseTree.ParserMessages)
                {
                    sb.AppendLine($"{msg.Message} at location: {msg.Location.ToUiString()}");
                }
                throw new DataParserException($"Parsing failed:\n\n{sb.ToString()}");
            }
            else
            {
                //printAst(parseTree, grammar.dispTree);
                //Console.ReadLine();
                //return;
                var objectList = new List<DataModel.Object>();
                var externalList = new List<TypeDeclaration>();
                foreach (var node in parseTree.Root.ChildNodes)
                {
                    if (node.ChildNodes[0].Term.Name == "object")
                    {
                        objectList.Add(parseObjectNode(node.ChildNodes[0]));
                    }
                    else
                    {
                        externalList.Add(parseExternal(node.ChildNodes[0]));
                    }
                }
                var global = new Global(objectList, Path.GetFileNameWithoutExtension(path), externalList);

                if (!validate(global, externalList, shouldValidate)) {
                    throw new DataParserException("Validation failed!");
                }

                return global;
            }
        }

        public class DataParserException: ApplicationException
        {
            public DataParserException(string msg): base(msg) { }
        }

        public class ParserConfig
        {
            public List<string> Primitives = new List<string> { "char", "string", "bool", "int", "double", "float", "int32", "int64" };
            public Dictionary<string, string> TypeMapping = new Dictionary<string, string> { };
        }
        public static ParserConfig parserConfig = new ParserConfig();

        public static ParserConfig readParserConfig(string path)
        {
            var config = JsonConvert.DeserializeObject<ParserConfig>(File.ReadAllText(path));

            config.Primitives = config.Primitives.Select(x => x.ToLower()).ToList();

            var mapping = new Dictionary<string, string>();
            foreach (var kv in config.TypeMapping)
            {
                mapping.Add(kv.Key.ToLower(), kv.Value);
            }

            config.TypeMapping = mapping;

            return config;
        }

        delegate void PrintFunction(ParseTreeNode node, int level);

        static void printAst(ParseTree output, PrintFunction print)
        {
            if (output.Status == ParseTreeStatus.Error)
            {
                foreach (var msg in output.ParserMessages)
                {
                    Console.WriteLine(msg);
                }
            }
            else
            {
                print(output.Root, 0);
            }
        }

        static Dictionary<string, string> getAttributes(ParseTreeNode node, int attributePosition)
        {
            var attributes = new Dictionary<string, string>();
            if (node.ChildNodes[attributePosition].ChildNodes.Count > 0)
            {
                foreach (var attributeNode in node.ChildNodes[attributePosition].ChildNodes[0].ChildNodes[0].ChildNodes)
                {
                    if (attributeNode.ChildNodes[0].Term.Name == "attribute_flag")
                    {
                        attributes.Add(attributeNode.ChildNodes[0].ChildNodes[0].Token.Value.ToString(), "true");
                    }
                    else
                    {
                        attributes.Add(attributeNode.ChildNodes[0].ChildNodes[0].Token.Value.ToString(), attributeNode.ChildNodes[0].ChildNodes[1].ChildNodes[0].Token.Value.ToString());
                    }
                }
            }
            return attributes;
        }

        static string getDefaultValue(ParseTreeNode node)
        {
            string default_value = "";
            if (node.ChildNodes[3].ChildNodes.Count > 0)
            {
                default_value = node.ChildNodes[3].ChildNodes[0].ChildNodes[0].ChildNodes[0].Token.Value.ToString();
            }
            return default_value;
        }

        static DataModel.Object parseObjectNode(ParseTreeNode node)
        {

            Dictionary<string, string> attributes = getAttributes(node, 1);
            var properties = new List<Property>();

            foreach (var propertyNode in node.ChildNodes[2].ChildNodes)
            {
                string typeName;
                bool list = false;
                if (propertyNode.ChildNodes[2].ChildNodes[0].Term.Name == "list")
                {
                    list = true;
                    typeName = propertyNode.ChildNodes[2].ChildNodes[0].ChildNodes[0].Token.Value.ToString();
                }
                else
                {
                    typeName = propertyNode.ChildNodes[2].ChildNodes[0].Token.Value.ToString();
                }
                if (parserConfig.TypeMapping.ContainsKey(typeName.ToLower()))
                {
                    typeName = parserConfig.TypeMapping[typeName.ToLower()];
                }

                var pt = new PropertyType(typeName, list, parserConfig.Primitives.Contains(typeName.ToLower()));

                var defaultValue = getDefaultValue(propertyNode);

                var p = new Property(
                    propertyNode.ChildNodes[1].Token.Value.ToString(),
                    pt,
                    getAttributes(propertyNode, 4),
                    defaultValue,
                    !string.IsNullOrWhiteSpace(defaultValue) || propertyNode.ChildNodes[0].ChildNodes.Count > 0
                );

                properties.Add(p);
            }
            return new DataModel.Object(node.ChildNodes[0].Token.Value.ToString(), properties, attributes);
        }

        static TypeDeclaration parseExternal(ParseTreeNode node)
        {
            return new TypeDeclaration(node.ChildNodes[0].Token.Value.ToString(), getAttributes(node, 1));
        }

        static bool validate(Global global, List<TypeDeclaration> externals, bool shouldValidate)
        {
            if (shouldValidate)
            {
                List<string> types = new List<string>();
                foreach (var o in global.Objects)
                {
                    if (types.Contains(o.Name.ToLower()))
                    {
                        Console.WriteLine($"Duplicate Type: {o.Name}");
                        return false;
                    }
                    else
                    {
                        types.Add(o.Name.ToLower());
                    }
                }

                foreach (var o in global.Objects)
                {
                    foreach (var p in o.Properties)
                    {
                        if (!types.Contains(p.Type.Name.ToLower()) && !externals.Any(x => x.Name.ToLower() == p.Type.Name.ToLower()) && !parserConfig.Primitives.Contains(p.Type.Name.ToLower()))
                        {
                            Console.WriteLine($"Type not defined for Property: {o.Name}.{p.Name}: {p.Type}");
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}
