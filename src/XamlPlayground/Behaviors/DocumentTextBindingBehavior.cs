using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Indentation.CSharp;
using AvaloniaEdit.TextMate;
using AvaloniaEdit.TextMate.Grammars;

namespace XamlPlayground.Behaviors;

public class DocumentTextBindingBehavior : Behavior<TextEditor>
{
    private TextEditor? _textEditor;
    private RegistryOptions? _registryOptions;
    private TextMate.Installation? _textMateInstallation;

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<DocumentTextBindingBehavior, string?>(nameof(Text));

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is { } textEditor)
        {
            _textEditor = textEditor;
            _textEditor.TextChanged += TextChanged;
 
            this.GetObservable(TextProperty).Subscribe(TextPropertyChanged);

            // TODO: Enable for WebAssembly
            // https://github.com/danipen/TextMateSharp/issues/9
            // https://github.com/AvaloniaUI/AvaloniaEdit/issues/201
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")))
            {
                _registryOptions = new RegistryOptions(ThemeName.LightPlus);
                _textMateInstallation = _textEditor.InstallTextMate(_registryOptions);
                _textMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(_registryOptions.GetLanguageByExtension(".xml").Id));
            }
            else
            {
                _textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
            }
        }
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (_textEditor is { })
        {
            _textEditor.TextChanged -= TextChanged;
        }
    }

    private void TextChanged(object? sender, EventArgs eventArgs)
    {
        if (_textEditor?.Document is { })
        {
            Text = _textEditor.Document.Text;
        }
    }

    private void TextPropertyChanged(string? text)
    {
        if (_textEditor?.Document is { } && text is { })
        {
            var caretOffset = _textEditor.CaretOffset;
            _textEditor.Document.Text = text;
            _textEditor.CaretOffset = caretOffset;
        }
    }
}
