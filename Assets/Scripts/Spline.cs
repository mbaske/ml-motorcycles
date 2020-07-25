using UnityEngine;
using System.Collections.Generic;

namespace MBaske
{
    public interface ISplineLocation
    {
        int Index { get; set; }
        Vector3 Position { get; set; }
    }
    
    // Adapted from original code by JPBotelho https://github.com/JPBotelho/Catmull-Rom-Splines

    /*  
        Catmull-Rom splines are Hermite curves with special tangent values.
        Hermite curve formula:
        (2t^3 - 3t^2 + 1) * p0 + (t^3 - 2t^2 + t) * m0 + (-2t^3 + 3t^2) * p1 + (t^3 - t^2) * m1
        For points p0 and p1 passing through points m0 and m1 interpolated over t = [0, 1]
        Tangent M[k] = (P[k+1] - P[k-1]) / 2
    */
    public class Spline
    {
        public class Point : ISplineLocation
        {
            public Vector3 Position { get; set; }
            public Vector3 Tangent { get; set; }
            public int Index { get; set; }

            public Point(Vector3 position, Vector3 tangent)
            {
                Position = position;
                Tangent = tangent;
            }
        }

        public int Count { get; private set; }

        private int resolution;
        private Point[] splinePoints;
        private List<Vector3> controlPoints;

        public Point GetPoint(int index)
        {
            return splinePoints[(index + Count) % Count];
        }

        public Spline(int resolution = 1)
        {
            controlPoints = new List<Vector3>();
            this.resolution = resolution;
        }

        public void AddControlPoint(Vector3 p)
        {
            controlPoints.Add(p);
        }

        public void DrawSpline(Color color, float duration = 0)
        {
            for (int i = 0; i < Count - 1; i++)
            {
                Debug.DrawLine(splinePoints[i].Position, splinePoints[i + 1].Position, color, duration);
            }
            Debug.DrawLine(splinePoints[0].Position, splinePoints[Count - 1].Position, color, duration);
        }

        public void DrawTangents(float extrusion, Color color, float duration = 0)
        {
            for (int i = 0; i < Count; i++)
            {
                Debug.DrawRay(splinePoints[i].Position, splinePoints[i].Tangent * extrusion, color, duration);
            }
        }

        public void GenerateSplinePoints()
        {
            int n = controlPoints.Count;
            Count = resolution * n;
            splinePoints = new Point[Count];
            float pointStep = 1 / (float)resolution;

            Vector3 p0, p1, p2, p3;
            Vector3 m0, m1;

            for (int i = 0; i < n; i++)
            {
                p0 = controlPoints[(i - 1 + n) % n];
                p1 = controlPoints[i];
                p2 = controlPoints[(i + 1) % n];
                p3 = controlPoints[(i + 2) % n];

                m0 = (p2 - p0) * 0.5f;
                m1 = (p3 - p1) * 0.5f;

                for (int j = 0; j < resolution; j++)
                {
                    float t = j * pointStep;
                    Point point = Evaluate(p1, p2, m0, m1, t);
                    point.Index = i * resolution + j;
                    splinePoints[point.Index] = point;
                }
            }
        }

        // Evaluates curve at t[0, 1]. Returns point/normal/tan struct. [0, 1] means clamped between 0 and 1.
        public static Point Evaluate(Vector3 start, Vector3 end, Vector3 tanPoint1, Vector3 tanPoint2, float t)
        {
            Vector3 position = CalculatePosition(start, end, tanPoint1, tanPoint2, t);
            Vector3 tangent = CalculateTangent(start, end, tanPoint1, tanPoint2, t);

            return new Point(position, tangent);
        }

        // Calculates curve position at t[0, 1]
        public static Vector3 CalculatePosition(Vector3 start, Vector3 end, Vector3 tanPoint1, Vector3 tanPoint2, float t)
        {
            //  Hermite curve formula:
            //  (2t^3 - 3t^2 + 1) * p0 + (t^3 - 2t^2 + t) * m0 + (-2t^3 + 3t^2) * p1 + (t^3 - t^2) * m1
            Vector3 position = (2.0f * t * t * t - 3.0f * t * t + 1.0f) * start
                + (t * t * t - 2.0f * t * t + t) * tanPoint1
                + (-2.0f * t * t * t + 3.0f * t * t) * end
                + (t * t * t - t * t) * tanPoint2;

            return position;
        }

        // Calculates tangent at t[0, 1]
        public static Vector3 CalculateTangent(Vector3 start, Vector3 end, Vector3 tanPoint1, Vector3 tanPoint2, float t)
        {
            //  Calculate tangents
            //  p'(t) = (6t² - 6t)p0 + (3t² - 4t + 1)m0 + (-6t² + 6t)p1 + (3t² - 2t)m1
            Vector3 tangent = (6 * t * t - 6 * t) * start
                + (3 * t * t - 4 * t + 1) * tanPoint1
                + (-6 * t * t + 6 * t) * end
                + (3 * t * t - 2 * t) * tanPoint2;

            return tangent.normalized;
        }


        // SEARCH

        private const int searchIncr = 10;

        public Point GetClosestPoint(Vector3 pos)
        {
            // Can't use C# List<T>.BinarySearch over the entire spline, because nearby points
            // aren't necessarily neighbouring points on a spline that curves in on itself.
            // We need to constrain the min/max indices first. Absent a seed index for a known 
            // previous point, we run a coarse incremental search over the whole spline 
            // in order to narrow down the search space.
            return GetClosestPoint(pos, IncrSearch(pos));
        }

        public Point GetClosestPoint(Vector3 pos, int seedIndex)
        {
            // Check the most likely options first, before proceeding with binary search.
            if (IsClosestIndex(pos, seedIndex))
            {
                return GetPoint(seedIndex); // hasn't moved to new spline point
            }
            else if (IsClosestIndex(pos, seedIndex + 1))
            {
                return GetPoint(seedIndex + 1); // has moved to next point
            }

            return GetPoint(BinSearch(pos, seedIndex - searchIncr, seedIndex + searchIncr));
        }

        private bool IsClosestIndex(Vector3 pos, int index)
        {
            float d = (GetPoint(index).Position - pos).sqrMagnitude;
            return (d < (GetPoint(index - 1).Position - pos).sqrMagnitude &&
                    d < (GetPoint(index + 1).Position - pos).sqrMagnitude);
        }

        private int IncrSearch(Vector3 pos)
        {
            int index = 0;
            float closest = Mathf.Infinity;
            for (int i = 0; i < Count; i += searchIncr)
            {
                float d = (GetPoint(i).Position - pos).sqrMagnitude;
                if (d < closest)
                {
                    closest = d;
                    index = i;
                }
            }
            return index;
        }

        private int BinSearch(Vector3 pos, int min, int max)
        {
            bool b = (pos - GetPoint(min).Position).sqrMagnitude < (pos - GetPoint(max).Position).sqrMagnitude;
            int delta = max - min, mid = min + delta / 2;
            return delta == 1 ? (b ? min : max) : BinSearch(pos, b ? min : mid, b ? mid : max);
        }
    }
}