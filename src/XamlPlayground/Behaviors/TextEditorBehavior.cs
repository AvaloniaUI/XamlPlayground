using Avalonia;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace XamlPlayground.Behaviors;

public class TextEditorBehavior : Behavior<TextEditor>
{
    public static readonly StyledProperty<string?> ExtensionProperty = 
        AvaloniaProperty.Register<TextEditorBehavior, string?>(nameof(Extension));

    public static readonly StyledProperty<bool> UseTextMateProperty = 
        AvaloniaProperty.Register<TextEditorBehavior, bool>(nameof(UseTextMate));

    private TextEditor? _textEditor;
    private RegistryOptions? _registryOptions;
    private TextMate.Installation? _textMateInstallation;

    public string? Extension
    {
        get => GetValue(ExtensionProperty);
        set => SetValue(ExtensionProperty, value);
    }

    public bool UseTextMate
    {
        get => GetValue(UseTextMateProperty);
        set => SetValue(UseTextMateProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is not { } textEditor)
        {
            return;
        }

        _textEditor = textEditor;

        if (UseTextMate)
        {
            _registryOptions = new RegistryOptions(ThemeName.DarkPlus);
            _textMateInstallation = _textEditor.InstallTextMate(_registryOptions);
            _textMateInstallation.SetGrammar(
                _registryOptions.GetScopeByLanguageId(_registryOptions.GetLanguageByExtension(Extension).Id));
        }
        else
        {
            _textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(Extension);
            // TODO: _textEditor.TextArea.Background = new SolidColorBrush(Color.Parse("#292929"));
        }
    }
}
