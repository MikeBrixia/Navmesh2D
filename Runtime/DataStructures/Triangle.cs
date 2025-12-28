using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NavMesh2D;
using UnityEngine;

namespace Navmesh2D
{
    [System.Serializable]
    public class Triangle
    {
        ///<summary>
        /// The vertices of this triangle in counter-clockwise order.
        ///</summary>
        [SerializeField] public Vector3[] vertices;

        ///<summary>
        /// The edges of this triangle in counter-clockwise order.
        ///</summary>
        [SerializeField] public Edge[] edges;
        
        ///<summary>
        /// The are of the triangle
        ///</summary>
        public float area
        {
            get
            {
                Vector3 a = vertices[0];
                Vector3 b = vertices[1];
                Vector3 c = vertices[2];
                return Mathf.Abs((a.x * (b.y - c.y) +
                          b.x * (c.y - a.y) +
                          c.x * (a.y - b.y)) / 2);
            }
        }

        ///<summary>
        /// Get the triangle circum circle.
        ///</summary>
        public Circle2D circumCircle
        {
            get
            {
                Vector2 A = vertices[0];
                Vector2 B = vertices[1];
                Vector2 C = vertices[2];
                Vector2 SqrA = new Vector2(Mathf.Pow(A.x, 2f), Mathf.Pow(A.y, 2f));
                Vector2 SqrB = new Vector2(Mathf.Pow(B.x, 2f), Mathf.Pow(B.y, 2f));
                Vector2 SqrC = new Vector2(Mathf.Pow(C.x, 2f), Mathf.Pow(C.y, 2f));
                float D = (A.x * (B.y - C.y) + B.x * (C.y - A.y) + C.x * (A.y - B.y)) * 2f;
                float x = ((SqrA.x + SqrA.y) * (B.y - C.y) + (SqrB.x + SqrB.y) * (C.y - A.y) + (SqrC.x + SqrC.y) * (A.y - B.y)) / D;
                float y = ((SqrA.x + SqrA.y) * (C.x - B.x) + (SqrB.x + SqrB.y) * (A.x - C.x) + (SqrC.x + SqrC.y) * (B.x - A.x)) / D;

                Vector2 center = new Vector2(x, y);
                float radius = Vector2.Distance(center, A);
                return new Circle2D(center, radius);
            }
        }
        
        public Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            this.vertices = new Vector3[3];
            this.vertices[0] = a;
            this.vertices[1] = b;
            this.vertices[2] = c;
            
            this.edges = new Edge[3];
            this.edges[0] = new Edge(a, b);
            this.edges[1] = new Edge(b, c);
            this.edges[2] = new Edge(c, a);
        }
        
        public Triangle(Edge AB, Edge BC, Edge CA)
        {
            this.edges = new Edge[3];
            this.edges[0] = AB;
            this.edges[1] = BC;
            this.edges[2] = CA;

            this.vertices = new Vector3[3];
            this.vertices[0] = AB.a;
            this.vertices[1] = BC.a;
            this.vertices[2] = CA.a;
        }

        ///<summary>
        /// Change orientation of triangle.
        ///</summary>
        public void ChangeOrientation()
        {
            Vector3 a = vertices[0];
            Vector3 b = vertices[1];
            Vector3 temp = a;
            a = b;
            b = temp;
        }
        
        ///<summary>
        /// Check if the given point is contained inside this triangle.
        ///</summary>
        public bool ContainsPoint(UnityEngine.Vector3 point)
        {
            Vector3 a = vertices[0];
            Vector3 b = vertices[1];
            Vector3 c = vertices[2];

            // Calculate area of this triangle
            float ABC = area;
            // Calculate the area of the triangle formed by our point, A and B
            float PAB = MathUtils.CalculateTriangleArea2D(point, a, b);
            // Calculate the area of the triangle formed by our point, B and C
            float PBC = MathUtils.CalculateTriangleArea2D(point, b, c);
            // Calculate the area of the triangle formed by our point, A and C
            float PAC = MathUtils.CalculateTriangleArea2D(point, a, c);
            // Check if the point is contained inside this triangle.
            return Mathf.Approximately(ABC, PAB + PBC + PAC);
        }
        
        public bool ContainsVertex(Vector3 vertex)
        {
            return vertices[0].Equals(vertex) | vertices[1].Equals(vertex) | vertices[2].Equals(vertex);
        }

        public bool ContainsEdge(Edge edge)
        {
            return edges.Contains(edge);
        }
        
        public override bool Equals(object obj)
        {
            Triangle other = (Triangle) obj;
            bool result = true;
            foreach(Vector3 vertex in vertices)
                if(other != null && !other.vertices.Contains(vertex))
                {
                    result = false;
                    break;
                }
            return result;
        }

        public override string ToString()
        {
            return "Triangle(V1: " + vertices[0] + ", V2: " + vertices[1] + ", V3: " + vertices[2] + ")";
        }

        public override int GetHashCode()
        {
            return ((IStructuralEquatable)vertices).GetHashCode(EqualityComparer<Vector3>.Default);
        }

        public void DrawTriangle(Color color)
        {
            foreach(Edge edge in edges)
               edge.DrawEdge();
        }
    }
}
