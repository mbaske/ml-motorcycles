using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

namespace MBaske.Motorcycles
{
    public class DriverAgent : Agent
    {
        private StatsRecorder m_Stats;
        private ArcadePhysics m_Physics;
        private Resetter m_Resetter;
        private Road m_Road;

        [SerializeField]
        private bool m_Train = true;
        [SerializeField, Tooltip("Multiplied with forward speed while on road.")]
        private float m_SpeedRewardFactor = 0.1f;
        [SerializeField, Tooltip("Multiplied with sqr. distance from road.")]
        private float m_OffRoadPenaltyFactor = 0.01f;
        [SerializeField, Tooltip("Penalty for collision enter AND stay.")]
        private float m_CollisionPenalty = 0.2f;

        [SerializeField]
        [Tooltip("Reset position after [value] seconds off road.")]
        private float m_OffRoadTimeout = 10;
        private float m_TimeStamp;
        [SerializeField]
        [Tooltip("Step interval for writing stats to Tensorboard.")]
        private int m_StatsInterval = 120;
        private int m_CollisionCount;
        private int m_OffRoadCount;

        private Vector3 m_RoadPosition;
        private Vector3 m_RoadForward;

        public override void Initialize()
        {
            m_Stats = Academy.Instance.StatsRecorder;
            m_Physics = GetComponent<ArcadePhysics>();
            m_Physics.Initialize();
            m_Road = FindObjectOfType<Road>();
            m_Resetter = new Resetter(transform);
        }

        public override void OnEpisodeBegin()
        {
            m_Resetter.Reset();
            m_Physics.ManagedReset();
            m_TimeStamp = Time.time;
            m_CollisionCount = 0;
            m_OffRoadCount = 0;
            m_Road.OnEpisodeBegin(this);
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            Vector3 pos = transform.position;
            m_Road.Find(pos, out m_RoadPosition, out m_RoadForward);
            Vector3 delta = pos - m_RoadPosition;

            float anglePos = Vector3.SignedAngle(m_RoadForward, delta, Vector3.up);
            sensor.AddObservation(Mathf.Clamp(anglePos / 90f, -1f, 1f));
            sensor.AddObservation(Sigmoid(delta.sqrMagnitude) * Mathf.Sign(anglePos));

            float angleFwd = Vector3.SignedAngle(m_RoadForward, transform.forward, Vector3.up);
            sensor.AddObservation(angleFwd / 180f);

            Vector3 localVelocity = m_Physics.Localize(m_Physics.Velocity);
            sensor.AddObservation(Sigmoid(localVelocity.x));
            sensor.AddObservation(Sigmoid(localVelocity.y));
            // Forward speed 0 - 50.
            // http://fooplot.com/#W3sidHlwZSI6MCwiZXEiOiIoeCowLjEpLygxK2FicygoeCowLjEpKSkqMi40LTEiLCJjb2xvciI6IiMwMDAwMDAifSx7InR5cGUiOjEwMDAsIndpbmRvdyI6WyItMjAuODc5NjU3NDUxOTIzMDciLCI3MC42NzMwNzY5MjMwNzY4OCIsIi0zLjg0NDI5OTMxNjQwNjI0NzMiLCI1LjMxMDk3NDEyMTA5Mzc0NjQiXX1d
            sensor.AddObservation(Mathf.Clamp(
                Sigmoid(localVelocity.z, 0.1f) * 2.4f - 1f, -1f, 1f));

            sensor.AddObservation(m_Physics.Inclination);
            sensor.AddObservation(m_Physics.WheelF.NormSteer);
            sensor.AddObservation(m_Physics.WheelR.NormMotorTorque);
            sensor.AddObservation(m_Physics.WheelF.NormBrakeTorque);
            sensor.AddObservation(m_Physics.WheelR.NormBrakeTorque);
            sensor.AddObservation(m_Physics.WheelF.IsGrounded);
            sensor.AddObservation(m_Physics.WheelR.IsGrounded);
            sensor.AddObservation(m_Physics.WheelF.GetHeightAboveGround());
            sensor.AddObservation(m_Physics.WheelR.GetHeightAboveGround());
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            m_Physics.ApplyActions(actionBuffers.ContinuousActions.Array, Time.fixedDeltaTime);
            m_Physics.Stabilize();

            if (m_Train)
            {
                Vector3 delta = transform.position - m_RoadPosition;
                float offRoad = delta.sqrMagnitude - 4; // TBD

                if (offRoad > 0)
                {
                    m_OffRoadCount++;
                    AddReward(-Mathf.Clamp01(offRoad * m_OffRoadPenaltyFactor));

                    if (Time.time - m_TimeStamp > m_OffRoadTimeout)
                    {
                        transform.position = m_RoadPosition;
                        transform.rotation = Quaternion.LookRotation(m_RoadForward);
                        m_Physics.ManagedReset();
                    }
                }
                else
                {
                    float speed = Vector3.Dot(m_RoadForward, m_Physics.Velocity);
                    AddReward(speed * m_SpeedRewardFactor);
                    m_TimeStamp = Time.time;
                }

                if (StepCount % m_StatsInterval == 0)
                {
                    float d = m_StatsInterval;
                    float speed = Vector3.Dot(m_RoadForward, m_Physics.Velocity);
                    m_Stats.Add("Driver/Speed", speed);
                    m_Stats.Add("Driver/Off Road Ratio", m_OffRoadCount / d);
                    m_Stats.Add("Driver/Collision Ratio", m_CollisionCount / d);
                    m_CollisionCount = 0;
                    m_OffRoadCount = 0;
                }
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var actions = actionsOut.ContinuousActions;
            actions[0] = Input.GetAxis("Vertical");
            actions[1] = Input.GetAxis("Jump");
            actions[2] = Input.GetAxis("Horizontal");
        }

        private void OnCollisionEnter(Collision collision)
        {
            AddReward(-m_CollisionPenalty);
            m_CollisionCount++;
        }

        private void OnCollisionStay(Collision collision)
        {
            AddReward(-m_CollisionPenalty);
            m_CollisionCount++;
        }

        private static float Sigmoid(float val, float scale = 1f)
        {
            val *= scale;
            return val / (1f + Mathf.Abs(val));
        }
    }
}