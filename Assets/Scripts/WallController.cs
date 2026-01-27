using UnityEngine;

public class WallController : MonoBehaviour
{
    [Header("Wall Settings")]
    public int maxHealth = 3;
    public int currentHealth;
    public int damagePerAttack = 1;
    
    [Header("Visual Settings")]
    public GameObject damageEffectPrefab;
    public GameObject destroyEffectPrefab;
    public AudioClip damageSound;
    public AudioClip destroySound;
    
    [Header("Visual Feedback")]
    public Sprite[] damageSprites; // Sprites for different damage levels
    public Color damagedColor = new Color(0.8f, 0.6f, 0.6f, 1f);
    public float shakeIntensity = 0.1f;
    public float shakeDuration = 0.2f;
    
    private BoardManager boardManager;
    private Vector2Int gridPosition;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isDestroyed = false;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Ensure wall has a collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            _ = gameObject.AddComponent<BoxCollider2D>();
        }
        
        // Set tag for identification
        gameObject.tag = "Wall";
        
        // Initialize health
        currentHealth = maxHealth;
        
        Debug.Log($"Wall created at {gridPosition} with {currentHealth} health");
    }
    
    public void Initialize(BoardManager manager, Vector2Int position)
    {
        boardManager = manager;
        gridPosition = position;
    }
    
    public void TakeDamage(int damage)
    {
        if (isDestroyed) return;
        
        currentHealth -= damage;
        
        Debug.Log($"Wall at {gridPosition} took {damage} damage. Health: {currentHealth}/{maxHealth}");
        
        // Visual feedback
        StartCoroutine(ShakeEffect());
        ChangeAppearanceBasedOnHealth();
        
        // Play damage effect
        if (damageEffectPrefab != null)
        {
            Instantiate(damageEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Play sound
        if (damageSound != null)
        {
            AudioSource.PlayClipAtPoint(damageSound, transform.position);
        }
        
        // Check if destroyed
        if (currentHealth <= 0)
        {
            DestroyWall();
        }
    }
    
    void ChangeAppearanceBasedOnHealth()
    {
        if (spriteRenderer == null) return;
        
        // Change color based on health percentage
        float healthPercentage = (float)currentHealth / maxHealth;
        spriteRenderer.color = Color.Lerp(damagedColor, originalColor, healthPercentage);
        
        // Change sprite if damage sprites are provided
        if (damageSprites != null && damageSprites.Length > 0)
        {
            int spriteIndex = Mathf.Clamp(maxHealth - currentHealth, 0, damageSprites.Length - 1);
            spriteRenderer.sprite = damageSprites[spriteIndex];
        }
    }
    
    System.Collections.IEnumerator ShakeEffect()
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            float x = originalPos.x + Random.Range(-shakeIntensity, shakeIntensity);
            float y = originalPos.y + Random.Range(-shakeIntensity, shakeIntensity);
            transform.position = new Vector3(x, y, originalPos.z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalPos;
    }
    
    void DestroyWall()
    {
        if (isDestroyed) return;
        
        isDestroyed = true;
        Debug.Log($"Wall destroyed at {gridPosition}");
        
        // Play destroy effect
        if (destroyEffectPrefab != null)
        {
            Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Play sound
        if (destroySound != null)
        {
            AudioSource.PlayClipAtPoint(destroySound, transform.position);
        }
        
        // Notify board manager to make cell passable
        if (boardManager != null)
        {
            boardManager.DestroyWallAtCell(gridPosition);
        }
        
        // Destroy the wall object
        Destroy(gameObject);
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Optional: Visual feedback when player is near
        if (other.CompareTag("Player") && spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.8f, 0.8f, 1f);
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        // Reset color when player leaves
        if (other.CompareTag("Player") && spriteRenderer != null)
        {
            ChangeAppearanceBasedOnHealth();
        }
    }
    
    // Public getters
    public Vector2Int GetGridPosition() => gridPosition;
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public bool IsDestroyed() => isDestroyed;
}