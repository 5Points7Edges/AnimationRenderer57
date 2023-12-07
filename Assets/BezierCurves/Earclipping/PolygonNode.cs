using UnityEngine;
using System.Collections.Generic;

namespace GenericShape
{
    public class PolygonNode
    {
        /// <summary>
        /// 顺时针方向组成的多边形的点
        /// </summary>
        private readonly Vector2[] points;

        public PolygonNode(Vector2[] points)
        {
            this.points = new Vector2[points.Length];
            points.CopyTo(this.points, 0);
        }
    }
}