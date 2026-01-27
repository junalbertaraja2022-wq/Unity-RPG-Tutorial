using UnityEngine;
using UnityEngine.Tilemaps;

public class BoardManager : MonoBehaviour
{
    private CellData[,] m_BoardData;
    private Tilemap m_Tilemap;
    private Grid m_Grid;

    [Header("Grid Settings")]
    public int Width = 10;
    public int Height = 10;
    
    [Header("Tile Settings")]
    public Tile[] GroundTiles;
    public Tile[] WallTiles;
    
    [Header("Wall Settings")]
    public GameObject WallPrefab;
    public int minWalls = 3;
    public int maxWalls = 7;
    
    [Header("Food Settings")]
    public GameObject FoodPrefab;
    public int foodCount = 5;
    
    [Header("Camera Settings")]
    public Camera mainCamera;
    public float padding = 1f;
    
    [Header("Board Offset")]
    public Vector2Int boardOffset = Vector2Int.zero;

    void Start()
    {
        InitializeComponents();
        GenerateBoard();
        GenerateWalls();
        GenerateFood();
       // CenterCamera();
    }

    void InitializeComponents()
    {
        m_Tilemap = GetComponentInChildren<Tilemap>();
        if (m_Tilemap == null)
        {
            GameObject tilemapObj = new GameObject("Tilemap");
            tilemapObj.transform.SetParent(transform);
            m_Tilemap = tilemapObj.AddComponent<Tilemap>();
            tilemapObj.AddComponent<TilemapRenderer>();
        }

        m_Grid = GetComponentInChildren<Grid>();
        if (m_Grid == null)
        {
            m_Grid = GetComponent<Grid>();
            if (m_Grid == null)
            {
                GameObject gridObj = new GameObject("Grid");
                gridObj.transform.SetParent(transform);
                m_Grid = gridObj.AddComponent<Grid>();
            }
        }

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void GenerateBoard()
    {
        m_Tilemap.ClearAllTiles();
        m_BoardData = new CellData[Width, Height];
        
        boardOffset = new Vector2Int(-Width / 2, -Height / 2);
        
        if (Width % 2 == 0) boardOffset.x += 1;
        if (Height % 2 == 0) boardOffset.y += 1;

        for (int y = 0; y < Height; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                Tile tile;
                m_BoardData[x, y] = new CellData();

                // Create borders (impassable)
                if (x == 0 || y == 0 || x == Width - 1 || y == Height - 1)
                {
                    tile = WallTiles[Random.Range(0, WallTiles.Length)];
                    m_BoardData[x, y].Passable = false;
                    
                    if (tile != null && tile.colliderType == Tile.ColliderType.None)
                    {
                        tile.colliderType = Tile.ColliderType.Grid;
                    }
                }
                else
                {
                    tile = GroundTiles[Random.Range(0, GroundTiles.Length)];
                    m_BoardData[x, y].Passable = true;
                }

                Vector3Int tilePosition = new Vector3Int(
                    boardOffset.x + x,
                    boardOffset.y + y,
                    0
                );

                m_Tilemap.SetTile(tilePosition, tile);
                
                if (!m_BoardData[x, y].Passable)
                {
                    m_Tilemap.SetColliderType(tilePosition, Tile.ColliderType.Grid);
                }
            }
        }

        AddCollisionComponents();
    }
    
    void GenerateWalls()
    {
        if (WallPrefab == null)
        {
            Debug.LogError("BoardManager: WallPrefab is not assigned!");
            return;
        }
        
        int wallCount = Random.Range(minWalls, maxWalls + 1);
        Debug.Log($"Generating {wallCount} walls...");
        
        int wallsPlaced = 0;
        int maxAttempts = 200;
        
        for (int i = 0; i < wallCount; ++i)
        {
            int attempts = 0;
            bool wallPlaced = false;
            
            while (attempts < maxAttempts && !wallPlaced)
            {
                // Get random position inside playable area
                int randomX = Random.Range(1, Width - 1);
                int randomY = Random.Range(1, Height - 1);
                
                CellData data = m_BoardData[randomX, randomY];
                
                // Check if cell is passable and doesn't already have an object
                if (data.Passable && data.ContainedObject == null)
                {
                    // Don't place walls too close to spawn (1,1)
                    float distanceToSpawn = Vector2Int.Distance(new Vector2Int(randomX, randomY), new Vector2Int(1, 1));
                    if (distanceToSpawn < 2f) // Minimum 2 cells away from spawn
                    {
                        attempts++;
                        continue;
                    }
                    
                    // Create wall object
                    GameObject newWall = Instantiate(WallPrefab);
                    
                    // Position it at the center of the cell
                    Vector3 worldPos = GetCellWorldPosition(randomX, randomY);
                    newWall.transform.position = worldPos;
                    
                    // Set parent for organization
                    newWall.transform.SetParent(transform);
                    
                    // Add WallController component if not present
                    WallController wallController = newWall.GetComponent<WallController>();
                    if (wallController == null)
                    {
                        wallController = newWall.AddComponent<WallController>();
                    }
                    
                    // Initialize wall with its grid position
                    wallController.Initialize(this, new Vector2Int(randomX, randomY));
                    
                    // Store reference in cell data
                    data.ContainedObject = newWall;
                    
                    // Mark cell as impassable
                    data.Passable = false;
                    
                    wallsPlaced++;
                    wallPlaced = true;
                    Debug.Log($"Wall placed at cell ({randomX}, {randomY})");
                }
                
                attempts++;
            }
            
            if (!wallPlaced)
            {
                Debug.LogWarning($"Could not place wall {i + 1} after {maxAttempts} attempts");
            }
        }
        
        Debug.Log($"Successfully placed {wallsPlaced}/{wallCount} walls");
    }
    
    void GenerateFood()
    {
        if (FoodPrefab == null)
        {
            Debug.LogError("BoardManager: FoodPrefab is not assigned!");
            return;
        }
        
        Debug.Log($"Generating {foodCount} food items...");
        
        int foodPlaced = 0;
        int maxAttempts = 100;
        
        for (int i = 0; i < foodCount; ++i)
        {
            int attempts = 0;
            bool foodPlacedThisIteration = false;
            
            while (attempts < maxAttempts && !foodPlacedThisIteration)
            {
                int randomX = Random.Range(1, Width - 1);
                int randomY = Random.Range(1, Height - 1);
                
                CellData data = m_BoardData[randomX, randomY];
                
                // Check if cell is passable and doesn't already have an object
                if (data.Passable && data.ContainedObject == null)
                {
                    GameObject newFood = Instantiate(FoodPrefab);
                    Vector3 worldPos = GetCellWorldPosition(randomX, randomY);
                    newFood.transform.position = worldPos;
                    newFood.transform.SetParent(transform);
                    
                    // Add FoodCollectible component if not present
                    FoodCollectible foodCollectible = newFood.GetComponent<FoodCollectible>();
                    if (foodCollectible == null)
                    {
                        foodCollectible = newFood.AddComponent<FoodCollectible>();
                    }
                    
                    data.ContainedObject = newFood;
                    foodPlaced++;
                    foodPlacedThisIteration = true;
                }
                
                attempts++;
            }
        }
        
        Debug.Log($"Successfully placed {foodPlaced}/{foodCount} food items");
    }
    
    void AddCollisionComponents()
    {
        TilemapCollider2D tilemapCollider = m_Tilemap.GetComponent<TilemapCollider2D>();
        if (tilemapCollider == null)
        {
            tilemapCollider = m_Tilemap.gameObject.AddComponent<TilemapCollider2D>();
            
            Rigidbody2D rb = m_Tilemap.gameObject.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = m_Tilemap.gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;
            }
            
            CompositeCollider2D compositeCollider = m_Tilemap.gameObject.GetComponent<CompositeCollider2D>();
            if (compositeCollider == null)
            {
                compositeCollider = m_Tilemap.gameObject.AddComponent<CompositeCollider2D>();
                tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            }
        }
    }
    
    // ... (Rest of your BoardManager methods like CenterCamera, GetCellWorldPosition, etc.)

    // Public methods
    public bool IsCellPassable(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return false;
        
        return m_BoardData[x, y].Passable;
    }
    
    public bool IsCellPassable(Vector2Int cell)
    {
        return IsCellPassable(cell.x, cell.y);
    }
    
    public CellData GetCellData(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return null;
        
        return m_BoardData[x, y];
    }
    
    public CellData GetCellData(Vector2Int cell)
    {
        return GetCellData(cell.x, cell.y);
    }
    
    public void ClearCellObject(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;
        
        m_BoardData[x, y].ContainedObject = null;
        m_BoardData[x, y].Passable = true; // Make cell passable after object is cleared
    }
    
    public void ClearCellObject(Vector2Int cell)
    {
        ClearCellObject(cell.x, cell.y);
    }
    
    public Vector3 GetCellWorldPosition(int x, int y)
    {
        Vector3Int gridPosition = new Vector3Int(
            boardOffset.x + x,
            boardOffset.y + y,
            0
        );
        
        // Return center of the cell
        return m_Tilemap.CellToWorld(gridPosition) + new Vector3(0.5f, 0.5f, 0);
    }
    
    public Vector3 GetCellWorldPosition(Vector2Int cell)
    {
        return GetCellWorldPosition(cell.x, cell.y);
    }
    
    public Vector2Int WorldToCell(Vector3 worldPosition)
    {
        Vector3Int gridPosition = m_Tilemap.WorldToCell(worldPosition);
        return new Vector2Int(
            gridPosition.x - boardOffset.x,
            gridPosition.y - boardOffset.y
        );
    }
    
    public int GetBoardWidth() => Width;
    public int GetBoardHeight() => Height;
    
    // Method to make wall cell passable after destruction
    public void DestroyWallAtCell(Vector2Int cell)
    {
        CellData data = GetCellData(cell);
        if (data != null && data.HasWall)
        {
            ClearCellObject(cell);
            Debug.Log($"Wall destroyed at cell {cell}. Cell is now passable.");
        }
    }
}