using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class AIManager
{
    public static IEnumerator MakeDecision(Character enemy)
    {
        yield return new WaitForSeconds(0.5f);
        
        // Get potential targets
        List<Character> targets = GetPotentialTargets(enemy);
        
        if (targets.Count > 0)
        {
            // Find best target
            Character bestTarget = FindBestTarget(enemy, targets);
            
            if (bestTarget != null)
            {
                // Try to attack
                if (CanAttack(enemy, bestTarget))
                {
                    yield return AttackTarget(enemy, bestTarget);
                }
                else
                {
                    // Move towards target
                    yield return MoveTowardsTarget(enemy, bestTarget);
                    
                    // Check if can attack after moving
                    if (CanAttack(enemy, bestTarget))
                    {
                        yield return AttackTarget(enemy, bestTarget);
                    }
                }
            }
        }
        
        // End turn
        enemy.EndTurn();
    }
    
    static List<Character> GetPotentialTargets(Character enemy)
    {
        List<Character> targets = new List<Character>();
        
        // Find all player characters
        foreach (Character character in GameManager.Instance.playerCharacters)
        {
            if (character.IsAlive())
            {
                targets.Add(character);
            }
        }
        
        return targets;
    }
    
    static Character FindBestTarget(Character enemy, List<Character> targets)
    {
        Character bestTarget = null;
        float bestScore = float.MinValue;
        
        foreach (Character target in targets)
        {
            float score = CalculateTargetScore(enemy, target);
            
            if (score > bestScore)
            {
                bestScore = score;
                bestTarget = target;
            }
        }
        
        return bestTarget;
    }
    
    static float CalculateTargetScore(Character enemy, Character target)
    {
        float score = 0f;
        
        // Distance (prefer closer targets)
        int distance = Mathf.Abs(enemy.gridPosition.x - target.gridPosition.x) +
                      Mathf.Abs(enemy.gridPosition.y - target.gridPosition.y);
        score -= distance * 0.5f;
        
        // Damage potential
        int potentialDamage = enemy.CalculateDamage(target);
        score += potentialDamage;
        
        // Target health (prefer weaker targets)
        float healthPercent = (float)target.currentHP / target.baseStats.maxHP;
        score += (1 - healthPercent) * 20;
        
        // Weapon triangle advantage
        if (enemy.equippedWeapon != null && target.equippedWeapon != null)
        {
            if (CombatSystem.HasAdvantage(enemy.equippedWeapon.weaponType, 
                                         target.equippedWeapon.weaponType))
            {
                score += 10;
            }
            else if (CombatSystem.HasDisadvantage(enemy.equippedWeapon.weaponType, 
                                                 target.equippedWeapon.weaponType))
            {
                score -= 10;
            }
        }
        
        return score;
    }
    
    static bool CanAttack(Character enemy, Character target)
    {
        int distance = Mathf.Abs(enemy.gridPosition.x - target.gridPosition.x) +
                      Mathf.Abs(enemy.gridPosition.y - target.gridPosition.y);
        
        return distance <= enemy.GetAttackRange();
    }
    
    static IEnumerator AttackTarget(Character enemy, Character target)
    {
        // Start battle
        GameManager.Instance.StartBattle(enemy, target);
        yield return new WaitForSeconds(1.5f);
    }
    
    static IEnumerator MoveTowardsTarget(Character enemy, Character target)
    {
        // Get reachable tiles
        HashSet<Vector2Int> reachableTiles = MapManager.Instance.GetReachableTiles(enemy);
        
        // Find tile closest to target that's in attack range
        Vector2Int bestTile = enemy.gridPosition;
        int bestDistance = int.MaxValue;
        
        foreach (Vector2Int tile in reachableTiles)
        {
            int distanceToTarget = Mathf.Abs(tile.x - target.gridPosition.x) +
                                  Mathf.Abs(tile.y - target.gridPosition.y);
            
            if (distanceToTarget < bestDistance)
            {
                bestDistance = distanceToTarget;
                bestTile = tile;
            }
        }
        
        // Move to best tile
        if (bestTile != enemy.gridPosition)
        {
            MapManager.Instance.MoveCharacter(enemy, bestTile);
            yield return new WaitForSeconds(0.5f);
        }
    }
}