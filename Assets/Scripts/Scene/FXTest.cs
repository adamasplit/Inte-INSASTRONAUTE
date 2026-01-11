using UnityEngine;
public class FXTest : MonoBehaviour
{
    public Canvas fxCanvas;
    public GameObject fxPrefab;

    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("Spawning FX at center of screen");
            var fx = Instantiate(fxPrefab, fxCanvas.transform);
            fx.transform.position = new Vector3(0, 0, 0);
        }
    }
}
