
using Unity.Mathematics;

namespace Navmesh2D
{
    [System.Serializable]
    public struct Triangle2D : System.IEquatable<Triangle2D>
    {
        public float2 A;
        public float2 B;
        public float2 C;
        
        public Edge2D AB;
        public Edge2D BC;
        public Edge2D CA;

        public Triangle2D(float2 A, float2 B, float2 C)
        {
            this.A = A;
            this.B = B;
            this.C = C;
           
            this.AB = new Edge2D(A, B);
            this.BC = new Edge2D(B, C);
            this.CA = new Edge2D(C, A);
        }

        public Triangle2D(Edge2D AB, Edge2D BC, Edge2D CA)
        {
            this.AB = AB;
            this.BC = BC;
            this.CA = CA;

            this.A = AB.A;
            this.B = BC.A;
            this.C = CA.A;
        }

        public bool Equals(Triangle2D other)
        {
            return A.Equals(other.A) & B.Equals(other.B) & C.Equals(other.C);
        }
    }
}
