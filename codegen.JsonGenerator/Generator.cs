using Codegen.DataModel;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace codegen.JsonGenerator
{
    [Codegen.Generator]
    public class Generator
    {
        public static void Execute(Global g, string path, List<string> args)
        {
            string output = JsonConvert.SerializeObject(g, Formatting.Indented);
            if (string.IsNullOrWhiteSpace(path))
            {
                Console.WriteLine(output);
            }
            else
            {
                System.IO.File.WriteAllText(path, output);
            }
        }
    }
}
