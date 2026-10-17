using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paper.Core.Hooks;
using Paper.Rendering.Silk.NET;
using Paper.Rendering.Silk.NET.HotReload;
using Sift.App;
using Sift.Core;
using Sift.Core.Extensions;
using Sift.Core.Factories;

// ── Sift ──────────────────────────────────────────────────────────────────────
//
// Layout is defined in src/App.csx + src/App.csss (hot reloaded while running).
// Program.cs only composes the services, creates the window and wires the host
// (SiftHost) that the CSX reads from. The grid itself is drawn by SceneRenderer
// into a texture shown with UI.Viewport.
// ─────────────────────────────────────────────────────────────────────────────

var services = new ServiceCollection();
services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Warning));
services.AddSiftCore();
services.AddSingleton<SiftHost>();

using var provider = services.BuildServiceProvider();

var host = SiftHost.Current = provider.GetRequiredService<SiftHost>();

// NOTE: A .sift file passed on the command line is opened, otherwise the demo scene
if (args.FirstOrDefault(x => !x.StartsWith("--") && x.EndsWith(".sift")) is { } file)
    host.OpenPath(file);
else
    DemoScene.Build(provider.GetRequiredService<Sequence>(), provider.GetRequiredService<ComponentFactory>());

var canvas = new Canvas("Sift", 1280, 800)
{
    MinimumWindowWidth = 900,
    MinimumWindowHeight = 560,
    // NOTE: Spot's look: a 52pt header in the title bar with the traffic lights centred in it
    MacUnifiedToolbar = true,
};

host.Attach(canvas);

canvas.AddCSXReferences(typeof(SiftHost).Assembly, typeof(Sequence).Assembly, typeof(Paper.Icons.IconSets).Assembly);
canvas.OnLoad = (gl, _, _, _) => host.OnLoad(gl);
canvas.PreRender = _ => host.Tick();

GlobalKeyFilter.KeyDown = host.HandleKeyDown;
GlobalKeyFilter.KeyUp = host.HandleKeyUp;

// NOTE: --play and --layers start playing / in the layers view, handy for checking the scene without clicking around
if (args.Contains("--play"))
    host.Sequencer.Play();

if (args.Contains("--layers"))
    host.StartInLayers = true;

// NOTE: --select x,y,z starts with that cell selected, for checking the inspector
if (Array.IndexOf(args, "--select") is var selectIndex and >= 0 && selectIndex + 1 < args.Length)
{
    var parts = args[selectIndex + 1].Split(',').Select(int.Parse).ToArray();
    host.Select(new Sift.Core.Grid.Vector3D(parts[0], parts[1], parts[2]));
}

canvas.MountCSXHotReload("src/App.csx", scopeId: "App");
canvas.Run();

host.Dispose();
