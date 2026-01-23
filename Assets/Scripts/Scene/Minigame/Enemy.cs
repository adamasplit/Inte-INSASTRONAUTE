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

    Color GetElementColor(Element elem)
    {
        switch (elem)
        {
            case Element.Fire: return Color.red;
            case Element.Water: return Color.blue;
            case Element.Earth: return Color.green;
            case Element.Air: return Color.cyan;
            default: return Color.white;
        }
    }
    public void Initialize(float health, float moveSpeed)
    {
        Element elem = Random.Range(0, 4) switch
        {
            0 => Element.Fire,
            1 => Element.Water,
            2 => Element.Earth,
            3 => Element.Air,
            _ => Element.Fire
        };
        hp = health;
        speed = moveSpeed;
        GetComponent<Image>().color = GetElementColor(elem);
    }
    public void TakeDamage(float dmg,Element element)
    {
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
        if (hp <= 0)
            StartCoroutine(Die());
    }

    IEnumerator Die()
    {
        deathEffect.gameObject.SetActive(true);
        deathEffect.Play();
        GameManager.Instance.AddScore(10);
        GetComponent<Image>().enabled = false;
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}
