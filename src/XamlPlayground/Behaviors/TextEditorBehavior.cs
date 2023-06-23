using Avalonia;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using Microsoft.Win32;

namespace XamlPlayground.Behaviors;

public class TextEditorBehavior : Behavior<TextEditor>
{
    public static readonly StyledProperty<string?> ExtensionProperty = 
        AvaloniaProperty.Register<TextEditorBehavior, string?>(nameof(Extension));

    private TextEditor? _textEditor;
    private RegistryOptions? _registryOptions;

    public string? Extension
    {
        get => GetValue(ExtensionProperty);
        set => SetValue(ExtensionProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is not { } textEditor)
        {
            return;
        }

        _textEditor = textEditor;

        _textEditor.TextArea.SelectionCornerRadius = 0;

        _textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(Extension);
    }
}
