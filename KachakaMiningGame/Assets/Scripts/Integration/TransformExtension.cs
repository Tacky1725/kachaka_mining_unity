using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace extension
{
    public static class TransformExtensions
    {
        private const int RoundDigits = 6;
        private const float Scale = 2.5f;
        private const float Scale2 = 2.0f;

        public static void DestroyImmediateIfExists<T>(this Transform transform) where T : Component
        {
            T component = transform.GetComponent<T>();
            if (component != null)
            {
                Object.DestroyImmediate(component);
            }
        }

        public static T AddComponentIfNotExists<T>(this Transform transform) where T : Component
        {
            T component = transform.GetComponent<T>();
            if (component == null)
            {
                component = transform.gameObject.AddComponent<T>();
            }

            return component;
        }

        public static void SetParentAndAlign(this Transform transform, Transform parent, bool keepLocalTransform = true)
        {
            Vector3 localPosition = transform.localPosition;
            Quaternion localRotation = transform.localRotation;
            transform.parent = parent;

            if (keepLocalTransform)
            {
                transform.position = transform.parent.position + localPosition;
                transform.rotation = transform.parent.rotation * localRotation;
            }
            else
            {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
        }

        public static bool HasExactlyOneChild(this Transform transform)
        {
            return transform.childCount == 1;
        }

        public static void MoveChildTransformToParent(this Transform parent, bool transferRotation = true)
        {
            Transform childTransform = parent.GetChild(0);
            parent.DetachChildren();

            parent.position = childTransform.position;
            parent.localScale = childTransform.localScale;

            if (transferRotation)
            {
                parent.rotation = childTransform.rotation;
                childTransform.localRotation = Quaternion.identity;
            }

            childTransform.parent = parent;
            childTransform.localPosition = Vector3.zero;
            childTransform.localScale = Vector3.one;
        }

        public static Vector3 Ros2Unity(this Vector3 vector3)
        {
            return new Vector3(vector3.x / Scale, vector3.y / Scale, vector3.z / Scale);
        }

        public static Vector3 Unity2Ros(this Vector3 vector3)
        {
            return new Vector3(vector3.x / Scale2, vector3.y / Scale2, vector3.z / Scale2);
        }

        public static Vector3 Ros2UnityScale(this Vector3 vector3)
        {
            return new Vector3(vector3.y, vector3.z, vector3.x);
        }

        public static Vector3 Unity2RosScale(this Vector3 vector3)
        {
            return new Vector3(vector3.z, vector3.x, vector3.y);
        }

        public static Quaternion Ros2Unity(this Quaternion quaternion)
        {
            return new Quaternion(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
        }

        public static Quaternion Unity2Ros(this Quaternion quaternion)
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, -90f);
            return rotation * quaternion;
        }

        public static double[] ToRoundedDoubleArray(this Vector3 vector3)
        {
            double[] values = new double[3];
            for (int i = 0; i < 3; i++)
            {
                values[i] = Math.Round(vector3[i], RoundDigits);
            }

            return values;
        }

        public static Vector3 ToVector3(this double[] array)
        {
            return new Vector3((float)array[0], (float)array[1], (float)array[2]);
        }

        public static string SetSeparatorChar(this string path)
        {
            return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }
    }
}
