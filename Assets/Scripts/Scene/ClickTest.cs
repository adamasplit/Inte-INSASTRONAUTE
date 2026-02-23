using UnityEngine;
using UnityEngine.EventSystems;
public class ClickTest : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerDown(PointerEventData e)
        => Debug.Log("DOWN");

    public void OnPointerUp(PointerEventData e)
        => Debug.Log("UP");
}