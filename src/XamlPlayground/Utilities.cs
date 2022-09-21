using System.Runtime.InteropServices;

namespace XamlPlayground;

public static class Utilities
{
    public static bool IsBrowser()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER"));
    }
}
