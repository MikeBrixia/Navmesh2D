using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Navmesh2D
{
    [System.Serializable]
    public struct Edge2D : System.IEquatable<Edge2D>
    {
        [SerializeField] public float2 A;
        [SerializeField] public float2 B;

        public Edge2D(float2 A, float2 B)
        {
            this.A = A;
            this.B = B;
        }

        public bool Equals(Edge2D other)
        {
            return (A.Equals(other.A) && B.Equals(other.B)) || (A.Equals(other.B) && B.Equals(other.A));
        }

        public override string ToString()
        {
            return "A: " + A.ToString() + " B: " + B.ToString();
        }
    }
}

