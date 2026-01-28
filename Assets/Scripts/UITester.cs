using UnityEngine;

public class UITester : MonoBehaviour
{
    public UIManager uiManager;
    public Character testCharacter;
    
    void Start()
    {
        if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
        
        // Test character info
        if (testCharacter != null && uiManager != null)
        {
            uiManager.UpdateCharacterInfo(testCharacter);
        }
        
        // Test phase display
        uiManager.ShowPhaseText("Test Phase");
        
        // Test game over (after delay)
        Invoke(nameof(TestGameOver), 3f);
    }
    
    void TestGameOver()
    {
        uiManager.ShowGameOverScreen(true); // Victory
    }
}