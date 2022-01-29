using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;
using Avalonia.Markup.Xaml;

namespace XamlPlayground.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ObservableCollection<SampleViewModel> _samples;
        private string? _xaml;
        private IControl? _control;
        private bool _enableAutoRun;

        public MainViewModel()
        {
            _samples = GetSamples(".xml");
            _xaml = _samples.FirstOrDefault()?.Xaml;
            _enableAutoRun = true;

            RunCommand = ReactiveCommand.Create(Run);

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

        public string? Xaml
        {
            get => _xaml;
            set => this.RaiseAndSetIfChanged(ref _xaml, value);
        }

        public bool EnableAutoRun
        {
            get => _enableAutoRun;
            set => this.RaiseAndSetIfChanged(ref _enableAutoRun, value);
        }

        public ICommand RunCommand { get; }

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
            Xaml = xaml;
        }
 
        private void Run()
        {
            try
            {
                if (_xaml is { })
                {
                    var control = AvaloniaRuntimeXamlLoader.Parse<IControl?>(_xaml);
                    if (control is { })
                    {
                        Control = control;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
