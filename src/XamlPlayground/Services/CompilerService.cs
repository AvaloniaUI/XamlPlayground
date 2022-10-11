using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace XamlPlayground.Services;

public static class CompilerService
{
    private static PortableExecutableReference[]? s_references;

    public static string? BaseUri { get; set; }

    private static async Task LoadReferences()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        if (Utilities.IsBrowser())
        {
            if (BaseUri is null)
            {
                return;
            }
            
            var appDomainReferences = new List<PortableExecutableReference>();
            var client = new HttpClient 
            {
                // BaseAddress = new Uri(BaseUri)
            };

            Console.WriteLine($"Loading references BaseUri: {BaseUri}");

            foreach(var reference in assemblies.Where(x => !x.IsDynamic))
            {
                try
                {
                    var name = reference.GetName().Name;
                    var requestUri = $"{BaseUri}managed/{name}.dll";
                    Console.WriteLine($"Loading reference requestUri: {requestUri}, FullName: {reference.FullName}");
                    var stream = await client.GetStreamAsync(requestUri);
                    appDomainReferences.Add(MetadataReference.CreateFromStream(stream));
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
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

    public static async Task<(Assembly? Assembly, AssemblyLoadContext? Context)> GetScriptAssembly(string code)
    {
        if (s_references is null)
        {
            await LoadReferences();
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
        var assembly = context.LoadFromStream(ms);

        return (assembly, context);
    }
}
