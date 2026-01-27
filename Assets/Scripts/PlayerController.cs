using UnityEngine;
using System;   


public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Spawn Settings")]
    public Vector2Int spawnPosition = new Vector2Int(1, 1);
    public bool useRandomSpawn = false;
    public bool respawnAtStart = true;

    [Header("Turn-Based Settings")]
    public int moveActionCost = 1; // How many action points a move costs

    [Header("References")]
    public BoardManager boardManager;
    public TurnManager turnManager;  // Add this reference

    private Vector2Int currentCell;
    private Vector2Int targetCell;  // Store target cell for movement
    private bool isMoving = false;
    private bool hasMoved = false;  // Track if movement input was received
    private Vector3 targetPosition;

    void Start()
    {
        // Try to find references if not assigned
        if (boardManager == null)
            boardManager = FindFirstObjectByType<BoardManager>();

        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();

        // Subscribe to turn events
        if (turnManager != null)
        {
            turnManager.OnPlayerTurnStart += OnPlayerTurnStart;
            turnManager.OnEnemyTurnStart += OnEnemyTurnStart;
        }

        if (respawnAtStart && boardManager != null)
        {
            Respawn();
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (turnManager != null)
        {
            turnManager.OnPlayerTurnStart -= OnPlayerTurnStart;
            turnManager.OnEnemyTurnStart -= OnEnemyTurnStart;
        }
    }

    void Update()
    {
        // Handle movement animation
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;
                currentCell = targetCell; // Update current cell after movement completes
            }
        }

        // Only handle input during player's turn
        if (turnManager != null && !turnManager.isPlayerTurn) return;

        HandleInput();

        // Check if we have a pending movement
        if (hasMoved)
        {
            // Check if the new position is passable
            if (boardManager != null)
            {
                // You'll need to add GetCellData method to BoardManager
                // For now, use IsCellPassable
                if (boardManager.IsCellPassable(targetCell))
                {
                    // Check if we have enough action points
                    if (turnManager != null && turnManager.CanTakeAction(moveActionCost))
                    {
                        // Consume action points and move
                        turnManager.Tick();
                        MoveToCell(targetCell);
                    }
                    else if (turnManager != null)
                    {
                        Debug.Log("Not enough action points to move!");
                    }
                }
                else
                {
                    Debug.Log($"Cannot move to ({targetCell.x}, {targetCell.y}) - cell is impassable!");
                }
            }

            hasMoved = false; // Reset movement flag
        }
    }

    void HandleInput()
    {
        if (isMoving || hasMoved) return; // Don't accept new input while moving

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
            // Set target cell but don't move yet
            targetCell = currentCell + moveDirection;
            hasMoved = true; // Flag that we want to move
        }

        // Debug/Test input
        HandleDebugInput();
    }

    void HandleDebugInput()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.rKey.wasPressedThisFrame)
            {
                Respawn();
            }
            else if (keyboard.tKey.wasPressedThisFrame)
            {
                SpawnRandom();
            }
            else if (keyboard.fKey.wasPressedThisFrame)
            {
                TeleportToSpawn();
            }
            else if (keyboard.spaceKey.wasPressedThisFrame && turnManager != null)
            {
                // Space to end turn early
                turnManager.ForceEndTurn();
            }
        }
#else
        if (Input.GetKeyDown(KeyCode.R))
        {
            Respawn();
        }
        else if (Input.GetKeyDown(KeyCode.T))
        {
            SpawnRandom();
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            TeleportToSpawn();
        }
        else if (Input.GetKeyDown(KeyCode.Space) && turnManager != null)
        {
            turnManager.ForceEndTurn();
        }
#endif
    }

    void MoveToCell(Vector2Int cell)
    {
        targetCell = cell;
        targetPosition = boardManager.GetCellWorldPosition(cell);
        isMoving = true;
    }

    // Turn event handlers
    void OnPlayerTurnStart()
    {
        Debug.Log("PlayerController: Player turn started");
        // Reset movement flags at start of turn
        hasMoved = false;
        isMoving = false;
    }

    void OnEnemyTurnStart()
    {
        Debug.Log("PlayerController: Enemy turn started");
        // Disable player input during enemy turn
    }

    // Public methods for spawning (keep existing methods)
    public void Respawn()
    {
        if (boardManager == null)
        {
            Debug.LogError("Cannot respawn: BoardManager is null!");
            return;
        }

        if (useRandomSpawn)
        {
            SpawnRandom();
        }
        else
        {
            SpawnAtCell(spawnPosition);
        }

        // Reset movement state
        hasMoved = false;
        isMoving = false;
    }

    public void SpawnAtCell(Vector2Int cell)
    {
        if (boardManager == null)
        {
            Debug.LogError("Cannot spawn: BoardManager is null!");
            return;
        }

        // Validate and find passable cell (keep your existing logic)
        if (!boardManager.IsCellPassable(cell))
        {
            Debug.LogWarning($"Spawn position {cell} is not passable!");
            // Add your fallback logic here
        }

        currentCell = cell;
        targetCell = cell;
        transform.position = boardManager.GetCellWorldPosition(cell);
        targetPosition = transform.position;
        isMoving = false;
        hasMoved = false;

        Debug.Log($"Player spawned at cell: {cell}");
    }

    // Keep other existing methods (SpawnRandom, TeleportToSpawn, etc.)
    public void SpawnRandom()
    {
        if (boardManager == null)
        {
            Debug.LogError("Cannot spawn randomly: BoardManager is null!");
            return;
        }

        Vector2Int randomCell = GetRandomPassableCell();
        if (randomCell.x >= 0 && randomCell.y >= 0)
        {
            SpawnAtCell(randomCell);
        }
    }

    public void TeleportToSpawn()
    {
        SpawnAtCell(spawnPosition);
    }

    Vector2Int GetRandomPassableCell()
    {
        if (boardManager == null) return new Vector2Int(-1, -1);

        // Your existing random cell logic
        // ...
        return new Vector2Int(1, 1); // Simplified
    }

    // Public getters
    public Vector2Int GetCurrentCell()
    {
        return currentCell;
    }

    public bool IsMoving()
    {
        return isMoving;
    }

    public bool HasActionPoints()
    {
        return turnManager != null && turnManager.CanTakeAction(moveActionCost);
    }
}