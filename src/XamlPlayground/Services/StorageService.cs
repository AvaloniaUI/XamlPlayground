using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;

namespace XamlPlayground.Services;

internal static class StorageService
{
    public static FilePickerFileType All { get; } = new("All")
    {
        Patterns = new[] { "*.*" },
        MimeTypes = new[] { "*/*" }
    };

    public static FilePickerFileType Xaml { get; } = new("Xaml")
    {
        Patterns = new[] { "*.xaml" },
        AppleUniformTypeIdentifiers = new[] { "public.xaml" },
        MimeTypes = new[] { "application/xaml" }
    };

    public static FilePickerFileType Axaml { get; } = new("Axaml")
    {
        Patterns = new[] { "*.axaml" },
        AppleUniformTypeIdentifiers = new[] { "public.axaml" },
        MimeTypes = new[] { "application/axaml" }
    };

    public static FilePickerFileType CSharp { get; } = new("C#")
    {
        Patterns = new[] { "*.cs" },
        AppleUniformTypeIdentifiers = new[] { "public.csharp-source" },
        MimeTypes = new[] { "text/plain" }
    };

    public static IStorageProvider? GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
        {
            return window.StorageProvider;
        }

        if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime { MainView: { } mainView })
        {
            var visualRoot = mainView.GetVisualRoot();
            if (visualRoot is TopLevel topLevel)
            {
                return topLevel.StorageProvider;
            }
        }

        return null;
    }
}
