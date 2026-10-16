using System.Numerics;

namespace Sift.App.Scene
{
    // NOTE: An orthographic camera over the grid. World units are cells: X right, Y up (away on screen once tilted),
    // and layers stacked along Z, LayerSpacing apart. The 2D and isometric views are just different poses of this
    // camera, so switching between them is an animation of these values
    public sealed class SceneCamera
    {
        public readonly record struct Pose(Vector3 Target, float Zoom, float Yaw, float Tilt, float LayerSpacing)
        {
            public static Pose Lerp(Pose from, Pose to, float amount) =>
                new(Vector3.Lerp(from.Target, to.Target, amount),
                    from.Zoom + (to.Zoom - from.Zoom) * amount,
                    from.Yaw + (to.Yaw - from.Yaw) * amount,
                    from.Tilt + (to.Tilt - from.Tilt) * amount,
                    from.LayerSpacing + (to.LayerSpacing - from.LayerSpacing) * amount);
        }

        private const double TRANSITION_SECONDS = 0.45;

        private Pose _from;
        private Pose _to;
        private double _transitionStartedAt = double.NegativeInfinity;

        public Pose Current { get; private set; }

        public bool IsAnimating { get; private set; }

        public SceneCamera(Pose initial)
        {
            Current = _from = _to = initial;
        }

        // NOTE: The pose the camera is heading to, for adjusting (e.g. panning) without waiting for an animation
        public Pose Destination => _to;

        public void AnimateTo(Pose pose, double now)
        {
            _from = Current;
            _to = pose;
            _transitionStartedAt = now;
            IsAnimating = true;
        }

        public void JumpTo(Pose pose)
        {
            Current = _from = _to = pose;
            IsAnimating = false;
        }

        public void Update(double now)
        {
            if (!IsAnimating)
                return;

            var amount = Math.Clamp((now - _transitionStartedAt) / TRANSITION_SECONDS, 0, 1);
            Current = Pose.Lerp(_from, _to, EaseInOut((float)amount));

            if (amount >= 1)
                IsAnimating = false;
        }

        public Matrix4x4 View =>
            Matrix4x4.CreateTranslation(-Current.Target)
            * Matrix4x4.CreateRotationZ(-Current.Yaw)
            * Matrix4x4.CreateRotationX(-Current.Tilt);

        // NOTE: Width and height in logical pixels, Zoom is logical pixels per cell
        public Matrix4x4 Projection(float width, float height)
        {
            var halfWidth = width / 2f / Current.Zoom;
            var halfHeight = height / 2f / Current.Zoom;

            return Matrix4x4.CreateOrthographicOffCenter(-halfWidth, halfWidth, -halfHeight, halfHeight, -1000f, 1000f);
        }

        public Matrix4x4 ViewProjection(float width, float height) =>
            View * Projection(width, height);

        // NOTE: The world-space directions of the screen's right and towards-the-viewer axes
        public Vector3 Right => new(View.M11, View.M21, View.M31);

        public Vector3 Forward => new(View.M13, View.M23, View.M33);

        // NOTE: Larger is nearer the viewer, for sorting back to front
        public float Depth(Vector3 world) =>
            Vector3.Transform(world, View).Z;

        public float LayerHeight(int layer) =>
            layer * Current.LayerSpacing;

        // NOTE: Where a point in the view (logical pixels from the top left) lands on a layer's plane
        public bool TryPick(float x, float y, float width, float height, float planeZ, out Vector3 world)
        {
            world = default;

            if (!Matrix4x4.Invert(ViewProjection(width, height), out var inverse))
                return false;

            var ndcX = x / width * 2f - 1f;
            var ndcY = 1f - y / height * 2f;

            var near = Vector4.Transform(new Vector4(ndcX, ndcY, 0f, 1f), inverse);
            var far = Vector4.Transform(new Vector4(ndcX, ndcY, 1f, 1f), inverse);
            var start = new Vector3(near.X, near.Y, near.Z) / near.W;
            var end = new Vector3(far.X, far.Y, far.Z) / far.W;

            if (MathF.Abs(end.Z - start.Z) < 1e-6f)
                return false;

            var amount = (planeZ - start.Z) / (end.Z - start.Z);
            world = start + (end - start) * amount;
            return true;
        }

        // NOTE: How many logical pixels a world point moves on screen per cell, for keeping sizes constant in pixels
        public float PixelsToWorld(float pixels) =>
            pixels / Current.Zoom;

        private static float EaseInOut(float amount) =>
            amount < 0.5f ? 4f * amount * amount * amount : 1f - MathF.Pow(-2f * amount + 2f, 3f) / 2f;
    }
}
