using UnityEngine;

public enum ItemType
{
    Weapon,
    Consumable,
    Accessory,
    KeyItem
}

[System.Serializable]
public class Item
{
    public string itemName;
    public ItemType itemType;
    public string description;
    public Sprite icon;
    public int value;
    public bool isConsumable = true;
    
    // For weapons
    public WeaponData weaponData;
    
    // For consumables
    public int healAmount;
    public bool canRevive;
    public int statBonus; // For stat-boosting items
    
    public Item(string name, ItemType type, string desc)
    {
        this.itemName = name;
        this.itemType = type;
        this.description = desc;
    }
    
    public virtual void Use(Character target)
    {
        switch (itemType)
        {
            case ItemType.Consumable:
                if (healAmount > 0)
                {
                    target.currentHP = Mathf.Min(target.currentHP + healAmount, target.baseStats.maxHP);
                }
                break;
                
            case ItemType.Weapon:
                if (weaponData != null)
                {
                    target.equippedWeapon = weaponData;
                }
                break;
        }
    }
}