using UnityEngine;

namespace MBaske
{
    public class PoseBlend : MonoBehaviour
    {
        [SerializeField]
        private Quaternion[] rotations;

        public void Blend(float t)
        {
            int i = t > Mathf.Epsilon ? 2 : (t < -Mathf.Epsilon ? 1 : 0);
            transform.localRotation = Quaternion.Slerp(rotations[0], rotations[i], Mathf.Abs(t));
        }

        // private void OnValidate() 
        // {
            // transform.localRotation = rotations[0];
            // rotations[2] = transform.localRotation;
        // }
    }
}