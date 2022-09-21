using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace XamlPlayground;

internal static class Compiler
{
    private static PortableExecutableReference[]? s_references;

    private static void LoadReferences()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        if (IsBrowser())
        {
            var appDomainReferences = new List<PortableExecutableReference>();
            var client = new HttpClient();

            foreach(var reference in assemblies.Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location)))
            {
                Console.WriteLine(reference.Location);
                var stream = client.GetStreamAsync("/_framework/_bin/" + reference.Location).Result;
                appDomainReferences.Add(MetadataReference.CreateFromStream(stream));
            }

            s_references = appDomainReferences.ToArray();
        }
        else
        {
            var appDomainReferences = new List<PortableExecutableReference>();

            foreach(var reference in assemblies.Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location)))
            {
                appDomainReferences.Add(MetadataReference.CreateFromFile(reference.Location));
            }

            s_references = appDomainReferences.ToArray();
        }
    }

    public static bool IsBrowser()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER"));
    }

    public static (Assembly? Assembly, AssemblyLoadContext? Context) GetScriptAssembly(string code)
    {
        if (s_references is null)
        {
            LoadReferences();
        }

        var stringText = SourceText.From(code, Encoding.UTF8);
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(stringText, parseOptions);
        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithOptimizationLevel(OptimizationLevel.Release);
        var compilation = CSharpCompilation.Create(Path.GetRandomFileName(), new[] { parsedSyntaxTree }, s_references, compilationOptions);

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);
        var errors = result.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error);
        if (!result.Success)
        {
            foreach (var error in errors)
            {
                Console.WriteLine(error);
            }

            return (null, null);
        }

        ms.Seek(0, SeekOrigin.Begin);

        var context = new AssemblyLoadContext(name: Path.GetRandomFileName(), isCollectible: true);
        return (context.LoadFromStream(ms), context);
    }
}
