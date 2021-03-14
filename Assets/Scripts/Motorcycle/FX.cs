using UnityEngine;

namespace MBaske.Motorcycles
{
    public class FX : MonoBehaviour
    {
        [SerializeField]
        private AudioSource motor1;
        [SerializeField]
        private AudioSource motor2;

        [SerializeField]
        private AudioSource tires1;
        [SerializeField]
        private AudioSource tires2;

        [SerializeField]
        private AudioSource thud1;
        [SerializeField]
        private AudioSource thud2;

        [SerializeField]
        private float smokeDensity = 10;

        [Space, SerializeField]
        private bool setMaterials;

        [SerializeField]
        private Renderer shirt;
        [SerializeField]
        private Material[] shirtMaterials;

        [SerializeField]
        private Renderer[] paint;
        [SerializeField]
        private Material[] paintMaterials;

        private PoseBlend[] laraAnim;
        private ParticleSystem particles;
        private ArcadePhysics physics;
        private bool groundedF, groundedR;

        private void Awake()
        {
            laraAnim = transform.parent.GetComponentsInChildren<PoseBlend>();
            particles = GetComponentInChildren<ParticleSystem>();
            physics = GetComponentInParent<ArcadePhysics>();

            if (setMaterials)
            {
                int i = System.Array.IndexOf(FindObjectsOfType<FX>(), this);
                var tmp = shirt.sharedMaterials;
                tmp[6] = shirtMaterials[i];
                shirt.sharedMaterials = tmp;
                foreach (var r in paint)
                {
                    r.material = paintMaterials[i];
                }
            }
        }

        private void Update()
        {
            float t = physics.WheelF.NormSteer;
            foreach (var pose in laraAnim)
            {
                pose.Blend(t);
            }

            float speed = Mathf.Min(physics.Velocity.magnitude / 25f, 1f);
            float drive = (speed + 0.5f) * (physics.WheelR.RPMRatio + 0.5f) * 0.5f;
            float jump = Mathf.Min(Mathf.Sqrt(Mathf.Abs(physics.Velocity.y)), 1f);
            float turn = Mathf.Abs(physics.Localize(physics.AngularVelocity).y / 2.5f);
            float slip = physics.WheelR.sLong * physics.WheelF.sLong;
            float fric = Mathf.Max(turn, slip < 1f ? slip : 0f);
            float brake = physics.WheelR.BrakeRatio + physics.WheelF.BrakeRatio;

            var emission = particles.emission;
            emission.rateOverDistance = fric * smokeDensity;

            motor1.volume = 0.65f - drive * 0.4f;
            motor1.pitch = 1f + drive;

            motor2.volume = drive;
            motor2.pitch = 0.7f + drive * 0.3f;

            tires1.volume = fric * 0.75f;
            tires2.volume = brake * speed * 2f;

            if (groundedR != physics.WheelR.IsGrounded)
            {
                groundedR = physics.WheelR.IsGrounded;
                if (groundedR)
                {
                    thud1.PlayOneShot(thud1.clip, jump);
                }
            }
            if (groundedF != physics.WheelF.IsGrounded)
            {
                groundedF = physics.WheelF.IsGrounded;
                if (groundedF)
                {
                    thud2.PlayOneShot(thud2.clip, jump);
                }
            }
        }
    }
}