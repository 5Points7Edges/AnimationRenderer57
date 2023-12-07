using UnityEngine;
using System.Collections.Generic;

namespace GenericShape
{
    public struct Triangle
    {
        public Vector2 a;
        public Vector2 b;
        public Vector2 c;

        public Triangle(Vector2 a, Vector2 b, Vector2 c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }

        /// <summary>
        /// 获取三角形面积
        /// </summary>
        /// <returns></returns>
        public float Area()
        {
            //  S=√[p(p-l1)(p-l2)(p-l3)]（p为半周长）
            float l1 = (b - a).magnitude;
            float l2 = (c - b).magnitude;
            float l3 = (a - c).magnitude;
            float p = (l1 + l2 + l3) * 0.5f;
            return Mathf.Sqrt(p * (p - l1) * (p - l2) * (p - l3));
        }

        /// <summary>
        /// 是否在三角形内
        /// </summary>
        /// <returns></returns>
        public bool Inside(Vector2 pos)
        {
            Vector3 pa = a - pos;
            Vector3 pb = b - pos;
            Vector3 pc = c - pos;
            Vector3 pab = Vector3.Cross(pa, pb);
            Vector3 pbc = Vector3.Cross(pb, pc);
            Vector3 pca = Vector3.Cross(pc, pa);
            float d1 = Vector3.Dot(pab, pbc);
            float d2 = Vector3.Dot(pab, pca);
            float d3 = Vector3.Dot(pbc, pca);
            return d1 > 0 && d2 > 0 && d3 > 0;
        }
    }

}