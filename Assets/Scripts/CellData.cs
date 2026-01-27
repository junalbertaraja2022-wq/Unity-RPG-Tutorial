using UnityEngine;

[System.Serializable]
public class CellData
{
    public bool Passable;
    public GameObject ContainedObject;
    
    public bool HasWall 
    { 
        get 
        {
            if (ContainedObject == null) return false;
            
            // Check by tag
            if (ContainedObject.CompareTag("Wall")) return true;
            
            // Check by component (more reliable)
            WallController wall = ContainedObject.GetComponent<WallController>();
            return wall != null;
        }
    }
}