namespace XamlPlayground;

internal static class Templates
{
    public static string s_code =
        "using Avalonia;\n" +
        "using Avalonia.Controls;\n" +
        "using Avalonia.Markup.Xaml;\n" +
        "\n" +
        "namespace XamlPlayground.Views\n" +
        "{\n" +
        "    public partial class SampleView : UserControl\n" +
        "    {\n" +
        // "        public SampleView()\n" +
        // "        {\n" +
        // "            InitializeComponent();\n" +
        // "        }\n" +
        // "\n" +
        // "        private void InitializeComponent()\n" +
        // "        {\n" +
        // "            // AvaloniaXamlLoader.Load(this);\n" +
        // "        }\n" +
        // "\n" +
        "        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)\n" +
        "        {\n" +
        "            var count = 0;\n" +
        "            var button = this.Find<Button>(\"button\");\n" +
        "            button.Click += (sender, e) => button.Content = $\"Clicked: {++count}\";\n" +
        "            base.OnAttachedToVisualTree(e);\n" +
        "        }\n" +
        "    }\n" +
        "}\n";

    public static string s_xaml = 
        // "<UserControl xmlns=\"https://github.com/avaloniaui\"\n" +
        // "             xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
        // "             x:Class=\"XamlPlayground.Views.SampleView\">\n" +
        "<UserControl xmlns=\"https://github.com/avaloniaui\">\n" +
        "    <Button Name=\"button\" Content=\"Click Me\" HorizontalAlignment=\"Center\" />\n" +
        "</UserControl>";
}
