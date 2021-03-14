using System.Collections.Generic;
using System;
using UnityEngine;
using KdTree.Math;
using KdTree;
using Random = UnityEngine.Random;

namespace MBaske.Motorcycles
{
    public class Road : MonoBehaviour
    {
        private int m_NumAgents;
        private IList<DriverAgent> m_Agents;

        [Serializable]
        private struct VertexList
        {
            public Vector3[] innerVertices;
            public Vector3[] outerVertices;
        }

        [SerializeField]
        private TextAsset m_VertexData;
        private VertexList m_VertexList;
        private KdTree<float, Vector3> m_Tree;
        private int m_Length;

        private readonly int m_MinSpacing = 100;
        private readonly int m_MaxSpacing = 200;
        private readonly int m_MinLength = 5;
        private readonly int m_MaxLength = 20;

        [SerializeField]
        private GameObject m_Roadblock;
        [SerializeField]
        private GameObject m_Barrel;
        [SerializeField]
        private GameObject m_Cone;


        private void Awake()
        {
            ReadVertexData();
            m_NumAgents = FindObjectsOfType<DriverAgent>().Length;
        }

        public void OnEpisodeBegin(DriverAgent agent)
        {
            m_Agents ??= new List<DriverAgent>();
            m_Agents.Add(agent);

            Find(agent.transform.position, out Vector3 pos, out Vector3 fwd);
            agent.transform.position = pos;
            agent.transform.rotation = Quaternion.LookRotation(fwd);

            if (m_Agents.Count == m_NumAgents)
            {
                m_Agents.Clear();
                RandomizeObstacles();
            }
        }


        public void Find(Vector3 nearby, out Vector3 pos, out Vector3 fwd)
        {
            var nodes = m_Tree.GetNearestNeighbours(new[] { nearby.x, nearby.y, nearby.z }, 1);
            var p = nodes[0].Point;
            pos = new Vector3(p[0], p[1], p[2]);
            fwd = nodes[0].Value;
        }

        private void ReadVertexData()
        {
            m_VertexList = JsonUtility.FromJson<VertexList>(m_VertexData.text);
            m_Length = m_VertexList.innerVertices.Length;
            m_Tree = new KdTree<float, Vector3>(3, new FloatMath());

            for (int i = 0; i < m_Length; i++)
            {
                Vector3 inner = m_VertexList.innerVertices[i];
                Vector3 outer = m_VertexList.outerVertices[i];

                Vector3 fwd = Vector3.Cross(inner - outer, Vector3.up).normalized;
                Vector3 mid = (inner + outer) * 0.5f;

                m_Tree.Add(new[] { mid.x, mid.y, mid.z }, fwd);
            }
        }

        private void RandomizeObstacles()
        {
            ReadVertexData();
            var container = CreateContainer("Obstacles");

            int i = m_MinSpacing;
            do
            {
                var prefab = RandomBool(0.2f)
                    ? m_Roadblock
                    : (RandomBool(0.5f)
                        ? m_Barrel
                        : m_Cone);
                bool side = RandomBool(0.5f);
                bool block = prefab == m_Roadblock;

                int n = Random.Range(m_MinLength, m_MaxLength + 1) * 2;
                int min = block ? n / 2 : 0;
                int max = block ? min + 1 : n;

                for (int j = min; j < max; j += 2)
                {
                    Vector3 inner = m_VertexList.innerVertices[(i + j) % m_Length];
                    Vector3 outer = m_VertexList.outerVertices[(i + j) % m_Length];
                    Vector3 fwd = Vector3.Cross(inner - outer, Vector3.up);
                    float t = j / (float)n * 0.5f + 0.05f; 
                    Vector3 p = Vector3.Lerp(side ? inner : outer, side ? outer : inner, t);
                    Instantiate(prefab, p, Quaternion.LookRotation(fwd), container);
                }

                i += Random.Range(m_MinSpacing, m_MaxSpacing + 1);
            }
            while (i < m_Length - m_MinSpacing);
        }

        private Transform CreateContainer(string name)
        {
            var tf = transform.Find(name);
            if (tf != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(tf.gameObject);
                }
                else
                {
                    DestroyImmediate(tf.gameObject);
                }
            }

            var container = new GameObject(name).transform;
            container.parent = transform;
            return container;
        }

        private static bool RandomBool(float probability = 0.5f)
        {
            return Random.value < probability;
        }
    }
}