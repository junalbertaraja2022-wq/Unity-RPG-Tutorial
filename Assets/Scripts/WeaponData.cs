using UnityEngine;

public enum WeaponType
{
    Sword,
    Lance,
    Axe,
    Bow,
    Tome,
    Anima,
    Light,
    Dark,
    Staff,
    Dragonstone,
    None
}

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Game/Weapon")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Info")]
    public string weaponName;
    public WeaponType weaponType;
    public Sprite icon;
    public string description;
    
    [Header("Combat Stats")]
    public int might = 5;
    public int hit = 80;
    public int crit = 0;
    public int range = 1;
    public int maxUses = 45;
    
    [Header("Properties")]
    public bool isMagic = false;
    public bool canCounterattack = true;
    public bool isBrave = false; // Attacks twice when initiating
    public bool isEffectiveAgainstArmor = false;
    public bool isEffectiveAgainstCavalry = false;
    public bool isEffectiveAgainstFliers = false;
    
    [Header("Weapon Triangle")]
    public bool hasWeaponTriangle = true;
    
    // Current uses (runtime only, not saved)
    [System.NonSerialized]
    public int currentUses;
    
    public void Initialize()
    {
        currentUses = maxUses;
    }
    
    public bool Use()
    {
        if (currentUses > 0)
        {
            currentUses--;
            return true;
        }
        return false;
    }
    
    public void Repair()
    {
        currentUses = maxUses;
    }
    
    public bool IsBroken()
    {
        return currentUses <= 0;
    }
    
    public float GetDurabilityPercent()
    {
        return (float)currentUses / maxUses;
    }
}