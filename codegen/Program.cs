using System;
using System.Collections.Immutable;
using System.Linq;
using Irony.Parsing;
using CommandLine;
using CommandLine.Text;
using System.Reflection;
using System.IO;
using Codegen.DataModel;
using Codegen;

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
    }
}

public class Program
{
    const string DEFAULT_OUTPUT = "<stdout>";
    const string DEFAULT_TEMPLATE = "main";
    public static string FileNameInternal { get; set; }

    class Options
    {
        [ValueOption(0)]
        public string File { get; set; }

        [Option('o', "out", Required = false, DefaultValue = "",
          HelpText = "Output file.")]
        public string Output { get; set; }

        [Option('d', "dir", Required = false, DefaultValue = "",
          HelpText = "Template Directory. (This directory should contain main.cshtml)")]
        public string TemplateDir { get; set; }

        [Option('u', "untyped", Required = false, DefaultValue = false,
          HelpText = "Don't validate types")]
        public bool Untyped { get; set; }

        [Option('g', "generator", Required = false, DefaultValue = "codegen.JsonGenerator",
          HelpText = "The generator to use.")]
        public string Generator { get; set; }

        [Option('p', "post-generation", Required = false, DefaultValue = "",
          HelpText = "Command to execute after generation.")]
        public string PostGeneration { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption()]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

    static void Main(string[] args)
    {
        var options = new Options();
        if (CommandLine.Parser.Default.ParseArguments(args, options))
        {
            if (string.IsNullOrWhiteSpace(options.TemplateDir))
            {
                options.TemplateDir = AppDomain.CurrentDomain.BaseDirectory;
            }

            try
            {
                Shared.Global = DataParser.Parse(options.File, !options.Untyped);

                Assembly asm;

                if (File.Exists(Path.Combine(options.TemplateDir, options.Generator + ".dll")))
                {
                    asm = Assembly.LoadFrom(Path.Combine(options.TemplateDir, options.Generator + ".dll"));
                    
                    foreach (var t in asm.GetTypes())
                    {
                        if (t.IsDefined(typeof(Codegen.Generator)))
                        {
                            foreach (var m in t.GetMethods())
                            {
                                if (m.Name.ToLower() == "execute")
                                {
                                    m.Invoke(null, new object[] { Shared.Global, options.Output });
                                }
                            }
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Generator not found.");
                    Console.WriteLine(options.GetUsage());
                }


                if (!string.IsNullOrWhiteSpace(options.PostGeneration))
                {
                    var process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = options.PostGeneration;
                    process.StartInfo.Arguments = $"\"{options.Output}\"";
                    process.Start();
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
