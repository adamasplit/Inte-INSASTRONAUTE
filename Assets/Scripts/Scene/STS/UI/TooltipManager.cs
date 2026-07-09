using UnityEngine;
public class TooltipManager:MonoBehaviour
{
    public static TooltipManager Instance;
    public Transform tooltipLayer;
    public GameObject tooltipPrefab;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    public void ShowTooltip(string name, string description, Vector3 position,bool erasePrevious=true)
    {
        if (erasePrevious)
        {
            foreach (Transform child in tooltipLayer)
            {
                Destroy(child.gameObject);
            }
        }
        Debug.Log($"Showing tooltip: {name} - {description} at position {position}");
        GameObject obj = Instantiate(tooltipPrefab, tooltipLayer);
        Tooltip tooltip = obj.GetComponent<Tooltip>();
        tooltip.SetTooltip(this,name, description);
        tooltip.transform.position = position;
    }
    public void HideTooltip()
    {
        foreach (Transform child in tooltipLayer)
        {
            Destroy(child.gameObject);
        }
    }
}