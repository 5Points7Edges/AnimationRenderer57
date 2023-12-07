
using UnityEngine;
using System.Collections.Generic;

namespace GenericShape
{
    public struct Circle
    {
        public Vector2 center;
        public float radius;

        public Circle(Vector2 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }
    }
}