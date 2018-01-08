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
        class SyntaxRoot
        {
            public List<SyntaxElement> Elements = new List<SyntaxElement>();
        }

        abstract class SyntaxElement
        {

        }

        class SyntaxComment : SyntaxElement
        {
            public string Text;
        }

        class SyntaxEmptyLine : SyntaxElement { }

        class SyntaxProperty : SyntaxElement
        {
            public string Comment = "";
            public string Name = "";
            public string Type = "";
            public string Default = "";
            public bool List = false;
            public bool Optional = false;
            public string Attributes = "";
        }

        class SyntaxObject : SyntaxElement
        {
            public string HeaderComment = "";
            public string FooterComment = "";
            public string Name = "";
            public string Attributes = "";
            public List<SyntaxElement> Elements = new List<SyntaxElement>();
        }

        class SyntaxExternalType : SyntaxElement
        {
            public string Name = "";
            public string Attributes = "";
            public string Comment = "";
        }

        public static string PrettyPrint(string path)
        {
            var text = File.ReadAllText(path);
            var grammar = new ObjectGrammar();
            var parser = new Parser(grammar);


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
                var root = new SyntaxRoot();

                int i = 0;
                while (i < parseTree.Tokens.Count)
                {
                    if (parseTree.Tokens[i].Category == TokenCategory.Comment)
                    {
                        root.Elements.Add(new SyntaxComment() { Text = parseTree.Tokens[i].Text });
                    }


                    if (parseTree.Tokens[i].Terminal.Name.ToLower() == "object")
                    {
                        i++;

                        SyntaxObject obj = new SyntaxObject()
                        {
                            Name = parseTree.Tokens[i].Text
                        };
                        i++;

                        if (parseTree.Tokens[i].Text == "[")
                        {
                            i++;

                            string prefix = "";
                            while (parseTree.Tokens[i].Terminal.Name != "]")
                            {
                                if (parseTree.Tokens[i].Terminal.Name == "comma")
                                {
                                    i++;
                                }

                                obj.Attributes += $"{prefix} {parseTree.Tokens[i].Text.Trim()}";
                                i++;

                                if (parseTree.Tokens[i].Terminal.Name == "=")
                                {
                                    obj.Attributes += " = ";
                                    i++;
                                    obj.Attributes += parseTree.Tokens[i].Text;
                                    i++;
                                }

                                prefix = ",";
                            }
                            i++;
                            obj.Attributes = "[" + obj.Attributes.Trim() + "]";
                        }
                        int headerLine = parseTree.Tokens[i].Location.Line;
                        i++;

                        while (parseTree.Tokens[i].Text != "}")
                        {
                            if (parseTree.Tokens[i].Category == TokenCategory.Comment)
                            {
                                if (headerLine == parseTree.Tokens[i].Location.Line)
                                {
                                    obj.HeaderComment = parseTree.Tokens[i].Text;
                                }
                                else
                                {
                                    obj.Elements.Add(new SyntaxComment() { Text = parseTree.Tokens[i].Text });
                                }
                            }

                            if (parseTree.Tokens[i].Terminal.Name.ToLower() == "identifier" || parseTree.Tokens[i].Terminal.Name.ToLower() == "?")
                            {

                                SyntaxProperty prop = new SyntaxProperty()
                                {
                                    Optional = (parseTree.Tokens[i].Terminal.Name.ToLower() == "?"),
                                    Name = parseTree.Tokens[i + ((parseTree.Tokens[i].Terminal.Name.ToLower() == "?") ? 1 : 0)].Text
                                };

                                int propLine = parseTree.Tokens[i].Location.Line;

                                if (prop.Optional)
                                {
                                    i++;
                                }
                                i++;
                                i++;
                                if (parseTree.Tokens[i].Terminal.Name.ToLower() == "list")
                                {
                                    prop.List = true;
                                    i++;
                                }
                                prop.Type = parseTree.Tokens[i].Text;
                                i++;

                                if (parseTree.Tokens[i].Terminal.Name == "=")
                                {
                                    i++;
                                    prop.Default = parseTree.Tokens[i].Text;
                                    prop.Optional = true;
                                    i++;
                                }

                                if (parseTree.Tokens[i].Terminal.Name == "[")
                                {
                                    i++;

                                    string prefix = "";
                                    while (parseTree.Tokens[i].Terminal.Name != "]")
                                    {
                                        if (parseTree.Tokens[i].Terminal.Name == "comma")
                                        {
                                            i++;
                                        }

                                        prop.Attributes += $"{prefix} {parseTree.Tokens[i].Text.Trim()}";
                                        i++;

                                        if (parseTree.Tokens[i].Terminal.Name == "=")
                                        {
                                            prop.Attributes += " = ";
                                            i++;
                                            prop.Attributes += parseTree.Tokens[i].Text;
                                            i++;
                                        }

                                        prefix = ",";
                                    }
                                    i++;
                                    prop.Attributes = "[" + prop.Attributes.Trim() + "]";
                                }

                                if (parseTree.Tokens[i].Category == TokenCategory.Comment)
                                {
                                    if (parseTree.Tokens[i].Location.Line == propLine)
                                    {
                                        prop.Comment = parseTree.Tokens[i].Text;
                                        obj.Elements.Add(prop);
                                    }
                                    else
                                    {
                                        obj.Elements.Add(prop);
                                        obj.Elements.Add(new SyntaxComment()
                                        {
                                            Text = parseTree.Tokens[i].Text
                                        });
                                    }
                                    i++;
                                }
                                else
                                {
                                    obj.Elements.Add(prop);
                                }
                            }
                        }
                        if (parseTree.Tokens[i + 1].Category == TokenCategory.Comment)
                        {
                            if (parseTree.Tokens[i].Location.Line == parseTree.Tokens[i + 1].Location.Line)
                            {
                                obj.FooterComment = parseTree.Tokens[i + 1].Text;
                                root.Elements.Add(obj);
                            }
                            else
                            {
                                root.Elements.Add(obj);
                                root.Elements.Add(new SyntaxComment()
                                {
                                    Text = parseTree.Tokens[i + 1].Text
                                });
                            }
                            i++;
                        }
                        else
                        {
                            root.Elements.Add(obj);
                        }
                    }

                    if (parseTree.Tokens[i].Terminal.Name.ToLower() == "external")
                    {
                        i++;
                        SyntaxExternalType ext = new SyntaxExternalType()
                        {
                            Name = parseTree.Tokens[i].Text
                        };
                        i++;
                        if (parseTree.Tokens[i].Terminal.Name == "[")
                        {
                            i++;

                            string prefix = "";
                            while (parseTree.Tokens[i].Terminal.Name != "]")
                            {
                                if (parseTree.Tokens[i].Terminal.Name == "comma")
                                {
                                    i++;
                                }

                                ext.Attributes += $"{prefix} {parseTree.Tokens[i].Text.Trim()}";
                                i++;

                                if (parseTree.Tokens[i].Terminal.Name == "=")
                                {
                                    ext.Attributes += " = ";
                                    i++;
                                    ext.Attributes += parseTree.Tokens[i].Text;
                                    i++;
                                }

                                prefix = ",";
                            }
                            i++;
                            ext.Attributes = "[" + ext.Attributes.Trim() + "]";
                        }

                        if (parseTree.Tokens[i].Category == TokenCategory.Comment)
                        {
                            ext.Comment = parseTree.Tokens[i].Text;
                            i++;
                        }
                        root.Elements.Add(ext);
                    }

                    i++;
                }

                var result = new StringBuilder();

                foreach (var element in root.Elements)
                {
                    if (element is SyntaxComment comment)
                    {
                        result.AppendLine(comment.Text.Trim());
                    }
                    else if (element is SyntaxObject obj)
                    {
                        if (string.IsNullOrWhiteSpace(obj.Attributes))
                        {
                            result.Append($"object {obj.Name.Trim()} {{");
                        }
                        else
                        {
                            result.Append($"object {obj.Name.Trim()} {obj.Attributes.Trim()} {{");
                        }

                        if (!string.IsNullOrWhiteSpace(obj.HeaderComment.Trim()))
                        {
                            result.AppendLine($" {obj.HeaderComment.Trim()}");
                        }
                        else
                        {
                            result.AppendLine();
                        }

                        var props = obj.Elements.Where(e => e is SyntaxProperty).Select(e => (SyntaxProperty)e);

                        var nameLength = props.Max(p => p.Name.Length + (p.Optional ? 2 : 0));
                        var typeLength = props.Max(p => p.Type.Length + (p.List ? 5 : 0));
                        var defaultLength = props.Max(p => p.Default.Trim().Length);
                        var hasDefault = props.Any(p => !string.IsNullOrWhiteSpace(p.Default));
                        var attrLength = props.Max(p => p.Attributes.Length);

                        foreach (var innerElement in obj.Elements)
                        {
                            if (innerElement is SyntaxComment innerComment)
                            {
                                result.AppendLine("  " + innerComment.Text.Trim());
                            }
                            else if (innerElement is SyntaxProperty p)
                            {

                                var propString = new StringBuilder();
                                if (p.Optional)
                                {
                                    propString.Append($"? {p.Name}".PadRight(nameLength));
                                }
                                else
                                {
                                    propString.Append($"{p.Name}".PadRight(nameLength));
                                }
                                propString.Append(" : ");
                                if (p.List)
                                {
                                    propString.Append($"List {p.Type}".PadRight(typeLength));
                                }
                                else
                                {
                                    propString.Append($"{p.Type}".PadRight(typeLength));
                                }

                                if (!string.IsNullOrWhiteSpace(p.Default.Trim()))
                                {
                                    propString.Append(" = ");
                                    propString.Append(p.Default.Trim().PadRight(defaultLength));
                                }
                                else
                                {
                                    propString.Append("".PadRight(defaultLength + (hasDefault ? 3 : 0)));
                                }
                                if (attrLength > 0)
                                {
                                    propString.Append($" {p.Attributes.PadRight(attrLength)}");
                                }
                                propString.Append(" " + p.Comment);
                                result.AppendLine("  " + propString.ToString().Trim());
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(obj.FooterComment))
                        {
                            result.AppendLine($"}} {obj.FooterComment.Trim()}");
                        }
                        else
                        {
                            result.AppendLine($"}}");
                        }
                        result.AppendLine();
                    }
                }


                foreach (var element in root.Elements)
                {
                    if (element is SyntaxExternalType ext)
                    {

                        if (string.IsNullOrWhiteSpace(ext.Attributes))
                        {
                            result.Append($"external {ext.Name.Trim()}");
                        }
                        else
                        {
                            result.Append($"external {ext.Name.Trim()} {ext.Attributes.Trim()}");
                        }

                        if (!string.IsNullOrWhiteSpace(ext.Comment.Trim()))
                        {
                            result.AppendLine($" {ext.Comment.Trim()}");
                        }
                        else
                        {
                            result.AppendLine();
                        }
                    }
                }
                //result.AppendLine();

                return result.ToString();
            }
        }

        public static Namespace Parse(string path, string config, string generator)
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
                var global = new Namespace(objectList, Path.GetFileNameWithoutExtension(path), externalList);

                if (!parserConfig.Untyped)
                {
                    if (!validate(global, externalList))
                    {
                        throw new DataParserException("Validation failed!");
                    }
                }

                return global;
            }
        }

        public class DataParserException : ApplicationException
        {
            public DataParserException(string msg) : base(msg) { }
        }

        public class ParserConfig
        {
            public bool Untyped = false;
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

        static bool validate(Namespace n, List<TypeDeclaration> externals)
        {
            List<string> types = new List<string>();
            foreach (var o in n.Objects)
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

            foreach (var o in n.Objects)
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
            return true;
        }
    }
}
