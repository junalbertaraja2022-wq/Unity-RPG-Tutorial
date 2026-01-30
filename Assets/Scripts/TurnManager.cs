using UnityEngine;

public class TurnManager
{
    public static TurnManager Instance { get; private set; }
    private int m_TurnCount;

   public TurnManager()
   {
       m_TurnCount = 1;
   }

   public void Tick()
   {
       m_TurnCount += 1;
       Debug.Log("Current turn count : " + m_TurnCount);
   }
}
