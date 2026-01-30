using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public MapGenerator mapGenerator;
    public PlayerController player;
    public Camera mainCamera;
    
    [Header("Settings")]
    public bool autoGenerateOnStart = true;
    
    void Start()
    {
        // Initialize references if not set
        if (mapGenerator == null)
            mapGenerator = FindObjectOfType<MapGenerator>();
            
        if (player == null)
            player = FindObjectOfType<PlayerController>();
            
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        // Generate map if needed
        if (autoGenerateOnStart && mapGenerator != null)
        {
            GenerateNewMap();
        }
        
        // Position camera
        if (mainCamera != null && mapGenerator != null)
        {
            PositionCamera();
        }
    }
    
    [ContextMenu("Generate New Map")]
    public void GenerateNewMap()
    {
        if (mapGenerator == null) return;
        
        // Generate the map
        mapGenerator.GenerateMap();
        
       
    }
    
   
    
    void PositionCamera()
    {
        if (mainCamera == null || mapGenerator == null) return;
        
        // Center camera on map
        float centerX = mapGenerator.mapWidth / 2f;
        float centerY = mapGenerator.mapHeight / 2f;
        
        mainCamera.transform.position = new Vector3(centerX, centerY, -10);
        
        // Adjust camera size to fit map (for orthographic camera)
        if (mainCamera.orthographic)
        {
            float aspectRatio = (float)Screen.width / Screen.height;
            float mapWidth = mapGenerator.mapWidth;
            float mapHeight = mapGenerator.mapHeight;
            
            // Calculate required orthographic size
            float sizeBasedOnWidth = (mapWidth / aspectRatio) / 2f;
            float sizeBasedOnHeight = mapHeight / 2f;
            
            mainCamera.orthographicSize = Mathf.Max(sizeBasedOnWidth, sizeBasedOnHeight) * 1.1f; // 10% padding
        }
    }
    
    void Update()
    {
        // Regenerate map on R key press
        if (Input.GetKeyDown(KeyCode.R))
        {
            GenerateNewMap();
        }
    }
}