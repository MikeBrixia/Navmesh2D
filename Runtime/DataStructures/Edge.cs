using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Navmesh2D
{
    public class Edge
    {
        public Vector3 a;
        public Vector3 b;
        public float length;

        public Edge(Vector3 a, Vector3 b)
        {
            this.a = a;
            this.b = b;
            this.length = UnityEngine.Vector3.Distance(a, b);
        }
        
        public bool isCrossingEdge(Edge edge)
        {
            // this edge
            float A1 = b.y - a.y;
            float B1 = a.x - b.x;
            float C1 = A1 * a.x + B1 * a.y;

            // other edge
            float A2 = edge.b.y - edge.a.y;
            float B2 = edge.a.x - edge.b.x;
            float C2 = A2 * edge.a.x - B2 * edge.a.y;

            float determinant = A1 * B2 - A2 * B1;

            bool isIntersecting = false;
            // if edges are not parallel find intersection point.
            if (determinant != 0f)
            {
                float x = (B2 * C1 - B1 * C2) / determinant;
                float y = (A1 * C2 - A2 * C1) / determinant;
                //point = new Vector3(x, y);
                isIntersecting = true;
            }

            return isIntersecting;
        }
        
        public bool ContainsVertex(Vector3 vertex)
        {
            return vertex.Equals(a) || vertex.Equals(b);
        }

        public override bool Equals(object obj)
        {
            Edge other = (Edge)obj;
            return other.a.Equals(a) && other.b.Equals(b) || other.b.Equals(a) && other.a.Equals(b);
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(a, b);
        }

        public override string ToString()
        {
            return "A: " + a.ToString() + " B: " + b.ToString();
        }

        public void DrawEdge()
        {
            Debug.DrawLine(a, b, Color.green, 10f);
        }

        public void DrawEdge(Color color)
        {
            Debug.DrawLine(a, b, color, 10f);
        }
    }
}

