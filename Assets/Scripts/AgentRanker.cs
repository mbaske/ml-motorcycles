using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

namespace MBaske
{
    public class AgentRanker : MonoBehaviour
    {
        private class agentComparer : IComparer<MotorcycleAgent>
        {
            int IComparer<MotorcycleAgent>.Compare(MotorcycleAgent a, MotorcycleAgent b)
            {
                return a.Compare.CompareTo(b.Compare);
            }
        }
        private static IComparer<MotorcycleAgent> comparer = new agentComparer();

        public MotorcycleAgent First { get; private set; }
        public MotorcycleAgent Last { get; private set; }
        private List<MotorcycleAgent> agents;

        [SerializeField, Tooltip("Penalty for being passed by other agents")]
        private float loseRankPenalty = -1f;
        [SerializeField, Tooltip("Check agent order every [value] steps")]
        private int updateRankInterval = 10;
        [SerializeField, Tooltip("Reset all agents every [value] steps")]
        private int forceResetInterval = -1;
        [SerializeField, Tooltip("Check agent spacing every [value] steps")]
        private int checkSpacingInterval = 60;
        [SerializeField, Tooltip("Reset all agents if first and last are more than [value] degrees apart")]
        private float maxSpacingDegrees = 30;

        public void AddAgent(MotorcycleAgent agent)
        {
            if (agents == null)
            {
                agents = new List<MotorcycleAgent>();
                Academy.Instance.AgentPreStep += OnAgentPreStep;
            }
            agents.Add(agent);
        }

        private void OnDestroy()
        {
            if (Academy.IsInitialized)
            {
                Academy.Instance.AgentPreStep -= OnAgentPreStep;
            }
        }

        private void OnAgentPreStep(int academyStepCount)
        {
            if (academyStepCount > 0)
            {
                if (forceResetInterval > 0 && academyStepCount % forceResetInterval == 0)
                {
                    ResetAll();
                }
                else if (checkSpacingInterval > 0 && academyStepCount % checkSpacingInterval == 0)
                {
                    if (Mathf.DeltaAngle(Last.Degrees, First.Degrees) > maxSpacingDegrees)
                    {
                        ResetAll();
                    }
                }
                else if (academyStepCount % updateRankInterval == 0)
                {
                    UpdateRanks(true);
                }
            }
        }

        private void ResetAll()
        {
            FindObjectOfType<Road>().OnReset();
            // Reset will modify agents list.
            List<MotorcycleAgent> tmp = new List<MotorcycleAgent>(agents);
            foreach (MotorcycleAgent agent in tmp)
            {
                agent.ResetAgent(true);
            }
        }

        public void UpdateRanks(bool penalize)
        {
            Last = agents[0];
            int n = agents.Count;
            for (int i = 1; i < n; i++)
            {
                if (Mathf.DeltaAngle(Last.Degrees, agents[i].Degrees) < 0)
                {
                    Last = agents[i];
                }
            }

            Vector3 pos = Last.transform.position;
            foreach (MotorcycleAgent agent in agents)
            {
                agent.Compare = (agent.transform.position - pos).sqrMagnitude;
            }
            agents.Sort(comparer);

            First = agents[n - 1];
            for (int i = 0; i < n; i++)
            {
                if (penalize && agents[i].Rank > i)
                {
                    // Debug.Log($"Rank change: {agents[i].name} {agents[i].Rank} -> {i}");
                    agents[i].AddReward(loseRankPenalty);
                }
                agents[i].Rank = i;
            }
        }
    }
}