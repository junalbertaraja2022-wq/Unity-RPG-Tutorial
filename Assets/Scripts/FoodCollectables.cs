// FoodCollectables.cs
using UnityEngine;

public class FoodCollectible : MonoBehaviour
{
    [Header("Food Settings")]
    public int foodValue = 10; // Amount of food this collectible gives
    public GameObject collectEffect; // Optional: Particle effect when collected
    public AudioClip collectSound; // Optional: Sound when collected
    
    [Header("Visual Settings")]
    public float rotationSpeed = 50f; // Rotation animation speed
    public float floatAmplitude = 0.2f; // Floating animation height
    public float floatSpeed = 2f; // Floating animation speed
    
    private Vector3 startPosition;
    private bool isCollected = false;
    
    void Start()
    {
        startPosition = transform.position;
        
        // Make sure we have a collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<CircleCollider2D>();
            (collider as CircleCollider2D).radius = 0.3f;
            collider.isTrigger = true;
        }
        
        // Make sure we have a sprite renderer
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = gameObject.AddComponent<SpriteRenderer>();
            // You can set a default sprite here or assign in inspector
        }
    }
    
    void Update()
    {
        if (isCollected) return;
        
        // Rotation animation
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        
        // Floating animation
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;
        
        // Check if player collected the food
        if (other.CompareTag("Player"))
        {
            Collect(other.gameObject);
        }
    }
    
    public void Collect(GameObject collector)
    {
        if (isCollected) return;
        
        isCollected = true;
        
        // Add food to GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddFood(foodValue);
            Debug.Log($"Collected food! +{foodValue} food");
        }
        
        // Play collection effect
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }
        
        // Play collection sound
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
        
        // Remove from board data
        RemoveFromBoardData();
        
        // Destroy the food object
        Destroy(gameObject);
    }
    
    void RemoveFromBoardData()
    {
        // Find BoardManager and remove this object from cell data
        BoardManager boardManager = FindFirstObjectByType<BoardManager>();
        if (boardManager != null)
        {
            Vector2Int cell = boardManager.WorldToCell(transform.position);
            boardManager.ClearCellObject(cell);
        }
    }
    
    // Optional: Visual feedback when player is near
    void OnTriggerStay2D(Collider2D other)
    {
        if (isCollected) return;
        
        if (other.CompareTag("Player"))
        {
            // Optional: Visual feedback (pulse effect, etc.)
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                float pulse = Mathf.Abs(Mathf.Sin(Time.time * 10f)) * 0.3f + 0.7f;
                renderer.color = new Color(1, 1, 1, pulse);
            }
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (isCollected) return;
        
        if (other.CompareTag("Player"))
        {
            // Reset visual feedback
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = Color.white;
            }
        }
    }
}