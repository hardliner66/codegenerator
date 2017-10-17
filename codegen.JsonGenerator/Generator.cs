using Codegen.DataModel;
using System;
using Newtonsoft.Json;

namespace codegen.JsonGenerator
{
    public class Generator
    {
        public static void Execute(Global g, string path)
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
