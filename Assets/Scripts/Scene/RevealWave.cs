using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class RevealWave : MonoBehaviour
{
    public Material maskMaterial;   // shader radial
    private float duration = 5f;     // durée totale de la vague
    private float maxRadius = 4f;    // rayon max pour le gradient
    public StarController[] stars;  // étoiles à révéler
    public ParticleSystem ps;       // particules à spawn autour

    private void Start()
    {
        foreach (var star in stars)
        {
            star.SetVisible(false); // invisibles au départ
        }
    }

    public async Task PlayRevealWave()
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float eased = EaseOutCubic(t);  
            float size=maxRadius*400f* eased;
            ((RectTransform)transform).sizeDelta = new Vector2(size, size);
            t += Time.deltaTime;
            float currentRadius = Mathf.Lerp(0f, maxRadius, eased);
            maskMaterial.SetFloat("_Radius", currentRadius);

            // Faire apparaître les étoiles progressivement
            foreach (var star in stars)
            {
                if (!star.IsVisible)
                {
                    float distance = star.transform.localPosition.magnitude;
                    if (distance <= currentRadius)
                    {
                        // fade-in et pulse
                        star.SetVisible(true);
                        _ = star.FadeIn(0.2f); // async fire-and-forget
                    }
                }
            }

            // Spawn quelques particules sur le bord
            SpawnParticlesOnCircle(currentRadius, 2); // 2 particules par frame

            await Task.Yield();
        }

        // sécurité : toutes les étoiles visibles
        foreach (var star in stars)
        {
            if (!star.IsVisible)
                star.SetVisible(true);
        }
    }

    void SpawnParticlesOnCircle(float radius, int count)
    {
        if (ps == null) return;

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 pos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;

            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams();
            emit.position = pos;
            emit.startColor = new Color(1f, 1f, 1f, 1f-radius/maxRadius); // plus transparent à mesure que la vague grandit
            ps.Emit(emit, 1);
        }
    }

    float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
    }
}
