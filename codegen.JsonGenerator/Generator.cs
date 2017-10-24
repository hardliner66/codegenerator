using Codegen.DataModel;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace codegen.JsonGenerator
{
    [Codegen.Generator]
    public class Generator
    {
        public static void Execute(Global g, DirectoryInfo directory, List<string> args)
        {
            string output = JsonConvert.SerializeObject(g, Formatting.Indented);
            if (string.IsNullOrWhiteSpace(directory.FullName))
            {
                Console.WriteLine(output);
            }
            else
            {
                System.IO.File.WriteAllText(System.IO.Path.Combine(directory.FullName, $"{g.Namespace}.json"), output);
            }
        }
    }
}
