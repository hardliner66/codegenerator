using Irony.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codegen
{
    class ObjectGrammar: Grammar
    {
        public ObjectGrammar(): base(false)
        {
            var identifier = new RegexBasedTerminal("[a-zA-Z][a-zA-Z0-9_]*");
            var value = new RegexBasedTerminal("[a-zA-Z0-9_]+");

            var @object = new NonTerminal("object");
            var objectList = new NonTerminal("objectList");
            var properties = new NonTerminal("properties");
            var property = new NonTerminal("property");
            var attributes = new NonTerminal("attributes");
            var attribute = new NonTerminal("attribute");
            var comma = ToTerm(",", "comma");

            attribute.Rule = (identifier + "=" + value) | identifier;
            attributes.Rule = MakeStarRule(attributes, comma, attribute) ;

            property.Rule = identifier + ":" + identifier + attributes;

            properties.Rule = MakePlusRule(properties, property);

            @object.Rule = identifier + attributes + "{" + properties + "}";

            objectList.Rule = MakePlusRule(objectList, @object);

            Root = objectList;

            MarkPunctuation("=", "[", "]", ":", "{", "}", ";");
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
