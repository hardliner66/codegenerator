# Creating a generator
 - Create a new Dll Project
 - Add reference to codegen.DataModel
 - Add the following class
 ```cs
    using System.Collections.Generic;
    using Codegen.DataModel;

    [Codegen.Generator]
    public class GeneratorName
    {
        public static GenerationResult Execute(Global g, List<string> args)
        {
            // insert generation code here

            // Do not write the generated code yourself, let the framework handle that for you
        }
    }
 ```
 - Copy compiled dll into the same directory as codegen.exe

#### Warning: Global is likely to be renamed


To use the generator, pass the name of the dll without extension as argument to codegen  
e.g.: If you have a generator in the file MyGenerator.dll, you can call it with:
```
codegen -g MyGenerator test.data
```
