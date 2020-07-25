using UnityEngine;

namespace MBaske
{
    public class FollowCam : MonoBehaviour
    {
        [SerializeField]
        private Transform target;
        [SerializeField]
        private float distance = 5;
        [SerializeField]
        private float camOffset = 5;
        [SerializeField]
        private float lookOffset = 1;

        private void LateUpdate()
        {
            Vector3 camPos = transform.position;
            camPos.y = 0;
            Vector3 targetPos = target.position;
            targetPos.y = 0;
            Vector3 deltaXZ = camPos - targetPos;
            if (deltaXZ.magnitude > distance)
            {
                camPos = targetPos + deltaXZ.normalized * distance;
            }
            camPos.y = target.position.y + camOffset;
            transform.position = camPos;
            transform.LookAt(target.position + Vector3.up * lookOffset);
        }
    }
}