using UnityEngine;
using System.Collections.Generic;

namespace MBaske
{
    public class Road : MonoBehaviour
    {
        // Spacing and offset as frame indices.
        public int AgentSpacing = 10;
        public int InitAgentOffset = 0;
        // Index2Deg for converting indices to degrees. 
        // Used for comparing agent positions via Mathf.DeltaAngle
        public float Index2Deg { get; private set; }
        // Total length: 4764.735m
        public Spline Spline { get; private set; }

        private struct VertexData
        {
            public Vector3[] innerVertices;
            public Vector3[] outerVertices;
        }
        [SerializeField]
        private TextAsset vertexData;
        private Frame[] frames;

        // Traffic cones.
        [SerializeField]
        private bool spawnCones;
        [SerializeField]
        private GameObject conePrefab;
        private Stack<GameObject> conePool;
        private Transform cones;

        public void Initialize()
        {
            if (frames == null)
            {
                GenerateFrames();
                if (spawnCones)
                {
                    InitCones();
                    RandomizeCones();
                }
            }
        }

        public void OnReset()
        {
            InitAgentOffset = Random.Range(0, frames.Length);
            if (spawnCones)
            {
                RandomizeCones();
            }
        }

        public Frame GetFrame(int index)
        {
            return frames[(index + frames.Length) % frames.Length];
        }

        public Frame UpdateInfo(IVehicleInfo info)
        {
            Spline.Point p = Spline.GetClosestPoint(info.Position, info.Frame.Index);
            return UpdateInfo(info, frames[p.Index]);
        }

        public Frame UpdateInfo(IVehicleInfo info, Frame frame, float disqualifyThresh = 5)
        {
            info.Frame = frame;

            // Normalized signed angle, 0 -> road direction, -1/+1 opp. direction.
            info.Angle = Vector3.SignedAngle(
                frame.Forward,
                Vector3.ProjectOnPlane(info.Forward, frame.Up),
                frame.Up);

            // Normalized signed deviation from middle of the road.
            Vector3 locPos = frame.LocalPosition(info.Position);
            info.Deviation = locPos.x / frame.Bounds.extents.x;

            info.IsOffRoad = Mathf.Abs(info.Deviation) > 1f;
            // disqualifyThresh = 5 -> bike is allowed max. 10m (5 x road extent) from mid.
            info.IsDisqualified = Mathf.Abs(info.Deviation) > disqualifyThresh;

            return frame;
        }

        // public float GetZDistance(ISplineLocation locA, ISplineLocation locB)
        // {
        //     Frame frame = GetFrame(locA.Index);
        //     // Partial z, first frame.
        //     float distance = frame.Bounds.extents.z - frame.LocalPosition(locA.Position).z;
        //     frame = GetFrame(locB.Index);
        //     // Partial z, last frame.
        //     distance += frame.Bounds.extents.z + frame.LocalPosition(locB.Position).z;
        //     // Inbetween frames.
        //     for (int i = locA.Index + 1; i < locB.Index; i++)
        //     {
        //         frame = GetFrame(locB.Index);
        //         distance += frame.Bounds.size.z;
        //     }
        //     return distance;
        // }

        // Technically, we could generate all frames from the vertex data directly,
        // because we have a lot of vertices (5000 per side) being close together (approx. 1m).
        // Using splines would make more sense if control points were sparse and frame
        // positions needed to be generated between them.

        private void GenerateFrames()
        {
            Vector3 worldPos = transform.position;
            VertexData vtxData = JsonUtility.FromJson<VertexData>(vertexData.text);
            int n = vtxData.innerVertices.Length;
            Index2Deg = 360f / (float)n;

            Spline inner = new Spline();
            Spline outer = new Spline();
            for (int i = 0; i < n; i++)
            {
                inner.AddControlPoint(worldPos + vtxData.innerVertices[i]);
                outer.AddControlPoint(worldPos + vtxData.outerVertices[i]);
            }
            inner.GenerateSplinePoints();
            outer.GenerateSplinePoints();

            Spline = new Spline();
            for (int i = 0; i < n; i++)
            {
                Spline.AddControlPoint(
                    (inner.GetPoint(i).Position + outer.GetPoint(i).Position) * 0.5f);
            }
            Spline.GenerateSplinePoints();

            frames = new Frame[n];
            for (int i = 0; i < n; i++)
            {
                frames[i] = Frame.FromLocation(Spline.GetPoint(i));
            }
            for (int i = 0; i < n; i++)
            {
                frames[i].Finalize(
                    inner.GetPoint(i).Position - outer.GetPoint(i).Position,
                    GetFrame(i + 1)
                );
            }
        }

        // TRAFFIC CONES

        private void InitCones()
        {
            conePool = new Stack<GameObject>();
            cones = new GameObject().transform;
            cones.name = "Cones";
            cones.parent = transform;
        }

        private void RandomizeCones()
        {
            for (int i = 0; i < cones.childCount; i++)
            {
                GameObject cone = cones.GetChild(i).gameObject;
                cone.SetActive(false);
                conePool.Push(cone);
            }

            int index = 0, spacing = 5, n = 0;
            do
            {
                index += Random.Range(100, 400);
                int length = Random.Range(5, 10);
                AddCones(index, length, spacing, ref n);
                index += length * spacing;
            }
            while (index < 4000);
        }

        private void AddCones(int startIndex, int length, int spacing, ref int n)
        {
            length *= spacing;
            float side = Mathf.Sign(Random.value - 0.5f);
            for (int i = 0; i <= length; i += spacing)
            {
                var frame = GetFrame(startIndex + i);
                // TBD difficulty
                float t = 0.8f - Mathf.Sin(i / (float)length * Mathf.PI) * 1.1f;
                GameObject cone = conePool.Count > 0
                    ? conePool.Pop()
                    : Instantiate(conePrefab, cones);
                cone.SetActive(true);
                cone.transform.position = frame.Position
                    + t * frame.Right * frame.Bounds.extents.x * side
                    + frame.Up * 0.5f;
                cone.transform.rotation = Quaternion.Euler(0, Random.Range(-180, 180), 0);
                n++;
            }
        }
    }
}