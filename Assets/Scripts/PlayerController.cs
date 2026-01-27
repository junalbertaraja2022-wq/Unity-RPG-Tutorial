// PlayerController.cs
    using UnityEngine;

public class PlayerController : MonoBehaviour
{
   [Header("Movement Settings")]
    public float moveSpeed = 5f;
    
    [Header("Food Consumption")]
    public int foodConsumptionPerMove = 1;
    public int foodConsumptionPerAttack = 1; // Attacking walls costs more food
    
    [Header("Combat Settings")]
    public int attackDamage = 1;
    public float attackRange = 1f;
    public float attackCooldown = 0.5f;
    public GameObject attackEffectPrefab;
    
    [Header("Spawn Settings")]
    public Vector2Int spawnPosition = new Vector2Int(1, 1);
    public bool useRandomSpawn = false;
    public bool respawnAtStart = true;
    
    [Header("Turn-Based Settings")]
    public int moveActionCost = 0; // Moving costs action points
    public int attackActionCost = 1; // Attacking costs more action points
    
    [Header("References")]
    public BoardManager boardManager;
    public TurnManager turnManager;
    
    private Vector2Int currentCell;
    private Vector2Int targetCell;
    private bool isMoving = false;
    private bool hasMoved = false;
    private bool isAttacking = false;
    private float lastAttackTime = 0f;
    private Vector3 targetPosition;
    
    void Start()
    {
        // Try to find references if not assigned
        if (boardManager == null)
            boardManager = FindFirstObjectByType<BoardManager>();
        
        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();
        
        if (turnManager != null)
        {
            turnManager.OnPlayerTurnStart += OnPlayerTurnStart;
            turnManager.OnEnemyTurnStart += OnEnemyTurnStart;
        }
        
        if (respawnAtStart && boardManager != null)
        {
            Respawn();
        }
        
        if (!gameObject.CompareTag("Player"))
        {
            gameObject.tag = "Player";
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
                currentCell = targetCell;
                CheckForCollectibles();
            }
        }
        
        // Only handle input during player's turn
        if (turnManager != null && !turnManager.isPlayerTurn) return;
        
        HandleInput();
        
        // Check if we have a pending movement
        if (hasMoved)
        {
            ProcessMovement();
            hasMoved = false;
        }
    }
    
    void ProcessMovement()
    {
        if (!IsCellWithinBounds(targetCell))
        {
            Debug.Log($"Cannot move to ({targetCell.x}, {targetCell.y}) - cell is outside board bounds!");
            return;
        }
        
        CellData cellData = boardManager.GetCellData(targetCell);
        if (cellData == null)
        {
            Debug.Log($"Cannot move to ({targetCell.x}, {targetCell.y}) - invalid cell!");
            return;
        }
        
        // Check if cell has a wall
        if (cellData.HasWall)
        {
            Debug.Log($"Cell ({targetCell.x}, {targetCell.y}) has a wall! Attempting to attack...");
            AttemptAttackWall(targetCell);
            return;
        }
        
        // Check if cell is passable
        if (!cellData.Passable)
        {
            Debug.Log($"Cannot move to ({targetCell.x}, {targetCell.y}) - cell is impassable!");
            return;
        }
        
        // Check if we have enough action points
        if (turnManager != null && !turnManager.CanTakeAction(moveActionCost))
        {
            Debug.Log("Not enough action points to move!");
            return;
        }
        
        // Try to consume food for movement
        if (!ConsumeFoodForMovement())
        {
            Debug.Log("Not enough food to move!");
            return;
        }
        
        // All checks passed - perform movement
        if (turnManager != null)
        {
            turnManager.Tick();
        }
        
        MoveToCell(targetCell);
    }
    
    void AttemptAttackWall(Vector2Int targetCell)
    {
        // Check if we have enough action points for attack
        if (turnManager != null && !turnManager.CanTakeAction(attackActionCost))
        {
            Debug.Log("Not enough action points to attack!");
            return;
        }
    
        // Check if we have enough food for attack
        if (!ConsumeFoodForAttack())
        {
            Debug.Log("Not enough food to attack!");
            return;
        }
    
        // Check attack cooldown
        if (Time.time - lastAttackTime < attackCooldown)
        {
            Debug.Log("Attack on cooldown!");
            return;
        }

        // Find the wall at target cell
        CellData cellData = boardManager.GetCellData(targetCell);
    
    if (cellData == null || cellData.ContainedObject == null)
        {
            Debug.Log("No object found at target cell!");
            return;
        }
    
    // Check if it's a wall using component rather than tag
    WallController wall = cellData.ContainedObject.GetComponent<WallController>();
    if (wall == null)
        {
            Debug.Log($"Object at cell {targetCell} is not a wall!");
            return;
        }
    
        // Perform attack
        AttackWall(wall, targetCell);
    }
    
    void AttackWall(WallController wall, Vector2Int targetCell)
    {
        Debug.Log($"Attacking wall at {targetCell} with {attackDamage} damage");
        
            // Consume action points
            if (turnManager != null)
            {
                for (int i = 0; i < attackActionCost; i++)
                {
                    turnManager.Tick();
                }
            }
        
            // Attack animation/effect
            PlayAttackEffect(targetCell);
            
            // Damage the wall
            wall.TakeDamage(attackDamage);
            
            lastAttackTime = Time.time;
        
        // If wall is destroyed, move to its cell
        if (wall.IsDestroyed())
            {
                Debug.Log($"Wall destroyed! Moving to cell {targetCell}");
                MoveToCell(targetCell);
            }
            else
            {
                Debug.Log($"Wall still has {wall.GetCurrentHealth()}/{wall.GetMaxHealth()} health");
            }
    }
    
    void PlayAttackEffect(Vector2Int targetCell)
    {
        if (attackEffectPrefab != null)
        {
            Vector3 targetPos = boardManager.GetCellWorldPosition(targetCell);
            Instantiate(attackEffectPrefab, targetPos, Quaternion.identity);
        }
        
        // Optional: Player attack animation
        // StartCoroutine(AttackAnimation(targetCell));
    }
    
    System.Collections.IEnumerator AttackAnimation(Vector2Int targetCell)
    {
        Vector3 originalPos = transform.position;
        Vector3 attackDir = (boardManager.GetCellWorldPosition(targetCell) - originalPos).normalized;
        float attackDistance = 0.3f;
        
        // Lunge forward
        transform.position = originalPos + attackDir * attackDistance;
        yield return new WaitForSeconds(0.1f);
        
        // Return to original position
        transform.position = originalPos;
    }
    
    bool ConsumeFoodForMovement()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance is null!");
            return true;
        }
        
        return GameManager.Instance.ConsumeFood(foodConsumptionPerMove);
    }
    
    bool ConsumeFoodForAttack()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance is null!");
            return true;
        }
        
        return GameManager.Instance.ConsumeFood(foodConsumptionPerAttack);
    }
    
    bool IsCellWithinBounds(Vector2Int cell)
    {
        if (boardManager == null) return false;
        
        return cell.x >= 0 && cell.x < boardManager.GetBoardWidth() &&
               cell.y >= 0 && cell.y < boardManager.GetBoardHeight();
    }
    
    void HandleInput()
    {
        if (isMoving || hasMoved || isAttacking) return;
        
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
            targetCell = currentCell + moveDirection;
            hasMoved = true;
        }
    }
    
    void CheckForCollectibles()
    {
        if (boardManager == null) return;
        
        CellData cellData = boardManager.GetCellData(currentCell);
        
        if (cellData != null && cellData.ContainedObject != null)
        {
            // Check for food
            FoodCollectible foodCollectible = cellData.ContainedObject.GetComponent<FoodCollectible>();
            if (foodCollectible != null)
            {
                foodCollectible.Collect(gameObject);
            }
        }
    }
    
    void MoveToCell(Vector2Int cell)
    {
        targetCell = cell;
        targetPosition = boardManager.GetCellWorldPosition(cell);
        isMoving = true;
    }
    
    void OnPlayerTurnStart()
    {
        Debug.Log("PlayerController: Player turn started");
        // Reset movement flags at start of turn
        hasMoved = false;
        isMoving = false;
        isAttacking = false;
    }
    
    void OnEnemyTurnStart()
    {
        Debug.Log("PlayerController: Enemy turn started");
        // Disable player input during enemy turn
    }
    
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
        
        CellData cellData = boardManager.GetCellData(cell);
        if (cellData != null && !cellData.Passable && !cellData.HasWall)
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
        isAttacking = false;
        
        CheckForCollectibles();
        
        Debug.Log($"Player spawned at cell: {cell}");
    }   
    
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
        
        int maxAttempts = 100;
        int attempts = 0;
        
        while (attempts < maxAttempts)
        {
            Vector2Int randomCell = new Vector2Int(
                Random.Range(1, boardManager.GetBoardWidth() - 1),
                Random.Range(1, boardManager.GetBoardHeight() - 1)
            );
            
            if (boardManager.IsCellPassable(randomCell))
            {
                return randomCell;
            }
            
            attempts++;
        }
        
        return new Vector2Int(1, 1); // Default fallback
    }
    
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