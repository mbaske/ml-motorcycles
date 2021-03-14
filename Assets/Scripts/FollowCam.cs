using UnityEngine;

namespace MBaske.Motorcycles
{
    public class FollowCam : MonoBehaviour
    {
        [SerializeField]
        private Transform m_Target;
        [SerializeField]
        private float m_Distance = 25;
        [SerializeField]
        private float m_CamOffset = 12;
        [SerializeField]
        private float m_LookOffset = 0;

        private void LateUpdate()
        {
            Vector3 camPos = transform.position;
            camPos.y = 0;
            Vector3 targetPos = m_Target.position;
            targetPos.y = 0;
            Vector3 deltaXZ = camPos - targetPos;
            if (deltaXZ.magnitude > m_Distance)
            {
                camPos = targetPos + deltaXZ.normalized * m_Distance;
            }
            camPos.y = m_Target.position.y + m_CamOffset;
            transform.position = camPos;
            transform.LookAt(m_Target.position + Vector3.up * m_LookOffset);
        }
    }
}