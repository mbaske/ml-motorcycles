using UnityEngine;

namespace MBaske
{
    public class UIBar : MonoBehaviour
    {
        [SerializeField]
        private bool centered;
        private RectTransform rt;

        private void Awake()
        {
            rt = GetComponent<RectTransform>();
        }

        public void SetValue(float value)
        {
            if (centered)
            {
                SetLeft(value > 0 ? 100 : 100 + value * 90);
                SetRight(value < 0 ? 100 : 100 + value * -90);
            }
            else
            {
                SetRight(190 + (value + 1f) * 0.5f * -180);
            }
        }

        private void SetLeft(float val)
        {
            rt.offsetMin = new Vector2(val, rt.offsetMin.y);
        }

        private void SetRight(float val)
        {
            rt.offsetMax = new Vector2(-val, rt.offsetMax.y);
        }
    }
}