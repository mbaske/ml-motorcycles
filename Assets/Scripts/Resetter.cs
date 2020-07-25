using UnityEngine;
using System.Collections.Generic;

namespace MBaske
{
    public class ResettableItem
    {
        private Transform tf;
        private Vector3 pos;
        private Quaternion rot;
        private Rigidbody rb;

        public ResettableItem(Transform tf)
        {
            this.tf = tf;
            pos = tf.localPosition;
            rot = tf.localRotation;
            rb = tf.GetComponent<Rigidbody>();
        }

        public void Reset()
        {
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }

            tf.localPosition = pos;
            tf.localRotation = rot;
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