using UnityEngine;

public class WeaponTest : MonoBehaviour
{
    public Character testCharacter;
    
    void Start()
    {
        if (testCharacter != null && testCharacter.equippedWeapon != null)
        {
            Debug.Log($"Character: {testCharacter.characterName}");
            Debug.Log($"Weapon: {testCharacter.equippedWeapon.weaponName}");
            Debug.Log($"Might: {testCharacter.equippedWeapon.might}");
            Debug.Log($"Hit: {testCharacter.equippedWeapon.hit}%");
            Debug.Log($"Range: {testCharacter.equippedWeapon.range}");
            Debug.Log($"Uses: {testCharacter.equippedWeapon.currentUses}/{testCharacter.equippedWeapon.maxUses}");
            
            // Test weapon use
            bool used = testCharacter.equippedWeapon.Use();
            Debug.Log($"Weapon used: {used}. Remaining: {testCharacter.equippedWeapon.currentUses}");
        }
        else
        {
            Debug.LogError("Character or weapon not assigned!");
        }
    }
}