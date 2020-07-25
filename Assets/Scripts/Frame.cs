using UnityEngine;

namespace MBaske
{
    public class Frame : ISplineLocation
    {
        public int Index { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Right { get; private set; }
        public Vector3 Up { get; private set; }
        public Vector3 Forward { get; private set; }
        public Bounds Bounds { get; private set; }
        public Quaternion Rotation { get; private set; }
        public Matrix4x4 Matrix { get; private set; }

        public float Curvature;

        public static Frame FromLocation(ISplineLocation loc)
        {
            Frame frame = new Frame();
            frame.Index = loc.Index;
            frame.Position = loc.Position;
            return frame;
        }

        public void Finalize(Vector3 orthogonal, Frame next)
        {
            Vector3 delta = next.Position - Position;
            Bounds = new Bounds(Position, // TODO bounds height?
                new Vector3(orthogonal.magnitude, 0, delta.magnitude));
            Forward = delta.normalized;
            Right = orthogonal.normalized;
            Up = Vector3.Cross(Forward, Right).normalized;
            Rotation = Quaternion.LookRotation(Forward, Up);
            Matrix = Matrix4x4.TRS(Position, Rotation, Vector3.one);

            Curvature = Mathf.Sign(Vector3.SignedAngle(Forward, next.Forward, Up));
        }

        public Vector3 LocalDirection(Vector3 worldDirection)
        {
            return Matrix.inverse.MultiplyVector(worldDirection);
        }

        public Vector3 LocalPosition(Vector3 worldPositon)
        {
            return Matrix.inverse.MultiplyPoint3x4(worldPositon);
        }

        public Vector3 WorldDirection(Vector3 localDirection)
        {
            return Matrix.MultiplyVector(localDirection);
        }

        public Vector3 WorldPosition(Vector3 localPositon)
        {
            return Matrix.MultiplyPoint3x4(localPositon);
        }

        public void DebugDraw(float duration = 0)
        {
            Debug.DrawRay(Position, Right * Bounds.extents.x, Color.red, duration);
            Debug.DrawRay(Position, Up, Color.green, duration);
            Debug.DrawRay(Position, Forward * Bounds.extents.z, Color.blue, duration);
        }
    }
}