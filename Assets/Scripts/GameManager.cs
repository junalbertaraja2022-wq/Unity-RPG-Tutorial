// GameManager.cs
using UnityEngine;
using System.Collections.Generic;

public enum GamePhase
{
    PlayerPhase,
    EnemyPhase,
    AllyPhase,
    BattlePhase,
    MenuPhase
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game Settings")]
    public int maxPartySize = 10;
    public int maxTurnTime = 60;
    public bool permadeath = true;
    
    [Header("References")]
    public MapManager mapManager;
    public UIManager uiManager;
    public CameraController cameraController;
    public GameObject playerCharacterPrefab;
    
    [Header("Player Data")]
    public List<Character> playerCharacters = new List<Character>();
    public List<Character> enemyCharacters = new List<Character>();
    public List<Character> allyCharacters = new List<Character>();
    
    [Header("Game State")]
    public GamePhase currentPhase = GamePhase.PlayerPhase;
    public int currentTurn = 1;
    public Character selectedCharacter;
    public Vector2Int selectedTile;
    
    private Character currentActiveCharacter;
    private bool isGameOver = false;
    private bool isPaused = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        InitializeGame();
        StartPlayerPhase();

         // Auto-find if not assigned
        if (mapManager == null) mapManager = FindFirstObjectByType<MapManager>();
        if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
        if (cameraController == null) cameraController = FindFirstObjectByType<CameraController>();
    
    InitializeGame();
    }
    
    void InitializeGame()
    {
        // Load map
        mapManager.GenerateMap();
        
        // Spawn characters
        SpawnCharacters();
        
        // Initialize UI
        uiManager.InitializeUI();

        // Set up camera
        cameraController.SetBounds(mapManager.GetMapBounds());
        
       
    }
    
    void SpawnCharacters()
    {
        // Example spawn positions
        playerCharacters = CreatePlayerCharacters();
        enemyCharacters = CreateEnemyUnits();
        
        // Place on map
        PlaceCharactersOnMap();
    }

    List<Character> CreatePlayerCharacters()
    {
        List<Character> players = new List<Character>();
        players.Add(CreatePlayerCharacter("Eirika", CharacterClass.Lord));
        players.Add(CreatePlayerCharacter("Seth", CharacterClass.Mercenary));
        players.Add(CreatePlayerCharacter("Vanessa", CharacterClass.Archer));
        return players;
    }

    Character CreatePlayerCharacter(string name, CharacterClass characterClass)
    {
        GameObject playerObj = Instantiate(playerCharacterPrefab);
        Character character = playerObj.GetComponent<Character>();
        character.characterName = name;
        character.characterClass = characterClass;

        // Assign weapon based on class
        switch (characterClass)
        {
            case CharacterClass.Lord:
            case CharacterClass.Mercenary:
                character.equippedWeapon = GetWeaponFromDatabase("iron_sword");
                break;

            case CharacterClass.Mage:
                character.equippedWeapon = GetWeaponFromDatabase("fire_tome");
                break;

            case CharacterClass.Archer:
                character.equippedWeapon = GetWeaponFromDatabase("iron_bow");
                break;

            default:
                character.equippedWeapon = GetWeaponFromDatabase("iron_sword");
                break;
        }

        return character;
    }

        WeaponData GetWeaponFromDatabase(string weaponID)
    {
        if (WeaponDatabase.Instance != null)
        {
            return WeaponDatabase.Instance.GetWeapon(weaponID);
        }
        
        // Fallback: Load from Resources
        return Resources.Load<WeaponData>($"Weapons/{weaponID}");
    }


    List<Character> CreateEnemyUnits()
    {
        List<Character> enemies = new List<Character>();
        
        for (int i = 0; i < 5; i++)
        {
            GameObject enemyObj = new GameObject($"Enemy {i+1}");
            Character soldier = enemyObj.AddComponent<Character>();
            soldier.characterName = $"Enemy {i+1}";
            soldier.characterClass = CharacterClass.Soldier;
            soldier.SetStats(new CharacterStats(22, 10, 3, 7, 4, 5, 9, 2));
            soldier.equippedWeapon = GetWeaponFromDatabase("iron_lance");
            enemies.Add(soldier);
        }
        
        return enemies;
    }
    
    void PlaceCharactersOnMap()
    {
        // Place player characters
        for (int i = 0; i < playerCharacters.Count; i++)
        {
            Vector2Int position = new Vector2Int(2 + i, 2);
            mapManager.PlaceCharacter(playerCharacters[i], position, Team.Player);
        }
        
        // Place enemy characters
        for (int i = 0; i < enemyCharacters.Count; i++)
        {
            Vector2Int position = new Vector2Int(8 + i, 8);
            mapManager.PlaceCharacter(enemyCharacters[i], position, Team.Enemy);
        }
    }
    
    void StartPlayerPhase()
    {
        currentPhase = GamePhase.PlayerPhase;
        uiManager.ShowPhaseText("Player Phase");
        
        // Reset all player characters' actions
        foreach (Character character in playerCharacters)
        {
            character.ResetActions();
        }
        
        // Select first character
        SelectCharacter(playerCharacters[0]);
    }
    
    void StartEnemyPhase()
    {
        currentPhase = GamePhase.EnemyPhase;
        uiManager.ShowPhaseText("Enemy Phase");
        
        StartCoroutine(ExecuteEnemyTurns());
    }
    
    System.Collections.IEnumerator ExecuteEnemyTurns()
    {
        foreach (Character enemy in enemyCharacters)
        {
            if (enemy.IsAlive())
            {
                currentActiveCharacter = enemy;
                cameraController.FocusOnCharacter(enemy);
                
                // AI decision making
                yield return StartCoroutine(AIManager.MakeDecision(enemy));
                
                yield return new WaitForSeconds(0.5f);
            }
        }
        
        EndEnemyPhase();
    }
    
    void EndEnemyPhase()
    {
        currentTurn++;
        StartPlayerPhase();
    }
    
    public void SelectCharacter(Character character)
    {
        if (selectedCharacter != null)
        {
            selectedCharacter.Deselect();
        }
        
        selectedCharacter = character;
        selectedCharacter.Select();
        
        // Show movement range
        mapManager.ShowMovementRange(character);
        
        // Update UI
        uiManager.UpdateCharacterInfo(character);
        
        // Move camera
        cameraController.FocusOnCharacter(character);
    }
    
    public void SelectTile(Vector2Int tilePosition)
    {
        selectedTile = tilePosition;
        
        if (selectedCharacter != null && 
            selectedCharacter.CanMoveTo(tilePosition) && 
            !selectedCharacter.hasMoved)
        {
            // Move character
            mapManager.MoveCharacter(selectedCharacter, tilePosition);
            selectedCharacter.hasMoved = true;
            
            // Show attack range
            mapManager.ShowAttackRange(selectedCharacter);
        }
        else if (selectedCharacter != null && 
                 selectedCharacter.CanAttackAt(tilePosition) && 
                 !selectedCharacter.hasAttacked)
        {
            // Check if enemy is at tile
            Character target = mapManager.GetCharacterAt(tilePosition);
            if (target != null && target.team != selectedCharacter.team)
            {
                StartBattle(selectedCharacter, target);
            }
        }
    }
    
    public void StartBattle(Character attacker, Character defender)
    {
        currentPhase = GamePhase.BattlePhase;
        
        // Calculate battle
        BattleResult result = CombatSystem.CalculateBattle(attacker, defender);
        
        // Show battle animation
        uiManager.ShowBattleAnimation(attacker, defender, result);
        
        // Apply results
        ApplyBattleResult(result);
        
        // Check if defender died
        if (!defender.IsAlive())
        {
            mapManager.RemoveCharacter(defender);
            
            if (defender.team == Team.Enemy)
            {
                enemyCharacters.Remove(defender);
            }
        }
        
        attacker.hasAttacked = true;
        
        // Return to appropriate phase
        currentPhase = GamePhase.PlayerPhase;
    }
    
    void ApplyBattleResult(BattleResult result)
    {
        result.attacker.currentHP -= result.damageToAttacker;
        result.defender.currentHP -= result.damageToDefender;
        
        // Gain experience
        if (result.damageToDefender > 0)
        {
            result.attacker.GainExperience(10);
        }
    }
    
    public void EndCharacterTurn()
    {
        selectedCharacter.EndTurn();
        
        // Find next character that can act
        Character nextCharacter = FindNextActiveCharacter();
        
        if (nextCharacter != null)
        {
            SelectCharacter(nextCharacter);
        }
        else
        {
            // All characters have acted, end phase
            StartEnemyPhase();
        }
    }
    
    Character FindNextActiveCharacter()
    {
        foreach (Character character in playerCharacters)
        {
            if (character.IsAlive() && !character.hasActed)
            {
                return character;
            }
        }
        return null;
    }
    
    public bool CheckGameOver()
    {
        // Check if all player characters are dead
        bool allPlayersDead = true;
        foreach (Character character in playerCharacters)
        {
            if (character.IsAlive())
            {
                allPlayersDead = false;
                break;
            }
        }
        
        // Check if all enemies are dead
        bool allEnemiesDead = true;
        foreach (Character character in enemyCharacters)
        {
            if (character.IsAlive())
            {
                allEnemiesDead = false;
                break;
            }
        }
        
        if (allPlayersDead)
        {
            GameOver(false);
            return true;
        }
        else if (allEnemiesDead)
        {
            GameOver(true);
            return true;
        }
        
        return false;
    }

    void GameOver(bool victory)
    {
        isGameOver = true;
        uiManager.ShowGameOverScreen(victory);
    }

    // Add these missing methods
    public void RestartGame()
    {
        Debug.Log("Restarting game...");

        // Reset game state
        isGameOver = false;
        currentTurn = 1;
        currentPhase = GamePhase.PlayerPhase;

        // Clear characters
        playerCharacters.Clear();
        enemyCharacters.Clear();
        allyCharacters.Clear();

        // Destroy existing character objects
        foreach (Character character in FindObjectsByType<Character>(FindObjectsSortMode.None))
        {
            Destroy(character.gameObject);
        }

        // Reinitialize game
        InitializeGame();
        StartPlayerPhase();
    }
    
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}