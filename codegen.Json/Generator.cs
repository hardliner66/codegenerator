using Codegen.DataModel;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace codegen.Json
{
    [Codegen.Generator]
    public class Generator
    {
        public static GenerationResult Execute(Namespace n, List<string> args)
        {
            return new GenerationResult()
            {
                Content = JsonConvert.SerializeObject(n, Formatting.Indented),
                FileName = $"{n.Name}.json"
            };
        }
    }
}
