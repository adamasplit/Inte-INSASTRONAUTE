using UnityEngine;
using UnityEngine.InputSystem;

public class RayDebug : MonoBehaviour
{
    public Camera cam;        // ta caméra principale
    public float rayLength = 20f;

    void Update()
    {
        Vector2 pointerPos = Pointer.current.position.ReadValue();

        if (cam == null) cam = Camera.main;

        Ray ray = cam.ScreenPointToRay(pointerPos);

        // Debug.DrawRay visible dans Scene View
        Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.red);

        // Affiche la hit position si le ray touche un objet
        if (Physics.Raycast(ray, out RaycastHit hit, rayLength))
        {
            Debug.DrawLine(hit.point, hit.point + Vector3.up * 0.2f, Color.green); // petit repère
            Debug.Log("Ray hit: " + hit.collider.name + " at " + hit.point);
        }
    }
}
