import { dotnet } from './dotnet.js'
import { registerAvaloniaModule } from './avalonia.js';

const is_browser = typeof window != "undefined";
if (!is_browser) throw new Error(`Expected to be running in a browser`);

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

await registerAvaloniaModule(dotnetRuntime);

const config = dotnetRuntime.getConfig();

const exports = await dotnetRuntime.getAssemblyExports(config.mainAssemblyName);
const gist = exports.XamlPlayground.Wasm.Interop.Gist();

let router = (evt) => {
    const url = window.location.hash.slice(1) || "/";
    console.log(window.location.hash);
    console.log(url);
    gist(url);
};

window.addEventListener('load', router);
window.addEventListener('hashchange', router);

const url = window.location.hash.slice(1) || "/";
console.log(window.location.hash);
console.log(url);

await dotnetRuntime.runMainAndExit(config.mainAssemblyName, ["dotnet", "is", "great!"]);
