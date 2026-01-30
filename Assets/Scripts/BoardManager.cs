using UnityEngine;
using UnityEngine.Tilemaps;

public class BoardManager : MonoBehaviour
{
    [System.Serializable]
    public class CellData
    {
        public bool Passable;
    }

    private CellData[,] m_BoardData;
    private Tilemap m_Tilemap;
    private Grid m_Grid;

    [Header("Grid Settings")]
    public int Width = 10;
    public int Height = 10;
    
    [Header("Tile Settings")]
    public Tile[] GroundTiles;
    public Tile[] WallTiles;
    
    [Header("Camera Settings")]
    public Camera mainCamera;
    public float padding = 1f; // Padding around grid in world units
    
    [Header("Board Offset")]
    public Vector2Int boardOffset = Vector2Int.zero; // For centering the board

    // Start is called before the first frame update
    void Start()
    {
        InitializeComponents();
        GenerateBoard();
        CenterCamera();
    }

    void InitializeComponents()
    {
        // Get or create tilemap
        m_Tilemap = GetComponentInChildren<Tilemap>();
        if (m_Tilemap == null)
        {
            GameObject tilemapObj = new GameObject("Tilemap");
            tilemapObj.transform.SetParent(transform);
            m_Tilemap = tilemapObj.AddComponent<Tilemap>();
            tilemapObj.AddComponent<TilemapRenderer>();
        }

        // Get grid component
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

        // Get main camera if not assigned
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void GenerateBoard()
    {
        // Clear existing tiles
        m_Tilemap.ClearAllTiles();
        
        // Initialize board data
        m_BoardData = new CellData[Width, Height];
        
        // Calculate offset to center the board
        boardOffset = new Vector2Int(-Width / 2, -Height / 2);
        
        // If dimensions are even, adjust to center properly
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
                    
                    // Set collider type for impassable tiles
                    if (tile != null)
                    {
                        // Ensure the wall tile has a collider
                        if (tile.colliderType == Tile.ColliderType.None)
                        {
                            tile.colliderType = Tile.ColliderType.Grid;
                        }
                    }
                }
                else
                {
                    tile = GroundTiles[Random.Range(0, GroundTiles.Length)];
                    m_BoardData[x, y].Passable = true;
                }

                // Calculate position with offset for centering
                Vector3Int tilePosition = new Vector3Int(
                    boardOffset.x + x,
                    boardOffset.y + y,
                    0
                );

                m_Tilemap.SetTile(tilePosition, tile);
                
                // Ensure collider is set for wall tiles
                if (!m_BoardData[x, y].Passable)
                {
                    m_Tilemap.SetColliderType(tilePosition, Tile.ColliderType.Grid);
                }
            }
        }

        // Add TilemapCollider2D for physics if not present
        TilemapCollider2D tilemapCollider = m_Tilemap.GetComponent<TilemapCollider2D>();
        if (tilemapCollider == null)
        {
            tilemapCollider = m_Tilemap.gameObject.AddComponent<TilemapCollider2D>();
            
            // Optional: Add CompositeCollider2D for better performance
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
                tilemapCollider.usedByComposite = true;
            }
        }
    }

    void CenterCamera()
    {
        if (mainCamera == null)
        {
            Debug.LogError("Main camera is not assigned!");
            return;
        }

        // Calculate board center in world coordinates
        Vector3 boardCenterWorld = CalculateBoardCenterWorld();
        
        // Calculate board size in world units
        Vector2 boardSizeWorld = CalculateBoardSizeWorld();
        
        // Center camera on board
        if (mainCamera.orthographic)
        {
            CenterOrthographicCamera(boardCenterWorld, boardSizeWorld);
        }
        else
        {
            CenterPerspectiveCamera(boardCenterWorld, boardSizeWorld);
        }
        
        Debug.Log($"Camera centered. Board center: {boardCenterWorld}, Board size: {boardSizeWorld}");
    }

    Vector3 CalculateBoardCenterWorld()
    {
        // Get the world position of the center cell
        Vector3Int centerCell = new Vector3Int(
            boardOffset.x + Width / 2,
            boardOffset.y + Height / 2,
            0
        );
        
        return m_Tilemap.CellToWorld(centerCell) + new Vector3(0.5f, 0.5f, 0);
    }

    Vector2 CalculateBoardSizeWorld()
    {
        // Calculate board size based on grid cell size
        Vector3 cellSize = m_Grid.cellSize;
        return new Vector2(Width * cellSize.x, Height * cellSize.y);
    }

    void CenterOrthographicCamera(Vector3 boardCenterWorld, Vector2 boardSizeWorld)
    {
        // Calculate required camera size to fit the entire board
        float screenRatio = (float)Screen.width / Screen.height;
        float boardRatio = boardSizeWorld.x / boardSizeWorld.y;
        
        float requiredSize;
        if (screenRatio >= boardRatio)
        {
            // Screen is wider than board (height is limiting)
            requiredSize = boardSizeWorld.y * 0.5f + padding;
        }
        else
        {
            // Board is wider than screen (width is limiting)
            requiredSize = (boardSizeWorld.x / screenRatio) * 0.5f + padding;
        }
        
        // Set camera properties
        mainCamera.orthographicSize = requiredSize;
        mainCamera.transform.position = new Vector3(
            boardCenterWorld.x,
            boardCenterWorld.y,
            mainCamera.transform.position.z
        );
    }

    void CenterPerspectiveCamera(Vector3 boardCenterWorld, Vector2 boardSizeWorld)
    {
        // For perspective camera, calculate distance needed to see entire board
        float distance = Mathf.Max(
            boardSizeWorld.x * 0.5f / Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad),
            boardSizeWorld.y * 0.5f / Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad)
        ) + padding;
        
        mainCamera.transform.position = new Vector3(
            boardCenterWorld.x,
            boardCenterWorld.y,
            -distance
        );
    }

    // Public methods for other scripts to use

    public bool IsCellPassable(int x, int y)
    {
        // Check bounds
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return false;
        
        return m_BoardData[x, y].Passable;
    }

    public bool IsCellPassable(Vector2Int cell)
    {
        return IsCellPassable(cell.x, cell.y);
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

    public Vector2Int GetBoardOffset()
    {
        return boardOffset;
    }

    public int GetBoardWidth()
    {
        return Width;
    }

    public int GetBoardHeight()
    {
        return Height;
    }

    // Editor method to regenerate board (optional)
    #if UNITY_EDITOR
    [ContextMenu("Regenerate Board")]
    void RegenerateBoard()
    {
        InitializeComponents();
        GenerateBoard();
        CenterCamera();
    }
    #endif

    // Draw gizmos in editor to visualize board bounds
    void OnDrawGizmosSelected()
    {
        if (m_Tilemap == null || m_BoardData == null) return;
        
        // Draw board bounds
        Gizmos.color = Color.yellow;
        
        Vector3 bottomLeft = GetCellWorldPosition(0, 0) - new Vector3(0.5f, 0.5f, 0);
        Vector3 topRight = GetCellWorldPosition(Width - 1, Height - 1) + new Vector3(0.5f, 0.5f, 0);
        
        Vector3 size = topRight - bottomLeft;
        Vector3 center = bottomLeft + size * 0.5f;
        
        Gizmos.DrawWireCube(center, new Vector3(size.x, size.y, 0.1f));
        
        // Draw impassable cells in red
        Gizmos.color = Color.red;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (!m_BoardData[x, y].Passable)
                {
                    Vector3 cellCenter = GetCellWorldPosition(x, y);
                    Gizmos.DrawWireCube(cellCenter, new Vector3(0.8f, 0.8f, 0.1f));
                }
            }
        }
    }
}