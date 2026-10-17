using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL;
using Sift.Core.Enums;
using Sift.Core.Extensions;
using Sift.Core.Grid;
using Sift.Core.Snapshots;

namespace Sift.App.Scene
{
    // NOTE: What the renderer needs to draw one frame
    public readonly record struct SceneFrame(
        SceneModel Model,
        SceneCamera Camera,
        int SelectedLayer,
        float LayersAmount,
        Guid? SelectedNodeId,
        Vector3D? HoverCell,
        Guid? DraggingNodeId,
        Vector3D? DragTarget,
        float Width,
        float Height,
        float DpiScale,
        double Tick,
        double Now);

    // NOTE: Draws the grid into a texture that Paper shows through UI.Viewport. Everything except the glows is drawn
    // back to front (painter's algorithm) so the translucent layers composite correctly without a depth buffer
    public sealed unsafe class SceneRenderer : IDisposable
    {
        private const int SAMPLES = 4;

        // NOTE: In cells
        private const float TILE_HALF_SIZE = 0.38f;
        private const float TILE_HEIGHT = 0.16f;
        private const float BOARD_MARGIN = 3f;
        private const int MIN_BOARD_WIDTH = 12;
        private const int MIN_BOARD_HEIGHT = 8;

        private const string FLAT_VERTEX = """
            #version 330 core
            layout(location = 0) in vec3 aPosition;
            layout(location = 1) in vec4 aColour;
            uniform mat4 uViewProjection;
            out vec4 vColour;
            void main() { gl_Position = uViewProjection * vec4(aPosition, 1.0); vColour = aColour; }
            """;

        private const string FLAT_FRAGMENT = """
            #version 330 core
            in vec4 vColour;
            out vec4 fragColour;
            void main() { fragColour = vColour; }
            """;

        // NOTE: Screen-aligned sprites with a soft falloff, sized in framebuffer pixels
        private const string GLOW_VERTEX = """
            #version 330 core
            layout(location = 0) in vec3 aCentre;
            layout(location = 1) in vec2 aCorner;
            layout(location = 2) in float aRadius;
            layout(location = 3) in vec4 aColour;
            uniform mat4 uViewProjection;
            uniform vec2 uPixelToClip;
            out vec2 vCorner;
            out vec4 vColour;
            void main()
            {
                vec4 position = uViewProjection * vec4(aCentre, 1.0);
                position.xy += aCorner * aRadius * uPixelToClip * position.w;
                gl_Position = position;
                vCorner = aCorner;
                vColour = aColour;
            }
            """;

        private const string GLOW_FRAGMENT = """
            #version 330 core
            in vec2 vCorner;
            in vec4 vColour;
            out vec4 fragColour;
            void main()
            {
                float distanceSquared = dot(vCorner, vCorner);
                float glow = exp(-distanceSquared * 5.0) + exp(-distanceSquared * 60.0) * 0.8;
                glow *= 1.0 - smoothstep(0.85, 1.0, sqrt(distanceSquared));
                fragColour = vec4(vColour.rgb * vColour.a * glow, 0.0);
            }
            """;

        private readonly GL _gl;
        private readonly uint _flatProgram;
        private readonly uint _glowProgram;
        private readonly int _flatViewProjection;
        private readonly int _glowViewProjection;
        private readonly int _glowPixelToClip;
        private readonly uint _flatVao;
        private readonly uint _flatVbo;
        private readonly uint _glowVao;
        private readonly uint _glowVbo;
        private readonly uint _texture;
        private readonly uint _resolveFramebuffer;
        private readonly uint _multisampleFramebuffer;
        private readonly uint _multisampleColour;

        private readonly List<float> _triangles = [];
        private readonly List<float> _glows = [];
        private readonly List<(float Depth, int Layer)> _layerOrder = [];
        private readonly List<(float Depth, NodeSnapshot Node)> _nodeOrder = [];
        private readonly List<(float Depth, int Start, int Count)> _faceOrder = [];

        private int _width;
        private int _height;

        private SceneFrame _frame;
        private Matrix4x4 _viewProjection;
        private float _layerSpacing;

        // NOTE: Stable for the renderer's lifetime, resizing reallocates its storage rather than replacing it
        public uint Texture => _texture;

        public (int MinX, int MinY, int MaxX, int MaxY) Board { get; private set; }

        public SceneRenderer(GL gl)
        {
            _gl = gl;

            _flatProgram = CreateProgram(FLAT_VERTEX, FLAT_FRAGMENT);
            _flatViewProjection = _gl.GetUniformLocation(_flatProgram, "uViewProjection");

            _glowProgram = CreateProgram(GLOW_VERTEX, GLOW_FRAGMENT);
            _glowViewProjection = _gl.GetUniformLocation(_glowProgram, "uViewProjection");
            _glowPixelToClip = _gl.GetUniformLocation(_glowProgram, "uPixelToClip");

            (_flatVao, _flatVbo) = CreateVertexArray([3, 4]);
            (_glowVao, _glowVbo) = CreateVertexArray([3, 2, 1, 4]);

            _texture = _gl.GenTexture();
            _resolveFramebuffer = _gl.GenFramebuffer();
            _multisampleFramebuffer = _gl.GenFramebuffer();
            _multisampleColour = _gl.GenRenderbuffer();

            Resize(1, 1);
        }

        public void Render(in SceneFrame frame)
        {
            _frame = frame;
            _layerSpacing = frame.Camera.Current.LayerSpacing;

            var pixelWidth = Math.Max(1, (int)MathF.Round(frame.Width * frame.DpiScale));
            var pixelHeight = Math.Max(1, (int)MathF.Round(frame.Height * frame.DpiScale));

            if (pixelWidth != _width || pixelHeight != _height)
                Resize(pixelWidth, pixelHeight);

            _viewProjection = frame.Camera.ViewProjection(frame.Width, frame.Height);

            _triangles.Clear();
            _glows.Clear();

            Board = MeasureBoard(frame);
            BuildLayers();
            BuildGlows();

            Draw();
        }

        public void Dispose()
        {
            _gl.DeleteProgram(_flatProgram);
            _gl.DeleteProgram(_glowProgram);
            _gl.DeleteVertexArray(_flatVao);
            _gl.DeleteVertexArray(_glowVao);
            _gl.DeleteBuffer(_flatVbo);
            _gl.DeleteBuffer(_glowVbo);
            _gl.DeleteTexture(_texture);
            _gl.DeleteFramebuffer(_resolveFramebuffer);
            _gl.DeleteFramebuffer(_multisampleFramebuffer);
            _gl.DeleteRenderbuffer(_multisampleColour);
        }

        // NOTE: The layers in view and how strongly each is drawn: the 2D view shows the selected layer with ghosts
        // of its neighbours, the isometric view shows the whole stack
        public static float LayerAlpha(int layer, int selectedLayer, float layersAmount)
        {
            var offset = Math.Abs(layer - selectedLayer);
            var planeAlpha = offset == 0 ? 1f : offset == 1 ? 0.2f : 0f;
            var layersAlpha = offset == 0 ? 1f : 0.55f;

            return planeAlpha + (layersAlpha - planeAlpha) * layersAmount;
        }

        public static (int Min, int Max) LayerRange(SceneModel model, int selectedLayer)
        {
            var min = selectedLayer;
            var max = selectedLayer;

            foreach (var node in model.Nodes)
            {
                min = Math.Min(min, node.Position.Z);
                max = Math.Max(max, node.Position.Z);
            }

            return (min, max);
        }

        public static (int MinX, int MinY, int MaxX, int MaxY) MeasureBoard(in SceneFrame frame)
        {
            int minX = 0, minY = 0, maxX = 0, maxY = 0;
            var any = false;

            foreach (var node in frame.Model.Nodes)
            {
                if (!any)
                {
                    minX = maxX = node.Position.X;
                    minY = maxY = node.Position.Y;
                    any = true;
                    continue;
                }

                minX = Math.Min(minX, node.Position.X);
                maxX = Math.Max(maxX, node.Position.X);
                minY = Math.Min(minY, node.Position.Y);
                maxY = Math.Max(maxY, node.Position.Y);
            }

            minX -= (int)BOARD_MARGIN;
            minY -= (int)BOARD_MARGIN;
            maxX += (int)BOARD_MARGIN;
            maxY += (int)BOARD_MARGIN;

            Grow(ref minX, ref maxX, MIN_BOARD_WIDTH);
            Grow(ref minY, ref maxY, MIN_BOARD_HEIGHT);

            return (minX, minY, maxX, maxY);

            static void Grow(ref int min, ref int max, int size)
            {
                var missing = size - (max - min + 1);

                if (missing <= 0)
                    return;

                min -= missing / 2;
                max += missing - missing / 2;
            }
        }

        private void BuildLayers()
        {
            var (minLayer, maxLayer) = LayerRange(_frame.Model, _frame.SelectedLayer);
            var board = Board;
            var centre = new Vector2((board.MinX + board.MaxX) / 2f, (board.MinY + board.MaxY) / 2f);

            _layerOrder.Clear();

            for (var layer = minLayer; layer <= maxLayer; layer++)
            {
                if (LayerAlpha(layer, _frame.SelectedLayer, _frame.LayersAmount) < 0.01f)
                    continue;

                _layerOrder.Add((_frame.Camera.Depth(new Vector3(centre, layer * _layerSpacing)), layer));
            }

            _layerOrder.Sort((a, b) => a.Depth.CompareTo(b.Depth));

            foreach (var (_, layer) in _layerOrder)
                BuildLayer(layer, LayerAlpha(layer, _frame.SelectedLayer, _frame.LayersAmount));

            BuildCrossLayerConnections();
        }

        private void BuildLayer(int layer, float alpha)
        {
            var board = Board;
            var z = layer * _layerSpacing;
            var pixel = _frame.Camera.PixelsToWorld(1f);

            var left = board.MinX - 0.5f;
            var right = board.MaxX + 0.5f;
            var bottom = board.MinY - 0.5f;
            var top = board.MaxY + 0.5f;

            // The board
            // NOTE: Ghost layers fade their board much more than their nodes, so they read as nodes floating above/below
            var boardAlpha = layer == _frame.SelectedLayer ? alpha : alpha * alpha;
            AddQuad(new(left, bottom, z), new(right, bottom, z), new(right, top, z), new(left, top, z), Theme.Rgba(Theme.Card, 0.94f * boardAlpha));

            // NOTE: Outline the selected layer in the isometric view so it's clear which one Enter opens
            if (layer == _frame.SelectedLayer && _frame.LayersAmount > 0.01f)
            {
                var edge = Theme.Rgba(Theme.Accent, 0.8f * _frame.LayersAmount);
                Vector3[] corners = [new(left, bottom, z), new(right, bottom, z), new(right, top, z), new(left, top, z)];

                for (var i = 0; i < 4; i++)
                    AddRibbon(corners[i], corners[(i + 1) % 4], pixel * 2f, edge);
            }

            for (var x = board.MinX; x <= board.MaxX + 1; x++)
            {
                var major = x % 4 == 0;
                AddRibbon(new(x - 0.5f, bottom, z), new(x - 0.5f, top, z), pixel, Theme.White((major ? 0.075f : 0.04f) * boardAlpha));
            }

            for (var y = board.MinY; y <= board.MaxY + 1; y++)
            {
                var major = y % 4 == 0;
                AddRibbon(new(left, y - 0.5f, z), new(right, y - 0.5f, z), pixel, Theme.White((major ? 0.075f : 0.04f) * boardAlpha));
            }

            // Hovered cell
            if (layer == _frame.SelectedLayer && _frame.HoverCell is { } hover && hover.X >= board.MinX && hover.X <= board.MaxX && hover.Y >= board.MinY && hover.Y <= board.MaxY)
                AddQuad(new(hover.X - 0.5f, hover.Y - 0.5f, z), new(hover.X + 0.5f, hover.Y - 0.5f, z),
                        new(hover.X + 0.5f, hover.Y + 0.5f, z), new(hover.X - 0.5f, hover.Y + 0.5f, z), Theme.White(0.06f * alpha));

            // Routes within the layer
            foreach (var connection in _frame.Model.Connections)
            {
                if (connection.From.Z != layer || connection.To.Z != layer)
                    continue;

                var lift = z + 0.002f;
                var from = new Vector3(connection.From.X, connection.From.Y, lift);
                var to = new Vector3(connection.To.X, connection.To.Y, lift);
                var colour = Theme.White((connection.HasTarget ? 0.09f : 0.045f) * alpha);

                AddRibbon(from, to, pixel * 2f, colour);
            }

            // Nodes, back to front
            _nodeOrder.Clear();

            foreach (var node in _frame.Model.Nodes)
            {
                if (node.Position.Z == layer)
                    _nodeOrder.Add((_frame.Camera.Depth(new Vector3(node.Position.X, node.Position.Y, z)), node));
            }

            _nodeOrder.Sort((a, b) => a.Depth.CompareTo(b.Depth));

            foreach (var (_, node) in _nodeOrder)
                BuildNode(node, z, node.Id == _frame.DraggingNodeId ? alpha * 0.35f : alpha, pixel);

            // NOTE: Where a dragged node would land, red when the cell is taken
            if (layer == _frame.SelectedLayer && _frame.DraggingNodeId is { } dragging && _frame.DragTarget is { } target
                && _frame.Model.TryGetNode(dragging, out var dragged) && target != dragged.Position)
            {
                var occupied = _frame.Model.NodeAt(target) != null;
                var colour = occupied ? Theme.Rgba(Theme.Error, 0.9f) : Theme.White(0.9f);
                var h = TILE_HALF_SIZE + 0.08f;
                var lift = z + TILE_HEIGHT + 0.004f;
                Vector3[] corners = [new(target.X - h, target.Y - h, lift), new(target.X + h, target.Y - h, lift), new(target.X + h, target.Y + h, lift), new(target.X - h, target.Y + h, lift)];

                if (!occupied)
                    AddQuad(corners[0], corners[1], corners[2], corners[3], NodeColour(dragged) with { W = 0.45f * alpha });

                for (var i = 0; i < 4; i++)
                    AddRibbon(corners[i], corners[(i + 1) % 4], pixel * 2f, colour, alongScreen: false);
            }
        }

        private void BuildNode(NodeSnapshot node, float z, float alpha, float pixel)
        {
            var colour = NodeColour(node);

            if (_frame.Model.IsSounding(node.Id))
                colour = Vector4.Lerp(colour, Vector4.One, 0.3f);

            colour.W = alpha;
            var sides = new Vector4(colour.X * 0.55f, colour.Y * 0.55f, colour.Z * 0.6f, alpha);

            var x = node.Position.X;
            var y = node.Position.Y;
            var h = TILE_HALF_SIZE;
            var topZ = z + TILE_HEIGHT;

            Vector3[] bottom = [new(x - h, y - h, z), new(x + h, y - h, z), new(x + h, y + h, z), new(x - h, y + h, z)];
            Vector3[] top = [new(x - h, y - h, topZ), new(x + h, y - h, topZ), new(x + h, y + h, topZ), new(x - h, y + h, topZ)];

            // NOTE: Sides back to front then the top, so the box reads as solid from any angle
            _faceOrder.Clear();

            for (var i = 0; i < 4; i++)
            {
                var next = (i + 1) % 4;
                var start = _triangles.Count;

                AddQuad(bottom[i], bottom[next], top[next], top[i], sides);
                _faceOrder.Add((_frame.Camera.Depth((bottom[i] + top[next]) / 2f), start, _triangles.Count - start));
            }

            SortFaces();

            AddQuad(top[0], top[1], top[2], top[3], colour);

            // Ports with something on them
            var marker = Theme.White(0.75f * alpha);
            var lift = topZ + 0.002f;

            foreach (var (direction, components) in node.Ports)
            {
                if (components.Count == 0)
                    continue;

                switch (direction)
                {
                    case Direction.Forwards:
                        AddSquare(new Vector3(x, y, lift), 0.07f, marker);
                        break;

                    case Direction.Backwards:
                        AddSquare(new Vector3(x, y, lift), 0.07f, new Vector4(0f, 0f, 0f, 0.45f * alpha));
                        break;

                    default:
                        var step = direction.ToVector();
                        var along = new Vector3(step.Y, step.X, 0f) * 0.17f;
                        var centre = new Vector3(x + step.X * 0.3f, y + step.Y * 0.3f, lift);
                        AddRibbon(centre - along, centre + along, 0.07f, marker, alongScreen: false);
                        break;
                }
            }

            if (_frame.SelectedNodeId == node.Id)
            {
                var outline = Theme.White(0.95f * alpha);
                var o = h + 0.08f;
                var oz = topZ + 0.003f;
                Vector3[] corners = [new(x - o, y - o, oz), new(x + o, y - o, oz), new(x + o, y + o, oz), new(x - o, y + o, oz)];

                for (var i = 0; i < 4; i++)
                    AddRibbon(corners[i], corners[(i + 1) % 4], pixel * 2f, outline, alongScreen: false);
            }
        }

        // NOTE: Forwards/backwards routes between layers, drawn over everything so they read in the isometric view
        private void BuildCrossLayerConnections()
        {
            var pixel = _frame.Camera.PixelsToWorld(1f);

            foreach (var connection in _frame.Model.Connections)
            {
                if (connection.From.Z == connection.To.Z)
                    continue;

                var fromAlpha = LayerAlpha(connection.From.Z, _frame.SelectedLayer, _frame.LayersAmount);
                var toAlpha = LayerAlpha(connection.To.Z, _frame.SelectedLayer, _frame.LayersAmount);
                var alpha = MathF.Max(fromAlpha, toAlpha) * (connection.HasTarget ? 0.35f : 0.15f);

                if (alpha < 0.01f)
                    continue;

                var from = new Vector3(connection.From.X, connection.From.Y, connection.From.Z * _layerSpacing + TILE_HEIGHT);
                var to = new Vector3(connection.To.X, connection.To.Y, connection.To.Z * _layerSpacing + TILE_HEIGHT);

                AddRibbon(from, to, pixel * 2f, Theme.Rgba(Theme.Message, alpha));
            }
        }

        private void BuildGlows()
        {
            var cellPixels = _frame.Camera.Current.Zoom * _frame.DpiScale;
            var lift = TILE_HEIGHT + 0.05f;

            foreach (var message in _frame.Model.Messages)
            {
                var step = message.Direction.ToVector();
                var distance = Math.Abs(message.EndPosition.X - message.Origin.X) + Math.Abs(message.EndPosition.Y - message.Origin.Y) + Math.Abs(message.EndPosition.Z - message.Origin.Z);
                var travelled = (float)Math.Clamp((_frame.Tick - message.EmitTime.Ticks) / message.TicksPerCell, 0, distance);

                var alpha = MessageAlpha(message.Origin.Z + step.Z * travelled);

                if (alpha < 0.01f)
                    continue;

                // NOTE: Fade out over the last cell when nothing will catch it
                if (message.TargetNodeId == null)
                    alpha *= Math.Clamp(distance - travelled, 0f, 1f);

                var colour = Theme.Rgba(Theme.Message, alpha);

                for (var trail = 5; trail >= 1; trail--)
                {
                    var back = MathF.Max(0f, travelled - trail * 0.14f);
                    var trailColour = colour with { W = alpha * (1f - trail / 6f) * 0.35f };
                    AddGlow(MessagePosition(message.Origin, step, back, lift), cellPixels * (0.42f - trail * 0.04f), trailColour);
                }

                AddGlow(MessagePosition(message.Origin, step, travelled, lift), cellPixels * 0.55f, colour);
            }

            foreach (var flash in _frame.Model.Flashes)
            {
                var alpha = LayerAlpha(flash.Position.Z, _frame.SelectedLayer, _frame.LayersAmount);
                var amount = (float)Math.Clamp((_frame.Now - flash.StartedAt) / SceneModel.FLASH_SECONDS, 0, 1);
                var fade = (1f - amount) * (1f - amount) * alpha;

                if (fade < 0.01f)
                    continue;

                var colour = flash.IsDissipation ? Theme.White(0.35f * fade)
                    : flash.IsPlay ? Theme.Rgba(Theme.Accent, fade)
                    : Theme.Rgba(Theme.Message, fade);

                var radius = flash.IsDissipation ? 0.4f + amount * 0.3f : 0.8f + amount * 0.6f;
                AddGlow(World(flash.Position, lift), cellPixels * radius, colour);
            }

            foreach (var node in _frame.Model.Nodes)
            {
                if (!_frame.Model.IsSounding(node.Id))
                    continue;

                var alpha = LayerAlpha(node.Position.Z, _frame.SelectedLayer, _frame.LayersAmount);
                AddGlow(World(node.Position, lift), cellPixels * 0.9f, Theme.Rgba(Theme.Midi, 0.55f * alpha));
            }
        }

        private float MessageAlpha(float layer)
        {
            var below = (int)MathF.Floor(layer);
            var amount = layer - below;

            return LayerAlpha(below, _frame.SelectedLayer, _frame.LayersAmount) * (1f - amount)
                + LayerAlpha(below + 1, _frame.SelectedLayer, _frame.LayersAmount) * amount;
        }

        private Vector3 MessagePosition(Vector3D origin, Vector3D step, float travelled, float lift) =>
            new(origin.X + step.X * travelled, origin.Y + step.Y * travelled, (origin.Z + step.Z * travelled) * _layerSpacing + lift);

        private Vector3 World(Vector3D position, float lift) =>
            new(position.X, position.Y, position.Z * _layerSpacing + lift);

        private static Vector4 NodeColour(NodeSnapshot node)
        {
            var types = node.Ports.Values.SelectMany(x => x).Select(x => x.Type).ToHashSet();

            if (types.Contains("midi")) return Theme.Rgba(Theme.Midi);
            if (types.Contains("emit")) return Theme.Rgba(Theme.Emit);
            if (types.Contains("redirect")) return Theme.Rgba(Theme.Redirect);
            if (types.Contains("log")) return Theme.Rgba(Theme.Log);

            return Theme.Rgba(Theme.Dimmed);
        }

        // ── Geometry ─────────────────────────────────────────────────────────

        private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector4 colour)
        {
            AddVertex(a, colour); AddVertex(b, colour); AddVertex(c, colour);
            AddVertex(a, colour); AddVertex(c, colour); AddVertex(d, colour);
        }

        private void AddSquare(Vector3 centre, float halfSize, Vector4 colour) =>
            AddQuad(centre + new Vector3(-halfSize, -halfSize, 0), centre + new Vector3(halfSize, -halfSize, 0),
                    centre + new Vector3(halfSize, halfSize, 0), centre + new Vector3(-halfSize, halfSize, 0), colour);

        // NOTE: A line as a thin quad. Along the screen keeps a constant on-screen width from any angle, otherwise it
        // lies flat on the layer
        private void AddRibbon(Vector3 from, Vector3 to, float width, Vector4 colour, bool alongScreen = true)
        {
            var direction = to - from;
            var normal = alongScreen ? Vector3.Cross(direction, _frame.Camera.Forward) : Vector3.Cross(direction, Vector3.UnitZ);

            if (normal.LengthSquared() < 1e-10f)
                return;

            var offset = Vector3.Normalize(normal) * (width / 2f);
            AddQuad(from - offset, to - offset, to + offset, from + offset, colour);
        }

        private void AddVertex(Vector3 position, Vector4 colour)
        {
            _triangles.Add(position.X); _triangles.Add(position.Y); _triangles.Add(position.Z);
            _triangles.Add(colour.X); _triangles.Add(colour.Y); _triangles.Add(colour.Z); _triangles.Add(colour.W);
        }

        private void AddGlow(Vector3 centre, float radius, Vector4 colour)
        {
            ReadOnlySpan<Vector2> corners = [new(-1, -1), new(1, -1), new(1, 1), new(-1, -1), new(1, 1), new(-1, 1)];

            foreach (var corner in corners)
            {
                _glows.Add(centre.X); _glows.Add(centre.Y); _glows.Add(centre.Z);
                _glows.Add(corner.X); _glows.Add(corner.Y);
                _glows.Add(radius);
                _glows.Add(colour.X); _glows.Add(colour.Y); _glows.Add(colour.Z); _glows.Add(colour.W);
            }
        }

        // NOTE: Reorders the most recently added faces back to front
        private void SortFaces()
        {
            if (_faceOrder.Count < 2)
                return;

            var first = _faceOrder[0].Start;
            var span = CollectionsMarshal.AsSpan(_triangles)[first..];
            var copy = span.ToArray();

            _faceOrder.Sort((a, b) => a.Depth.CompareTo(b.Depth));

            var offset = 0;

            foreach (var (_, start, count) in _faceOrder)
            {
                copy.AsSpan(start - first, count).CopyTo(span[offset..]);
                offset += count;
            }
        }

        // ── GL ───────────────────────────────────────────────────────────────

        private void Draw()
        {
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _multisampleFramebuffer);
            _gl.Viewport(0, 0, (uint)_width, (uint)_height);
            _gl.Disable(EnableCap.DepthTest);
            _gl.Disable(EnableCap.ScissorTest);
            _gl.Disable(EnableCap.StencilTest);

            var page = Theme.Rgba(Theme.Page);
            _gl.ClearColor(page.X, page.Y, page.Z, 1f);
            _gl.Clear(ClearBufferMask.ColorBufferBit);

            var viewProjection = _viewProjection;

            // NOTE: Alpha accumulates to 1 so the texture stays opaque when Paper composites it
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);

            _gl.UseProgram(_flatProgram);
            _gl.UniformMatrix4(_flatViewProjection, 1, false, (float*)&viewProjection);
            Upload(_flatVao, _flatVbo, _triangles);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)(_triangles.Count / 7));

            _gl.BlendFuncSeparate(BlendingFactor.One, BlendingFactor.One, BlendingFactor.Zero, BlendingFactor.One);

            _gl.UseProgram(_glowProgram);
            _gl.UniformMatrix4(_glowViewProjection, 1, false, (float*)&viewProjection);
            _gl.Uniform2(_glowPixelToClip, 2f / _width, 2f / _height);
            Upload(_glowVao, _glowVbo, _glows);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint)(_glows.Count / 10));

            _gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _multisampleFramebuffer);
            _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _resolveFramebuffer);
            _gl.BlitFramebuffer(0, 0, _width, _height, 0, 0, _width, _height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Nearest);

            // NOTE: Hand the context back as Paper expects it (it sets its own viewport and blending each frame)
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            _gl.BindVertexArray(0);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            _gl.UseProgram(0);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        private void Upload(uint vao, uint vbo, List<float> data)
        {
            _gl.BindVertexArray(vao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

            var span = CollectionsMarshal.AsSpan(data);

            fixed (float* pointer = span)
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(span.Length * sizeof(float)), pointer, BufferUsageARB.DynamicDraw);
        }

        private void Resize(int width, int height)
        {
            _width = width;
            _height = height;

            _gl.BindTexture(TextureTarget.Texture2D, _texture);
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            _gl.BindTexture(TextureTarget.Texture2D, 0);

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _resolveFramebuffer);
            _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, _texture, 0);

            _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _multisampleColour);
            _gl.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, SAMPLES, InternalFormat.Rgba8, (uint)width, (uint)height);
            _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _multisampleFramebuffer);
            _gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, _multisampleColour);

            var status = _gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);

            if (status != GLEnum.FramebufferComplete)
                throw new InvalidOperationException($"Scene framebuffer is incomplete: {status}");

            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private (uint Vao, uint Vbo) CreateVertexArray(int[] attributeSizes)
        {
            var vao = _gl.GenVertexArray();
            var vbo = _gl.GenBuffer();

            _gl.BindVertexArray(vao);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

            var stride = (uint)(attributeSizes.Sum() * sizeof(float));
            var offset = 0;

            for (var i = 0; i < attributeSizes.Length; i++)
            {
                _gl.EnableVertexAttribArray((uint)i);
                _gl.VertexAttribPointer((uint)i, attributeSizes[i], VertexAttribPointerType.Float, false, stride, (void*)(offset * sizeof(float)));
                offset += attributeSizes[i];
            }

            _gl.BindVertexArray(0);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

            return (vao, vbo);
        }

        private uint CreateProgram(string vertexSource, string fragmentSource)
        {
            var vertex = CompileShader(ShaderType.VertexShader, vertexSource);
            var fragment = CompileShader(ShaderType.FragmentShader, fragmentSource);

            var program = _gl.CreateProgram();
            _gl.AttachShader(program, vertex);
            _gl.AttachShader(program, fragment);
            _gl.LinkProgram(program);

            _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var linked);

            if (linked == 0)
                throw new InvalidOperationException($"Scene shader failed to link: {_gl.GetProgramInfoLog(program)}");

            _gl.DetachShader(program, vertex);
            _gl.DetachShader(program, fragment);
            _gl.DeleteShader(vertex);
            _gl.DeleteShader(fragment);

            return program;
        }

        private uint CompileShader(ShaderType type, string source)
        {
            var shader = _gl.CreateShader(type);
            _gl.ShaderSource(shader, source);
            _gl.CompileShader(shader);

            _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var compiled);

            if (compiled == 0)
                throw new InvalidOperationException($"Scene {type} failed to compile: {_gl.GetShaderInfoLog(shader)}");

            return shader;
        }
    }
}
