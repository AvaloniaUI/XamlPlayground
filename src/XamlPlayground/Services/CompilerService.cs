using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Loader;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace XamlPlayground.Services;

public static class CompilerService
{
    private static PortableExecutableReference[]? s_references;

    public static string? BaseUri { get; set; }

    [UnconditionalSuppressMessage("Trimming", "IL3000")]
    private static void LoadReferences()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        var appDomainReferences = new List<PortableExecutableReference>();

        foreach(var assembly in assemblies)
        {
            if (!string.IsNullOrWhiteSpace(assembly.Location))
            {
                appDomainReferences.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
            else
            {
                unsafe
                {
                    if (assembly.TryGetRawMetadata(out var blob, out var length))
                    {
                        var moduleMetadata = ModuleMetadata.CreateFromMetadata((IntPtr)blob, length);
                        var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
                        appDomainReferences.Add(assemblyMetadata.GetReference());
                    }
                }
            }
        }

        s_references = appDomainReferences.ToArray();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026")]
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
                Debug.WriteLine(error);
            }

            return (null, null);
        }

        ms.Seek(0, SeekOrigin.Begin);

        var context = new AssemblyLoadContext(name: Path.GetRandomFileName(), isCollectible: true);
        var assembly = context.LoadFromStream(ms);

        return (assembly, context);
    }
}
