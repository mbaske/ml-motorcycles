using UnityEngine;
using System.Collections.Generic;

namespace MBaske.Motorcycles
{
    public class ResettableItem
    {
        private Transform m_Transform;
        private Vector3 m_Position;
        private Quaternion m_Rotation;
        private Rigidbody m_Rigidbody;

        public ResettableItem(Transform tf)
        {
            this.m_Transform = tf;
            m_Position = tf.localPosition;
            m_Rotation = tf.localRotation;
            m_Rigidbody = tf.GetComponent<Rigidbody>();
        }

        public void Reset()
        {
            if (m_Rigidbody != null)
            {
                m_Rigidbody.velocity = Vector3.zero;
                m_Rigidbody.angularVelocity = Vector3.zero;
                m_Rigidbody.Sleep();
            }

            m_Transform.localPosition = m_Position;
            m_Transform.localRotation = m_Rotation;
        }
    }

    public class Resetter
    {
        private List<ResettableItem> items;

        public Resetter(Transform tf)
        {
            items = new List<ResettableItem>();
            Add(tf);
        }

        public void Reset()
        {
            foreach (ResettableItem item in items)
            {
                item.Reset();
            }
        }

        private void Add(Transform tf)
        {
            items.Add(new ResettableItem(tf));

            for (int i = 0; i < tf.childCount; i++)
            {
                Add(tf.GetChild(i));
            }
        }
    }
}