using UnityEngine;
using KSPWheel;

namespace MBaske
{
    public interface IPhysicsInfo
    {
        Vector3 Velocity { get; }
        Vector3 AngularVelocity { get; }
        Vector3 Inclination3 { get; }
        float Inclination { get; }
        bool IsGrounded { get; }
    }

    [RequireComponent(typeof(Rigidbody))]
    public class ArcadePhysics : MonoBehaviour, IPhysicsInfo
    {
        public Vector3 Velocity => rb.velocity;
        public Vector3 AngularVelocity => rb.angularVelocity;
        public Vector3 Inclination3 => new Vector3(COM.right.y, COM.up.y, COM.forward.y);
        public float Inclination => Mathf.Max(0, COM.up.y);
        public bool IsGrounded => WheelF.IsGrounded || WheelR.IsGrounded;

        public KSPWheelComponent WheelF;
        public KSPWheelComponent WheelR;

        [SerializeField, Tooltip("Center of mass")]
        private Transform COM;
        private Rigidbody rb;

        [SerializeField]
        private Transform[] stabilizers;
        [SerializeField, Range(0f, 1f)]
        private float stabilize = 1f;
        private const float targetGroundDistance = 0.71f;

        [SerializeField, Tooltip("Speed -> Steering tilt amount")]
        private AnimationCurve speedTilt;

        public void Initialize()
        {
            rb = GetComponent<Rigidbody>();
            rb.centerOfMass = transform.InverseTransformPoint(COM.position);

            WheelF.Initialize();
            WheelR.Initialize();
        }

        public void OnReset()
        {
            WheelF.OnReset();
            WheelR.OnReset();
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        public void OnAgentAction(float[] actions, float deltaTime)
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
            return COM.InverseTransformVector(v);
        }

        public void Stabilize()
        {
            float steer = WheelF.NormSteer;
            Vector3 locAV = Localize(AngularVelocity);

            if (IsGrounded)
            {
                Vector3 counterTorque = Vector3.back * locAV.z
                      + Vector3.down * locAV.y * (1f - Mathf.Abs(steer));
                rb.AddRelativeTorque(counterTorque * stabilize, ForceMode.VelocityChange);
                float speed = Velocity.magnitude;
                // Tilt factor for the current speed, NOT an angle.
                float tiltFactor = speedTilt.Evaluate(speed);

                for (int i = 0; i < stabilizers.Length; i++)
                {
                    Transform s = stabilizers[i];
                    Vector3 pos = s.position;
                    Vector3 dir = -s.up;
                    if (Physics.Raycast(pos, dir, out RaycastHit hit, 2f, LayerMasks.GROUND))
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
                        rb.AddForceAtPosition(dir * -error * stabilize * brakeMult * slopeMult,
                            pos, ForceMode.VelocityChange);
                        // Debug.Log(hit.distance);
                    }
                }
            }
            else
            {
                Vector3 keepUpright = Vector3.back * Pow(COM.right.y) + Vector3.right * Pow(COM.forward.y);
                rb.AddRelativeTorque((keepUpright - locAV * 0.25f) * stabilize, ForceMode.VelocityChange);
            }
        }

        private static float Pow(float val, float exp = 2f)
        {
            return Mathf.Pow(Mathf.Abs(val), exp) * Mathf.Sign(val);
        }
    }
}