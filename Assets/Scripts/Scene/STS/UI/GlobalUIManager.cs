using UnityEngine;
using UnityEngine.UI;
public class GlobalUIManager : MonoBehaviour
{
    public static GlobalUIManager Instance;

    public bool selectionMode;
    public int requiredCount;

    void Awake()
    {
        Instance = this;
    }

    public void EnableCardSelection(int count)
    {
        selectionMode = true;
        requiredCount = count;
    }

    public void DisableCardSelection()
    {
        selectionMode = false;
        requiredCount = 0;
    }
    
}