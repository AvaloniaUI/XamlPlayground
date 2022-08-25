﻿using System;
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

namespace XamlPlayground.ViewModels
{
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

        public MainViewModel()
        {
            _editorFontSize = 12;
            _samples = GetSamples(".xml");
            _enableAutoRun = true;

            _xaml = new TextDocument { Text = _samples.FirstOrDefault()?.Xaml };
            _xaml.TextChanged += async (_, _) => await Run(_xaml.Text, _code?.Text);

            _code = new TextDocument { Text = _samples.FirstOrDefault()?.Code };
            _code.TextChanged += async (_, _) => await Run(_xaml.Text, _code.Text);

            OpenFileCommand = ReactiveCommand.CreateFromTask(async () => await OpenFile());

            SaveFileCommand = ReactiveCommand.CreateFromTask(async () => await SaveFile());

            RunCommand = ReactiveCommand.CreateFromTask(async () => await Run(_xaml.Text, _code.Text));

            GistCommand = new AsyncRelayCommand<string>(Gist);

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

#if ENABLE_CODE
        public bool EnableCode { get; } = true;
#else
        public bool EnableCode { get; } = false;
#endif

        public ICommand RunCommand { get; }

        public ICommand GistCommand { get; }

        private async Task<(string Xaml, string Code)> GetGistContent(string id)
        {
            var client = new GitHubClient(new ProductHeaderValue("XamlPlayground"));
            var gist = await client.Gist.Get(id);
            var xaml = gist.Files.FirstOrDefault(x => string.Compare(x.Key, "Main.axaml", StringComparison.OrdinalIgnoreCase) == 0).Value;
            var code = gist.Files.FirstOrDefault(x => string.Compare(x.Key, "Main.axaml.cs", StringComparison.OrdinalIgnoreCase) == 0).Value;
            return (xaml?.Content ?? "", code?.Content ?? "");
        }

        public async Task Gist(string id)
        {
            try
            {
                var gist = await GetGistContent(id);
                _xaml.Text = gist.Xaml;
                _code.Text = gist.Code;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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

            samples.Add(new SampleViewModel("Playground", Templates.s_playground, Templates.s_code, Open));

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

        private async Task OpenFile()
        {
            var storageProvider = StorageService.GetStorageProvider();
            if (storageProvider is null)
            {
                return;
            }

            var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open project",
                FileTypeFilter = GetFileTypes(),
                AllowMultiple = false
            });

            var file = result.FirstOrDefault();

            if (file is not null)
            {
                _openFile = file;
                if (_openFile.CanOpenRead)
                {
                    try
                    {
                        await using var stream = await _openFile.OpenReadAsync();
                        using var reader = new StreamReader(stream);
                        var fileContent = await reader.ReadToEndAsync();
                        await Open(fileContent, "");
                        reader.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        Debug.WriteLine(ex.StackTrace);
                    }
                }
            }
        }

        private async Task SaveFile()
        {
            if (_openFile is null)
            {
                var storageProvider = StorageService.GetStorageProvider();
                if (storageProvider is null)
                {
                    return;
                }

                var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save project",
                    FileTypeChoices = GetFileTypes(),
                    SuggestedFileName = Path.GetFileNameWithoutExtension("project"),
                    DefaultExtension = "axaml",
                    ShowOverwritePrompt = true
                });

                if (file is not null)
                {
                    _openFile = file;
                    if (_openFile.CanOpenWrite)
                    {
                        try
                        {
                            await using var stream = await _openFile.OpenWriteAsync();
                            await using var writer = new StreamWriter(stream);
                            await writer.WriteAsync(_xaml.Text);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            Debug.WriteLine(ex.StackTrace);
                        }
                    }
                }
            }
            else if (_openFile.CanOpenWrite)
            {
                await using var stream = await _openFile.OpenWriteAsync();
                await using var writer = new StreamWriter(stream);
                await writer.WriteAsync(_xaml.Text);
            }
        }

        private async Task Run(string? xaml, string? code)
        {
            try
            {
                _previous?.Context?.Unload();

                Assembly? scriptAssembly = null;
#if ENABLE_CODE
                if (code is { } && !string.IsNullOrWhiteSpace(code) && !Compiler.IsBrowser())
                {
                    try
                    {
                        _previous = await Task.Run(() => Compiler.GetScriptAssembly(code));
                        if (_previous?.Assembly is { })
                        {
                            scriptAssembly = _previous?.Assembly;
                        }
                        else
                        {
                            throw new Exception("Failed to compile code.");
                        }
                    }
                    catch (Exception e)
                    {
                        LastErrorMessage = e.Message;
                        Console.WriteLine(e);
                        return;
                    }
                }
#endif
                var control = AvaloniaRuntimeXamlLoader.Parse<IControl?>(xaml, scriptAssembly);
                if (control is { })
                {
                    Control = control;
                    LastErrorMessage = null;
                }
            }
            catch (Exception e)
            {
                LastErrorMessage = e.Message;
                Console.WriteLine(e);
            }
        }
    }
}
