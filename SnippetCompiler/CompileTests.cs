// Copyright Gradientspace Corp. All Rights Reserved.
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Xml.Schema;

namespace SnippetCompiler
{
    public static class CompileTests
    {

        // this is based on https://laurentkempe.com/2019/02/18/dynamically-compile-and-run-code-using-dotNET-Core-3.0/,
        // uses Microsoft.CodeAnalysis which is the Roslyn thing

        // also worth looking at:
        // https://github.com/RickStrahl/Westwind.Scripting / https://weblog.west-wind.com/posts/2022/Jun/07/Runtime-CSharp-Code-Compilation-Revisited-for-Roslyn


        public static void TestLiveCompile()
        {
            string FilePath = "C:\\tempcode\\test_code.cs";
            string SourceCode = File.ReadAllText(FilePath);

            byte[]? CompiledAssembly = null;
            using (var peStream = new MemoryStream())
            {
                var result = GenerateCode(SourceCode).Emit(peStream);

                if (!result.Success)
                {
                    Console.WriteLine("Compilation done with error.");

                    var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (var diagnostic in failures)
                    {
                        Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                }
                else
                {
                    Console.WriteLine("Compilation done without any error.");
                    peStream.Seek(0, SeekOrigin.Begin);
                    CompiledAssembly = peStream.ToArray();
                }
            }

            if (CompiledAssembly == null)
                return;


            string[] args = new[] { "Argument1" };
            var assemblyLoadContextWeakRef = LoadAndExecute(CompiledAssembly, args);

            // why 8? this is weird...
            for (var i = 0; i < 8 && assemblyLoadContextWeakRef.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Console.WriteLine(assemblyLoadContextWeakRef.IsAlive ? "Unloading failed!" : "Unloading success!");
        }



        private static CSharpCompilation GenerateCode(string sourceCode)
        {
            var codeString = SourceText.From(sourceCode);
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp11);

            var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
            };
            Assembly.GetEntryAssembly()?.GetReferencedAssemblies().ToList()
                .ForEach(a => references.Add(MetadataReference.CreateFromFile(Assembly.Load(a).Location)));

            return CSharpCompilation.Create("Hello.dll",
                new[] { parsedSyntaxTree },
                references: references,
                //options: new CSharpCompilationOptions(OutputKind.ConsoleApplication,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference LoadAndExecute(byte[] compiledAssembly, string[] args)
        {
            using (var asm = new MemoryStream(compiledAssembly))
            {
                var assemblyLoadContext = new SimpleUnloadableAssemblyLoadContext();

                var assembly = assemblyLoadContext.LoadFromStream(asm);

                Type[] Types = assembly.GetExportedTypes();
                if (Types[0].Name == "DynamicCode1")
                {
                    MethodInfo? StaticMethod = Types[0].GetMethod("ComputeAdd");
                    if (StaticMethod != null && StaticMethod.IsStatic)
                    {
                        ParameterInfo[] parameters = StaticMethod.GetParameters();
                        object[] parametersArray = new object[] { 1, 2 };
                        object? ReturnValue = StaticMethod.Invoke(null, parametersArray);
                        if ( ReturnValue != null )
                        {
                            Type ReturnType = StaticMethod.ReturnType;
                            if ( ReturnType == typeof(int) )
                            {
                                int IntValue = (int)ReturnValue;
                                System.Console.WriteLine("Dynamic Code function returned " + IntValue);
                            }
                        }
                    }

                }

                // this is for executing main? in an executable assembly?
                //var entry = assembly.EntryPoint;
                //_ = (entry != null && entry.GetParameters().Length > 0)
                //    ? entry.Invoke(null, new object[] { args })
                //    : entry.Invoke(null, null);

                assemblyLoadContext.Unload();

                return new WeakReference(assemblyLoadContext);
            }
        }

    }



    internal class SimpleUnloadableAssemblyLoadContext : AssemblyLoadContext
    {
        public SimpleUnloadableAssemblyLoadContext()
            : base(true)
        {
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return null;
        }
    }

}
