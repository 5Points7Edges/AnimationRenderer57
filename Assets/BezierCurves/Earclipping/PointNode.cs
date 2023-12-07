using UnityEngine;
using System.Collections.Generic;

namespace GenericShape
{
    public class PointNode
    {
        public Vector2 Position;
        public PointNode PreviousNode;
        public PointNode NextNode;

        public PointNode(Vector2 Position)
        {
            this.Position = Position;
        }
    }
}