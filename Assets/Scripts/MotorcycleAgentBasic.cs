using UnityEngine;
using Unity.MLAgents;

namespace MBaske
{
    [RequireComponent(typeof(ArcadePhysics))]
    public class MotorcycleAgentBasic : Agent
    {
        protected ArcadePhysics physics;
        private Resetter resetter;

        public override void Initialize()
        {
            resetter = new Resetter(transform);
            physics = GetComponent<ArcadePhysics>();
            physics.Initialize();
        }

        public override void OnActionReceived(float[] actions)
        {
            physics.OnAgentAction(actions, Time.fixedDeltaTime);
            physics.Stabilize();
        }

        public override void Heuristic(float[] actions)
        {
            actions[0] = Input.GetAxis("Vertical"); // < 0 rear brake / > 0 throttle
            actions[0] = Mathf.Abs(actions[0]) > Mathf.Epsilon ? actions[0] : 0f;
            actions[1] = Input.GetKey(KeyCode.LeftShift) ? 1 : 0; // front brake
            actions[2] = Input.GetAxis("Horizontal"); // steering
            
            if (Input.GetKey(KeyCode.Space))
            {
                // both brakes
                actions[0] = -1;
                actions[1] = 1;
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                ResetAgent(true);
            }
        }

        public virtual void ResetAgent(bool initPos = false)
        {
            physics.OnReset();

            if (initPos)
            {
                resetter.Reset();
            }
        }
    }
}

