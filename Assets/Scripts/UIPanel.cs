using UnityEngine;
using UnityEngine.UI;

namespace MBaske
{
    public class UIPanel : MonoBehaviour
    {
        [SerializeField]
        private MotorcycleAgent agent;

        [SerializeField]
        private Text text;
        private ArcadePhysics physics;

        [SerializeField]
        private UIBar throttle;
        [SerializeField]
        private UIBar steering;
        [SerializeField]
        private UIBar rearBrake;
        [SerializeField]
        private UIBar frontBrake;
        [SerializeField]
        private UIBar speed;
        [SerializeField]
        private UIBar angle;
        [SerializeField]
        private UIBar deviation;

        private void Awake()
        {
            if (agent == null)
            {
                agent = FindObjectsOfType<MotorcycleAgent>()[0];
            }
            physics = agent.GetComponent<ArcadePhysics>();
        }

        private void Update()
        {
            float s = Mathf.Min(physics.Velocity.magnitude, 35f);
            text.text = $"THROTTLE\nSTEERING\nREAR BRAKE\nFRONT BRAKE\n\nSPEED {Mathf.RoundToInt(s * 3.6f)} kph\nANGLE\nDEVIATION";
            speed.SetValue(s / 17.5f - 1f);

            throttle.SetValue(physics.WheelR.NormMotorTorque);
            steering.SetValue(physics.WheelF.NormSteer);
            rearBrake.SetValue(physics.WheelR.NormBrakeTorque);
            frontBrake.SetValue(physics.WheelF.NormBrakeTorque);
            deviation.SetValue(Mathf.Clamp(agent.Info.Deviation, -1f, 1f));
            angle.SetValue(agent.Info.Angle / 90f);
        }
    }
}


