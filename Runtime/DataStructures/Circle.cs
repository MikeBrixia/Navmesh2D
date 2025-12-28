using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Navmesh2D
{
    public class Circle2D
    {
        public float radius;

        public Vector2 center;

        ///<summary>
        ///The diamater of the circle
        ///</summary>
        public float diamater
        {
            get
            {
                return radius * 2;
            }
        }

        ///<summary>
        /// Circle area
        ///</summary>
        public float area
        {
            get
            {
                return Mathf.PI * (radius * radius);
            }
        }

        public Circle2D(Vector2 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }

        public bool ContainsPoint(Vector2 point)
        {
            return Vector2.Distance(point, center) < radius;
        }

        public bool ContainsPointInclusive(Vector2 point)
        {
            return Vector2.Distance(point, center) <= radius;
        }
    }
}

