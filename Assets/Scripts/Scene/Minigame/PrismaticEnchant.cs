using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PrismaticEnchant : MonoBehaviour
{
    private SpriteRenderer sr;
    private Material mat;
    private Vector2 offset = Vector2.zero;
    private float maxAlpha;
    private float currentAlpha;
    private bool increasing = true;

    public float speed = 0.5f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        mat = sr.material;
        offset = new Vector2(-1f, -1f);
        maxAlpha = sr.color.a;
        currentAlpha = 0f;
        sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f);
    }

    void Update()
    {
        // Pulsation de l'alpha
        if (increasing)
        {
            currentAlpha += Time.deltaTime;
            if (currentAlpha >= maxAlpha)
            {
                currentAlpha = maxAlpha;
                increasing = false;
            }
        }
        else
        {
            currentAlpha -= Time.deltaTime;
            if (currentAlpha <= 0f)
            {
                currentAlpha = 0f;
                increasing = true;
            }
        }
        sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, currentAlpha);
        offset += new Vector2(1f, 1f).normalized * speed * Time.deltaTime;
        if (offset.x > 1f&& offset.y > 1f&&currentAlpha<=0.1f)
        {
            offset.x = -1f;
            offset.y = -1f;
        }
        
        mat.mainTextureOffset = offset;
        
    }
}
