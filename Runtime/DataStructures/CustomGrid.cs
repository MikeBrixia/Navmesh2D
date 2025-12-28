using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Navmesh2D
{
    
    public enum EGridType { Hexagonal, Quad }
    public struct GridCell
    {
        public float2 position;
        public float2 center;
        public GridCell(float2 position, float2 center)
        {
            this.position = position;
            this.center = center;
        }
    }
    
    public struct CustomGrid
    {
        public GridData data;

        public CustomGrid(int width, int height, Vector2 cellSize, Vector2 origin)
        {
            this.data = new GridData();
            // Initialize grid
            this.data.size.x = width;
            this.data.size.y = height;
            this.data.cellSize = cellSize;
            this.data.origin = origin;
        }
        
        public CustomGrid(GridData data)
        {
            this.data = data;
        }

        public float2 GetWorldPosition(int x, int y)
        {
            return (new float2(x, y) * data.cellSize  + data.origin);
        }
        
        public float2 GetCellCenter(int x, int y)
        {
           return GetWorldPosition(x, y) + data.cellSize * 0.5f;
        }

        public void DrawGrid(Color gridColor)
        {
            for (int x = 0; x < data.size.x; x++)
                for (int y = 0; y < data.size.y; y++)
                {
                    Debug.DrawLine((Vector2)GetWorldPosition(x, y), (Vector2)GetWorldPosition(x, y + 1), gridColor);
                    Debug.DrawLine((Vector2)GetWorldPosition(x, y), (Vector2)GetWorldPosition(x + 1, y), gridColor);
                }
            Debug.DrawLine((Vector2)GetWorldPosition(0, data.size.y), (Vector2)GetWorldPosition(data.size.x, data.size.y), gridColor);
            Debug.DrawLine((Vector2)GetWorldPosition(data.size.x, 0), (Vector2)GetWorldPosition(data.size.x, data.size.y), gridColor);
        }

        ///<summary>
        /// Get grid cell x and y from world position.
        ///</summary>
        ///<returns> The width(x) and height(y) of the cell the world position correspond to </returns>
        public void GetXY(float2 worldPosition, out int x, out int y)
        {
            x = (int) math.floor(worldPosition.x / data.cellSize.x - data.origin.x);
            y = (int) math.floor(worldPosition.y / data.cellSize.y - data.origin.y);
        }
        
        public NativeArray<int2> GetNearbyCells(float2 worldPosition)
        {
            int x = 0;
            int y = 0;
            GetXY(worldPosition, out x, out y);
            NativeArray<int2> nearbyCells = new NativeArray<int2>(8, Allocator.Temp);
            //east cell.
            nearbyCells[0] = SetCell(x+1, y);
            //west cell.
            nearbyCells[1] = SetCell(x-1, y);
            //north cell.
            nearbyCells[2] = SetCell(x, y+1);
            //south cell.
            nearbyCells[3] = SetCell(x, y-1);
            //north-east cell.
            nearbyCells[4] = SetCell(x+1, y+1);
            //north-west cell.
            nearbyCells[5] = SetCell(x-1, y+1); 
            //south-east cell.
            nearbyCells[6] = SetCell(x+1, y-1); 
            //south_west cell.
            nearbyCells[7] = SetCell(x-1, y-1);
            return nearbyCells;
        }
        
        private int2 SetCell(int x, int y)
        {
            int2 cell = int2.zero;
            if((x > 0 && y > 0) && (x < data.size.x && y < data.size.y))
                cell = new int2(x, y);
            return cell;
        }
        
        public GridCell GetCell(int x, int y)
        {
           float2 position = GetWorldPosition(x , y);
           float2 center = position + data.cellSize * 0.5f;
           return new GridCell(position, center);
        }

        public bool Equals(CustomGrid other)
        {
            return data.size.x == other.data.size.x & data.size.y == other.data.size.y & data.cellSize.Equals(other.data.cellSize);
        }
    }

    public struct GridData
    {
        public int2 size;
        public float2 origin;
        public float2 cellSize;
        public EGridType gridType;

        public GridData(int2 size, float2 origin, float2 cellSize, EGridType gridType)
        {
           this.size = size;
           this.origin = origin;
           this.cellSize = cellSize;
           this.gridType = gridType;
        }
    }
}

