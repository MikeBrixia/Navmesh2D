using System.Collections;
using System.Collections.Generic;
using Navmesh2D;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace NavMesh2D
{
  
  public static class MathUtils
  {
    public static bool IsQuadrilateralConvex(NativeArray<Edge2D> polygon)
    {
      // Get polygon points.
      NativeArray<float2> points = new NativeArray<float2>(polygon.Length, Allocator.Temp);
      for (int i = 0; i < points.Length; i++)
        points[i] = polygon[i].A;

      bool isConvex = true;
      for (int i = 0; i < points.Length; i++)
      {
        // pick three nodes at a time i,j,k
        int j = (i + 1) % points.Length;
        int k = (i + 2) % points.Length;
        float2 A = points[i];
        float2 B = points[j];
        float2 C = points[k];

        Triangle2D triangle = new Triangle2D(A, B, C);

        // check nodes after the three and wrap around to grab first nodes also
        for (int r = 3; r < points.Length; r++)
        {
          float2 point = points[(r + i) % points.Length];

          // if _any_ node is interior to ABC then non-convex
          if (ContainsPoint(triangle, point))
          {
            isConvex = false;
            break;
          }
        }
      }
            
      // Free allocated memory
      points.Dispose();
            
      return isConvex;
    }
    
            ///<summary>
        /// Check if two line segments intersects in 2D.
        ///</summary>
        ///<returns> True if the two segments intersect, false otherwise.</returns>
        public static bool LineSegmentIntersection2D(Vector2 aStart, Vector2 aEnd, Vector2 bStart, Vector2 bEnd,
                                                     bool shouldIncludeEndPoints = true)
        {
            // To avoid floating point precision issues we can add a small value.
            float epsilon = 0.01f;
            bool isIntersecting = false;

            float denominator = (bEnd.y - bStart.y) * (aEnd.x - aStart.x) - (bEnd.x - bStart.x) * (aEnd.y - aStart.y);

            //Make sure the denominator is > 0, if not the lines are parallel.
            if (denominator != 0f)
            {
                float u_a = ((bEnd.x - bStart.x) * (aStart.y - bStart.y) - (bEnd.y - bStart.y) * (aStart.x - bStart.x)) / denominator;
                float u_b = ((aEnd.x - aStart.x) * (aStart.y - bStart.y) - (aEnd.y - aStart.y) * (aStart.x - bStart.x)) / denominator;

                //Are the line segments intersecting if the end points are the same.
                if (shouldIncludeEndPoints)
                    //Is intersecting if u_a and u_b are between 0 and 1 or exactly 0 or 1.
                    isIntersecting = u_a >= 0f + epsilon && u_a <= 1f - epsilon && u_b >= 0f + epsilon && u_b <= 1f - epsilon;
                else
                    //Is intersecting if u_a and u_b are between 0 and 1.
                    isIntersecting = u_a > 0f + epsilon && u_a < 1f - epsilon && u_b > 0f + epsilon && u_b < 1f - epsilon;
            }

            return isIntersecting;
        }
        
        ///<summary>
        /// Check if two line segments intersects in 2D.
        ///</summary>
        ///<returns> True if the two segments intersect, false otherwise.</returns>
        public static bool LineSegmentIntersection2D(float2 aStart, float2 aEnd, float2 bStart, float2 bEnd,
                                                     bool shouldIncludeEndPoints = true, float epsilon = 0.01f)
        {
            bool isIntersecting = false;

            float denominator = (bEnd.y - bStart.y) * (aEnd.x - aStart.x) - (bEnd.x - bStart.x) * (aEnd.y - aStart.y);

            //Make sure the denominator is > 0, if not the lines are parallel.
            if (denominator != 0f)
            {
                float u_a = ((bEnd.x - bStart.x) * (aStart.y - bStart.y) - (bEnd.y - bStart.y) * (aStart.x - bStart.x)) / denominator;
                float u_b = ((aEnd.x - aStart.x) * (aStart.y - bStart.y) - (aEnd.y - aStart.y) * (aStart.x - bStart.x)) / denominator;

                //Are the line segments intersecting if the end points are the same.
                if (shouldIncludeEndPoints)
                    //Is intersecting if u_a and u_b are between 0 and 1 or exactly 0 or 1.
                    isIntersecting = u_a >= 0f + epsilon && u_a <= 1f - epsilon && u_b >= 0f + epsilon && u_b <= 1f - epsilon;
                else
                    //Is intersecting if u_a and u_b are between 0 and 1.
                    isIntersecting = u_a > 0f + epsilon && u_a < 1f - epsilon && u_b > 0f + epsilon && u_b < 1f - epsilon;
            }

            return isIntersecting;
        }
        
        
                public static circle2D GetTriangleCircumCircle(Triangle2D triangle)
        {
            float2 A = triangle.A;
            float2 B = triangle.B;
            float2 C = triangle.C;
            float2 SqrA = new float2(math.pow(A.x, 2f), math.pow(A.y, 2f));
            float2 SqrB = new float2(math.pow(B.x, 2f), math.pow(B.y, 2f));
            float2 SqrC = new float2(math.pow(C.x, 2f), math.pow(C.y, 2f));
            float D = (A.x * (B.y - C.y) + B.x * (C.y - A.y) + C.x * (A.y - B.y)) * 2f;
            float x = ((SqrA.x + SqrA.y) * (B.y - C.y) + (SqrB.x + SqrB.y) * (C.y - A.y) + (SqrC.x + SqrC.y) * (A.y - B.y)) / D;
            float y = ((SqrA.x + SqrA.y) * (C.x - B.x) + (SqrB.x + SqrB.y) * (A.x - C.x) + (SqrC.x + SqrC.y) * (B.x - A.x)) / D;

            float2 center = new float2(x, y);
            float radius = math.distance(center, A);
            return new circle2D(radius, center);
        }

        ///<summary>
        /// Calculate the area of the given 2D triangle.
        ///</summary>
        public static float CalculateTriangleArea2D(Triangle2D triangle)
        {
            float2 a = triangle.A;
            float2 b = triangle.B;
            float2 c = triangle.C;
            return math.abs((a.x * (b.y - c.y) +
                             b.x * (c.y - a.y) +
                             c.x * (a.y - b.y)) / 2);
        }
        
        ///<summary>
        /// Get the area of the given triangle.
        ///</summary>
        public static float CalculateTriangleArea2D(Vector3 a, Vector3 b, Vector3 c)
        {
            return Mathf.Abs((a.x * (b.y - c.y) + 
                              b.x * (c.y - a.y) + 
                              c.x * (a.y - b.y)) / 2);
        }
        
        ///<summary>
        /// Calculate the area of the given 2D triangle
        ///</summary>
        public static float CalculateTriangleArea2D(float2 A, float2 B, float2 C)
        {
            return math.abs((A.x * (B.y - C.y) +
                             B.x * (C.y - A.y) +
                             C.x * (A.y - B.y)) / 2);
        }
        
        public static bool ContainsPoint(Triangle2D triangle, float2 point)
        {
            float2 a = triangle.A;
            float2 b = triangle.B;
            float2 c = triangle.C;

            // Calculate area of this triangle
            float ABC = CalculateTriangleArea2D(triangle);
            // Calculate the area of the triangle formed by our point, A and B
            float PAB = CalculateTriangleArea2D(point, a, b);
            // Calculate the area of the triangle formed by our point, B and C
            float PBC = CalculateTriangleArea2D(point, b, c);
            // Calculate the area of the triangle formed by our point, A and C
            float PAC = CalculateTriangleArea2D(point, a, c);
            // Check if the point is contained inside this triangle.
            return ABC.Equals(PAB + PBC + PAC);
        }

        public static bool ContainsPoint(circle2D circle, float2 point)
        {
            return math.distance(circle.center, point) < circle.radius;
        }
        
        public static bool ContainsVertex(Triangle2D triangle, float2 vertex)
        {
            return triangle.A.Equals(vertex) | triangle.B.Equals(vertex) | triangle.C.Equals(vertex);
        }

        public static bool ContainsEdge(Triangle2D triangle, Edge2D edge)
        {
            return triangle.AB.Equals(edge) | triangle.BC.Equals(edge) | triangle.CA.Equals(edge);
        }
        
        public static bool ContainsVertex(Edge2D edge, float2 vertex)
        {
            return edge.A.Equals(vertex) || edge.B.Equals(vertex);
        }
  }
}

  
  

