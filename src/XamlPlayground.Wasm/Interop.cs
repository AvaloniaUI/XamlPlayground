using System.Runtime.InteropServices.JavaScript;

namespace XamlPlayground.Wasm;

internal static partial class Interop
{
    [JSImport("getBaseUri", "interop.js")]
    public static partial string GetBaseUri();
}
