using UnityEngine;

public static class CharacterFactory
{
    public static Character CreateCharacter(CharacterClass characterClass, string name)
    {
        // Create a new GameObject
        GameObject characterObject = new GameObject(name);
        
        // Add Character component
        Character character = characterObject.AddComponent<Character>();
        character.characterName = name;
        character.characterClass = characterClass;
        
        // Set default stats based on class
        SetDefaultStats(character, characterClass);
        
        // Set growth rates based on class
        SetGrowthRates(character, characterClass);
        
        return character;
    }
    
    private static void SetDefaultStats(Character character, CharacterClass characterClass)
    {
        switch (characterClass)
        {
            case CharacterClass.Lord:
                character.baseStats = new CharacterStats(20, 8, 7, 8, 5, 6, 8, 7);
                character.movementRange = 5;
                break;
                
            case CharacterClass.Mage:
                character.baseStats = new CharacterStats(18, 4, 12, 5, 7, 6, 3, 8);
                character.movementRange = 5;
                break;
                
            case CharacterClass.Soldier:
                character.baseStats = new CharacterStats(22, 10, 3, 7, 4, 5, 9, 2);
                character.movementRange = 5;
                break;
                
            case CharacterClass.Archer:
                character.baseStats = new CharacterStats(19, 6, 4, 9, 6, 5, 6, 3);
                character.movementRange = 5;
                break;
                
            case CharacterClass.Knight:
                character.baseStats = new CharacterStats(24, 11, 2, 5, 3, 4, 12, 1);
                character.movementRange = 4;
                break;
                
            default:
                character.baseStats = new CharacterStats(20, 7, 5, 7, 5, 5, 7, 5);
                character.movementRange = 5;
                break;
        }
        
        character.currentHP = character.baseStats.maxHP;
    }
    
    private static void SetGrowthRates(Character character, CharacterClass characterClass)
    {
        switch (characterClass)
        {
            case CharacterClass.Lord:
                character.growthRates = new CharacterStats(80, 45, 35, 50, 40, 45, 40, 30);
                break;
                
            case CharacterClass.Mage:
                character.growthRates = new CharacterStats(60, 20, 50, 40, 45, 30, 15, 40);
                break;
                
            case CharacterClass.Soldier:
                character.growthRates = new CharacterStats(70, 50, 10, 45, 35, 30, 50, 15);
                break;
                
            default:
                character.growthRates = new CharacterStats(70, 40, 30, 40, 40, 40, 40, 25);
                break;
        }
    }
}