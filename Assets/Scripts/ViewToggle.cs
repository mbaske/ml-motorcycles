using UnityEngine;

namespace MBaske
{
    public class ViewToggle : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] detail;
        [SerializeField]
        private GameObject[] simple;

        [SerializeField]
        private bool pretty;

        private void OnValidate()
        {
            foreach (GameObject obj in detail)
            {
                obj.SetActive(pretty);
            }
            foreach (GameObject obj in simple)
            {
                obj.SetActive(!pretty);
            }
        }
    }
}