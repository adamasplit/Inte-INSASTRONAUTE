using UnityEngine;
using UnityEngine.UI;

public class TurnIcon : MonoBehaviour
{
    public Image outline;
    public Image portrait;
    public Image background;
    public GameObject strikeThrough;
    public void Set(Character character)
    {
        portrait.sprite = character.portrait; // ou placeholder
        if (portrait.sprite == null)
            portrait.gameObject.SetActive(false);
        if (character.isPlayer)
            background.color = Color.blue;
        else
            background.color = Color.red;
    }
    public void SetPreview(bool preview)
    {
        if (preview)
        {
            background.color = new Color(background.color.r, background.color.g, background.color.b, 0.5f);
            portrait.color = new Color(portrait.color.r, portrait.color.g, portrait.color.b, 0.5f);
        }
        else
        {             
            portrait.color = new Color(portrait.color.r, portrait.color.g, portrait.color.b, 1f);
            background.color = new Color(background.color.r, background.color.g, background.color.b, 1f);
        }
    }
    public void SetRemoved()
    {
        Debug.Log("Setting turn icon as removed");
        background.color = Color.gray;
        outline.color = Color.gray;
        strikeThrough.SetActive(true);
    }

    public void SetDelayed()
    {
        outline.color = Color.red;
    }
    public void SetAdvanced()
    {
        outline.color = Color.green;
    }
    public void ClearVisuals()
    {
        outline.color = Color.black;
        strikeThrough.SetActive(false);
    }
}