using UnityEngine;

public struct BattleResult
{
    public Character attacker;
    public Character defender;
    public int damageToAttacker;
    public int damageToDefender;
    public bool attackerHit;
    public bool defenderHit;
    public bool attackerCritical;
    public bool defenderCritical;
    public bool defenderCountered;
}

public static class CombatSystem
{
    public static BattleResult CalculateBattle(Character attacker, Character defender)
    {
        BattleResult result = new BattleResult();
        result.attacker = attacker;
        result.defender = defender;
        
        // Attacker's attack
        result.attackerHit = CalculateHit(attacker, defender);
        result.attackerCritical = CalculateCritical(attacker, defender);
        
        if (result.attackerHit)
        {
            result.damageToDefender = CalculateDamage(attacker, defender, result.attackerCritical);
        }
        
        // Check if defender can counterattack
        if (CanCounterattack(attacker, defender))
        {
            result.defenderCountered = true;
            result.defenderHit = CalculateHit(defender, attacker);
            result.defenderCritical = CalculateCritical(defender, attacker);
            
            if (result.defenderHit)
            {
                result.damageToAttacker = CalculateDamage(defender, attacker, result.defenderCritical);
            }
        }
        
        // Check for double attack (speed difference)
        if (CanDoubleAttack(attacker, defender))
        {
            // Second attack
            if (CalculateHit(attacker, defender))
            {
                bool secondCritical = CalculateCritical(attacker, defender);
                result.damageToDefender += CalculateDamage(attacker, defender, secondCritical);
            }
        }
        
        return result;
    }
    
    static bool CalculateHit(Character attacker, Character defender)
    {
        if (attacker.equippedWeapon == null) return false;
        
        MapTile defenderTile = MapManager.Instance.GetTile(defender.gridPosition);
        
        int hitRate = attacker.baseStats.skill * 2 + 
                     attacker.baseStats.luck / 2 + 
                     attacker.equippedWeapon.hit;
        
        int avoid = defender.baseStats.speed * 2 + 
                   defender.baseStats.luck / 2 +
                   (defenderTile?.avoidBonus ?? 0);
        
        int finalHitRate = Mathf.Clamp(hitRate - avoid, 0, 100);
        
        return Random.Range(0, 100) < finalHitRate;
    }
    
    static bool CalculateCritical(Character attacker, Character defender)
    {
        if (attacker.equippedWeapon == null) return false;
        
        int critRate = attacker.baseStats.skill / 2 + 
                      attacker.equippedWeapon.crit -
                      (defender.baseStats.luck / 2);
        
        critRate = Mathf.Clamp(critRate, 0, 100);
        
        return Random.Range(0, 100) < critRate;
    }
    
    static int CalculateDamage(Character attacker, Character defender, bool isCritical)
    {
        if (attacker.equippedWeapon == null) return 0;
        
        MapTile defenderTile = MapManager.Instance.GetTile(defender.gridPosition);
        
        int attackPower = attacker.equippedWeapon.isMagic ? 
                         attacker.baseStats.magic : 
                         attacker.baseStats.strength;
        
        attackPower += attacker.equippedWeapon.might;
        
        int defense = attacker.equippedWeapon.isMagic ?
                     defender.baseStats.resistance :
                     defender.baseStats.defense;
        
        defense += (defenderTile?.defenseBonus ?? 0);
        
        int damage = Mathf.Max(0, attackPower - defense);
        
        if (isCritical)
        {
            damage *= 3;
        }
        
        // Apply weapon triangle advantage
        damage = ApplyWeaponTriangle(attacker.equippedWeapon.weaponType, 
                                    defender.equippedWeapon?.weaponType ?? WeaponType.None, 
                                    damage);
        
        return damage;
    }
    
    static bool CanCounterattack(Character attacker, Character defender)
    {
        if (defender.equippedWeapon == null) return false;
        
        // Check range
        int distance = Mathf.Abs(attacker.gridPosition.x - defender.gridPosition.x) +
                      Mathf.Abs(attacker.gridPosition.y - defender.gridPosition.y);
        
        if (distance > defender.GetAttackRange()) return false;
        
        // Some weapons can't counterattack (like certain magic tomes)
        return defender.equippedWeapon.canCounterattack;
    }
    
    static bool CanDoubleAttack(Character attacker, Character defender)
    {
        int speedDifference = attacker.baseStats.speed - defender.baseStats.speed;
        return speedDifference >= 4;
    }
    
    static int ApplyWeaponTriangle(WeaponType attackerType, WeaponType defenderType, int damage)
    {
        // Sword > Axe > Lance > Sword
        // Anima > Light > Dark > Anima
        
        if (HasAdvantage(attackerType, defenderType))
        {
            damage = Mathf.FloorToInt(damage * 1.2f); // +20% damage
        }
        else if (HasDisadvantage(attackerType, defenderType))
        {
            damage = Mathf.FloorToInt(damage * 0.8f); // -20% damage
        }
        
        return damage;
    }

   public  static bool HasAdvantage(WeaponType attacker, WeaponType defender)
    {
        return (attacker == WeaponType.Sword && defender == WeaponType.Axe) ||
               (attacker == WeaponType.Axe && defender == WeaponType.Lance) ||
               (attacker == WeaponType.Lance && defender == WeaponType.Sword) ||
               (attacker == WeaponType.Anima && defender == WeaponType.Light) ||
               (attacker == WeaponType.Light && defender == WeaponType.Dark) ||
               (attacker == WeaponType.Dark && defender == WeaponType.Anima);
    }

   public static bool HasDisadvantage(WeaponType attacker, WeaponType defender)
    {
        return (attacker == WeaponType.Sword && defender == WeaponType.Lance) ||
               (attacker == WeaponType.Axe && defender == WeaponType.Sword) ||
               (attacker == WeaponType.Lance && defender == WeaponType.Axe) ||
               (attacker == WeaponType.Anima && defender == WeaponType.Dark) ||
               (attacker == WeaponType.Light && defender == WeaponType.Anima) ||
               (attacker == WeaponType.Dark && defender == WeaponType.Light);
    }
}