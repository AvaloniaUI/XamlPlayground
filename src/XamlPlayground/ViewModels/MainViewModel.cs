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
using ReactiveUI;
using Avalonia.Markup.Xaml;
using Octokit;
using AvaloniaEdit.Document;

namespace XamlPlayground.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private static string s_code =
            "using Avalonia;\n" +
            "using Avalonia.Controls;\n" +
            "using Avalonia.Markup.Xaml;\n" +
            "\n" +
            "namespace XamlPlayground.Views\n" +
            "{\n" +
            "    public class SampleView : UserControl\n" +
            "    {\n" +
            "        public SampleView()\n" +
            "        {\n" +
            "            InitializeComponent();\n" +
            "        }\n" +
            "\n" +
            "        private void InitializeComponent()\n" +
            "        {\n" +
            "            // AvaloniaXamlLoader.Load(this);\n" +
            "        }\n" +
            "\n" +
            "        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)\n" +
            "        {\n" +
            "            var count = 0;\n" +
            "            var button = this.Find<Button>(\"button\");\n" +
            "            button.Click += (sender, e) => button.Content = $\"Clicked: {++count}\";\n" +
            "            base.OnAttachedToVisualTree(e);\n" +
            "        }\n" +
            "    }\n" +
            "}\n";

        private static string s_playground = 
            "<Grid xmlns=\"https://github.com/avaloniaui\"\n" +
#if ENABLE_CODE
            "      xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
            "      x:Class=\"XamlPlayground.Views.SampleView\">\n" +
            "    <Button Name=\"button\" Content=\"Click Me\" HorizontalAlignment=\"Center\" />\n" +
#else
            "      xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">\n" +
            "\n" +
#endif
            "</Grid>";

        private int _editorFontSize;
        private ObservableCollection<SampleViewModel> _samples;
        private readonly TextDocument _xaml;
        private readonly TextDocument _code;
        private IControl? _control;
        private bool _enableAutoRun;
        private string? _lastErrorMessage;
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
            RunCommand = ReactiveCommand.CreateFromTask(async () => await Run(_xaml.Text, _code.Text));

            GistCommand = ReactiveCommand.CreateFromTask<string>(Gist);

            this.WhenAnyValue(x => x.Xaml)
                .WhereNotNull()
                .Subscribe(XamlChanged);

            this.WhenAnyValue(x => x.Code)
                .WhereNotNull()
                .Subscribe(CodeChanged);

            this.WhenAnyValue(x => x.EnableAutoRun)
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

        public ObservableCollection<SampleViewModel> Samples
        {
            get => _samples;
            set => this.RaiseAndSetIfChanged(ref _samples, value);
        }

        public int EditorFontSize
        {
            get => _editorFontSize;
            set => this.RaiseAndSetIfChanged(ref _editorFontSize, value);
        }

        public IControl? Control
        {
            get => _control;
            set => this.RaiseAndSetIfChanged(ref _control, value);
        }

        public TextDocument Xaml
        {
            get => _xaml;
        }

        public TextDocument Code
        {
            get => _code;
        }

        public bool EnableAutoRun
        {
            get => _enableAutoRun;
            set => this.RaiseAndSetIfChanged(ref _enableAutoRun, value);
        }

        public string? LastErrorMessage
        {
            get => _lastErrorMessage;
            set => this.RaiseAndSetIfChanged(ref _lastErrorMessage, value);
        }

        public ICommand OpenFileCommand { get; }
        public ICommand RunCommand { get; }

        public ICommand GistCommand { get; }

        private async Task<(string Xaml,string Code)> GetGistContent(string id)
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

            samples.Add(new SampleViewModel("Playground", s_playground, s_code, Open));

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
            var ofd = new OpenFileDialog();
            var result = await ofd.ShowAsync(new Window());
            if (result is not null)
            {
                string filePath = String.Join("", result);
                var fileContent = File.ReadLines(filePath);
                await Open(String.Join("\n", fileContent), "File");
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
