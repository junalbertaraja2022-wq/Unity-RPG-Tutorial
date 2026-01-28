using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MapTest : MonoBehaviour
{
    [Header("Weapon Info")]
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI weaponStatsText;
    public Image weaponIconImage;
    public Slider weaponDurabilitySlider;
    void Start()
    {
        MapManager mapManager = FindFirstObjectByType<MapManager>();
        if (mapManager != null)
        {
            mapManager.GenerateMap();
            Debug.Log("Map generated successfully!");
        }
        else
        {
            Debug.LogError("MapManager not found!");
        }
    }

    public void UpdateWeaponInfo(Character character)
    {
        if (character.equippedWeapon != null)
        {
            WeaponData weapon = character.equippedWeapon;
            
            weaponNameText.text = weapon.weaponName;
            weaponStatsText.text = $"Mt:{weapon.might} Hit:{weapon.hit}% Crt:{weapon.crit}% Rng:{weapon.range}";
            
            if (weaponIconImage != null && weapon.icon != null)
            {
                weaponIconImage.sprite = weapon.icon;
            }
            
            if (weaponDurabilitySlider != null)
            {
                weaponDurabilitySlider.value = weapon.GetDurabilityPercent();
            }
        }
    }
}