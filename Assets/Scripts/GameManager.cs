using UnityEngine;
using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    [Header("Game References")]
    public BoardManager boardManager;
    public PlayerController playerController;
    public TurnManager turnManager;
    
    [Header("UI Toolkit References")]
    public UIDocument uiDocument;
    
    [Header("Game Settings")]
    public bool spawnPlayerOnStart = true;
    public Vector2Int playerSpawnCell = new Vector2Int(1, 1);
    
    [Header("Resource Settings")]
    public int startingFood = 100;
    public int foodPerTurn = 10;
    public int foodConsumptionRate = 5;
    
    // Private integer member that stores how much food you currently have
    private int currentFood;
    
    // Private variable of type Label to store reference to the Label
    private Label foodLabel;
    
    // Event for food changes
    public event System.Action<int> OnFoodChanged;
    
    // Singleton pattern
    public static GameManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        InitializeGame();
        InitializeUI();
    }
    
    void InitializeGame()
    {
        // Initialize food
        currentFood = startingFood;
        Debug.Log($"GameManager: Initialized with {currentFood} food");
        
        // Find references if not assigned
        if (boardManager == null)
            boardManager = FindFirstObjectByType<BoardManager>();
        
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();
        
        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>();
        
        // Find UIDocument if not assigned
        if (uiDocument == null)
        {
            uiDocument = FindFirstObjectByType<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogWarning("GameManager: No UIDocument found in scene. Food UI will not be displayed.");
            }
        }
        
        // Make sure player has reference to board manager
        if (playerController != null && playerController.boardManager == null && boardManager != null)
        {
            playerController.boardManager = boardManager;
        }
        
        // Make sure player has reference to turn manager
        if (playerController != null && playerController.turnManager == null && turnManager != null)
        {
            playerController.turnManager = turnManager;
        }
        
        // Register the OnTurnHappen method to TurnManager.OnTick callback
        if (turnManager != null)
        {
            turnManager.OnTick += OnTurnHappen;
            Debug.Log("GameManager: Registered OnTurnHappen to TurnManager.OnTick");
        }
        else
        {
            Debug.LogError("GameManager: TurnManager not found! Cannot register OnTurnHappen");
        }
        
        // Register to other turn events as well (optional)
        if (turnManager != null)
        {
            turnManager.OnPlayerTurnStart += OnPlayerTurnStart;
            turnManager.OnEnemyTurnStart += OnEnemyTurnStart;
            turnManager.OnTurnEnd += OnTurnEnd;
        }
        
        // Only spawn if everything is ready
        if (boardManager != null && playerController != null && playerController.boardManager != null)
        {
            playerController.spawnPosition = playerSpawnCell;
            playerController.Respawn();
        }
        else
        {
            Debug.LogError("Cannot spawn player - missing references!");
        }
        
        // Update UI with initial food value
        UpdateFoodUI();
    }
    
    /// <summary>
    /// Initializes the UI Toolkit elements
    /// </summary>
    void InitializeUI()
    {
        if (uiDocument == null)
        {
            Debug.LogError("GameManager: UIDocument is not assigned!");
            return;
        }
        
        // Get the root VisualElement
        VisualElement root = uiDocument.rootVisualElement;
        
        // Try to find the food label by name
        foodLabel = root.Q<Label>("food-label");
        
        // If not found by name, try to find by class
        if (foodLabel == null)
        {
            foodLabel = root.Q<Label>(className: "food-label");
        }
        
        // If still not found, try to find any label and check its name
        if (foodLabel == null)
        {
            var allLabels = root.Query<Label>().ToList();
            foreach (var label in allLabels)
            {
                if (label.name == "food-label" || label.ClassListContains("food-label"))
                {
                    foodLabel = label;
                    break;
                }
            }
        }
        
        // Create the label if it doesn't exist
        if (foodLabel == null)
        {
            Debug.LogWarning("GameManager: Food label not found in UI. Creating one...");
            CreateFoodLabel(root);
        }
        
        // Subscribe to food change event for UI updates
        OnFoodChanged += UpdateFoodLabel;
        
        // Set initial food value
        UpdateFoodLabel(currentFood);
    }
    
    /// <summary>
    /// Creates a food label if one doesn't exist in the UI
    /// </summary>
    void CreateFoodLabel(VisualElement root)
    {
        foodLabel = new Label();
        foodLabel.name = "food-label";
        foodLabel.AddToClassList("food-label");
        foodLabel.text = $"Food: {currentFood}";
        foodLabel.style.fontSize = 20;
        foodLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        foodLabel.style.color = Color.white;
        
        // REMOVED: Background color styling
        // foodLabel.style.backgroundColor = new Color(0, 0, 0, 0.7f);
        // foodLabel.style.paddingLeft = 10;
        // foodLabel.style.paddingRight = 10;
        // foodLabel.style.paddingTop = 5;
        // foodLabel.style.paddingBottom = 5;
        // foodLabel.style.marginTop = 10;
        // foodLabel.style.marginLeft = 10;
        // foodLabel.style.borderTopLeftRadius = 5;
        // foodLabel.style.borderTopRightRadius = 5;
        // foodLabel.style.borderBottomLeftRadius = 5;
        // foodLabel.style.borderBottomRightRadius = 5;
        
        // Add to root
        root.Add(foodLabel);
    }
    
    void OnDestroy()
    {
        // Unregister from events to prevent memory leaks
        if (turnManager != null)
        {
            turnManager.OnTick -= OnTurnHappen;
            turnManager.OnPlayerTurnStart -= OnPlayerTurnStart;
            turnManager.OnEnemyTurnStart -= OnEnemyTurnStart;
            turnManager.OnTurnEnd -= OnTurnEnd;
        }
        
        // Unsubscribe from food change event
        OnFoodChanged -= UpdateFoodLabel;
    }
    
    /// <summary>
    /// Method that will be called when a turn happens (registered to TurnManager.OnTick)
    /// </summary>
    void OnTurnHappen()
    {
        Debug.Log("GameManager: OnTurnHappen called");
        
        // Update food resources
        UpdateFood();
        
        // Check game conditions
        CheckGameConditions();
    }
    
    /// <summary>
    /// Updates food resources each turn
    /// </summary>
    void UpdateFood()
    {
        int oldFood = currentFood;
        
        // Add food per turn (like harvesting)
        currentFood += foodPerTurn;
        
        // Consume food (like feeding units)
        currentFood -= foodConsumptionRate;
        
        // Clamp food to not go negative
        currentFood = Mathf.Max(0, currentFood);
        
        Debug.Log($"GameManager: Food updated. Current: {currentFood} (+{foodPerTurn}, -{foodConsumptionRate})");
        
        // Trigger food change event
        if (oldFood != currentFood)
        {
            OnFoodChanged?.Invoke(currentFood);
        }
        
        // Check if food is critically low
        if (currentFood <= 20)
        {
            Debug.LogWarning($"Warning: Food supply is critically low! ({currentFood})");
            
            // Update label color for warning
            if (foodLabel != null)
            {
                if (currentFood <= 10)
                    foodLabel.style.color = Color.red;
                else
                    foodLabel.style.color = Color.yellow;
            }
            
            if (currentFood <= 0)
            {
                OnFoodDepleted();
            }
        }
        else if (foodLabel != null)
        {
            // Reset to normal color if food is sufficient
            foodLabel.style.color = Color.white;
        }
    }
    
    /// <summary>
    /// Updates the food label text
    /// </summary>
    void UpdateFoodLabel(int foodAmount)
    {
        if (foodLabel != null)
        {
            foodLabel.text = $"Food: {foodAmount}";
            
            // Optional: Add visual feedback when food changes
            if (foodAmount < currentFood)
            {
                // Food decreased - show red flash
                StartCoroutine(FlashLabelColor(Color.red, 0.3f));
            }
            else if (foodAmount > currentFood)
            {
                // Food increased - show green flash
                StartCoroutine(FlashLabelColor(Color.green, 0.3f));
            }
        }
        else
        {
            Debug.LogWarning("GameManager: Food label is null! Cannot update UI.");
        }
    }
    
    /// <summary>
    /// Coroutine to flash the label color
    /// </summary>
    System.Collections.IEnumerator FlashLabelColor(Color flashColor, float duration)
    {
        if (foodLabel == null) yield break;
        
        Color originalColor = foodLabel.style.color.value;
        foodLabel.style.color = flashColor;
        
        yield return new WaitForSeconds(duration);
        
        if (foodLabel != null)
        {
            // Return to appropriate color based on food level
            if (currentFood <= 10)
                foodLabel.style.color = Color.red;
            else if (currentFood <= 20)
                foodLabel.style.color = Color.yellow;
            else
                foodLabel.style.color = originalColor;
        }
    }
    
    /// <summary>
    /// Updates the food display in the UI (legacy method, kept for compatibility)
    /// </summary>
    void UpdateFoodUI()
    {
        // This method is kept for compatibility with existing code
        // It now just triggers the event
        OnFoodChanged?.Invoke(currentFood);
    }
    
    /// <summary>
    /// Called when player runs out of food
    /// </summary>
    void OnFoodDepleted()
    {
        Debug.LogError("GameManager: Food depleted! Game over condition triggered.");
        
        // Update label to show game over
        if (foodLabel != null)
        {
            foodLabel.text = "GAME OVER - No Food!";
            foodLabel.style.color = Color.red;
            foodLabel.style.fontSize = 24;
        }
        
        // Implement game over logic
        Debug.Log("Game Over! Restarting game...");
        Invoke(nameof(RestartGame), 2f); // Restart after 2 seconds
    }
    
    /// <summary>
    /// Checks various game conditions each turn
    /// </summary>
    void CheckGameConditions()
    {
        // Check win condition (example: reach a certain turn)
        if (turnManager != null && turnManager.currentTurn >= 10)
        {
            Debug.Log("Congratulations! You survived 10 turns!");
            
            // Update UI to show victory
            if (foodLabel != null)
            {
                foodLabel.text = "VICTORY! Survived 10 turns!";
                foodLabel.style.color = Color.green;
            }
        }
        
        // Check if player is still alive
        if (playerController == null)
        {
            Debug.LogError("Player is dead! Game over.");
            
            if (foodLabel != null)
            {
                foodLabel.text = "GAME OVER - Player Died!";
                foodLabel.style.color = Color.red;
            }
            
            RestartGame();
        }
    }
    
    /// <summary>
    /// Event handler for player turn start
    /// </summary>
    void OnPlayerTurnStart()
    {
        Debug.Log("GameManager: Player turn started");
        
        // REMOVED: Background color change for player turn
        // if (foodLabel != null)
        // {
        //     foodLabel.style.backgroundColor = new Color(0, 0.3f, 0, 0.7f);
        // }
    }
    
    /// <summary>
    /// Event handler for enemy turn start
    /// </summary>
    void OnEnemyTurnStart()
    {
        Debug.Log("GameManager: Enemy turn started");
        
        // REMOVED: Background color change for enemy turn
        // if (foodLabel != null)
        // {
        //     foodLabel.style.backgroundColor = new Color(0.3f, 0, 0, 0.7f);
        // }
    }
    
    /// <summary>
    /// Event handler for turn end
    /// </summary>
    void OnTurnEnd()
    {
        Debug.Log("GameManager: Turn ended");
    }
    
    /// <summary>
    /// Adds food to the player's resources
    /// </summary>
    public void AddFood(int amount)
    {
        int oldFood = currentFood;
        currentFood -= amount;
        Debug.Log($"Added {amount} food. Total: {currentFood}");
        
        
    }
    
    /// <summary>
    /// Consumes food from the player's resources
    /// </summary>
    public bool ConsumeFood(int amount)
    {
        if (currentFood <= amount)
        {
            int oldFood = currentFood;
            currentFood -= amount;
            Debug.Log($"Consumed {amount} food. Remaining: {currentFood}");
            
           
        }
        
        Debug.LogWarning($"Not enough food to consume {amount}. Current: {currentFood}");
        return false;
    }
    
    /// <summary>
    /// Gets the current amount of food
    /// </summary>
    public int GetCurrentFood()
    {
        return currentFood;
    }
    
    /// <summary>
    /// Sets the current amount of food
    /// </summary>
    public void SetCurrentFood(int amount)
    {
        int oldFood = currentFood;
        currentFood = Mathf.Max(0, amount);
        
        Debug.Log($"GameManager: Current food set to {currentFood}");
    }
    
    /// <summary>
    /// Checks if there's enough food for a certain action
    /// </summary>
    public bool HasEnoughFood(int amount)
    {
        return currentFood >= amount;
    }
    
    public void RestartGame()
    {
        Debug.Log("GameManager: Restarting game...");
        
        // Reset food
        currentFood = startingFood;
        
        // Reset turn manager
        if (turnManager != null)
            turnManager.ResetTurns();
        
        // Respawn player
        if (playerController != null)
            playerController.Respawn();
        
        // Update UI
        if (foodLabel != null)
        {
            foodLabel.text = $"Food: {currentFood}";
            foodLabel.style.color = Color.white;
            foodLabel.style.fontSize = 20;
            // REMOVED: Background color reset
            // foodLabel.style.backgroundColor = new Color(0, 0, 0, 0.7f);
        }
        
        Debug.Log("Game restarted!");
    }
    
    void Update()
    {
        // Example: Restart game with R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
        
        // Example: Add food with F key (for testing)
        if (Input.GetKeyDown(KeyCode.F))
        {
            AddFood(50);
        }
        
        // Example: Consume food with C key (for testing)
        if (Input.GetKeyDown(KeyCode.C))
        {
            ConsumeFood(30);
        }
        
        // Example: Display current food with Space key (for debugging)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log($"Current food: {currentFood}");
        }
    }
}