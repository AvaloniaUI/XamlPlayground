using System;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace XamlPlayground.Behaviors;

public class XamlTextEditorBehavior : Behavior<TextEditor>
{
    private TextEditor? _textEditor;
    private RegistryOptions? _registryOptions;
    private TextMate.Installation? _textMateInstallation;

    protected override void OnAttached()
    {
        base.OnAttached();

        if (AssociatedObject is { } textEditor)
        {
            _textEditor = textEditor;

            // TODO: Enable for WebAssembly
            // https://github.com/danipen/TextMateSharp/issues/9
            // https://github.com/AvaloniaUI/AvaloniaEdit/issues/201
            if (Utilities.IsBrowser())
            {
                _textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");

                /*
                _textEditor.KeyDown += (_, args) =>
                {
                    Console.WriteLine($"KeyDown: {args.Key.ToString()}");
                };
                //*/
            }
            else
            {
                _registryOptions = new RegistryOptions(ThemeName.LightPlus);
                _textMateInstallation = _textEditor.InstallTextMate(_registryOptions);
                _textMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(_registryOptions.GetLanguageByExtension(".xml").Id));
            }
        }
    }
}
