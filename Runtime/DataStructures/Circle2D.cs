using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Navmesh2D
{
    public struct circle2D
    {
        public float radius;
        public float diamater;
        public float2 center;

        public circle2D(float radius, float2 center)
        {
           this.radius = radius;
           this.diamater = radius * 2;
           this.center = center;
        }
    }

}

