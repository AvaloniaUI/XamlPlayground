using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
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
        private ObservableCollection<SampleViewModel> _samples;
        private TextDocument _xaml;
        private IControl? _control;
        private bool _enableAutoRun;
        private string? _lastErrorMessage;

        public MainViewModel()
        {
            _samples = GetSamples(".xml");
            _enableAutoRun = true;
            _xaml = new TextDocument();
            _xaml.Text = _samples.FirstOrDefault()?.Xaml;
            _xaml.TextChanged += (_, _) => Run();

            RunCommand = ReactiveCommand.Create(Run);
            
            GistCommand = ReactiveCommand.CreateFromTask<string>(Gist);

            this.WhenAnyValue(x => x.Xaml)
                .WhereNotNull()
                .Subscribe(_ =>
                {
                    if (_enableAutoRun)
                    {
                        Run();
                    }
                });

            this.WhenAnyValue(x => x.EnableAutoRun)
                .DistinctUntilChanged()
                .Subscribe(x =>
                {
                    if (x)
                    {
                        Run();
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

            var playground = 
                "<Grid xmlns=\"https://github.com/avaloniaui\"\n" +
                "      xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">\n" +
                "\n" +
                "</Grid>";

            samples.Add(new SampleViewModel("Playground", playground, Open));

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
                        samples.Add(new SampleViewModel(name, xaml, Open));
                    }
                }
            }

            return samples;
        }

        private void Open(string xaml)
        {
            Control = null;
            LastErrorMessage = null;
            Xaml.Text = xaml;
        }
 
        private void Run()
        {
            try
            {
                if (_xaml is { })
                {
                    var control = AvaloniaRuntimeXamlLoader.Parse<IControl?>(_xaml.Text);
                    if (control is { })
                    {
                        Control = control;
                        LastErrorMessage = null;
                    }
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
