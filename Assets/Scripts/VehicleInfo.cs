using UnityEngine;

namespace MBaske
{
    public interface IVehicleInfo
    {
        Vector3 Position { get; set; } 
        Vector3 Forward { get; set; } 
        Frame Frame { get; set; }
        float Angle { get; set; }
        float Deviation { get; set; }
        bool IsOffRoad { get; set; }
        bool IsDisqualified { get; set; }
        IPhysicsInfo PhysicsInfo { get; set; }
        void Update(Transform transform);
    }

    public class VehicleInfo : IVehicleInfo
    {
        public Vector3 Position { get; set; } 
        public Vector3 Forward { get; set; } 
        public Frame Frame { get; set; }
        public float Angle { get; set; }
        public float Deviation { get; set; }
        public bool IsOffRoad { get; set; }
        public bool IsDisqualified { get; set; }
        public IPhysicsInfo PhysicsInfo { get; set; }

        public void Update(Transform transform)
        {
            Position = transform.position;
            Forward = transform.forward;
        } 
    }
}
