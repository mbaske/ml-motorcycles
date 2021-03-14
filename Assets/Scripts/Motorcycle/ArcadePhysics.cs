using UnityEngine;
using KSPWheel;

namespace MBaske.Motorcycles
{
    [RequireComponent(typeof(Rigidbody))]
    public class ArcadePhysics : MonoBehaviour
    {
        public Vector3 Velocity => m_Rigidbody.velocity;
        public Vector3 AngularVelocity => m_Rigidbody.angularVelocity;
        public Vector3 Inclination => new Vector3(m_COM.right.y, m_COM.up.y, m_COM.forward.y);
        public bool IsGrounded => WheelF.IsGrounded || WheelR.IsGrounded;

        public KSPWheelComponent WheelF;
        public KSPWheelComponent WheelR;

        [SerializeField, Tooltip("Center of mass")]
        private Transform m_COM;
        private Rigidbody m_Rigidbody;

        [SerializeField]
        private Transform[] m_Stabilizers;
        [SerializeField, Range(0f, 1f)]
        private float m_Stabilize = 1f;
        private const float targetGroundDistance = 0.71f;

        [SerializeField, Tooltip("Speed -> Steering tilt amount")]
        private AnimationCurve m_SpeedTilt;

        public void Initialize()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Rigidbody.centerOfMass = transform.InverseTransformPoint(m_COM.position);

            WheelF.Initialize();
            WheelR.Initialize();
        }

        public void ManagedReset()
        {
            WheelF.ManagedReset();
            WheelR.ManagedReset();
            m_Rigidbody.velocity = Vector3.zero;
            m_Rigidbody.angularVelocity = Vector3.zero;
        }

        public void ApplyActions(float[] actions, float deltaTime)
        {
            float throttle = actions[0] > 0f ? actions[0] : 0f;
            float rearBrake = actions[0] < 0f ? -actions[0] : 0f;
            float frontBrake = actions[1] > 0f ? actions[1]: 0f; // ignores < 0
            float steering = actions[2];

            WheelR.UpdateWheel(throttle, rearBrake, 0, deltaTime);
            WheelF.UpdateWheel(-1f, frontBrake, steering, deltaTime);
        }

        public Vector3 Localize(Vector3 v)
        {
            return m_COM.InverseTransformVector(v);
        }

        public void Stabilize()
        {
            float steer = WheelF.NormSteer;
            Vector3 locAV = Localize(AngularVelocity);

            if (IsGrounded)
            {
                Vector3 counterTorque = Vector3.back * locAV.z
                      + Vector3.down * locAV.y * (1f - Mathf.Abs(steer));
                m_Rigidbody.AddRelativeTorque(counterTorque * m_Stabilize, ForceMode.VelocityChange);
                float speed = Velocity.magnitude;
                // Tilt factor for the current speed, NOT an angle.
                float tiltFactor = m_SpeedTilt.Evaluate(speed); 

                for (int i = 0; i < m_Stabilizers.Length; i++)
                {
                    Transform s = m_Stabilizers[i];
                    Vector3 pos = s.position;
                    Vector3 dir = -s.up;
                    if (Physics.Raycast(pos, dir, out RaycastHit hit, 2f, LayerMasks.Ground))
                    {
                        bool isFront = s.localPosition.z > 0;
                        // Braking reduces stabilization at opposit end.
                        float brakeRdct = (isFront ? WheelR.BrakeRatio : WheelF.BrakeRatio) * 0.8f;
                        float brakeMult = 1f - brakeRdct * Mathf.Min(speed / 3f, 1f);
                        // Tilt is more pronounced in front than at the rear.
                        float tiltBias = isFront ? 1.1f : 0.9f;
                        // Reduce stabilization at slope.
                        float slopeMult = Mathf.Max(0, hit.normal.y);
                        // Tilt is symmetrically scaled with the steering amount.
                        float tiltAmount = steer * Mathf.Sign(s.localPosition.x) * tiltFactor;
                        // The down vector offset we need to compensate at each stabilizer.
                        float error = targetGroundDistance - hit.distance - tiltAmount * tiltBias;
                        m_Rigidbody.AddForceAtPosition(dir * -error * m_Stabilize * brakeMult * slopeMult,
                            pos, ForceMode.VelocityChange);
                        // Debug.Log(hit.distance);
                    }
                }
            }
            else
            {
                Vector3 keepUpright = Vector3.back * Pow(m_COM.right.y) + Vector3.right * Pow(m_COM.forward.y);
                m_Rigidbody.AddRelativeTorque((keepUpright - locAV * 0.25f) * m_Stabilize, ForceMode.VelocityChange);
            }
        }

        private static float Pow(float val, float exp = 2f)
        {
            return Mathf.Pow(Mathf.Abs(val), exp) * Mathf.Sign(val);
        }
    }
}