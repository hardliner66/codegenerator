using Irony.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Codegen
{
    class ObjectGrammar : Grammar
    {
        public ObjectGrammar() : base(false)
        {
            //var identifier = new RegexBasedTerminal("identifier", "[a-zA-Z][a-zA-Z0-9_]*", "list");
            IdentifierTerminal identifier = TerminalFactory.CreateCSharpIdentifier("Identifier");
            //var identifier = new RegexBasedTerminal("identifier", @"\b((?!list)[a-zA-Z0-9_])+\b");
            var value = new RegexBasedTerminal("name", @"\b[a-zA-Z0-9_]+\b");
            var str = new QuotedValueLiteral("value", "\"", TypeCode.String);

            var external = new NonTerminal("external");
            var line = new NonTerminal("line");

            var @object = new NonTerminal("object");
            var objectList = new NonTerminal("objectList");
            var properties = new NonTerminal("properties");
            var property = new NonTerminal("property");
            var attributeList_opt = new NonTerminal("attributeList_opt");
            var attributeList = new NonTerminal("attributeList");
            var attributes = new NonTerminal("attributes");
            var attribute = new NonTerminal("attribute");
            var attribute_kv = new NonTerminal("attribute_kv");
            var attribute_value = new NonTerminal("attribute_value");
            var attribute_flag = new NonTerminal("attribute_flag");
            var default_value = new NonTerminal("default_value");
            var default_value_opt = new NonTerminal("default_value_opt");
            var type = new NonTerminal("type");
            var list = new NonTerminal("list");
            var comma = ToTerm(",", "comma");

            CommentTerminal SingleLineComment = new CommentTerminal("SingleLineComment", "//", "\r", "\n", "\u2085", "\u2028", "\u2029");
            CommentTerminal DelimitedComment = new CommentTerminal("DelimitedComment", "/*", "*/");
            NonGrammarTerminals.Add(SingleLineComment);
            NonGrammarTerminals.Add(DelimitedComment);

            external.Rule = ToTerm("external") + identifier + attributeList_opt;

            line.Rule = external | @object;

            attribute_value.Rule = value | str;

            attribute_kv.Rule = identifier + "=" + attribute_value;
            attribute_flag.Rule = identifier;
            attribute.Rule = attribute_kv | attribute_flag;
            attributes.Rule = MakePlusRule(attributes, comma, attribute);

            attributeList.Rule = "[" + attributes + "]";
            attributeList_opt.Rule = Empty | attributeList;

            list.Rule = "List" + identifier;
            type.Rule = identifier | list;

            default_value.Rule = ToTerm("=") + attribute_value;
            default_value_opt.Rule = Empty | default_value;

            property.Rule = identifier + ":" + type + default_value_opt + attributeList_opt;

            properties.Rule = MakePlusRule(properties, property);

            @object.Rule = ToTerm("object") + identifier + attributeList_opt + "{" + properties + "}";

            objectList.Rule = MakePlusRule(objectList, line);

            Root = objectList;

            MarkPunctuation("=", "[", "]", ":", "{", "}", ";", "list", "object", "external");
        }

        public bool isValid(string sourceCode, Grammar grammar)
        {
            LanguageData language = new LanguageData(grammar);
            Parser parser = new Parser(language);
            ParseTree parseTree = parser.Parse(sourceCode);
            ParseTreeNode root = parseTree.Root;
            return root != null;
        }

        public ParseTreeNode getRoot(string sourceCode, Grammar grammar)
        {
            LanguageData language = new LanguageData(grammar);
            Parser parser = new Parser(language);
            ParseTree parseTree = parser.Parse(sourceCode);
            ParseTreeNode root = parseTree.Root;
            return root;
        }

        public void dispTree(ParseTreeNode node, int level)
        {
            for (int i = 0; i < level; i++)
                Console.Write("  ");
            Console.WriteLine(node);

            foreach (ParseTreeNode child in node.ChildNodes)
                dispTree(child, level + 1);
        }
    }
}
