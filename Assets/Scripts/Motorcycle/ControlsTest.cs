using UnityEngine;

namespace MBaske.Motorcycles
{
    public class ControlsTest : MonoBehaviour
    {
        private ArcadePhysics m_Physics;

        private void Awake()
        {
            m_Physics = GetComponent<ArcadePhysics>();
            m_Physics.Initialize();
        }

        private void FixedUpdate()
        {
            m_Physics.ApplyActions(new float[]
                {
                    Input.GetAxis("Vertical"),
                    Input.GetAxis("Jump"),
                    Input.GetAxis("Horizontal")
                },
                Time.fixedDeltaTime
                );

            m_Physics.Stabilize();
        }
    }
}