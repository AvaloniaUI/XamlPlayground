using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
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
            //"      xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">\n" +
            "      xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
            "      x:Class=\"XamlPlayground.Views.SampleView\">\n" +
            "    <Button Name=\"button\" Content=\"Click Me\" HorizontalAlignment=\"Center\" />\n" +
            //"\n" +
            "</Grid>";

        private ObservableCollection<SampleViewModel> _samples;
        private readonly TextDocument _xaml;
        private readonly TextDocument _code;
        private IControl? _control;
        private bool _enableAutoRun;
        private string? _lastErrorMessage;
        private bool _update;

        public MainViewModel()
        {
            _samples = GetSamples(".xml");
            _enableAutoRun = true;

            _xaml = new TextDocument { Text = _samples.FirstOrDefault()?.Xaml };
            _xaml.TextChanged += (_, _) => Run(_xaml.Text, _code?.Text);

            _code = new TextDocument { Text = _samples.FirstOrDefault()?.Code };
            _code.TextChanged += (_, _) => Run(_xaml.Text, _code.Text);

            RunCommand = ReactiveCommand.Create(() => Run(_xaml.Text, _code.Text));

            GistCommand = ReactiveCommand.CreateFromTask<string>(Gist);

            this.WhenAnyValue(x => x.Xaml)
                .WhereNotNull()
                .Subscribe(_ =>
                {
                    if (_enableAutoRun && !_update)
                    {
                        _update = true;
                        Run(_xaml.Text, _code.Text);
                        _update = false;
                    }
                });

            this.WhenAnyValue(x => x.Code)
                .WhereNotNull()
                .Subscribe(_ =>
                {
                    if (_enableAutoRun && !_update)
                    {
                        _update = true;
                        Run(_xaml.Text, _code.Text);
                        _update = false;
                    }
                });
            
            this.WhenAnyValue(x => x.EnableAutoRun)
                .DistinctUntilChanged()
                .Subscribe(x =>
                {
                    if (x && !_update)
                    {
                        _update = true;
                        Run(_xaml.Text, _code.Text);
                        _update = false;
                    }
                });
        }

        public ObservableCollection<SampleViewModel> Samples
        {
            get => _samples;
            set => this.RaiseAndSetIfChanged(ref _samples, value);
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

        public ICommand RunCommand { get; }

        public ICommand GistCommand { get; }

        private async Task<string> GetGistContent(string id)
        {
            var client = new GitHubClient(new ProductHeaderValue("XamlPlayground"));
            var gist = await client.Gist.Get(id);
            var file = gist.Files.First(x => string.Compare(x.Key, "Main.axaml", StringComparison.OrdinalIgnoreCase) == 0).Value;
            return file.Content;
        }

        public async Task Gist(string id)
        {
            try
            {
                Xaml.Text = await GetGistContent(id);
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

        private void Open(string xaml, string? code)
        {
            Control = null;
            LastErrorMessage = null;

            _update = true;

            Xaml.Text = xaml;
            Code.Text = code;

            if (_enableAutoRun)
            {
                Run(_xaml.Text, _code.Text);
            }

            _update = false;
        }
 
        private void Run(string? xaml, string? code)
        {
            try
            {
                Assembly? scriptAssembly = null;

                if (code is { } && !string.IsNullOrWhiteSpace(code) && !Compiler.IsBrowser())
                {
                    try
                    {
                        scriptAssembly = Compiler.GetScriptAssembly(code, "XamlPlayground.Views");
                    }
                    catch (Exception e)
                    {
                        LastErrorMessage = e.Message;
                        Console.WriteLine(e);
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
            catch (Exception e)
            {
                LastErrorMessage = e.Message;
                Console.WriteLine(e);
            }
        }
    }
}
