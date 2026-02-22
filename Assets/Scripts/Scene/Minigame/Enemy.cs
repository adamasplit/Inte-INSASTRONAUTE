using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class Enemy : MonoBehaviour
{
    public Element element;
    public float hp;
    public float speed;
    public ParticleSystem deathEffect;
    public ParticleSystem hitEffect;
    public ParticleSystem criticalEffect;
    public ParticleSystem weakEffect;
    private bool prismatic;
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer prismaticOverlay;
    public GameObject targetIndicator;
    public bool dead = false;
    private Vector3 lastPosition;
    void Update()
    {
        
        float distance = Mathf.Abs(lastPosition.y-transform.position.y);
        if (distance>0.01f)
        {
            //Debug.Log("Enemy moved from " + lastPosition + " to " + transform.position);
            if (distance>5f&&lastPosition.y>transform.position.y)
            {
                Debug.LogWarning("Enemy moved a large distance! Possible teleportation bug.");
                transform.position = lastPosition;
            }
        }
        lastPosition = transform.position;
    }
    public void Initialize(float health, float moveSpeed)
    {
        if (this == null) return;
        GetComponent<RectTransform>().position = new Vector3(transform.position.x, 3.5f, 0);
        if (Random.Range(0, 20) == 0)
        {
            prismatic = true;
            element = Element.Prismatic;
        }
        else
        {
            Element elem = Random.Range(0, 7) switch
            {
                0 => Element.Rite,
                1 => Element.Asso,
                2 => Element.Event,
            3 => Element.Bar_boite,
            4 => Element.Liste,
            5 => Element.Personne,
            6 => Element.Galere_spam,
            _ => Element.Fire
            };
            element = elem;
        }
        hp = health;
        speed = moveSpeed;
        spriteRenderer.color = ElementCalculator.GetElementColor(ElementCalculator.GetFirstWeakElement(element));
        if (prismatic)
        {
            prismaticOverlay.gameObject.SetActive(true);
        }
    }
    public void TakeDamage(float dmg,Element element)
    {
        if (dead) return;
        switch (ElementCalculator.GetEffectiveness(element, this.element))
        {
        case Effectiveness.Strong:
                if (criticalEffect)
                {
                    criticalEffect.gameObject.SetActive(true);
                    criticalEffect.Play();
                }
                break;
            case Effectiveness.Weak:
                if (weakEffect)
                {
                    weakEffect.gameObject.SetActive(true);
                    weakEffect.Play();
                }
                break;
            case Effectiveness.Normal:
                if (hitEffect)
                {
                    hitEffect.gameObject.SetActive(true);
                    hitEffect.Play();
                }
                break;
        }
        
        hp -= dmg;
        if (hp <= 0 && !dead)
        {
            dead = true;
            StartCoroutine(Die());
        }
    }

    IEnumerator Die()
    {
        deathEffect.gameObject.SetActive(true);
        deathEffect.Play();
        GameManager.Instance.AddScore(10);
        spriteRenderer.enabled = false;
        if (prismatic||Random.Range(0, 10) == 0)
        {
            FindFirstObjectByType<EnemySpawner>().spawnedEnemies++;
        }
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }
    public void ShowTargetIndicator()
    {
        if (targetIndicator != null)        
        {
            targetIndicator.SetActive(true);
        }
    }
    public void HideTargetIndicator()
    {
        if (targetIndicator != null)
        {
            targetIndicator.SetActive(false);
        }
    }
}
