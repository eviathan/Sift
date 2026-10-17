using System.Numerics;

namespace Sift.App.Scene
{
    // NOTE: Small vector icons drawn on a tile's top face, one per component type. Shapes are in a unit square
    // (-1..1, y up) mapped onto the tile, so they scale with zoom and tilt with the board
    public sealed partial class SceneRenderer
    {
        private const int CIRCLE_SEGMENTS = 18;

        private Vector3 _iconCentre;
        private float _iconScale;
        private Vector4 _iconColour;

        private void AddIcon(string type, Vector3 centre, float scale, Vector4 colour)
        {
            _iconCentre = centre;
            _iconScale = scale;
            _iconColour = colour;

            switch (type)
            {
                case "emit":
                    Triangle(new(-0.55f, -0.7f), new(0.75f, 0f), new(-0.55f, 0.7f));
                    break;

                case "clock":
                    Ring(0f, 0f, 0.8f, 0.2f);
                    Bar(new(0f, 0f), new(0f, 0.5f), 0.18f);
                    Bar(new(0f, 0f), new(0.38f, 0f), 0.18f);
                    break;

                case "redirect":
                    Bar(new(-0.6f, -0.6f), new(-0.6f, 0.25f), 0.24f);
                    Bar(new(-0.6f, 0.25f), new(0.25f, 0.25f), 0.24f);
                    Triangle(new(0.2f, -0.15f), new(0.75f, 0.25f), new(0.2f, 0.65f));
                    break;

                case "split":
                    Bar(new(-0.7f, 0f), new(0.7f, 0f), 0.24f);
                    Bar(new(0f, -0.7f), new(0f, 0.7f), 0.24f);
                    break;

                case "rotate":
                    Ring(0f, 0f, 0.62f, 0.2f);
                    Triangle(new(0.3f, 0.45f), new(0.95f, 0.45f), new(0.62f, -0.05f));
                    break;

                case "chance":
                    Ring(0f, 0f, 0.8f, 0.16f, segments: 4, rotation: MathF.PI / 4f);
                    Disc(-0.28f, 0.28f, 0.15f);
                    Disc(0.28f, -0.28f, 0.15f);
                    break;

                case "scatter":
                    Disc(0f, 0f, 0.17f);
                    Disc(-0.55f, 0.55f, 0.14f);
                    Disc(0.55f, 0.5f, 0.14f);
                    Disc(-0.5f, -0.55f, 0.14f);
                    Disc(0.6f, -0.45f, 0.14f);
                    break;

                case "divide":
                    Disc(0f, 0.5f, 0.17f);
                    Bar(new(-0.65f, 0f), new(0.65f, 0f), 0.22f);
                    Disc(0f, -0.5f, 0.17f);
                    break;

                case "gate":
                    Bar(new(-0.32f, -0.65f), new(-0.32f, 0.65f), 0.28f);
                    Bar(new(0.32f, -0.65f), new(0.32f, 0.65f), 0.28f);
                    break;

                case "toggle":
                    Ring(0f, 0f, 0.7f, 0.18f);
                    Disc(-0.22f, 0f, 0.38f);
                    break;

                case "delay":
                    Triangle(new(-0.55f, 0.7f), new(0.55f, 0.7f), new(0f, 0f));
                    Triangle(new(0f, 0f), new(0.55f, -0.7f), new(-0.55f, -0.7f));
                    break;

                case "move":
                    Chevron(-0.3f);
                    Chevron(0.25f);
                    break;

                case "transpose":
                    Bar(new(0f, -0.7f), new(0f, 0.2f), 0.24f);
                    Triangle(new(-0.5f, 0.15f), new(0.5f, 0.15f), new(0f, 0.75f));
                    break;

                case "midi":
                    Disc(-0.22f, -0.42f, 0.3f);
                    Bar(new(0.04f, -0.42f), new(0.04f, 0.68f), 0.16f);
                    Bar(new(0.04f, 0.68f), new(0.45f, 0.38f), 0.16f);
                    break;

                case "notes":
                    Disc(-0.48f, -0.48f, 0.24f);
                    Disc(0.32f, -0.3f, 0.24f);
                    Bar(new(-0.28f, -0.48f), new(-0.28f, 0.55f), 0.14f);
                    Bar(new(0.52f, -0.3f), new(0.52f, 0.72f), 0.14f);
                    Bar(new(-0.28f, 0.55f), new(0.52f, 0.72f), 0.2f);
                    break;

                case "chord":
                    Disc(0f, -0.52f, 0.24f);
                    Disc(0f, 0f, 0.24f);
                    Disc(0f, 0.52f, 0.24f);
                    break;

                case "log":
                    Bar(new(-0.6f, 0.45f), new(0.6f, 0.45f), 0.18f);
                    Bar(new(-0.6f, 0f), new(0.6f, 0f), 0.18f);
                    Bar(new(-0.6f, -0.45f), new(0.25f, -0.45f), 0.18f);
                    break;

                default:
                    Disc(0f, 0f, 0.3f);
                    break;
            }
        }

        private Vector3 IconPoint(float u, float v) =>
            _iconCentre + new Vector3(u * _iconScale, v * _iconScale, 0f);

        private void Triangle(Vector2 a, Vector2 b, Vector2 c)
        {
            AddVertex(IconPoint(a.X, a.Y), _iconColour);
            AddVertex(IconPoint(b.X, b.Y), _iconColour);
            AddVertex(IconPoint(c.X, c.Y), _iconColour);
        }

        private void Disc(float u, float v, float radius)
        {
            for (var i = 0; i < CIRCLE_SEGMENTS; i++)
            {
                var a0 = i * MathF.Tau / CIRCLE_SEGMENTS;
                var a1 = (i + 1) * MathF.Tau / CIRCLE_SEGMENTS;

                Triangle(new(u, v), new(u + MathF.Cos(a0) * radius, v + MathF.Sin(a0) * radius), new(u + MathF.Cos(a1) * radius, v + MathF.Sin(a1) * radius));
            }
        }

        private void Ring(float u, float v, float radius, float width, int segments = CIRCLE_SEGMENTS, float rotation = 0f)
        {
            var inner = radius - width / 2f;
            var outer = radius + width / 2f;

            for (var i = 0; i < segments; i++)
            {
                var a0 = rotation + i * MathF.Tau / segments;
                var a1 = rotation + (i + 1) * MathF.Tau / segments;
                var (c0, s0, c1, s1) = (MathF.Cos(a0), MathF.Sin(a0), MathF.Cos(a1), MathF.Sin(a1));

                Triangle(new(u + c0 * inner, v + s0 * inner), new(u + c0 * outer, v + s0 * outer), new(u + c1 * outer, v + s1 * outer));
                Triangle(new(u + c0 * inner, v + s0 * inner), new(u + c1 * outer, v + s1 * outer), new(u + c1 * inner, v + s1 * inner));
            }
        }

        // NOTE: A thick line segment
        private void Bar(Vector2 from, Vector2 to, float width)
        {
            var direction = Vector2.Normalize(to - from);
            var normal = new Vector2(-direction.Y, direction.X) * (width / 2f);
            var cap = direction * (width / 2f);

            var a = from - cap - normal;
            var b = to + cap - normal;
            var c = to + cap + normal;
            var d = from - cap + normal;

            Triangle(a, b, c);
            Triangle(a, c, d);
        }

        private void Chevron(float u)
        {
            Bar(new(u - 0.3f, 0.55f), new(u + 0.2f, 0f), 0.24f);
            Bar(new(u + 0.2f, 0f), new(u - 0.3f, -0.55f), 0.24f);
        }
    }
}
