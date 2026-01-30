using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    public int mapWidth = 50;
    public int mapHeight = 50;
    
    [Header("Tile Assets")]
    public Tilemap floorTilemap;
    public Tilemap wallTilemap;
    public Tile[] floorTiles;
    public Tile[] wallTiles;
    
    [Header("Grid Reference")]
    public Grid grid; // Added Grid reference
    
    [Header("Variety Settings")]
    [Range(0f, 1f)] public float tileVarietyChance = 0.3f;
    
    [Header("Generation Settings")]
    public GenerationAlgorithm algorithm = GenerationAlgorithm.RandomWalk;
    
    [Header("Random Walk Settings")]
    [Range(0.1f, 0.9f)] public float fillPercentage = 0.45f;
    public int randomWalkSteps = 2500;
    
    [Header("Cellular Automata Settings")]
    [Range(0.1f, 0.9f)] public float initialWallChance = 0.45f;
    public int smoothingIterations = 5;
    
    [Header("Room Settings")]
    public int minRoomSize = 4;
    public int maxRoomSize = 10;
    public int maxRooms = 20;
    
    [Header("Debug")]
    public bool generateOnStart = true;
    public bool clearOnGenerate = true;
    
    public enum TileType { Void, Floor, Wall };
    public enum GenerationAlgorithm { RandomWalk, CellularAutomata, RoomBased }
    
    private TileType[,] mapData;
    
    void Start()
    {
        // Try to find missing components
        if (grid == null)
            grid = GetComponent<Grid>();
        if (grid == null)
            grid = GetComponentInChildren<Grid>();
        
        if (floorTilemap == null)
            floorTilemap = GetComponentInChildren<Tilemap>();
        
        if (wallTilemap == null)
        {
            Tilemap[] tilemaps = GetComponentsInChildren<Tilemap>();
            foreach (var tm in tilemaps)
            {
                if (tm != floorTilemap && wallTilemap == null)
                    wallTilemap = tm;
            }
        }
        
        if (generateOnStart)
        {
            GenerateMap();
        }
    }
    
    [ContextMenu("Generate Map")]
    public void GenerateMap()
    {
        GenerateMapData();
        RenderMap();
    }
    
    [ContextMenu("Clear Map")]
    public void ClearMap()
    {
        if (floorTilemap != null) floorTilemap.ClearAllTiles();
        if (wallTilemap != null) wallTilemap.ClearAllTiles();
        mapData = null;
    }
    
    void GenerateMapData()
    {
        mapData = new TileType[mapWidth, mapHeight];
        
        switch (algorithm)
        {
            case GenerationAlgorithm.RandomWalk:
                GenerateWithRandomWalk();
                break;
            case GenerationAlgorithm.CellularAutomata:
                GenerateWithCellularAutomata();
                break;
            case GenerationAlgorithm.RoomBased:
                GenerateWithRooms();
                break;
        }
        
        AddBorderWalls();
    }
    
    void GenerateWithRandomWalk()
    {
        int x = mapWidth / 2;
        int y = mapHeight / 2;
        
        for (int i = -2; i <= 2; i++)
        {
            for (int j = -2; j <= 2; j++)
            {
                if (IsInBounds(x + i, y + j))
                {
                    mapData[x + i, y + j] = TileType.Floor;
                }
            }
        }
        
        for (int i = 0; i < randomWalkSteps; i++)
        {
            int dir = Random.Range(0, 4);
            switch (dir)
            {
                case 0: x++; break;
                case 1: x--; break;
                case 2: y++; break;
                case 3: y--; break;
            }
            
            x = Mathf.Clamp(x, 1, mapWidth - 2);
            y = Mathf.Clamp(y, 1, mapHeight - 2);
            
            mapData[x, y] = TileType.Floor;
        }
    }
    
    void GenerateWithCellularAutomata()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (x == 0 || x == mapWidth - 1 || y == 0 || y == mapHeight - 1)
                {
                    mapData[x, y] = TileType.Wall;
                }
                else
                {
                    mapData[x, y] = Random.value < initialWallChance ? TileType.Wall : TileType.Floor;
                }
            }
        }
        
        for (int i = 0; i < smoothingIterations; i++)
        {
            SmoothMap();
        }
    }
    
    void SmoothMap()
    {
        TileType[,] newMap = new TileType[mapWidth, mapHeight];
        
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                int neighborWallCount = CountNeighborWalls(x, y);
                
                if (neighborWallCount > 4)
                    newMap[x, y] = TileType.Wall;
                else if (neighborWallCount < 4)
                    newMap[x, y] = TileType.Floor;
                else
                    newMap[x, y] = mapData[x, y];
            }
        }
        
        mapData = newMap;
    }
    
    int CountNeighborWalls(int x, int y)
    {
        int wallCount = 0;
        
        for (int nx = x - 1; nx <= x + 1; nx++)
        {
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                if (IsInBounds(nx, ny))
                {
                    if (nx == x && ny == y)
                        continue;
                        
                    if (mapData[nx, ny] == TileType.Wall)
                        wallCount++;
                }
                else
                {
                    wallCount++;
                }
            }
        }
        
        return wallCount;
    }
    
    void GenerateWithRooms()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                mapData[x, y] = TileType.Wall;
            }
        }
        
        Room[] rooms = new Room[maxRooms];
        
        for (int i = 0; i < maxRooms; i++)
        {
            int roomWidth = Random.Range(minRoomSize, maxRoomSize + 1);
            int roomHeight = Random.Range(minRoomSize, maxRoomSize + 1);
            
            int roomX = Random.Range(1, mapWidth - roomWidth - 1);
            int roomY = Random.Range(1, mapHeight - roomHeight - 1);
            
            Room newRoom = new Room(roomX, roomY, roomWidth, roomHeight);
            
            bool failed = false;
            foreach (Room otherRoom in rooms)
            {
                if (otherRoom != null && newRoom.Intersects(otherRoom))
                {
                    failed = true;
                    break;
                }
            }
            
            if (!failed)
            {
                for (int x = roomX; x < roomX + roomWidth; x++)
                {
                    for (int y = roomY; y < roomY + roomHeight; y++)
                    {
                        mapData[x, y] = TileType.Floor;
                    }
                }
                
                rooms[i] = newRoom;
                
                if (i > 0)
                {
                    ConnectRooms(rooms[i - 1], newRoom);
                }
            }
        }
    }
    
    void ConnectRooms(Room roomA, Room roomB)
    {
        Vector2Int centerA = roomA.center;
        Vector2Int centerB = roomB.center;
        
        if (Random.value < 0.5f)
        {
            CreateHorizontalCorridor(centerA.x, centerB.x, centerA.y);
            CreateVerticalCorridor(centerA.y, centerB.y, centerB.x);
        }
        else
        {
            CreateVerticalCorridor(centerA.y, centerB.y, centerA.x);
            CreateHorizontalCorridor(centerA.x, centerB.x, centerB.y);
        }
    }
    
    void CreateHorizontalCorridor(int x1, int x2, int y)
    {
        int start = Mathf.Min(x1, x2);
        int end = Mathf.Max(x1, x2);
        
        for (int x = start; x <= end; x++)
        {
            if (IsInBounds(x, y))
            {
                mapData[x, y] = TileType.Floor;
            }
        }
    }
    
    void CreateVerticalCorridor(int y1, int y2, int x)
    {
        int start = Mathf.Min(y1, y2);
        int end = Mathf.Max(y1, y2);
        
        for (int y = start; y <= end; y++)
        {
            if (IsInBounds(x, y))
            {
                mapData[x, y] = TileType.Floor;
            }
        }
    }
    
    void AddBorderWalls()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (x == 0 || x == mapWidth - 1 || y == 0 || y == mapHeight - 1)
                {
                    mapData[x, y] = TileType.Wall;
                }
            }
        }
    }
    
    bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < mapWidth && y >= 0 && y < mapHeight;
    }
    
    void RenderMap()
    {
        if (clearOnGenerate)
        {
            ClearMap();
        }
        
        if (floorTiles == null || floorTiles.Length == 0)
        {
            Debug.LogError("No floor tiles assigned!");
            return;
        }
        
        if (wallTiles == null || wallTiles.Length == 0)
        {
            Debug.LogError("No wall tiles assigned!");
            return;
        }
        
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                
                if (mapData[x, y] == TileType.Floor)
                {
                    Tile floorTileToUse = floorTiles[Random.Range(0, floorTiles.Length)];
                    floorTilemap.SetTile(tilePos, floorTileToUse);
                    
                    if (ShouldPlaceWall(x, y))
                    {
                        Tile wallTileToUse = wallTiles[Random.Range(0, wallTiles.Length)];
                        wallTilemap.SetTile(tilePos, wallTileToUse);
                    }
                }
                else if (mapData[x, y] == TileType.Wall)
                {
                    Tile wallTileToUse = wallTiles[Random.Range(0, wallTiles.Length)];
                    wallTilemap.SetTile(tilePos, wallTileToUse);
                }
            }
        }
    }
    
    bool ShouldPlaceWall(int x, int y)
    {
        if (mapData[x, y] != TileType.Floor) return false;
        
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                
                int nx = x + dx;
                int ny = y + dy;
                
                if (IsInBounds(nx, ny) && mapData[nx, ny] == TileType.Wall)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
 /*-------------------------------------------------------------------------------------------------------------------------------------------------------------------*/   
    // ADD THESE METHODS FOR PLAYER CONTROLLER:
    public Vector3 GetCellWorldPosition(int x, int y)
    {
        if (grid != null)
        {
            return grid.GetCellCenterWorld(new Vector3Int(x, y, 0));
        }
        else if (floorTilemap != null)
        {
            return floorTilemap.GetCellCenterWorld(new Vector3Int(x, y, 0));
        }
        else
        {
            // Fallback: calculate manually
            return new Vector3(x + 0.5f, y + 0.5f, 0);
        }
    }
    
    public Vector3 GetCellWorldPosition(Vector2Int cell)
    {
        return GetCellWorldPosition(cell.x, cell.y);
    }
    
    public bool IsCellPassable(int x, int y)
    {
        if (!IsInBounds(x, y)) return false;
        return mapData[x, y] == TileType.Floor;
    }
    
    public bool IsCellPassable(Vector2Int cell)
    {
        return IsCellPassable(cell.x, cell.y);
    }
    
    public Vector2Int? FindRandomFloorPosition()
    {
        List<Vector2Int> floorPositions = new List<Vector2Int>();
        
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (mapData[x, y] == TileType.Floor)
                {
                    floorPositions.Add(new Vector2Int(x, y));
                }
            }
        }
        
        if (floorPositions.Count > 0)
        {
            return floorPositions[Random.Range(0, floorPositions.Count)];
        }
        
        return null;
    }
    
    class Room
    {
        public int x, y, width, height;
        
        public Vector2Int center
        {
            get { return new Vector2Int(x + width / 2, y + height / 2); }
        }
        
        public Room(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }
        
        public bool Intersects(Room other)
        {
            return x <= other.x + other.width + 1 &&
                   x + width + 1 >= other.x &&
                   y <= other.y + other.height + 1 &&
                   y + height + 1 >= other.y;
        }
    }
}