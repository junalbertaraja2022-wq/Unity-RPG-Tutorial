using UnityEngine;
using System.Collections.Generic;

public enum TileType
{
    Plain,
    Forest,
    Mountain,
    Fort,
    Village,
    Castle,
    Water,
    Desert
}

[System.Serializable]
public class MapTile
{
    public TileType type;
    public Vector2Int position;
    public int movementCost = 1;
    public int defenseBonus = 0;
    public int avoidBonus = 0;
    public int movementCostModifier = 0;
    public GameObject visual;
    public Character occupant;
    
    public bool IsPassable(Character character)
    {
        if (occupant != null && occupant != character)
        {
            return false; // Tile is occupied
        }
        
        // Check if character can traverse this tile type
        switch (type)
        {
            case TileType.Mountain:
                return character.characterClass == CharacterClass.PegasusKnight || 
                       character.characterClass == CharacterClass.WyvernRider;
            case TileType.Water:
                return character.characterClass == CharacterClass.PegasusKnight;
            default:
                return true;
        }
    }
    
    public int GetMovementCost(Character character)
    {
        int cost = movementCost;
        
        // Class-specific movement
        if (type == TileType.Forest)
        {
            if (character.characterClass == CharacterClass.Thief)
            {
                cost = 1;
            }
        }
        
        return cost;
    }
}

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }
    
    [Header("Map Settings")]
    public int width = 15;
    public int height = 10;
    public float cellSize = 1f;
    
    [Header("Tile Prefabs")]
    public GameObject plainTilePrefab;
    public GameObject forestTilePrefab;
    public GameObject mountainTilePrefab;
    public GameObject fortTilePrefab;
    public GameObject waterTilePrefab;
    
    [Header("Visuals")]
    public GameObject movementRangePrefab;
    public GameObject attackRangePrefab;
    public GameObject pathIndicatorPrefab;
    
    private MapTile[,] grid;
    private Dictionary<Character, Vector2Int> characterPositions = new Dictionary<Character, Vector2Int>();
    private List<GameObject> rangeIndicators = new List<GameObject>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    
    public void GenerateMap()
    {
        grid = new MapTile[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                CreateTile(x, y);
            }
        }
        
        // Add some terrain features
        AddTerrainFeatures();
    }
    
    void CreateTile(int x, int y)
    {
        MapTile tile = new MapTile();
        tile.position = new Vector2Int(x, y);
        
        // Random terrain (you'd want more sophisticated generation)
        float noise = Mathf.PerlinNoise(x * 0.3f, y * 0.3f);
        
        if (noise < 0.2f)
        {
            tile.type = TileType.Water;
            tile.movementCost = 3;
            tile.defenseBonus = -10;
            tile.avoidBonus = -10;
            tile.visual = Instantiate(waterTilePrefab, GridToWorld(x, y), Quaternion.identity, transform);
        }
        else if (noise < 0.4f)
        {
            tile.type = TileType.Forest;
            tile.movementCost = 2;
            tile.defenseBonus = 10;
            tile.avoidBonus = 20;
            tile.visual = Instantiate(forestTilePrefab, GridToWorld(x, y), Quaternion.identity, transform);
        }
        else if (noise < 0.5f)
        {
            tile.type = TileType.Mountain;
            tile.movementCost = 4;
            tile.defenseBonus = 30;
            tile.avoidBonus = 10;
            tile.visual = Instantiate(mountainTilePrefab, GridToWorld(x, y), Quaternion.identity, transform);
        }
        else
        {
            tile.type = TileType.Plain;
            tile.movementCost = 1;
            tile.defenseBonus = 0;
            tile.avoidBonus = 0;
            tile.visual = Instantiate(plainTilePrefab, GridToWorld(x, y), Quaternion.identity, transform);
        }
        
        grid[x, y] = tile;
    }
    
    void AddTerrainFeatures()
    {
        // Add a fortress in the middle
        int centerX = width / 2;
        int centerY = height / 2;
        
        for (int x = centerX - 1; x <= centerX + 1; x++)
        {
            for (int y = centerY - 1; y <= centerY + 1; y++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    grid[x, y].type = TileType.Fort;
                    grid[x, y].defenseBonus = 20;
                    grid[x, y].avoidBonus = 10;
                    
                    if (grid[x, y].visual != null)
                    {
                        Destroy(grid[x, y].visual);
                    }
                    
                    grid[x, y].visual = Instantiate(fortTilePrefab, GridToWorld(x, y), Quaternion.identity, transform);
                }
            }
        }
    }
    
    public Vector3 GridToWorld(int x, int y)
    {
        return new Vector3(x * cellSize, y * cellSize, 0);
    }
    
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return GridToWorld(gridPos.x, gridPos.y);
    }
    
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / cellSize),
            Mathf.FloorToInt(worldPos.y / cellSize)
        );
    }
    
    public void PlaceCharacter(Character character, Vector2Int position, Team team)
    {
        if (!IsValidPosition(position)) return;
        
        character.gridPosition = position;
        character.team = team;
        characterPositions[character] = position;
        grid[position.x, position.y].occupant = character;
        
        // Position character in world
        character.transform.position = GridToWorld(position);
    }
    
    public void MoveCharacter(Character character, Vector2Int targetPosition)
    {
        if (!IsValidPosition(targetPosition)) return;
        if (!CanMoveTo(character, targetPosition)) return;
        
        // Clear old position
        if (characterPositions.ContainsKey(character))
        {
            Vector2Int oldPos = characterPositions[character];
            grid[oldPos.x, oldPos.y].occupant = null;
        }
        
        // Set new position
        character.gridPosition = targetPosition;
        characterPositions[character] = targetPosition;
        grid[targetPosition.x, targetPosition.y].occupant = character;
        
        // Animate movement
        StartCoroutine(MoveCharacterAnimation(character, targetPosition));
    }
    
    System.Collections.IEnumerator MoveCharacterAnimation(Character character, Vector2Int targetPosition)
    {
        Vector3 startPos = character.transform.position;
        Vector3 endPos = GridToWorld(targetPosition);
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            character.transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        character.transform.position = endPos;
    }
    
    public void RemoveCharacter(Character character)
    {
        if (characterPositions.ContainsKey(character))
        {
            Vector2Int pos = characterPositions[character];
            grid[pos.x, pos.y].occupant = null;
            characterPositions.Remove(character);
        }
    }
    
    public Character GetCharacterAt(Vector2Int position)
    {
        if (!IsValidPosition(position)) return null;
        return grid[position.x, position.y].occupant;
    }
    
    public MapTile GetTile(Vector2Int position)
    {
        if (!IsValidPosition(position)) return null;
        return grid[position.x, position.y];
    }
    
    public void ShowMovementRange(Character character)
    {
        ClearRangeIndicators();
        
        HashSet<Vector2Int> reachableTiles = GetReachableTiles(character);
        
        foreach (Vector2Int tilePos in reachableTiles)
        {
            GameObject indicator = Instantiate(movementRangePrefab, GridToWorld(tilePos), Quaternion.identity);
            rangeIndicators.Add(indicator);
        }
    }
    
    public void ShowAttackRange(Character character)
    {
        ClearRangeIndicators();
        
        HashSet<Vector2Int> attackRange = GetAttackRange(character);
        
        foreach (Vector2Int tilePos in attackRange)
        {
            GameObject indicator = Instantiate(attackRangePrefab, GridToWorld(tilePos), Quaternion.identity);
            rangeIndicators.Add(indicator);
        }
    }
    
  public HashSet<Vector2Int> GetReachableTiles(Character character)
    {
        HashSet<Vector2Int> reachable = new HashSet<Vector2Int>();
        Queue<Vector2Int> toExplore = new Queue<Vector2Int>();
        Dictionary<Vector2Int, int> moveCosts = new Dictionary<Vector2Int, int>();
        
        Vector2Int startPos = character.gridPosition;
        toExplore.Enqueue(startPos);
        moveCosts[startPos] = 0;
        
        while (toExplore.Count > 0)
        {
            Vector2Int current = toExplore.Dequeue();
            int currentCost = moveCosts[current];
            
            if (currentCost >= character.GetMovementRange()) continue;
            
            // Check all four directions
            Vector2Int[] directions = {
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, -1)
            };
            
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = current + dir;
                
                if (!IsValidPosition(neighbor)) continue;
                
                MapTile tile = grid[neighbor.x, neighbor.y];
                if (!tile.IsPassable(character)) continue;
                
                int moveCost = tile.GetMovementCost(character);
                int newCost = currentCost + moveCost;
                
                if (newCost <= character.GetMovementRange() && 
                    (!moveCosts.ContainsKey(neighbor) || newCost < moveCosts[neighbor]))
                {
                    moveCosts[neighbor] = newCost;
                    toExplore.Enqueue(neighbor);
                    reachable.Add(neighbor);
                }
            }
        }
        
        return reachable;
    }
    
    HashSet<Vector2Int> GetAttackRange(Character character)
    {
        HashSet<Vector2Int> attackRange = new HashSet<Vector2Int>();
        int range = character.GetAttackRange();
        
        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                if (Mathf.Abs(dx) + Mathf.Abs(dy) > range) continue;
                
                Vector2Int targetPos = character.gridPosition + new Vector2Int(dx, dy);
                
                if (IsValidPosition(targetPos))
                {
                    attackRange.Add(targetPos);
                }
            }
        }
        
        return attackRange;
    }
    
    bool CanMoveTo(Character character, Vector2Int targetPosition)
    {
        HashSet<Vector2Int> reachable = GetReachableTiles(character);
        return reachable.Contains(targetPosition);
    }
    
    void ClearRangeIndicators()
    {
        foreach (GameObject indicator in rangeIndicators)
        {
            Destroy(indicator);
        }
        rangeIndicators.Clear();
    }
    
    public Bounds GetMapBounds()
    {
        Vector3 center = GridToWorld(width / 2, height / 2);
        Vector3 size = new Vector3(width * cellSize, height * cellSize, 0);
        return new Bounds(center, size);
    }
    
    bool IsValidPosition(Vector2Int position)
    {
        return position.x >= 0 && position.x < width && 
               position.y >= 0 && position.y < height;
    }
}