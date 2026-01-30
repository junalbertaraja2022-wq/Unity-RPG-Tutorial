using UnityEngine;


public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    
    [Header("References")]
    public MapGenerator mapGenerator;
    
    private Vector2Int currentCell;
    private bool isMoving = false;
    private Vector3 targetPosition;
    
    void Start()
    {
        if (mapGenerator == null)
        {
            mapGenerator = FindFirstObjectByType<MapGenerator>();
        }
        
        // Start at a passable cell (e.g., 1,1 inside the borders)
        currentCell = new Vector2Int(1, 1);
        transform.position = mapGenerator.GetCellWorldPosition(currentCell);
    }
    
    void Update()
    {
        // Handle movement
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;
            }
        }
        
        // Handle input
        HandleInput();
    }
    
    void HandleInput()
    {
        if (isMoving) return;
        
        Vector2Int moveDirection = Vector2Int.zero;
        
        #if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                moveDirection = Vector2Int.up;
            else if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
                moveDirection = Vector2Int.down;
            else if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
                moveDirection = Vector2Int.left;
            else if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
                moveDirection = Vector2Int.right;
        }
        #else
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            moveDirection = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            moveDirection = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            moveDirection = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            moveDirection = Vector2Int.right;
        #endif
        
        if (moveDirection != Vector2Int.zero)
        {
            TryMove(moveDirection);
        }
    }
    
    void TryMove(Vector2Int direction)
    {
        Vector2Int targetCell = currentCell + direction;
        
        // Check if target cell is passable
        if (mapGenerator.IsCellPassable(targetCell))
        {
            MoveToCell(targetCell);
        }
        else
        {
            Debug.Log($"Cannot move to ({targetCell.x}, {targetCell.y}) - cell is impassable!");
        }
    }
    
    void MoveToCell(Vector2Int cell)
    {
        currentCell = cell;
        targetPosition = mapGenerator.GetCellWorldPosition(cell);
        isMoving = true;
    }
}