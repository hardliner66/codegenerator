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
        public static GenerationResult Execute(Global g, List<string> args)
        {
            return new GenerationResult()
            {
                Content = JsonConvert.SerializeObject(g, Formatting.Indented),
                FileName = $"{g.Namespace}.json"
            };
        }
    }
}
