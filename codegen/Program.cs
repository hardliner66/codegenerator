using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Irony.Parsing;
using RazorEngine;
using RazorEngine.Templating; // For extension methods.
using System.Runtime.Serialization;
using CommandLine;
using CommandLine.Text;

namespace codegen
{
    public class Property
    {
        public string name { get; set; }
        public string type { get; set; }
        public Dictionary<string, string> attributes { get; set; } = new Dictionary<string, string>();
    }

    public class Object
    {
        public string name { get; set; }
        public List<Property> properties { get; set; } = new List<Property>();
        public Dictionary<string, string> attributes { get; set; } = new Dictionary<string, string>();
    }

    public class Global
    {
        public List<Object> objects { get; set; } = new List<Object>();
    }

    public class Program
    {
        const string DEFAULT_OUTPUT = "<stdout>";

        class Options
        {
            [Option('d', "data", Required = true,
              HelpText = "Data file to be processed.")]
            public string DataFile { get; set; }

            [Option('t', "template-dir", Required = true,
              HelpText = "Template Directory. (This directory should contain main.cshtml)")]
            public string TemplateDir { get; set; }

            [Option('o', "out", DefaultValue = DEFAULT_OUTPUT,
              HelpText = "Out file.")]
            public string OutputFile { get; set; }

            [ParserState]
            public IParserState LastParserState { get; set; }

            [HelpOption]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this,
                  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            }
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

        public class Helper
        {
            public bool IsSet(Dictionary<string, string> attributes, string name)
            {
                return attributes.ContainsKey(name) && attributes[name].ToLower() == "true";
            }

            public string Get(Dictionary<string, string> attributes, string name, string defaultValue)
            {
                return attributes.ContainsKey(name) ? attributes[name] : defaultValue;
            }
        }

        public class MyCustomizedTemplate<T> : TemplateBase<T>
        {
            public new T Model
            {
                get { return base.Model; }
                set { base.Model = value; }
            }

            public MyCustomizedTemplate(): base()
            {
                Helper = new Helper();
            }
            public Helper Helper { get; set; }
        }

        static Dictionary<string, string> getAttributes(ParseTreeNode node, int attributePosition)
        {
            var attributes = new Dictionary<string, string>();
            if (node.ChildNodes[attributePosition].ChildNodes.Count > 0)
            {
                foreach (var attributeNode in node.ChildNodes[attributePosition].ChildNodes)
                {
                    if (attributeNode.ChildNodes.Count == 1)
                    {
                        attributes.Add(attributeNode.ChildNodes[0].Token.Value.ToString(), "true");
                    }
                    else
                    {
                        attributes.Add(attributeNode.ChildNodes[0].Token.Value.ToString(), attributeNode.ChildNodes[1].Token.Value.ToString());
                    }
                }
            }
            return attributes;
        }

        static void Main(string[] args)
        {

            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                try
                {
                    var grammar = new ObjectGrammar();
                    Console.Clear();
                    //var language = new LanguageData(grammar);
                    var parser = new Irony.Parsing.Parser(grammar);

                    var text = System.IO.File.ReadAllText(options.DataFile);

                    var parseTree = parser.Parse(text);

                    if (parseTree.Status == ParseTreeStatus.Error)
                    {
                        foreach (var msg in parseTree.ParserMessages)
                        {
                            Console.WriteLine($"{msg.Message} at location: {msg.Location.ToUiString()}");
                        }
                    }
                    else
                    {
                        //printAst(parseTree, grammar.dispTree);
                        //Console.ReadLine();
                        //return;
                        Global global = new Global();
                        foreach (var objectNode in parseTree.Root.ChildNodes)
                        {
                            var @object = new Object() { name = objectNode.ChildNodes[0].Token.Value.ToString() };

                            @object.attributes = getAttributes(objectNode, 1);

                            foreach (var propertyNode in objectNode.ChildNodes[2].ChildNodes)
                            {
                                var p = new Property()
                                {
                                    name = propertyNode.ChildNodes[0].Token.Value.ToString(),
                                    type = propertyNode.ChildNodes[1].Token.Value.ToString()
                                };

                                p.attributes = getAttributes(propertyNode, 2);
                                @object.properties.Add(p);
                            }

                            global.objects.Add(@object);
                        }

                        var config = new RazorEngine.Configuration.TemplateServiceConfiguration();
                        config.DisableTempFileLocking = true;
                        config.EncodedStringFactory = new RazorEngine.Text.RawStringFactory();
                        config.CachingProvider = new DefaultCachingProvider(t => { });
                        config.BaseTemplateType = typeof(MyCustomizedTemplate<>);

                        var service = RazorEngineService.Create(config);

                        string path = options.TemplateDir;

                        foreach (var f in System.IO.Directory.EnumerateFiles(path, "*.cshtml"))
                        {
                            string template = System.IO.File.ReadAllText(f);

                            var name = System.IO.Path.GetFileNameWithoutExtension(f);

                            if (name != "main")
                            {
                                service.AddTemplate(name, template);
                            }
                        }

                        service.Compile(System.IO.File.ReadAllText(System.IO.Path.Combine(path, "main.cshtml")), "main", null);

                        var result = service.Run("main", null, global);

                        if (options.OutputFile == DEFAULT_OUTPUT)
                        {
                            Console.WriteLine(result);
                        }
                        else
                        {
                            System.IO.File.WriteAllText(options.OutputFile, result, System.Text.Encoding.UTF8);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.ReadLine();
            }
        }
    }
}
