using System;
using System.Runtime.InteropServices.JavaScript;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using XamlPlayground.ViewModels;
using XamlPlayground.Views;

namespace XamlPlayground.Wasm;

internal static partial class Interop
{
    [JSExport]
    internal static void Gist(string? id)
    {
        Console.WriteLine($"Gist: {id}");

        if (id is { } && !string.IsNullOrEmpty(id) && id.Length > 1)
        {
            if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime single)
            {
                if (single.MainView is MainView mainView)
                {
                    if (mainView.DataContext is MainViewModel vm)
                    {
                        vm.Gist(id.TrimStart("/".ToCharArray()));
                    }
                }
            }
        }
    }

    [JSImport("getBaseUri", "interop.js")]
    public static partial string GetBaseUri();
}
