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

        [Option('p', "pretty", Required = false, DefaultValue = false,
          HelpText = "Pretty prints the input file")]
        public bool Pretty { get; set; }

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

    public static Namespace Sort(Namespace n)
    {
        n.Objects = n.Objects.Select((o) =>
        {
            o.Properties.Sort((p1, p2) => p1.Name.CompareTo(p2.Name));
            return o;
        }).ToList();
        n.Objects.Sort((x, y) => x.Name.CompareTo(y.Name));

        return n;
    }
    public static bool Compare(string obj, int index, Dictionary<string, string> g1, Dictionary<string, string> g2)
    {
        if (g1.Count != g2.Count)
        {
            Console.WriteLine($"{obj}[{index}].Attributes.Count mismatch!");
            return false;
        }
        foreach (var kv in g1)
        {
            if (!g2.ContainsKey(kv.Key))
            {
                Console.WriteLine($"{obj}[{index}].Attributes[\"{kv.Key}\"].Key mismatch!");
                return false;
            }
            if (g2[kv.Key] != kv.Value)
            {
                Console.WriteLine($"{obj}[{index}].Attributes[\"{kv.Key}\"].Value mismatch!");
                return false;
            }
        }
        return true;
    }

    public static bool Compare(Namespace n1, Namespace n2)
    {
        if (n1.Name != n2.Name)
        {
            Console.WriteLine("Namespace mismatch!");
            return false;
        }

        if (n1.ExternalTypes.Count != n2.ExternalTypes.Count)
        {
            Console.WriteLine("ExternalTypes.Count mismatch!");
            return false;
        }
        for (var i = 0; i < n1.ExternalTypes.Count; i++)
        {
            if (n1.ExternalTypes[i].Name != n2.ExternalTypes[i].Name)
            {
                Console.WriteLine($"ExternalTypes[{i}].Name mismatch!");
                return false;
            }
            if (!Compare("ExternalTypes", i, n1.ExternalTypes[i].Attributes, n2.ExternalTypes[i].Attributes))
            {
                return false;
            }
        }


        if (n1.Objects.Count != n2.Objects.Count)
        {
            Console.WriteLine("Objects.Count mismatch!");
            return false;
        }

        for (var i = 0; i < n1.Objects.Count; i++)
        {
            if (n1.Objects[i].Name != n2.Objects[i].Name)
            {
                Console.WriteLine($"Objects[{i}].Name mismatch!");
                return false;
            }

            if (!Compare("Objects", i, n1.Objects[i].Attributes, n2.Objects[i].Attributes))
            {
                return false;
            }

            if (n1.Objects[i].Properties.Count != n2.Objects[i].Properties.Count)
            {
                Console.WriteLine($"Objects[{i}].Properties.Count mismatch!");
                return false;
            }

            for (var n = 0; n < n1.Objects[i].Properties.Count; n++)
            {
                if (n1.Objects[i].Properties[n].Name != n2.Objects[i].Properties[n].Name)
                {
                    Console.WriteLine($"Objects[{i}].Properties[{n}].Name mismatch!");
                    return false;
                }

                if (n1.Objects[i].Properties[n].Optional != n2.Objects[i].Properties[n].Optional)
                {
                    Console.WriteLine($"Objects[{i}].Properties[{n}].Optional mismatch!");
                    return false;
                }

                if (n1.Objects[i].Properties[n].Default != n2.Objects[i].Properties[n].Default)
                {
                    Console.WriteLine($"Objects[{i}].Properties[{n}].Default mismatch!");
                    return false;
                }

                if (n1.Objects[i].Properties[n].Type.Name != n2.Objects[i].Properties[n].Type.Name)
                {
                    Console.WriteLine($"Objects[{i}].Properties[{n}].Type.Name mismatch!");
                    return false;
                }

                if (n1.Objects[i].Properties[n].Type.IsPrimitive != n2.Objects[i].Properties[n].Type.IsPrimitive)
                {
                    Console.WriteLine($"Objects[{i}].Properties[{n}].Type.IsPrimitive mismatch!");
                    return false;
                }

                if (n1.Objects[i].Properties[n].Type.IsList != n2.Objects[i].Properties[n].Type.IsList)
                {
                    Console.WriteLine($"Objects[{i}].Properties[{n}].Type.IsList mismatch!");
                    return false;
                }

                if (!Compare($"Objects[{i}].Properties", n, n1.Objects[i].Properties[n].Attributes, n1.Objects[i].Properties[n].Attributes))
                {
                    return false;
                }
            }
        }
        return true;
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
                if (options.Pretty)
                {
                    if (options.Recursive)
                    {
                        foreach (var file in new DirectoryInfo(options.File).GetFiles("*.cdl", SearchOption.AllDirectories))
                        {
                            var originalText = File.ReadAllText(file.FullName);
                            var original = Sort(DataParser.Parse(file.FullName, "", "Format"));
                            var result = DataParser.PrettyPrint(file.FullName);

                            File.WriteAllText(file.FullName, result);
                            try
                            {
                                var afterFormat = Sort(DataParser.Parse(file.FullName, "", "Format"));

                                if (!Compare(original, afterFormat))
                                {
                                    Console.WriteLine($"Formatting changed the content of the file. This is a bug, please open an issue. ({file.FullName})");
                                    File.WriteAllText(file.FullName, originalText);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error while formatting. File cannot be parsed. This is a bug, please open an issue. ({file.FullName}) {ex.Message}");
                                File.WriteAllText(file.FullName, originalText);
                            }
                        }
                    }
                    else
                    {
                        var originalText = File.ReadAllText(options.File);
                        var original = Sort(DataParser.Parse(options.File, "", "Format"));
                        var result = DataParser.PrettyPrint(options.File);

                        File.WriteAllText(options.File, result);
                        try
                        {
                            var afterFormat = Sort(DataParser.Parse(options.File, "", "Format"));

                            if (!Compare(original, afterFormat))
                            {
                                Console.WriteLine($"Formatting changed the content of the file. This is a bug, please open an issue. ({options.File})");
                                File.WriteAllText(options.File, originalText);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error while formatting. File cannot be parsed. This is a bug, please open an issue. ({options.File}) {ex.Message}");
                            File.WriteAllText(options.File, originalText);
                        }
                    }
                }
                else
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
                                                var global = DataParser.Parse(file.FullName, options.ConfigFile, options.Generator);
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
                                            var global = DataParser.Parse(options.File, options.ConfigFile, options.Generator);
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
