using UnityEngine;
using System.Collections.Generic;

public enum CharacterClass
{
    Lord,
    Cavalier,
    Knight,
    Archer,
    Mage,
    Cleric,
    Thief,
    Mercenary,
    Soldier,
    PegasusKnight,
    WyvernRider
}

public enum Team
{
    Player,
    Enemy,
    Ally
}

[System.Serializable]
public struct CharacterStats
{
    public int maxHP;
    public int strength;
    public int magic;
    public int skill;
    public int speed;
    public int luck;
    public int defense;
    public int resistance;
    
    public CharacterStats(int hp, int str, int mag, int skl, int spd, int lck, int def, int res)
    {
        this.maxHP = hp;
        this.strength = str;
        this.magic = mag;
        this.skill = skl;
        this.speed = spd;
        this.luck = lck;
        this.defense = def;
        this.resistance = res;
    }
}

public class Character : MonoBehaviour
{
    [Header("Character Info")]
    public string characterName;
    public CharacterClass characterClass;
    public int level = 1;
    public int experience = 0;
    public Team team;
    
    [Header("Stats")]
    public CharacterStats baseStats;
    public CharacterStats growthRates;
    public int currentHP;
    
    [Header("Equipment")]
    public WeaponData equippedWeapon;
    public Item[] inventory = new Item[4];
    
    [Header("Position & Movement")]
    public Vector2Int gridPosition;
    public int movementRange = 5;
    
    [Header("Battle Status")]
    public bool hasMoved = false;
    public bool hasAttacked = false;
    public bool hasActed = false;
    public bool isSelected = false;
    
    [Header("Visuals")]
    public Sprite portrait;
    public Sprite battleSprite;
    public Animator animator;
    
    // Skills and abilities
    private List<Skill> skills = new List<Skill>();
    
    void Start()
    {
        currentHP = baseStats.maxHP;

        // Initialize weapon if assigned
        if (equippedWeapon != null)
        {
            // Use explicit namespace if WeaponData is namespaced, e.g. MyGame.Weapons.WeaponData
            equippedWeapon.Initialize();
        }
    }
    
    public void SetStats(CharacterStats stats)
    {
        baseStats = stats;
        currentHP = stats.maxHP;
    }
    
    public int GetMovementRange()
    {
        int range = movementRange;
        
        // Apply terrain effects
        if (MapManager.Instance != null)
        {
            MapTile tile = MapManager.Instance.GetTile(gridPosition);
            if (tile != null)
            {
                range += tile.movementCostModifier;
            }
        }
        
        return Mathf.Max(1, range);
    }
    
    public int GetAttackRange()
    {
        if (equippedWeapon != null)
        {
            // Use explicit namespace if WeaponData is namespaced, e.g. MyGame.Weapons.WeaponData
            return equippedWeapon.range;
        }
        return 1;
    }
    
    public bool CanMoveTo(Vector2Int targetPosition)
    {
        if (hasMoved) return false;
        
        // Check distance
        int distance = Mathf.Abs(targetPosition.x - gridPosition.x) + 
                      Mathf.Abs(targetPosition.y - gridPosition.y);
        
        return distance <= GetMovementRange();
    }
    
    public bool CanAttackAt(Vector2Int targetPosition)
    {
        if (hasAttacked) return false;
        
        int distance = Mathf.Abs(targetPosition.x - gridPosition.x) + 
                      Mathf.Abs(targetPosition.y - gridPosition.y);
        
        return distance <= GetAttackRange();
    }
    
    public int CalculateDamage(Character target)
    {
        if (equippedWeapon == null || equippedWeapon.IsBroken()) return 0;
        
        int attackPower = 0;
        int defense = 0;
        
        if (equippedWeapon.isMagic)
        {
            attackPower = baseStats.magic + equippedWeapon.might;
            defense = target.baseStats.resistance;
        }
        else
        {
            attackPower = baseStats.strength + equippedWeapon.might;
            defense = target.baseStats.defense;
        }
        
        // Calculate hit rate
        int hitRate = baseStats.skill * 2 + baseStats.luck / 2 + equippedWeapon.hit;
        int avoid = target.baseStats.speed * 2 + target.baseStats.luck / 2;
        int finalHitRate = Mathf.Clamp(hitRate - avoid, 0, 100);
        
        // Check if attack hits
        bool hits = Random.Range(0, 100) < finalHitRate;
        
        if (!hits) return 0;
        
        // Calculate damage
        int damage = Mathf.Max(0, attackPower - defense);
        
        // Check for critical hit
        int critRate = baseStats.skill / 2 + equippedWeapon.crit;
        bool isCritical = Random.Range(0, 100) < critRate;

        if (isCritical)
        {
            damage *= 3;
        }
        
        equippedWeapon.Use();
        
        return damage;
    }
    
    public void GainExperience(int exp)
    {
        experience += exp;
        
        while (experience >= 100)
        {
            experience -= 100;
            LevelUp();
        }
    }
    
    void LevelUp()
    {
        level++;
        
        // Increase stats based on growth rates
        IncreaseStatByGrowth(ref baseStats.maxHP, growthRates.maxHP);
        IncreaseStatByGrowth(ref baseStats.strength, growthRates.strength);
        IncreaseStatByGrowth(ref baseStats.magic, growthRates.magic);
        IncreaseStatByGrowth(ref baseStats.skill, growthRates.skill);
        IncreaseStatByGrowth(ref baseStats.speed, growthRates.speed);
        IncreaseStatByGrowth(ref baseStats.luck, growthRates.luck);
        IncreaseStatByGrowth(ref baseStats.defense, growthRates.defense);
        IncreaseStatByGrowth(ref baseStats.resistance, growthRates.resistance);
        
        // Heal some HP on level up
        currentHP += Mathf.CeilToInt(baseStats.maxHP * 0.3f);
        currentHP = Mathf.Min(currentHP, baseStats.maxHP);
        
        // Check for new skills
        CheckForNewSkills();
    }
    
    void IncreaseStatByGrowth(ref int stat, int growthRate)
    {
        if (Random.Range(0, 100) < growthRate)
        {
            stat++;
        }
    }
    
    void CheckForNewSkills()
    {
        // Add skills based on level and class
        if (level >= 5 && !HasSkill("Desperation"))
        {
            skills.Add(new Skill("Desperation", "When HP ≤ 50%, unit makes follow-up attack immediately"));
        }
        
        if (level >= 10 && !HasSkill("Vantage"))
        {
            skills.Add(new Skill("Vantage", "When HP ≤ 50%, unit attacks first when enemy initiates combat"));
        }
    }
    
    bool HasSkill(string skillName)
    {
        foreach (Skill skill in skills)
        {
            if (skill.name == skillName) return true;
        }
        return false;
    }
    
    public void Select()
    {
        isSelected = true;
        // Visual feedback
        if (animator != null)
        {
            animator.SetBool("Selected", true);
        }
    }
    
    public void Deselect()
    {
        isSelected = false;
        if (animator != null)
        {
            animator.SetBool("Selected", false);
        }
    }
    
    public void ResetActions()
    {
        hasMoved = false;
        hasAttacked = false;
        hasActed = false;
    }
    
    public void EndTurn()
    {
        hasActed = true;
        Deselect();
    }
    
    public bool IsAlive()
    {
        return currentHP > 0;
    }
    
    public void TakeDamage(int damage)
    {
        currentHP = Mathf.Max(0, currentHP - damage);
        
        if (currentHP <= 0)
        {
            Die();
        }
    }
    
    void Die()
    {
        // Death animation
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        
        // Remove from map
        MapManager.Instance?.RemoveCharacter(this);
        
        // Destroy after animation
        Destroy(gameObject, 2f);
    }
}

public class Skill
{
    public string name;
    public string description;
    public int activationRequirement; // HP threshold or other condition
    
    public Skill(string name, string description)
    {
        this.name = name;
        this.description = description;
    }
}