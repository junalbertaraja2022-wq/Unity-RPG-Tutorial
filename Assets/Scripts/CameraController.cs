using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public float moveSpeed = 10f;
    public float zoomSpeed = 5f;
    public float minZoom = 5f;
    public float maxZoom = 20f;
    public float focusSpeed = 5f;
    
    [Header("Bounds")]
    public Bounds bounds;
    public bool useBounds = true;
    
    private Camera mainCamera;
    private Vector3 dragOrigin;
    private bool isDragging = false;
    
    void Start()
    {
        mainCamera = GetComponent<Camera>();
    }
    
    void Update()
    {
        HandleMouseInput();
        HandleKeyboardInput();
        HandleZoom();
        ClampCameraPosition();
    }
    
    void HandleMouseInput()
    {
        // Right click drag
        if (Input.GetMouseButtonDown(1))
        {
            dragOrigin = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            isDragging = true;
        }
        
        if (Input.GetMouseButton(1) && isDragging)
        {
            Vector3 difference = dragOrigin - mainCamera.ScreenToWorldPoint(Input.mousePosition);
            transform.position += difference;
        }
        
        if (Input.GetMouseButtonUp(1))
        {
            isDragging = false;
        }
        
        // Middle click to focus on character
        if (Input.GetMouseButtonDown(2))
        {
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int gridPos = MapManager.Instance.WorldToGrid(mousePos);
            
            Character character = MapManager.Instance.GetCharacterAt(gridPos);
            if (character != null)
            {
                FocusOnCharacter(character);
            }
        }
    }
    
    void HandleKeyboardInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        Vector3 movement = new Vector3(horizontal, vertical, 0) * moveSpeed * Time.deltaTime;
        transform.position += movement;
    }
    
    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (scroll != 0)
        {
            float newSize = mainCamera.orthographicSize - scroll * zoomSpeed;
            mainCamera.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }
    }
    
    void ClampCameraPosition()
    {
        if (!useBounds) return;
        
        float vertExtent = mainCamera.orthographicSize;
        float horzExtent = vertExtent * Screen.width / Screen.height;
        
        float leftBound = bounds.min.x + horzExtent;
        float rightBound = bounds.max.x - horzExtent;
        float bottomBound = bounds.min.y + vertExtent;
        float topBound = bounds.max.y - vertExtent;
        
        float clampedX = Mathf.Clamp(transform.position.x, leftBound, rightBound);
        float clampedY = Mathf.Clamp(transform.position.y, bottomBound, topBound);
        
        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }
    
    public void FocusOnCharacter(Character character)
    {
        Vector3 targetPosition = character.transform.position;
        targetPosition.z = transform.position.z;
        
        StartCoroutine(SmoothFocus(targetPosition));
    }
    
    System.Collections.IEnumerator SmoothFocus(Vector3 targetPosition)
    {
        float elapsed = 0f;
        Vector3 startPosition = transform.position;
        
        while (elapsed < 1f)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / 1f);
            elapsed += Time.deltaTime * focusSpeed;
            yield return null;
        }
        
        transform.position = targetPosition;
    }
    
    public void SetBounds(Bounds newBounds)
    {
        bounds = newBounds;
        useBounds = true;
    }
}