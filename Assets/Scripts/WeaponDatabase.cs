using UnityEngine;
using System.Collections.Generic;

public class WeaponDatabase : MonoBehaviour
{
    public static WeaponDatabase Instance { get; private set; }
    
    [System.Serializable]
    public class WeaponEntry
    {
        public string weaponID;
        public WeaponData weaponData;
    }
    
    public List<WeaponEntry> weapons = new List<WeaponEntry>();
    
    private Dictionary<string, WeaponData> weaponDictionary = new Dictionary<string, WeaponData>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void InitializeDatabase()
    {
        foreach (WeaponEntry entry in weapons)
        {
            if (!weaponDictionary.ContainsKey(entry.weaponID))
            {
                weaponDictionary.Add(entry.weaponID, entry.weaponData);
            }
        }
    }
    
    public WeaponData GetWeapon(string weaponID)
    {
        if (weaponDictionary.ContainsKey(weaponID))
        {
            return weaponDictionary[weaponID];
        }
        return null;
    }
    
    public List<WeaponData> GetWeaponsByType(WeaponType type)
    {
        List<WeaponData> result = new List<WeaponData>();
        foreach (WeaponEntry entry in weapons)
        {
            if (entry.weaponData.weaponType == type) // If ambiguity persists, use: entry.weaponData.<Namespace>.weaponType
            {
                result.Add(entry.weaponData);
            }
        }
        return result;
    }
}