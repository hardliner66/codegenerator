﻿using System;
using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Irony.Parsing;
using RazorEngine;
using RazorEngine.Templating; // For extension methods.
using System.Runtime.Serialization;
using CommandLine;
using CommandLine.Text;

namespace m2svcgen
{
    public class Property
    {
        public string Name { get; }
        public string Type { get; }
        public bool List { get; }

        public ImmutableDictionary<string, string> Attributes { get; }
        public Property(string name, string type, bool list, ImmutableDictionary<string, string> attributes)
        {
            Name = name;
            Type = type;
            List = list;
            Attributes = attributes;
        }
    }

    public class Object
    {
        public string Name { get; }
        public ImmutableList<Property> Properties { get; }
        public ImmutableDictionary<string, string> Attributes { get; }
        public Object(string name, ImmutableList<Property> properties, ImmutableDictionary<string, string> attributes)
        {
            Name = name;
            Properties = properties;
            Attributes = attributes;
        }
    }

    public class Global
    {
        public ImmutableList<Object> Objects { get; }
        public string Namespace { get; }
        public Global(ImmutableList<Object> objects, string @namespace)
        {
            Objects = objects;
            Namespace = @namespace;
        }
    }

    public class Program
    {
        const string DEFAULT_OUTPUT = "<stdout>";
        public static string FileNameInternal { get; set; }

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
            public bool IsSet(ImmutableDictionary<string, string> attributes, string name)
            {
                return attributes.ContainsKey(name) && attributes[name].ToLower() == "true";
            }

            public string Get(ImmutableDictionary<string, string> attributes, string name, string defaultValue)
            {
                return attributes.ContainsKey(name) ? attributes[name] : defaultValue;
            }

            public string FileName { get { return FileNameInternal; } set { FileNameInternal = value; } }
        }

        public class MyCustomizedTemplate<T> : TemplateBase<T>
        {
            public new T Model
            {
                get { return base.Model; }
                //set { base.Model = value; }
            }

            public MyCustomizedTemplate() : base()
            {
                Helper = new Helper();
            }
            public Helper Helper { get; }
        }

        static ImmutableDictionary<string, string> getAttributes(ParseTreeNode node, int attributePosition)
        {
            var attributes = ImmutableDictionary<string, string>.Empty;
            if (node.ChildNodes[attributePosition].ChildNodes.Count > 0)
            {
                foreach (var attributeNode in node.ChildNodes[attributePosition].ChildNodes[0].ChildNodes[0].ChildNodes)
                {
                    if (attributeNode.ChildNodes[0].Term.Name == "attribute_flag")
                    {
                        attributes = attributes.Add(attributeNode.ChildNodes[0].ChildNodes[0].Token.Value.ToString(), "true");
                    }
                    else
                    {
                        attributes = attributes.Add(attributeNode.ChildNodes[0].ChildNodes[0].Token.Value.ToString(), attributeNode.ChildNodes[0].ChildNodes[1].Token.Value.ToString());
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
                        ImmutableList<Object> objectList = ImmutableList<Object>.Empty;
                        foreach (var objectNode in parseTree.Root.ChildNodes)
                        {
                            ImmutableDictionary<string, string> attributes = getAttributes(objectNode, 1);
                            ImmutableList<Property> properties = ImmutableList<Property>.Empty;

                            foreach (var propertyNode in objectNode.ChildNodes[2].ChildNodes)
                            {
                                string type;
                                bool list = false;
                                if (propertyNode.ChildNodes[1].ChildNodes[0].Term.Name == "list")
                                {
                                    list = true;
                                    type = propertyNode.ChildNodes[1].ChildNodes[0].ChildNodes[0].Token.Value.ToString();
                                }
                                else
                                {
                                    type = propertyNode.ChildNodes[1].ChildNodes[0].Token.Value.ToString();
                                }
                                var p = new Property(
                                    propertyNode.ChildNodes[0].Token.Value.ToString(),
                                    type,
                                    list,
                                    getAttributes(propertyNode, 2)
                                );

                                properties = properties.Add(p);
                            }

                            objectList = objectList.Add(new Object(objectNode.ChildNodes[0].Token.Value.ToString(), properties, attributes));
                        }
                        Global global = new Global(objectList, System.IO.Path.GetFileNameWithoutExtension(options.DataFile));

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
