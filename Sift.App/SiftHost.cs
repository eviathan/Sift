using System.Numerics;
using Paper.Core.Threading;
using Paper.Rendering.Silk.NET;
using Silk.NET.OpenGL;
using Sift.App.Scene;
using Sift.Core;
using Sift.Core.Commands;
using Sift.Core.Events;
using Sift.Core.Factories;
using Sift.Core.Grid;
using Sift.Core.Snapshots;

namespace Sift.App
{
    public enum ViewMode
    {
        // NOTE: One layer, slightly tilted so messages leaving it for the layers above and below can be seen
        Plane,

        // NOTE: The whole stack of layers in an isometric view, for choosing a layer
        Layers
    }

    // NOTE: Everything the Paper UI (src/*.csx) shows and does goes through here, like Prism's EditorHost. CSX reads
    // its properties when it renders, and anything that changes them calls MarkDirty so Paper renders again
    public sealed class SiftHost : IDisposable
    {
        public static SiftHost Current { get; set; } = null!;

        private const float PLANE_TILT = 28f * MathF.PI / 180f;
        private const float PLANE_LAYER_SPACING = 2.4f;
        private const float LAYERS_TILT = 54.7356f * MathF.PI / 180f;
        private const float LAYERS_YAW = 45f * MathF.PI / 180f;
        private const float LAYERS_LAYER_SPACING = 3f;
        private const float DEFAULT_ZOOM = 56f;
        private const float MIN_ZOOM = 16f;
        private const float MAX_ZOOM = 160f;
        private const float WHEEL_LAYER_THRESHOLD = 1f;
        private const int SIXTEENTHS_PER_BAR = 16;
        private const int TICKS_PER_SIXTEENTH = Time.TicksPerQuarterNote / 4;

        private readonly RuntimeEventHub _events;
        private Canvas? _canvas;
        private GL? _gl;
        private SceneRenderer? _renderer;
        private RuntimeEventSubscription? _subscription;
        private Task<SequenceSnapshot>? _pendingSnapshot;

        private float _viewWidth = 800f;
        private float _viewHeight = 600f;
        private bool _hasViewSize;
        private Vector2 _planeCentre;
        private float _planeZoom = DEFAULT_ZOOM;
        private float _wheelLayerAccumulator;
        private (float X, float Y)? _panAnchor;
        private string _positionLabel = "1.1.1";
        private bool _commandHeld;
        private bool _shiftHeld;

        public Sequence Sequence { get; }
        public Sequencer Sequencer { get; }
        public SequencerRuntime Runtime { get; }
        public ComponentFactory Components { get; }

        public SceneModel Scene { get; } = new();
        public SceneCamera Camera { get; }

        public ViewMode Mode { get; private set; } = ViewMode.Plane;
        public int Layer { get; private set; }
        public Guid? SelectedNodeId { get; private set; }
        public Vector3D? SelectedCell { get; private set; }
        public Vector3D? HoverCell { get; private set; }

        // NOTE: Switch to the layers view once the first frame has laid out the scene
        public bool StartInLayers { get; set; }

        public uint SceneTexture => _renderer?.Texture ?? 0;
        public string PositionLabel => _positionLabel;
        public bool IsPlaying => Sequencer.IsPlaying;
        public int Tempo => Sequence.Tempo;
        public string QuantisationLabel => $"{Sequence.Quantisation.Numerator}/{Sequence.Quantisation.Denominator}";

        public NodeSnapshot? SelectedNode =>
            SelectedNodeId is { } id && Scene.TryGetNode(id, out var node) ? node : null;

        public (int Min, int Max) Layers =>
            SceneRenderer.LayerRange(Scene, Layer);

        public SiftHost(Sequence sequence, Sequencer sequencer, SequencerRuntime runtime, ComponentFactory components, RuntimeEventHub events)
        {
            Sequence = sequence;
            Sequencer = sequencer;
            Runtime = runtime;
            Components = components;
            _events = events;

            Camera = new SceneCamera(PlanePose());
        }

        public void Attach(Canvas canvas) =>
            _canvas = canvas;

        // NOTE: Called from Canvas.OnLoad once the GL context exists
        public void OnLoad(GL gl)
        {
            _gl = gl;
            _renderer = new SceneRenderer(gl);

            // NOTE: Subscribe before the snapshot so no event falls between them
            _subscription = _events.Subscribe();
            Scene.Load(Runtime.GetSnapshotAsync().GetAwaiter().GetResult());

            var board = SceneRenderer.MeasureBoard(Frame(0));
            _planeCentre = new Vector2((board.MinX + board.MaxX) / 2f, (board.MinY + board.MaxY) / 2f);
            Camera.JumpTo(PlanePose());
        }

        // NOTE: Called from Canvas.PreRender every frame Paper renders
        public void Tick()
        {
            if (_renderer == null || _subscription == null)
                return;

            var uiChanged = ReadEvents();

            if (SelectedNodeId is { } selected && !Scene.TryGetNode(selected, out _))
            {
                SelectedNodeId = null;
                uiChanged = true;
            }

            Scene.PruneEffects();

            var now = SceneModel.Seconds;
            Camera.Update(now);

            _renderer.Render(Frame(now));

            if (StartInLayers && _hasViewSize)
            {
                StartInLayers = false;
                SetMode(ViewMode.Layers);
            }

            var label = FormatPosition(Scene.IsPlaying ? Scene.CurrentTick : 0);

            if (label != _positionLabel)
            {
                _positionLabel = label;
                uiChanged = true;
            }

            // NOTE: Only keep Paper rendering every frame while something is moving
            if (_canvas != null)
                _canvas.AlwaysRender = Scene.IsPlaying || Camera.IsAnimating || Scene.HasActiveEffects;

            if (uiChanged)
                MarkDirty();
        }

        public void MarkDirty() =>
            _canvas?.RequestRender();

        public void Dispose()
        {
            _subscription?.Dispose();
            _renderer?.Dispose();
        }

        // ── Transport ────────────────────────────────────────────────────────

        public void TogglePlay()
        {
            if (Sequencer.IsPlaying)
                Sequencer.Stop();
            else
                Sequencer.Play();

            MarkDirty();
        }

        public void NudgeTempo(int amount)
        {
            Sequence.Tempo = (ushort)Math.Clamp(Sequence.Tempo + amount, 20, 300);
            MarkDirty();
        }

        public void Undo()
        {
            _ = Runtime.Undo();
            MarkDirty();
        }

        public void Redo()
        {
            _ = Runtime.Redo();
            MarkDirty();
        }

        public void DeleteNode(Guid nodeId)
        {
            _ = Runtime.Submit(new RemoveNode(nodeId));
            MarkDirty();
        }

        // ── View ─────────────────────────────────────────────────────────────

        public void ToggleMode() =>
            SetMode(Mode == ViewMode.Plane ? ViewMode.Layers : ViewMode.Plane);

        public void SetMode(ViewMode mode)
        {
            if (mode == Mode)
                return;

            Mode = mode;
            HoverCell = null;
            Camera.AnimateTo(mode == ViewMode.Plane ? PlanePose() : LayersPose(), SceneModel.Seconds);
            MarkDirty();
        }

        public void SetLayer(int layer)
        {
            if (layer == Layer)
                return;

            Layer = layer;
            SelectedNodeId = null;
            SelectedCell = null;
            HoverCell = null;
            Camera.AnimateTo(Mode == ViewMode.Plane ? PlanePose() : LayersPose(), SceneModel.Seconds);
            MarkDirty();
        }

        // ── Pointer input from the scene's overlay ───────────────────────────

        public void PointerMove(float x, float y, float width, float height)
        {
            SetViewSize(width, height);

            if (_panAnchor is { } anchor)
            {
                _planeCentre -= new Vector2(x - anchor.X, -(y - anchor.Y)) / _planeZoom;
                _panAnchor = (x, y);
                Camera.JumpTo(PlanePose());
                MarkDirty();
                return;
            }

            var hover = Mode == ViewMode.Plane ? PickCell(x, y) : null;

            if (hover != HoverCell)
            {
                HoverCell = hover;
                MarkDirty();
            }
        }

        public void PointerLeave()
        {
            _panAnchor = null;

            if (HoverCell != null)
            {
                HoverCell = null;
                MarkDirty();
            }
        }

        public void PointerDown(float x, float y, float width, float height, int button)
        {
            SetViewSize(width, height);

            if (button != 0)
            {
                if (Mode == ViewMode.Plane)
                    _panAnchor = (x, y);
                return;
            }

            if (Mode == ViewMode.Layers)
            {
                if (PickLayer(x, y) is { } layer)
                    SetLayer(layer);
                return;
            }

            var cell = PickCell(x, y);
            SelectedCell = cell;
            SelectedNodeId = cell is { } position ? Scene.NodeAt(position)?.Id : null;
            MarkDirty();
        }

        public void PointerUp() =>
            _panAnchor = null;

        public void DoubleClick()
        {
            if (Mode == ViewMode.Layers)
                SetMode(ViewMode.Plane);
        }

        public void Wheel(float x, float y, float width, float height, float deltaX, float deltaY, bool zoom)
        {
            SetViewSize(width, height);

            if (Mode == ViewMode.Layers)
            {
                _wheelLayerAccumulator += deltaY;

                if (MathF.Abs(_wheelLayerAccumulator) >= WHEEL_LAYER_THRESHOLD)
                {
                    var (min, max) = Layers;
                    SetLayer(Math.Clamp(Layer + Math.Sign(_wheelLayerAccumulator), min, max));
                    _wheelLayerAccumulator = 0;
                }

                return;
            }

            if (zoom)
            {
                // NOTE: Zoom about the pointer so what's under it stays put
                Camera.TryPick(x, y, width, height, Camera.LayerHeight(Layer), out var before);
                _planeZoom = Math.Clamp(_planeZoom * MathF.Pow(1.1f, deltaY), MIN_ZOOM, MAX_ZOOM);
                Camera.JumpTo(PlanePose());
                Camera.TryPick(x, y, width, height, Camera.LayerHeight(Layer), out var after);
                _planeCentre += new Vector2(before.X - after.X, before.Y - after.Y);
            }
            else
            {
                _planeCentre += new Vector2(-deltaX, deltaY) * (12f / _planeZoom);
            }

            Camera.JumpTo(PlanePose());
            MarkDirty();
        }

        // NOTE: From Paper's GlobalKeyFilter, which only passes the key's name, so the modifiers are tracked here.
        // Returns true when the key was used
        public bool HandleKeyDown(string key)
        {
            if (TrackModifier(key, isDown: true))
                return false;

            switch (key)
            {
                case "Space":
                    TogglePlay();
                    return true;

                case "Tab":
                    ToggleMode();
                    return true;

                case "Escape":
                    if (Mode == ViewMode.Layers)
                    {
                        SetMode(ViewMode.Plane);
                    }
                    else
                    {
                        SelectedNodeId = null;
                        SelectedCell = null;
                        MarkDirty();
                    }
                    return true;

                case "Enter":
                case "KeypadEnter":
                    if (Mode != ViewMode.Layers)
                        return false;

                    SetMode(ViewMode.Plane);
                    return true;

                case "Up":
                case "RightBracket":
                    SetLayer(Layer + 1);
                    return true;

                case "Down":
                case "LeftBracket":
                    SetLayer(Layer - 1);
                    return true;

                case "Delete":
                case "Backspace":
                    if (SelectedNodeId is not { } id)
                        return false;

                    DeleteNode(id);
                    return true;

                case "Z":
                    if (!_commandHeld)
                        return false;

                    if (_shiftHeld) Redo();
                    else Undo();
                    return true;
            }

            return false;
        }

        public bool HandleKeyUp(string key)
        {
            TrackModifier(key, isDown: false);
            return false;
        }

        private bool TrackModifier(string key, bool isDown)
        {
            switch (key)
            {
                case "SuperLeft":
                case "SuperRight":
                case "ControlLeft":
                case "ControlRight":
                    _commandHeld = isDown;
                    return true;

                case "ShiftLeft":
                case "ShiftRight":
                    _shiftHeld = isDown;
                    return true;
            }

            return false;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private bool ReadEvents()
        {
            var uiChanged = false;

            if (_pendingSnapshot is { IsCompleted: true } snapshot)
            {
                _pendingSnapshot = null;

                if (snapshot.IsCompletedSuccessfully)
                {
                    Scene.Load(snapshot.Result);
                    uiChanged = true;
                }
            }

            while (_subscription!.Reader.TryRead(out var runtimeEvent))
                uiChanged |= Scene.Apply(runtimeEvent);

            if (Scene.NeedsResync && _pendingSnapshot == null)
                _pendingSnapshot = Runtime.GetSnapshotAsync();

            return uiChanged;
        }

        private SceneFrame Frame(double now) =>
            new(Scene,
                Camera,
                Layer,
                LayersAmount,
                SelectedNodeId,
                Mode == ViewMode.Plane ? HoverCell : null,
                _viewWidth,
                _viewHeight,
                ReadDpiScale(),
                Scene.CurrentTick,
                now);

        // NOTE: How far the camera is through the move from the 2D pose to the isometric one, for fading layers in step
        private float LayersAmount =>
            Math.Clamp((Camera.Current.Tilt - PLANE_TILT) / (LAYERS_TILT - PLANE_TILT), 0f, 1f);

        private SceneCamera.Pose PlanePose() =>
            new(new Vector3(_planeCentre, Layer * PLANE_LAYER_SPACING), _planeZoom, 0f, PLANE_TILT, PLANE_LAYER_SPACING);

        private SceneCamera.Pose LayersPose()
        {
            var board = SceneRenderer.MeasureBoard(Frame(0));
            var (minLayer, maxLayer) = Layers;
            var centre = new Vector2((board.MinX + board.MaxX) / 2f, (board.MinY + board.MaxY) / 2f);

            // NOTE: Fit the stack's footprint, measured with the isometric rotation alone
            var rotation = Matrix4x4.CreateRotationZ(-LAYERS_YAW) * Matrix4x4.CreateRotationX(-LAYERS_TILT);
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;

            foreach (var layer in new[] { minLayer, maxLayer })
            {
                foreach (var corner in new[] { new Vector2(board.MinX - 0.5f, board.MinY - 0.5f), new Vector2(board.MaxX + 0.5f, board.MinY - 0.5f), new Vector2(board.MaxX + 0.5f, board.MaxY + 0.5f), new Vector2(board.MinX - 0.5f, board.MaxY + 0.5f) })
                {
                    var projected = Vector3.Transform(new Vector3(corner - centre, (layer - Layer) * LAYERS_LAYER_SPACING), rotation);
                    minX = MathF.Min(minX, projected.X);
                    maxX = MathF.Max(maxX, projected.X);
                    minY = MathF.Min(minY, projected.Y);
                    maxY = MathF.Max(maxY, projected.Y);
                }
            }

            // NOTE: The selected layer stays centred, so allow for the stack extending further one way
            var halfWidth = MathF.Max(MathF.Abs(minX), MathF.Abs(maxX));
            var halfHeight = MathF.Max(MathF.Abs(minY), MathF.Abs(maxY));
            var zoom = Math.Clamp(MathF.Min(_viewWidth / (halfWidth * 2f), _viewHeight / (halfHeight * 2f)) * 0.85f, MIN_ZOOM / 2f, MAX_ZOOM);

            return new(new Vector3(centre, Layer * LAYERS_LAYER_SPACING), zoom, LAYERS_YAW, LAYERS_TILT, LAYERS_LAYER_SPACING);
        }

        private Vector3D? PickCell(float x, float y)
        {
            if (!Camera.TryPick(x, y, _viewWidth, _viewHeight, Camera.LayerHeight(Layer), out var world))
                return null;

            var cell = new Vector3D((int)MathF.Round(world.X), (int)MathF.Round(world.Y), Layer);
            var board = _renderer?.Board ?? default;

            return cell.X >= board.MinX && cell.X <= board.MaxX && cell.Y >= board.MinY && cell.Y <= board.MaxY ? cell : null;
        }

        // NOTE: The nearest layer whose board is under the pointer
        private int? PickLayer(float x, float y)
        {
            var board = _renderer?.Board ?? default;
            var (min, max) = Layers;
            int? picked = null;
            var pickedDepth = float.MinValue;

            for (var layer = min; layer <= max; layer++)
            {
                if (!Camera.TryPick(x, y, _viewWidth, _viewHeight, Camera.LayerHeight(layer), out var world))
                    continue;

                if (world.X < board.MinX - 0.5f || world.X > board.MaxX + 0.5f || world.Y < board.MinY - 0.5f || world.Y > board.MaxY + 0.5f)
                    continue;

                var depth = Camera.Depth(world);

                if (depth > pickedDepth)
                {
                    picked = layer;
                    pickedDepth = depth;
                }
            }

            return picked;
        }

        private void SetViewSize(float width, float height)
        {
            if (width <= 1 || height <= 1 || (width == _viewWidth && height == _viewHeight))
                return;

            _viewWidth = width;
            _viewHeight = height;
            _hasViewSize = true;

            // NOTE: Paper reports the size while drawing, after this frame's scene was rendered, so render again. Posted
            // because a request made mid-draw is cleared when the draw finishes
            UiThread.Post(MarkDirty);
        }

        // NOTE: From the viewport's onSizeChanged, in logical pixels
        public void SetViewportSize(int width, int height) =>
            SetViewSize(width, height);

        // NOTE: Paper sets the GL viewport to the window's framebuffer each frame, which tells us the backing scale
        private unsafe float ReadDpiScale()
        {
            if (_gl == null || _canvas == null || _canvas.ScreenSize.X <= 0)
                return 1f;

            Span<int> viewport = stackalloc int[4];
            _gl.GetInteger(GetPName.Viewport, viewport);

            return viewport[2] > 0 ? viewport[2] / (float)_canvas.ScreenSize.X : 1f;
        }

        private static string FormatPosition(double tick)
        {
            var sixteenths = (long)(tick / TICKS_PER_SIXTEENTH);
            var bar = sixteenths / SIXTEENTHS_PER_BAR + 1;
            var beat = sixteenths % SIXTEENTHS_PER_BAR / 4 + 1;
            var sixteenth = sixteenths % 4 + 1;

            return $"{bar}.{beat}.{sixteenth}";
        }
    }
}
