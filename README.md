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
        public static GenerationResult Execute(Namespace n, List<string> args)
        {
            // insert generation code here

            // do not write the generated code to a file yourself
            // just return the code inside the generation result
            // the framework will handle directory creation
            // and writing of the file
        }
    }
 ```
 - Copy compiled dll into the same directory as codegen.exe


To use the generator, pass the name of the dll without extension as argument to codegen  
e.g.: If you have a generator in the file MyGenerator.dll, you can call it with:
```
codegen -g MyGenerator test.data
```

[Syntax Usage Examples](https://bitbucket.org/hardliner66/codegenerator/downloads/Syntax.pdf)
