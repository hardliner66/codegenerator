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
    public static class Shared
    {
        public static Global Global;
    }

    public class Helper
    {
        public Global GetModel()
        {
            return Shared.Global;
        }

        public bool IsSet(ImmutableDictionary<string, string> attributes, string name)
        {
            return attributes.ContainsKey(name) && attributes[name].ToLower() == "true";
        }

        public T Get<T>(ImmutableDictionary<string, string> attributes, string name, T defaultValue)
        {
            if (attributes.ContainsKey(name))
            {
                try
                {
                    return (T)Convert.ChangeType(attributes[name], typeof(T));
                }
                catch
                {
                }
            }
            return defaultValue;
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

    public class Program
    {
        const string DEFAULT_OUTPUT = "<stdout>";
        const string DEFAULT_TEMPLATE = "main";
        public static string FileNameInternal { get; set; }

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

            [Option('b', "base-template", DefaultValue = DEFAULT_TEMPLATE,
              HelpText = "Out file.")]
            public string BaseTemplate { get; set; }

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

                string generatedDll = Path.GetTempFileName();

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
                if (File.Exists(generatedDll))
                {
                    File.Delete(generatedDll);
                }
                return;
            }

            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                try
                {
                    string path = options.TemplateDir;

                    Shared.Global = DataParser.Parse(options.DataFile, !options.Untyped);
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

                    foreach (var f in Directory.EnumerateFiles(path, "*.cshtml"))
                    {
                        string template = File.ReadAllText(f);

                        var name = Path.GetFileNameWithoutExtension(f);

                        if (name.ToLower() != options.BaseTemplate)
                        {
                            service.AddTemplate(name, template);
                        }
                    }

                    service.Compile(File.ReadAllText(Path.Combine(path, options.BaseTemplate + ".cshtml")), options.BaseTemplate, null);

                    var result = service.Run(options.BaseTemplate, null, Shared.Global);

                    if (options.OutputFile == DEFAULT_OUTPUT)
                    {
                        Console.WriteLine(result);
                    }
                    else
                    {
                        File.WriteAllText(options.OutputFile, result, System.Text.Encoding.UTF8);
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
