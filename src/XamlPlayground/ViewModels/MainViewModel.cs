using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Octokit;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReactiveMarbles.PropertyChanged;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Diagnostics;
using XamlPlayground.Services;

namespace XamlPlayground.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<SampleViewModel> _samples;
    [ObservableProperty] private TextDocument _xaml;
    [ObservableProperty] private TextDocument _code;
    [ObservableProperty] private IControl? _control;
    [ObservableProperty] private bool _enableAutoRun;
    [ObservableProperty] private string? _lastErrorMessage;
    [ObservableProperty] private int _editorFontSize;
    private bool _update;
    private (Assembly? Assembly, AssemblyLoadContext? Context)? _previous;
    private IStorageFile? _openXamlFile;
    private IStorageFile? _openCodeFile;

    public MainViewModel()
    {
        _editorFontSize = 12;
        _samples = GetSamples(".xml");
        _enableAutoRun = true;

        _xaml = new TextDocument { Text = _samples.FirstOrDefault()?.Xaml };
        _xaml.TextChanged += async (_, _) => await Run(_xaml.Text, _code?.Text);

        _code = new TextDocument { Text = _samples.FirstOrDefault()?.Code };
        _code.TextChanged += async (_, _) => await Run(_xaml.Text, _code.Text);

        OpenXamlFileCommand = new AsyncRelayCommand(async () => await OpenXamlFile());

        SaveXamlFileCommand = new AsyncRelayCommand(async () => await SaveXamlFile());

        OpenCodeFileCommand = new AsyncRelayCommand(async () => await OpenCodeFile());

        SaveCodeFileCommand = new AsyncRelayCommand(async () => await SaveCodeFile());

        RunCommand = new AsyncRelayCommand(async () => await Run(_xaml.Text, _code.Text));

        GistCommand = new AsyncRelayCommand<string?>(Gist);

        this.WhenChanged(x => x.Xaml)
            .DistinctUntilChanged()
            .Where(x => x is not  null)
            .Subscribe(XamlChanged);

        this.WhenChanged(x => x.Code)
            .DistinctUntilChanged()
            .Where(x => x is not  null)
            .Subscribe(CodeChanged);

        this.WhenChanged(x => x.EnableAutoRun)
            .DistinctUntilChanged()
            .Subscribe(EnableAutoRunChanged);

        async void XamlChanged(TextDocument _)
        {
            if (_enableAutoRun && !_update)
            {
                _update = true;
                await Run(_xaml.Text, _code.Text);
                _update = false;
            }
        }

        async void CodeChanged(TextDocument _)
        {
            if (_enableAutoRun && !_update)
            {
                _update = true;
                await Run(_xaml.Text, _code.Text);
                _update = false;
            }
        }

        async void EnableAutoRunChanged(bool enableAutoRun)
        {
            if (enableAutoRun && !_update)
            {
                _update = true;
                await Run(_xaml.Text, _code.Text);
                _update = false;
            }
        }

    }

    public ICommand RunCommand { get; }

    public ICommand GistCommand { get; }

    public ICommand OpenXamlFileCommand { get; }

    public ICommand SaveXamlFileCommand { get; }

    public ICommand OpenCodeFileCommand { get; }

    public ICommand SaveCodeFileCommand { get; }

    private async Task<(string Xaml, string Code)> GetGistContent(string id)
    {
        var client = new GitHubClient(new ProductHeaderValue("XamlPlayground"));
        var gist = await client.Gist.Get(id);
        var xaml = gist.Files.FirstOrDefault(x => string.Compare(x.Key, "Main.axaml", StringComparison.OrdinalIgnoreCase) == 0).Value;
        var code = gist.Files.FirstOrDefault(x => string.Compare(x.Key, "Main.axaml.cs", StringComparison.OrdinalIgnoreCase) == 0).Value;
        return (xaml?.Content ?? "", code?.Content ?? "");
    }

    public async Task Gist(string? id)
    {
        if (id is null)
        {
            return;
        }
        try
        {
            var (xaml, code) = await GetGistContent(id);
            _xaml.Text = xaml;
            _code.Text = code;
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    private string? GetSampleName(string resourceName)
    {
        var parts = resourceName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return $"{parts[parts.Length - 2]}";
        }

        return null;
    }

    private string? LoadResourceString(string name)
    {
        var assembly = typeof(MainViewModel).Assembly;
        using var stream = assembly.GetManifestResourceStream(name);
        if (stream == null)
        {
            return null;
        }
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private ObservableCollection<SampleViewModel> GetSamples(string sampleExtension)
    {
        var samples = new ObservableCollection<SampleViewModel>();
        var assembly = typeof(MainViewModel).Assembly;
        var resourceNames = assembly.GetManifestResourceNames();

        samples.Add(new SampleViewModel("Code", Templates.s_xaml, Templates.s_code, Open));

        foreach (var resourceName in resourceNames)
        {
            if (!resourceName.EndsWith(sampleExtension, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (LoadResourceString(resourceName) is { } xaml)
            {
                if (GetSampleName(resourceName) is { } name)
                {
                    samples.Add(new SampleViewModel(name, xaml, string.Empty, Open));
                }
            }
        }

        return samples;
    }

    private async Task Open(string xaml, string? code)
    {
        Control = null;
        LastErrorMessage = null;

        _update = true;

        Xaml.Text = xaml;
        Code.Text = code;

        if (_enableAutoRun)
        {
            await Run(_xaml.Text, _code.Text);
        }

        _update = false;
    }

    private static List<FilePickerFileType> GetXamlFileTypes()
    {
        return new List<FilePickerFileType>
        {
            StorageService.Axaml,
            StorageService.Xaml,
            StorageService.All
        };
    }

    private static List<FilePickerFileType> GetCodeFileTypes()
    {
        return new List<FilePickerFileType>
        {
            StorageService.CSharp,
            StorageService.All
        };
    }

    private async Task Run(string? xaml, string? code)
    {
        try
        {
            // TODO: Unload previously loaded assembly.
            // _previous?.Context?.Unload();

            Assembly? scriptAssembly = null;

            if (code is { } && !string.IsNullOrWhiteSpace(code))
            {
                try
                {
                    _previous = await Task.Run(async () => await CompilerService.GetScriptAssembly(code));
                    if (_previous?.Assembly is { })
                    {
                        scriptAssembly = _previous?.Assembly;
                        Console.WriteLine($"Compiled assembly: {scriptAssembly?.GetName().Name}");
                    }
                    else
                    {
                        throw new Exception("Failed to compile code.");
                    }
                }
                catch (Exception exception)
                {
                    LastErrorMessage = exception.Message;
                    Console.WriteLine(exception);
                    return;
                }
            }

            var control = AvaloniaRuntimeXamlLoader.Parse<IControl?>(xaml, scriptAssembly);
            if (control is { })
            {
                Control = control;
                LastErrorMessage = null;
            }
        }
        catch (Exception exception)
        {
            LastErrorMessage = exception.Message;
            Console.WriteLine(exception);
        }
    }

    private async Task OpenXamlFile()
    {
        var storageProvider = StorageService.GetStorageProvider();
        if (storageProvider is null)
        {
            return;
        }

        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open xaml",
            FileTypeFilter = GetXamlFileTypes(),
            AllowMultiple = false
        });

        var file = result.FirstOrDefault();
        if (file is not null)
        {
            if (file.CanOpenRead)
            {
                try
                {
                    _openXamlFile = file;
                    await using var stream = await _openXamlFile.OpenReadAsync();
                    using var reader = new StreamReader(stream);
                    var fileContent = await reader.ReadToEndAsync();
                    await Open(fileContent, _code.Text);
                    reader.Dispose();
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
        }
    }

    private async Task SaveXamlFile()
    {
        if (_openXamlFile is null)
        {
            var storageProvider = StorageService.GetStorageProvider();
            if (storageProvider is null)
            {
                return;
            }

            var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save xaml",
                FileTypeChoices = GetXamlFileTypes(),
                SuggestedFileName = Path.GetFileNameWithoutExtension("playground"),
                DefaultExtension = "axaml",
                ShowOverwritePrompt = true
            });

            if (file is not null)
            {
                if (file.CanOpenWrite)
                {
                    try
                    {
                        _openXamlFile = file;
                        await using var stream = await _openXamlFile.OpenWriteAsync();
                        await using var writer = new StreamWriter(stream);
                        await writer.WriteAsync(_xaml.Text);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                    }
                }
            }
        }
        else if (_openXamlFile.CanOpenWrite)
        {
            await using var stream = await _openXamlFile.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(_xaml.Text);
        }
    }

    private async Task OpenCodeFile()
    {
        var storageProvider = StorageService.GetStorageProvider();
        if (storageProvider is null)
        {
            return;
        }

        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open code",
            FileTypeFilter = GetCodeFileTypes(),
            AllowMultiple = false
        });

        var file = result.FirstOrDefault();
        if (file is not null)
        {
            if (file.CanOpenRead)
            {
                try
                {
                    _openCodeFile = file;
                    await using var stream = await _openCodeFile.OpenReadAsync();
                    using var reader = new StreamReader(stream);
                    var fileContent = await reader.ReadToEndAsync();
                    await Open(_xaml.Text, fileContent);
                    reader.Dispose();
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
        }
    }

    private async Task SaveCodeFile()
    {
        if (_openCodeFile is null)
        {
            var storageProvider = StorageService.GetStorageProvider();
            if (storageProvider is null)
            {
                return;
            }

            var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save code",
                FileTypeChoices = GetCodeFileTypes(),
                SuggestedFileName = Path.GetFileNameWithoutExtension("playground"),
                DefaultExtension = "cs",
                ShowOverwritePrompt = true
            });

            if (file is not null)
            {
                if (file.CanOpenWrite)
                {
                    try
                    {
                        _openCodeFile = file;
                        await using var stream = await _openCodeFile.OpenWriteAsync();
                        await using var writer = new StreamWriter(stream);
                        await writer.WriteAsync(_code.Text);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                    }
                }
            }
        }
        else if (_openCodeFile.CanOpenWrite)
        {
            await using var stream = await _openCodeFile.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(_code.Text);
        }
    }
}
