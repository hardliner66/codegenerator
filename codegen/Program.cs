using System;
using System.Collections.Generic;
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
using System.Dynamic;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;
using System.Security.Policy;
using System.Security;
using System.Security.Permissions;

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
                                    getAttributes(propertyNode, 3),
                                    getDefaultValue(propertyNode)
                                );

                                properties = properties.Add(p);
                            }

                            objectList = objectList.Add(new Object(objectNode.ChildNodes[0].Token.Value.ToString(), properties, attributes));
                        }
                        Global global = new Global(objectList, System.IO.Path.GetFileNameWithoutExtension(options.DataFile));
                        
                        string path = options.TemplateDir;

                        var config = new RazorEngine.Configuration.TemplateServiceConfiguration();
                        config.DisableTempFileLocking = true;
                        config.EncodedStringFactory = new RazorEngine.Text.RawStringFactory();
                        config.CachingProvider = new DefaultCachingProvider(t => { });



                        if (System.IO.File.Exists(System.IO.Path.Combine(path, "helper.cs")))
                        {
                            CSharpCodeProvider provider = new CSharpCodeProvider();
                            CompilerParameters parameters = new CompilerParameters();
                            
                            parameters.ReferencedAssemblies.Add(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "RazorEngine.dll"));
                            parameters.ReferencedAssemblies.Add(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "System.Collections.Immutable.dll"));
                            parameters.ReferencedAssemblies.Add(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "codegen.exe"));
                            parameters.ReferencedAssemblies.Add("System.Runtime.dll");
                            // True - memory generation, false - external file generation
                            parameters.GenerateInMemory = true;
                            // True - exe file generation, false - dll file generation
                            parameters.GenerateExecutable = false;

                            parameters.OutputAssembly = options.TempDll; // ;

                            CompilerResults results = provider.CompileAssemblyFromSource(parameters, System.IO.File.ReadAllText(System.IO.Path.Combine(path, "helper.cs")));
                            if (results.Errors.HasErrors)
                            {
                                StringBuilder sb = new StringBuilder();

                                foreach (CompilerError error in results.Errors)
                                {
                                    sb.AppendLine(String.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText));
                                }

                                throw new InvalidOperationException(sb.ToString());
                            }
                            Assembly assembly = results.CompiledAssembly;
                            Type program = assembly.GetType("Codegen.Template`1");
                            
                            Assembly.LoadFrom(options.TempDll);
                            config.BaseTemplateType = program;
                        } else
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
