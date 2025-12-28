
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using System.Linq;
using UnityEngine.SceneManagement;

namespace Navmesh2D
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider2D))]
    [RequireComponent(typeof(PolygonCollider2D))]
    [ExecuteInEditMode]
    public class NavMesh2D : MonoBehaviour
    {
        [SerializeField] private NavMeshData navData;
        [SerializeField] private ContactFilter2D filter;
        [SerializeField] private float boundThickness = 1f;
        
        ///<summary>
        /// The number of corner points we will use
        /// to form the circle collider shape. The greater this
        /// number, the more accurate will be navigation mesh generation
        /// for circle colliders.
        ///</summary>
        [SerializeField] private int circleCornersCount = 8;
        
        [Header("Grid properties")]
        [SerializeField] private EGridType gridType;
        private int2 cellSize = new int2(1, 1);
        private GridData gridData;

        [Header("Components")]
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private BoxCollider2D volume;
        private PolygonCollider2D navCollider;
        [SerializeField] private CircleCollider2D Test;
        public int pointsTest = 1;
        
        // Editor only variables
#if UNITY_EDITOR
        public bool realtimeCook = true;
        private string assetFilepath;
#endif
        
        // Start is called before the first frame update
        void Start()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
            volume = GetComponent<BoxCollider2D>();
        }
        
        void Update()
        {
#if UNITY_EDITOR
            if (realtimeCook)
                BakeNavigationMesh();
#endif      
        }

        void OnValidate()
        {
            // Initialize GameObject data.
            volume = GetComponent<BoxCollider2D>();
            volume.isTrigger = true;
            navCollider = GetComponent<PolygonCollider2D>();
            navCollider.isTrigger = true;
            filter.useLayerMask = true;

            // Cache the current scene name
            string sceneName = SceneManager.GetActiveScene().name;

            // Path for the folder which contains all the game scenes assets.
            string scenesFilepath = "Assets/Scenes";

            // Check if the Scenes folder exist, if not then create it.
            if (!AssetDatabase.IsValidFolder(scenesFilepath))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            // If there's no folder related to the current scene create it.
            if (!AssetDatabase.IsValidFolder(scenesFilepath + "/" + sceneName))
                AssetDatabase.CreateFolder(scenesFilepath, sceneName);

            // Path to the folder which contains all the navigation data for the current scene.
            string currentSceneFilepath = "Assets/Scenes/" + sceneName;

            // If there's no folder for navigation data create it.
            if (!AssetDatabase.IsValidFolder(currentSceneFilepath + "/NavigationData"))
                AssetDatabase.CreateFolder(currentSceneFilepath, "NavigationData");

            assetFilepath = currentSceneFilepath + "/NavigationData/";
        }

        // Update this navigation mesh asset with supplied data.
        // if there's not asset this method will create one.
        private void UpdateNavigationAsset(List<Triangle2D> navigableSurface)
        {
            // The name of this game object entity
            string objectName = gameObject.name;
            
            // The GUID identifier for the requested asset.
            string[] assetGuid = AssetDatabase.FindAssets(objectName + "_Data");
            
            // Does a nav mesh data asset exist?
            if (assetGuid.Length == 0)
            {
                // If a nav mesh data asset doesn't exist, create it.
                navData = ScriptableObject.CreateInstance<NavMeshData>();
                navData.name = gameObject.name + "_Data";
                AssetDatabase.CreateAsset(navData, assetFilepath + objectName + "_Data.asset");
                EditorUtility.SetDirty(navData);
                AssetDatabase.SaveAssets();
            }
            else
            {
                // use guid to retrieve the asset path.
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid[0]);
                // Load the asset stored at assetPath.
                navData = AssetDatabase.LoadAssetAtPath<NavMeshData>(assetPath);
            }
            
            // Using the navMeshVolume determines the width and height of the nav mesh grid.
            int width = Mathf.FloorToInt(volume.size.x / cellSize.x);
            int height = Mathf.FloorToInt(volume.size.y / cellSize.y);

            // Write baked data to a navigation data asset to be used by
            // game entities.
            navData.cellSize = cellSize;
            navData.size = new int2(width, height);
            navData.gridType = gridType;
            navData.origin = (Vector2)volume.bounds.min;
            navData.navigableSurface = navigableSurface.ToArray();

            // Save asset.
            EditorUtility.SetDirty(navData);
            AssetDatabase.SaveAssets();
        }

        public void BakeNavigationMesh()
        {
            // Begin navigation mesh cooking, this method will generate
            // the actual mesh and return the nav collider shape(In triangles).
            List<Triangle2D> navigableSurface = CookNavMesh();
            
            // Update this NavMesh2D asset with the computed data.
            UpdateNavigationAsset(navigableSurface);
        }

        private List<Triangle2D> CookNavMesh()
        {
            // Initialize input data.
            NativeList<float2> vertices = new NativeList<float2>(Allocator.TempJob);
            NativeList<Edge2D> holes = new NativeList<Edge2D>(Allocator.TempJob);
            
            // Generate nav mesh geometry.
            BuildGeometry(ref vertices, ref holes);
            
            // Triangulate using input data and generate a collision shape and mesh for the navigation.
            ConstrainedDelaunayTriangulation triangulation = new ConstrainedDelaunayTriangulation(vertices, holes);
            triangulation.Run();
            GenerateNavigationMesh(triangulation.triangles);
            List<Triangle2D> triangles = triangulation.triangles.ToArray().ToList();
            
            // Free all the memory allocated for the job.
            triangulation.Dispose();
            triangulation.triangles.Dispose();
            vertices.Dispose();
            holes.Dispose();
            
            return triangles;
        }

        ///<summary>
        /// Build all navigation meshes inside the scene.
        /// You should never call this manually unless you're trying
        /// to write your own runtime navigation mesh generation, but for
        /// 99% of cases you should be fine without calling this method, use
        /// instead the built in support for Runtime navigation mesh generation.
        ///</summary>
        [MenuItem("Window/AI/Navigation 2D/Build Navigation")]
        public static void CookAllNavMeshes()
        {
            NavMesh2D[] navMeshes = FindObjectsOfType<NavMesh2D>();
            int length = navMeshes.Length;
            
            // When there is more 1 or more nav meshes in the scene starts cooking them on different threads.
            if (length > 0)
            {
                NativeArray<JobHandle> handlers = new NativeArray<JobHandle>(length, Allocator.TempJob);
                ConstrainedDelaunayTriangulation[] triangulations = new ConstrainedDelaunayTriangulation[length];
                for (int i = 0; i < length; i++)
                {
                    NavMesh2D navMesh = navMeshes[i];
                    
                    // Initialize input data
                    NativeList<float2> vertices = new NativeList<float2>(Allocator.TempJob);
                    NativeList<Edge2D> holes = new NativeList<Edge2D>(Allocator.TempJob);
                    
                    // Generate this navigation mesh geometry data.
                    navMesh.BuildGeometry(ref vertices, ref holes);
                    
                    // Create triangulation job and schedule it. Each nav mesh triangulation will run
                    // on a separate thread to speed-up the baking process.
                    ConstrainedDelaunayTriangulation triangulation = new ConstrainedDelaunayTriangulation(vertices, holes);
                    JobHandle handle = triangulation.Schedule();
                    
                    // Free allocated input data.
                    vertices.Dispose(handle);
                    holes.Dispose(handle);
                    
                    // Store job handle and job for mesh generation and memory
                    // cleanup later on.
                    handlers[i] = handle;
                    triangulations[i] = triangulation;
                }
                JobHandle.CompleteAll(handlers);

                // When triangulation of all meshes has ended start generating
                // the actual navigation mesh for all nav meshes in the scene.
                for (int i = 0; i < length; i++)
                {
                    NavMesh2D navMesh = navMeshes[i];
                    ConstrainedDelaunayTriangulation triangulation = triangulations[i];
                    
                    // Generate the actual navigation mesh
                    navMesh.GenerateNavigationMesh(triangulation.triangles);
                    
                    // Update the navigation mesh asset with the computed data.
                    navMesh.UpdateNavigationAsset(triangulation.triangles.ToArray().ToList());
                }
                
                // Free all the memory allocated for the jobs.
                handlers.Dispose();
                foreach (ConstrainedDelaunayTriangulation triangulation in triangulations)
                {
                    triangulation.Dispose();
                    triangulation.triangles.Dispose();
                }
            }
        }

        ///<summary>
        /// Generate the nav mesh geometry.
        ///</summary>
        private void BuildGeometry(ref NativeList<float2> vertexBuffer, ref NativeList<Edge2D> holes)
        {
            // All the nav mesh obstacle collider inside the nav mesh volume.
            List<Collider2D> obstacleCollider = new List<Collider2D>();
            // The number of nav mesh obstacles inside the nav mesh volume.
            int obstaclesNumber = volume.Overlap(filter, obstacleCollider);
            // The thickness value of the border between nav mesh obstacles.
            Vector2 thickness = new Vector2(boundThickness, boundThickness);
            
            // Are there 1 or more obstacles?
            if (obstaclesNumber > 0)
            {
                List<Vector2> volumeCorners = GetBoxColliderCorners(volume);
                vertexBuffer.Add(volumeCorners[0]);
                vertexBuffer.Add(volumeCorners[1]);
                vertexBuffer.Add(volumeCorners[2]);
                vertexBuffer.Add(volumeCorners[3]);
                vertexBuffer.Add(new Vector2(volumeCorners[1].x - volume.bounds.extents.x, volumeCorners[1].y));
                vertexBuffer.Add(new Vector2(volumeCorners[2].x - volume.bounds.extents.x, volumeCorners[2].y));
                
                foreach (Collider2D collider in obstacleCollider)
                {
                    if (collider is CircleCollider2D circleCollider2D)
                    {
                        TransformCircleColliderPointsIntoNavPoints(circleCollider2D, ref vertexBuffer, ref holes);
                    }
                    else if (collider is BoxCollider2D boxCollider2D)
                    {
                        TransformBoxColliderPointsIntoNavPoints(boxCollider2D, ref vertexBuffer, ref holes);
                    }
                }
            }
        }

        /// <summary>
        /// Get circle colliders corners and transform all of them into nav points and constraints.
        /// </summary>
        /// <param name="circleCollider"> The collider to transform</param>
        /// <param name="vertexBuffer"> The vertices transformed as nav points</param>
        /// <param name="holes"> The constraint shape of the circleCollider. </param>
        private void TransformCircleColliderPointsIntoNavPoints(CircleCollider2D circleCollider, ref NativeList<float2> vertexBuffer, 
                                                                ref NativeList<Edge2D> holes)
        {
            Vector2[] cornerPoints = GetCircleColliderCorners(circleCollider, circleCornersCount);
            
            // Initialize corner points as navigation points
            foreach (Vector2 point in cornerPoints)
                SetNavPoint(point, vertexBuffer, circleCollider);
            
            // Initialize constraint shape for this circle collider
            for (int i = 0; i < cornerPoints.Length; i++)
            {
                int nextIndex = (i + 1) % cornerPoints.Length;
                Edge2D edge = new Edge2D(cornerPoints[i], cornerPoints[nextIndex]);
                holes.Add(edge);
            }
        }
        
        /// <summary>
        /// Get the given number of corners points of a circle collider.
        /// </summary>
        /// <param name="circleCollider"> The collider we want to find it's corners</param>
        /// <param name="cornersCount">The number of parts in which the circle is gonna be divided</param>
        /// <returns> An array of 2D points representing the circle corners</returns>
        private Vector2[] GetCircleColliderCorners(CircleCollider2D circleCollider, int cornersCount)
        {
            Vector2 center = circleCollider.offset;
            // Divide circle area into many pieces of a given angle value.
            // a circle divided in 4 pieces, for example, will have an angle offset of 90 degrees.
            int angleOffset = 360 / cornersCount;
            float radius = circleCollider.radius;
            
            float[] angles = new float[cornersCount];
            // First angle offset should always be 0
            angles[0] = 0f;
            // Create new circle piece by performing an
            // offset of the previous angle by angleOffset amount.
            for (int i = 1; i < cornersCount; i++)
                angles[i] = angles[i - 1] + angleOffset;
            
            // Transform component reference of the circle collider.
            Transform colliderTransform = circleCollider.transform;
            
            Vector2[] points = new Vector2[cornersCount];
            // Find a point on the circle using the circle equation.
            for (int i = 0; i < cornersCount; i++)
            {
                // Get current angle in radians.
                float currentAngle = angles[i] * Mathf.Deg2Rad;
                Vector2 angleDirection = new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle));
                Vector2 point = center + radius * angleDirection;
                
                // Transform point position from local to world.
                points[i] = (Vector2) colliderTransform.transform.position + point;
            }
            
            return points;
        }
        
        private void TransformBoxColliderPointsIntoNavPoints(BoxCollider2D boxCollider, ref NativeList<float2> vertexBuffer, 
                                                             ref NativeList<Edge2D> holes)
        {
            // The collider bounding box corners.
            List<Vector2> corners = GetBoxColliderCorners(boxCollider);

            // Initialize collider corners and set them as nav mesh
            // points.
            Vector2 bottomLeft = corners[0];
            SetNavPoint(bottomLeft, vertexBuffer, boxCollider);
            Vector2 bottomRight = corners[1];
            SetNavPoint(bottomRight, vertexBuffer, boxCollider);
            Vector2 topRight = corners[2];
            SetNavPoint(topRight, vertexBuffer, boxCollider);
            Vector2 topLeft = corners[3];
            SetNavPoint(topLeft, vertexBuffer, boxCollider);
                    
            // Create the holes(constraints) of the navigation mesh for
            // Constrained Delaunay triangulation.
            holes.Add(new Edge2D(bottomLeft, bottomRight));
            holes.Add(new Edge2D(bottomRight, topRight));
            holes.Add(new Edge2D(topRight, topLeft));
            holes.Add(new Edge2D(topLeft, bottomLeft));
        }
        
        private void SetNavPoint(float2 point, NativeList<float2> vertices, Collider2D ignore)
        {
            Collider2D[] collider = Physics2D.OverlapPointAll(point, filter.layerMask);
            if ((collider.Length == 0 || collider[0] == ignore) && volume.OverlapPoint(point))
                vertices.Add(point);
        }
        
        // Get the corners of given the 2D box collider starting from bottom left
        // in counter-clockwise order.
        private List<Vector2> GetBoxColliderCorners(BoxCollider2D targetCollider)
        {
            List<Vector2> cornerPoints = new List<Vector2>();

            Transform colliderTransform = targetCollider.transform;
            
            // Set the scale factor to be uniform(1,1,0),
            // this will stop scale from affecting the points transformation.
            Matrix4x4 localToWorld = colliderTransform.localToWorldMatrix;
            localToWorld.SetTRS(colliderTransform.position, colliderTransform.rotation, new Vector3(1, 1,0));
            
            Vector2 size = targetCollider.bounds.extents;
            Vector2 offset = targetCollider.offset;
            
            // Compute collider corners.
            Vector2 bottomLeft = offset - size;
            Vector2 bottomRight = new Vector2(offset.x + size.x, offset.y - size.y);
            Vector2 topRight = offset + size;
            Vector2 topLeft = new Vector2(offset.x - size.x, offset.y + size.y);
            
            // Convert corners to world space and add them to the return list in counter-clockwise order
            // starting from bottom left corner.
            cornerPoints.Add(localToWorld.MultiplyPoint(bottomLeft));
            cornerPoints.Add(localToWorld.MultiplyPoint(bottomRight));
            cornerPoints.Add(localToWorld.MultiplyPoint(topRight));
            cornerPoints.Add(localToWorld.MultiplyPoint(topLeft));
            
            return cornerPoints;
        }
        
        // Using nav mesh geometry data, triangulate and generate the final navigation data
        public void GenerateNavigationMesh(NativeArray<Triangle2D> triangles)
        {
            // Build collision shape from triangulation.
            navCollider.pathCount = 0;
            Vector2[] path = new Vector2[3];
            for (int i = 0; i < triangles.Count(); i++)
            {
                Triangle2D triangle = triangles[i];
                float2 offset = new float2(transform.position.x, transform.position.y);
                path[0] = triangle.A - offset;
                path[1] = triangle.B - offset;
                path[2] = triangle.C - offset;
                navCollider.pathCount++;
                navCollider.SetPath(i, path);
            }

            // Create navigation mesh using collision shape.
            if (meshFilter.sharedMesh != null)
                meshFilter.sharedMesh.Clear();

            // Generate Mesh using polygon collider data.
            Mesh mesh = navCollider.CreateMesh(true, true);
            
            // Convert mesh vertices to local space.
            if (mesh != null)
            {
                Vector3[] vertexBuffer = mesh.vertices;
                for (int i = 0; i < vertexBuffer.Length; i++)
                    vertexBuffer[i] = vertexBuffer[i] - transform.position;
                mesh.vertices = vertexBuffer;
                meshFilter.sharedMesh = mesh;
            }
        }
    }
}


