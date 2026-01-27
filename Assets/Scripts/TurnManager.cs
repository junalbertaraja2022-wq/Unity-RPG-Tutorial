// TurnManager.cs
using System;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [Header("Turn Settings")]
    public int currentTurn = 1;
    public int maxTurns = 100; // Optional: limit total turns
    public bool isPlayerTurn = true;
    
   [Header("Action Points")]
    public int maxActionPoints = 2; // Reduced from 3
    public int currentActionPoints = 3;
    
    // Events for turn changes
    public event Action OnPlayerTurnStart;
    public event Action OnEnemyTurnStart;
    public event Action OnTurnEnd;
    public event Action OnTick; // ADDED: Event for each tick
    
    // Singleton pattern for easy access
    public static TurnManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: keep across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        StartPlayerTurn();
    }
    
    // Call this to advance one "tick" or action
    public void Tick()
    {
        if (!isPlayerTurn) return;
        
        // Consume one action point
        currentActionPoints--;
        
        Debug.Log($"Tick! Action points remaining: {currentActionPoints}");
        
        // Invoke the OnTick event
        OnTick?.Invoke(); // ADDED: Invoke the tick event
        
        // If no action points left, end turn
        if (currentActionPoints <= 0)
        {
            EndPlayerTurn();
        }
        
        OnTurnEnd?.Invoke();
    }
    
    // Reset action points and start player's turn
    public void StartPlayerTurn()
    {
        isPlayerTurn = true;
        currentActionPoints = maxActionPoints;
        
        Debug.Log($"Player turn {currentTurn} started. Action points: {currentActionPoints}");
        OnPlayerTurnStart?.Invoke();
    }
    
    // End player's turn, start enemy turn
    public void EndPlayerTurn()
    {
        isPlayerTurn = false;
        Debug.Log($"Player turn {currentTurn} ended.");
        
        // Start enemy turn logic here
        StartEnemyTurn();
    }
    
    void StartEnemyTurn()
    {
        Debug.Log($"Enemy turn {currentTurn} started.");
        OnEnemyTurnStart?.Invoke();
        
        // Simulate enemy actions
        // You would call your enemy AI here
        
        // After enemy actions complete, start next player turn
        EndEnemyTurn();
    }
    
    void EndEnemyTurn()
    {
        currentTurn++;
        Debug.Log($"Enemy turn ended. Starting turn {currentTurn}");
        
        StartPlayerTurn();
    }
    
    // Check if player can take an action
    public bool CanTakeAction(int actionCost = 1)
    {
        return isPlayerTurn && currentActionPoints >= actionCost;
    }
    
    // Force end turn (for testing or special conditions)
    public void ForceEndTurn()
    {
        if (isPlayerTurn)
        {
            EndPlayerTurn();
        }
    }
    
    // Reset turn manager
    public void ResetTurns()
    {
        currentTurn = 1;
        currentActionPoints = maxActionPoints;
        isPlayerTurn = true;
        StartPlayerTurn();
    }
    
    // Optional: Method to subscribe to tick events
    public void SubscribeToTick(Action callback)
    {
        OnTick += callback;
    }
    
    // Optional: Method to unsubscribe from tick events
    public void UnsubscribeFromTick(Action callback)
    {
        OnTick -= callback;
    }
}