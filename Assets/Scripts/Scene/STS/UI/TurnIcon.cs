using UnityEngine;
using UnityEngine.UI;

public class TurnIcon : MonoBehaviour
{
    public Image outline;
    public Image portrait;
    public Image background;
    public GameObject strikeThrough;
    public CanvasGroup canvasGroup;

    Vector3 targetPosition;

    public float moveSpeed = 10f;
    public bool preview = false;

    public void Set(Character character)
    {
        portrait.sprite = character.portrait;

        portrait.gameObject.SetActive(portrait.sprite != null);

        background.color = character.isPlayer ? Color.blue : Color.red;
    }

    void Update()
    {
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            new Vector3(targetPosition.x, transform.localPosition.y, targetPosition.z),
            Time.deltaTime * moveSpeed
        );
        if (preview)
        {
            transform.localPosition = targetPosition;
        }
    }

    public void SetTargetPosition(Vector3 pos)
    {
        targetPosition = pos;
    }

    public void SetPreview(bool preview)
    {
        canvasGroup.alpha = preview ? 0.5f : 1f;
        this.preview=preview;
    }
    TurnVisualType currentType;
    public void SetType(TurnVisualType type)
    {
        if (type == currentType)
            return;
        currentType = type;
        switch (type)
        {
            case TurnVisualType.Normal:
                ClearVisuals();
                break;
            case TurnVisualType.Removed:
                SetRemoved();
                break;
            case TurnVisualType.Added:
                outline.color = Color.green;
                break;
            case TurnVisualType.Delayed:
                SetDelayed();
                break;
            case TurnVisualType.Advanced:
                SetAdvanced();
                break;
        }
    }

    public void SetRemoved()
    {
        background.color = Color.gray;
        outline.color = Color.gray;
        strikeThrough.SetActive(true);
    }

    public void SetDelayed()
    {
        outline.color = Color.red;
        transform.localPosition += new Vector3(0, -20, 0);
    }

    public void SetAdvanced()
    {
        outline.color = Color.green;
        transform.localPosition += new Vector3(0, 20, 0);
    }

    public void ClearVisuals()
    {
        outline.color = Color.black;
        strikeThrough.SetActive(false);
    }
}