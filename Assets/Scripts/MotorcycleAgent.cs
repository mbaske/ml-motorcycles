using UnityEngine;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;

namespace MBaske
{
    public class MotorcycleAgent : MotorcycleAgentBasic
    {
        // Competing Agents ---------------------------------------------------------
        public int Rank { get; set; }
        public float Compare { get; set; }
        public float Degrees => Info.Frame.Index * road.Index2Deg;
        private AgentRanker ranker;
        private bool isCompeting;
        private bool isAdded;
        // --------------------------------------------------------------------------

        public IVehicleInfo Info { get; private set; }

        [SerializeField]
        private float speedRewardFactor = 0.005f;
        [SerializeField]
        private float offRoadPenaltyFactor = 0.1f;
        [SerializeField]
        private float collisionPenalty = 0.5f;
        [SerializeField]
        private float endEpisodePenalty = 1f;

        [SerializeField]
        private int[] lookAhead;
        [SerializeField]
        private Road road;

        private int idleStepCount;
        private const int maxIdleSteps = 300;
        private static Vector4[] rays = InitRays();
        private List<float> rayObs;

        public override void Initialize()
        {
            base.Initialize();

            road.Initialize();
            Info = new VehicleInfo() { PhysicsInfo = physics };
            rayObs = new List<float>(rays.Length);

            ranker = FindObjectOfType<AgentRanker>();
            isCompeting = ranker != null && ranker.gameObject.activeSelf;

            ResetPosition();
        }

        public override void OnEpisodeBegin()
        {
            idleStepCount = 0;
            CastRays(GetRayOrigin()); // fill buffer

            if (isCompeting)
            {
                if (!isAdded)
                {
                    ranker.AddAgent(this);
                    isAdded = true;
                }
                ranker.UpdateRanks(false);
            }
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            Info.Position = transform.position;
            Info.Forward = transform.forward;
            road.UpdateInfo(Info);

            if (Info.IsOffRoad)
            {
                AddReward(Mathf.Abs(Info.Deviation) * -offRoadPenaltyFactor);
            }
            else
            {
                AddReward(Vector3.Dot(Info.Frame.Forward, physics.Velocity) * speedRewardFactor);
            }

            sensor.AddObservation(Info.Angle / 180f);
            sensor.AddObservation(Mathf.Clamp(Info.Deviation * 0.2f, -1f, 1f));
            sensor.AddObservation(physics.Inclination3);
            Vector3 v = physics.Localize(physics.Velocity);
            sensor.AddObservation(Normalization.Sigmoid(v.x));
            sensor.AddObservation(Normalization.Sigmoid(v.y));
            // Forward Speed: 0 - 25 m/s => 0 - 90 km/h
            // http://fooplot.com/#W3sidHlwZSI6MCwiZXEiOiJzcXJ0KHgqMC4wNCkqMi0xIiwiY29sb3IiOiIjMDAwMDAwIn0seyJ0eXBlIjoxMDAwLCJ3aW5kb3ciOlsiLTI1LjA2MzAwMTE5MjUzMjk2NSIsIjExNy45ODgxNDYyNjg0MDQzOSIsIi01LjgzMzQzOTgyNjk2NTMyMyIsIjguNDcxNjc0OTE5MTI4NDA3Il19XQ--
            sensor.AddObservation(Mathf.Min(Mathf.Sqrt(Mathf.Max(v.z, 0f) * 0.04f) * 2f - 1f, 1f));
            sensor.AddObservation(Normalization.Sigmoid(physics.AngularVelocity));

            sensor.AddObservation(physics.WheelR.NormMotorTorque);
            sensor.AddObservation(physics.WheelF.NormSteer);
            sensor.AddObservation(physics.WheelR.NormBrakeTorque);
            sensor.AddObservation(physics.WheelF.NormBrakeTorque);
            sensor.AddObservation(physics.WheelR.GetHeightAboveGround());
            sensor.AddObservation(physics.WheelF.GetHeightAboveGround());
            
            Vector3 origin = GetRayOrigin();

            foreach (int i in lookAhead)
            {
                Vector3 p = road.GetFrame(Info.Frame.Index + i).Position;
                sensor.AddObservation((p - origin).normalized);
                Debug.DrawRay(p, Vector3.up, Color.white);
            }

            foreach (float distance in rayObs)
            {
                // Buffered
                sensor.AddObservation(distance);
            }
            CastRays(origin);
            foreach (float distance in rayObs)
            {
                // Current
                sensor.AddObservation(distance);
            }
        }

        public override void OnActionReceived(float[] actions)
        {
            base.OnActionReceived(actions);

            idleStepCount = physics.Velocity.sqrMagnitude < 1f ? idleStepCount + 1 : 0;
            if (Info.IsDisqualified || idleStepCount == maxIdleSteps)
            {
                AddReward(-endEpisodePenalty);
                ResetAgent();
            }
        }

        public override void ResetAgent(bool initPos = false)
        {
            physics.OnReset();

            if (initPos)
            {
                ResetPosition();
            }
            else if (isCompeting)
            {
                PlaceAtFrame(road.GetFrame(ranker.Last.Info.Frame.Index - road.AgentSpacing));
            }

            EndEpisode();
        }

        private void ResetPosition()
        {
            int i = System.Array.IndexOf(FindObjectsOfType<MotorcycleAgent>(), this);
            PlaceAtFrame(road.GetFrame(road.InitAgentOffset + i * road.AgentSpacing));
        }

        private void PlaceAtFrame(Frame frame)
        {
            transform.position = frame.Position
                + frame.Up * 0.25f
                + frame.Right * Random.Range(-0.75f, 0.75f);
            transform.rotation = frame.Rotation;
            Info.Position = transform.position;
            Info.Forward = transform.forward;
            road.UpdateInfo(Info, frame);
        }

        private Vector3 GetRayOrigin()
        {
            Vector3 pos = Info.Frame.LocalPosition(Info.Position);
            pos.y = 0.75f; // offset
            pos = Info.Frame.WorldPosition(pos);
            return pos;
        }

        public void CastRays(Vector3 origin)
        {
            rayObs.Clear();
            Quaternion rotation = Info.Frame.Rotation * Quaternion.Euler(0, Info.Angle, 0);

            foreach (Vector4 v in rays)
            {
                if (Physics.SphereCast(origin, 0.75f, rotation * v, out RaycastHit hit, v.w,
                    LayerMasks.DETECTABLE))
                {
                    rayObs.Add(hit.distance / v.w * 2f - 1f);
                    Debug.DrawLine(origin, hit.point, Color.red);
                }
                else
                {
                    rayObs.Add(1f);
                    Debug.DrawRay(origin, rotation * v * v.w, Color.blue);
                }
            }
        }

        private void OnCollisionEnter(Collision other) 
        {
            int layer = other.gameObject.layer;
            if (layer == Layers.OBSTACLE || layer == Layers.AGENT)
            {
                if (Vector3.Dot(Info.Forward, other.transform.position - Info.Position) > 0)
                {
                    // Penalize tailing agent only.
                    AddReward(-collisionPenalty);
                }
            }    
        }

        private static Vector4[] InitRays()
        {
            List<Vector4> list = new List<Vector4>();
            Vector4 v = new Vector4();
            for (int i = -20; i <= 20; i += 10)
            {
                v = Quaternion.Euler(0, i, 0) * Vector3.forward;
                v.w = 15;
                list.Add(v);
            }
            for (int i = -180; i <= -30; i += 30)
            {
                v = Quaternion.Euler(0, i, 0) * Vector3.forward;
                v.w = 5;
                list.Add(v);
                v = Quaternion.Euler(0, -i, 0) * Vector3.forward;
                v.w = 5;
                list.Add(v);
            }
            return list.ToArray();
        }
    }
}

