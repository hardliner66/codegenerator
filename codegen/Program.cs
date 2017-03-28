using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Irony.Parsing;
using RazorEngine;
using RazorEngine.Templating; // For extension methods.
using CommandLine;
using CommandLine.Text;
using System.Reflection;
using System.Security.Policy;
using System.Security;
using System.Security.Permissions;
using System.IO;
using CSScriptLibrary;
using Newtonsoft.Json;

namespace Codegen
{
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

        //public string FileName { get { return FileNameInternal; } set { FileNameInternal = value; } }
    }

    public class CustomizedTemplate<T> : TemplateBase<T>
    {
        public new T Model
        {
            get { return base.Model; }
            //set { base.Model = value; }
        }

        public CustomizedTemplate() : base()
        {
            Helper = new Helper();
        }
        public Helper Helper { get; }

        Dictionary<string, dynamic> Functions { get; set; } = new Dictionary<string, dynamic>();
    }
    public class Property
    {
        public string Name { get; }
        public string Type { get; }
        public bool List { get; }
        public string Default { get; }

        public ImmutableDictionary<string, string> Attributes { get; }
        public Property(string name, string type, bool list, ImmutableDictionary<string, string> attributes, string default_value)
        {
            Name = name;
            Type = type;
            List = list;
            Attributes = attributes;
            Default = default_value;
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

    public class ParserConfig
    {
        public List<string> Primitives = new List<string> { "char", "string", "bool", "int", "double", "float", "int32", "int64" };
        public Dictionary<string, string> PrimitiveMapping = new Dictionary<string, string> { };
    }

    public class Program
    {
        const string DEFAULT_OUTPUT = "<stdout>";
        public static string FileNameInternal { get; set; }

        public static ParserConfig parserConfig = new ParserConfig();

        public static ParserConfig readParserConfig(string path)
        {
            var config = JsonConvert.DeserializeObject<ParserConfig>(File.ReadAllText(path));

            config.Primitives = config.Primitives.Select(x => x.ToLower()).ToList();

            var mapping = new Dictionary<string, string>();
            foreach(var kv in config.PrimitiveMapping)
            {
                mapping.Add(kv.Key.ToLower(), kv.Value);
            }

            config.PrimitiveMapping = mapping; 

            return config;
        }

        class Options
        {
            [Option('d', "data", Required = true,
              HelpText = "Data file to be processed.")]
            public string DataFile { get; set; }

            [Option('t', "template-dir", Required = true,
              HelpText = "Template Directory. (This directory should contain main.cshtml)")]
            public string TemplateDir { get; set; }

            [Option('u', "untyped", Required = false,
              HelpText = "Don't validate types")]
            public bool Untyped { get; set; }

            [Option('o', "out", DefaultValue = DEFAULT_OUTPUT,
              HelpText = "Out file.")]
            public string OutputFile { get; set; }

            [Option('@', Required = false)]
            public string TempDll { get; set; }

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
                        attributes = attributes.Add(attributeNode.ChildNodes[0].ChildNodes[0].Token.Value.ToString(), attributeNode.ChildNodes[0].ChildNodes[1].ChildNodes[0].Token.Value.ToString());
                    }
                }
            }
            return attributes;
        }

        static ImmutableDictionary<string, string> getAttributesForProperty(ParseTreeNode node, int attributePosition)
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
                        attributes = attributes.Add(attributeNode.ChildNodes[0].ChildNodes[0].Token.Value.ToString(), attributeNode.ChildNodes[0].ChildNodes[1].ChildNodes[0].Token.Value.ToString());
                    }
                }
            }
            return attributes;
        }

        static string getDefaultValue(ParseTreeNode node)
        {
            string default_value = "";
            if (node.ChildNodes[2].ChildNodes.Count > 0)
            {
                default_value = node.ChildNodes[2].ChildNodes[0].ChildNodes[0].ChildNodes[0].Token.Value.ToString();
            }
            return default_value;
        }

        static Object parseObjectNode(ParseTreeNode node)
        {

            ImmutableDictionary<string, string> attributes = getAttributes(node, 1);
            ImmutableList<Property> properties = ImmutableList<Property>.Empty;

            foreach (var propertyNode in node.ChildNodes[2].ChildNodes)
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
                if(parserConfig.PrimitiveMapping.ContainsKey(type.ToLower()))
                {
                    type = parserConfig.PrimitiveMapping[type.ToLower()];
                }
                var p = new Property(
                    propertyNode.ChildNodes[0].Token.Value.ToString(),
                    type,
                    list,
                    getAttributes(propertyNode, 3),
                    getDefaultValue(propertyNode)
                );

                properties = properties.Add(p);
            }
            return new Object(node.ChildNodes[0].Token.Value.ToString(), properties, attributes);
        }

        static string parseExternal(ParseTreeNode node)
        {
            return node.ChildNodes[0].Token.Value.ToString();
        }

        static bool validate(Global global, ImmutableList<string> externals, bool shouldValidate)
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
                        if (!types.Contains(p.Type.ToLower()) && !externals.Any(x => x.ToLower() == p.Type.ToLower()) && !parserConfig.Primitives.Contains(p.Type.ToLower()))
                        {
                            Console.WriteLine($"Type not defined for Property: {o.Name}.{p.Name}: {p.Type}");
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        static Assembly CompileHelper(string path, string asmPath)
        {
            CSScript.ShareHostRefAssemblies = true;
            CSScript.CompileCode(File.ReadAllText(path), asmPath, false);

            return Assembly.LoadFrom(asmPath);
        }

        static void Main(string[] args)
        {
            if (AppDomain.CurrentDomain.IsDefaultAppDomain())
            {
                // RazorEngine cannot clean up from the default appdomain...
                Console.WriteLine("Switching to secound AppDomain, for RazorEngine...");
                AppDomainSetup adSetup = new AppDomainSetup();
                adSetup.ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
                var current = AppDomain.CurrentDomain;
                // You only need to add strongnames when your appdomain is not a full trust environment.
                var strongNames = new StrongName[0];

                string generatedDll = System.IO.Path.GetTempFileName();

                List<string> arguments = new List<string>(args);
                arguments.Add("-@");
                arguments.Add(generatedDll);

                var domain = AppDomain.CreateDomain(
                    "MyMainDomain", null,
                    current.SetupInformation, new PermissionSet(PermissionState.Unrestricted),
                    strongNames);
                var exitCode = domain.ExecuteAssembly(Assembly.GetExecutingAssembly().Location, arguments.ToArray());
                // RazorEngine will cleanup. 
                AppDomain.Unload(domain);
                if (System.IO.File.Exists(generatedDll))
                {
                    System.IO.File.Delete(generatedDll);
                }
                return;
            }

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
                        ImmutableList<string> externalList = ImmutableList<string>.Empty;
                        foreach (var node in parseTree.Root.ChildNodes)
                        {
                            if (node.ChildNodes[0].Term.Name == "object")
                            {
                                objectList = objectList.Add(parseObjectNode(node.ChildNodes[0]));
                            }
                            else
                            {
                                externalList = externalList.Add(parseExternal(node.ChildNodes[0]));
                            }
                        }
                        Global global = new Global(objectList, System.IO.Path.GetFileNameWithoutExtension(options.DataFile));
                        string path = options.TemplateDir;

                        if (File.Exists(Path.Combine(path, "config.json")))
                        {
                            parserConfig = readParserConfig(Path.Combine(path, "config.json"));
                        }
                        else if (File.Exists("config.json"))
                        {
                            parserConfig = readParserConfig("config.json");
                        }

                        if (validate(global, externalList, !options.Untyped))
                        {
                            var config = new RazorEngine.Configuration.TemplateServiceConfiguration();
                            config.DisableTempFileLocking = true;
                            config.EncodedStringFactory = new RazorEngine.Text.RawStringFactory();
                            config.CachingProvider = new DefaultCachingProvider(t => { });

                            if (File.Exists(Path.Combine(path, "helper.cs")))
                            {
                                Assembly assembly = CompileHelper(Path.Combine(path, "helper.cs"), options.TempDll);

                                config.BaseTemplateType = assembly.GetType("Codegen.Template`1");
                            }
                            else
                            {
                                config.BaseTemplateType = typeof(CustomizedTemplate<>);
                            }

                            var service = RazorEngineService.Create(config);

                            foreach (var f in System.IO.Directory.EnumerateFiles(path, "*.cshtml"))
                            {
                                string template = System.IO.File.ReadAllText(f);

                                var name = System.IO.Path.GetFileNameWithoutExtension(f);

                                if (name.ToLower() != "main")
                                {
                                    service.AddTemplate(name, template);
                                }
                            }

                            service.Compile(File.ReadAllText(Path.Combine(path, "main.cshtml")), "main", null);

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
                        else
                        {
                            Console.WriteLine("Validation Failed!");
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
