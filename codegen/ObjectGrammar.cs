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
            var value = new RegexBasedTerminal("name", @"\b[a-zA-Z0-9_\.]+\b");
            var str = new QuotedValueLiteral("value", "\"", TypeCode.String);
            var num = new NumberLiteral("num", NumberOptions.IntOnly);

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
            var intAttributeList = new NonTerminal("intAttributeList");
            var intAttributes = new NonTerminal("intAttributes");
            var intAttribute = new NonTerminal("intAttribute");
            var intAttribute_kv = new NonTerminal("intAttribute_kv");
            var optional_flag = new NonTerminal("optional_flag");
            var optional_flag_opt = new NonTerminal("optional_flag_opt");
            var default_value = new NonTerminal("default_value");
            var default_value_opt = new NonTerminal("default_value_opt");
            var type = new NonTerminal("type");
            var comma = ToTerm(",", "comma");
            var comment = new RegexBasedTerminal("comment", @"\/\/.*");
            var comment_opt = new NonTerminal("comment")
            {
                Rule = Empty | comment
            };


            var list = new NonTerminal("list");
            var @enum = new NonTerminal("enum");
            var dict = new NonTerminal("dict");

            @enum.Rule = ToTerm("enum") + identifier + intAttributeList;

            external.Rule = ToTerm("external") + identifier + attributeList_opt;

            line.Rule = comment | @enum | external | @object;
          
            intAttribute_kv.Rule = identifier + "=" + num;
            intAttribute.Rule = intAttribute_kv | attribute_flag;
            intAttributes.Rule = MakePlusRule(intAttributes, comma, intAttribute);

            intAttributeList.Rule = "[" + intAttributes + "]";
            

            attribute_value.Rule = value | str;

            attribute_kv.Rule = identifier + "=" + attribute_value;
            attribute_flag.Rule = identifier;
            attribute.Rule = attribute_kv | attribute_flag;
            attributes.Rule = MakePlusRule(attributes, comma, attribute);

            attributeList.Rule = "[" + attributes + "]";
            attributeList_opt.Rule = Empty | attributeList;

            list.Rule = "List" + identifier;
            dict.Rule = "Map" + identifier + identifier;
            type.Rule = identifier | list | dict;

            default_value.Rule = ToTerm("=") + attribute_value;
            default_value_opt.Rule = Empty | default_value;

            optional_flag.Rule = "?";
            optional_flag_opt.Rule = Empty | optional_flag;

            property.Rule = optional_flag_opt + identifier + ":" + type + default_value_opt + attributeList_opt + comment_opt;

            properties.Rule = MakeStarRule(properties, property);

            @object.Rule = ToTerm("object") + identifier + attributeList_opt + "{" + comment_opt + properties + "}" + comment_opt;

            objectList.Rule = MakePlusRule(objectList, line);

            Root = objectList;

            MarkPunctuation("=", "[", "]", ":", "{", "}", "//", "?", ";", "list", "map", "object", "external");
        }

        public bool IsValid(string sourceCode, Grammar grammar)
        {
            LanguageData language = new LanguageData(grammar);
            Parser parser = new Parser(language);
            ParseTree parseTree = parser.Parse(sourceCode);
            ParseTreeNode root = parseTree.Root;
            return root != null;
        }

        public ParseTreeNode GetRoot(string sourceCode, Grammar grammar)
        {
            LanguageData language = new LanguageData(grammar);
            Parser parser = new Parser(language);
            ParseTree parseTree = parser.Parse(sourceCode);
            ParseTreeNode root = parseTree.Root;
            return root;
        }

        public void DispTree(ParseTreeNode node, int level)
        {
            for (int i = 0; i < level; i++)
                Console.Write("  ");
            Console.WriteLine(node);

            foreach (ParseTreeNode child in node.ChildNodes)
                DispTree(child, level + 1);
        }
    }
}
