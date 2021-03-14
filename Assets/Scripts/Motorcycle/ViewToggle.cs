using UnityEngine;
using System.Collections.Generic;

namespace MBaske.Motorcycles
{
    public class ViewToggle : MonoBehaviour
    {
        [SerializeField]
        private bool m_Detailed;

        private void OnValidate()
        { 
            var list = new List<GameObject>();
            DeepFind(transform, "ViewSimple", list);

            foreach (GameObject obj in list)
            {
                obj.SetActive(!m_Detailed);
            }

            list.Clear();
            DeepFind(transform, "ViewDetail", list);
            DeepFind(transform, "Lara", list);
            DeepFind(transform, "FX", list);

            foreach (GameObject obj in list)
            {
                obj.SetActive(m_Detailed);
            }
        }

        private static void DeepFind(Transform tf, string name, IList<GameObject> list)
        {
            if (tf.name == name)
            {
                list.Add(tf.gameObject);
            }

            for (int i = 0; i < tf.childCount; i++)
            {
                DeepFind(tf.GetChild(i), name, list);
            }
        }
    }
}