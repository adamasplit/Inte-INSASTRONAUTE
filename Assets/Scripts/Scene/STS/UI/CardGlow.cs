using UnityEngine;
using UnityEngine.UI;
public class CardGlow : MonoBehaviour
{
    public RawImage glowImage;
    public float speed = 0.5f;

    private Material mat;

    void Awake()
    {
        mat = glowImage.material;
    }

    void Update()
    {
        Vector2 offset = mat.mainTextureOffset;
        offset.x -= Time.deltaTime * speed;
        offset.y += Time.deltaTime * speed;
        mat.mainTextureOffset = offset;
    }
}