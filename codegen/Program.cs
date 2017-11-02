using System;
using System.Linq;
using Irony.Parsing;
using CommandLine;
using CommandLine.Text;
using System.Reflection;
using System.IO;
using Codegen.DataModel;
using Codegen;
using System.Collections.Generic;

public class Program
{
    const string DEFAULT_OUTPUT = "<stdout>";
    const string DEFAULT_TEMPLATE = "main";
    public static string FileNameInternal { get; set; }

    class Options
    {
        [ValueOption(0)]
        public string File { get; set; }

        [Option('c', "config", Required = false, DefaultValue = "config.json",
          HelpText = "Parser configuration file.")]
        public string ConfigFile { get; set; }

        [Option('d', "dir", Required = false, DefaultValue = "",
          HelpText = "Template Directory. (This directory should contain the generator)")]
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

        [Option('r', "recursive", Required = false, DefaultValue = false,
            HelpText = "Recursively searches cdl files and generates output files beside them.")]
        public bool Recursive { get; set; }

        [Option('o', "out", Required = false, DefaultValue = "",
          HelpText = "Output directory.")]
        public string OutputDir { get; set; }

        [ValueList(typeof(List<string>))]
        public List<string> Args { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption()]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

    void Generate(Options options)
    {
        
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
                Assembly asm;
                
                if (!string.IsNullOrWhiteSpace(options.OutputDir))
                {
                    if (!Directory.Exists(options.OutputDir)) { Directory.CreateDirectory(options.OutputDir); }
                }

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
                                    if (options.Recursive)
                                    {
                                        foreach (var file in new DirectoryInfo(options.File).GetFiles("*.cdl", SearchOption.AllDirectories))
                                        {
                                            var global = DataParser.Parse(file.FullName, !options.Untyped, options.ConfigFile);
                                            var result = (GenerationResult)(m.Invoke(null, new object[] { global, options.Args is null ? new List<string> { } : options.Args }));

                                            var fullPath = Path.Combine(file.Directory.FullName, result.FileName);
                                            File.WriteAllText(fullPath, result.Content);

                                            if (!string.IsNullOrWhiteSpace(options.PostGeneration))
                                            {
                                                var process = new System.Diagnostics.Process();
                                                process.StartInfo.FileName = options.PostGeneration;
                                                process.StartInfo.Arguments = $"\"{fullPath}\"";
                                                process.Start();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var global = DataParser.Parse(options.File, !options.Untyped, options.ConfigFile);
                                        var result = (GenerationResult)(m.Invoke(null, new object[] { global, options.Args is null ? new List<string> { } : options.Args }));
                                        if (string.IsNullOrWhiteSpace(options.OutputDir))
                                        {
                                            Console.WriteLine(result.Content);
                                        }
                                        else
                                        {
                                            File.WriteAllText(Path.Combine(options.OutputDir, result.FileName), result.Content);
                                        }

                                        if (!string.IsNullOrWhiteSpace(options.PostGeneration))
                                        {
                                            var process = new System.Diagnostics.Process();
                                            process.StartInfo.FileName = options.PostGeneration;
                                            process.StartInfo.Arguments = $"\"{result.FileName}\"";
                                            process.Start();
                                        }
                                    }
                                    break;
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
