using UnityEngine;
using System.Collections.Generic;
public class InstantDamageAttack : MonoBehaviour, IAttackBehaviour
{
    public void ExecuteAttack(Tower tower,Column column, List<Enemy> targets, CardData card)
    {
        foreach (Enemy target in targets)
        {
            if (target == null) continue;
            target.TakeDamage(card.baseDamage, card.element);
            
        }
        GameObject vfx=Resources.Load<GameObject>("VFX/"+card.cardId);
        if (vfx != null)
        {
            GameObject instance = Instantiate(vfx, tower.transform);
            instance.GetComponent<AutoDestroyVFX>()?.SetDuration(card.vfxDuration);
            instance.GetComponent<IVFX>()?.Fire(tower.transform.position, targets.ConvertAll(e => e.transform.position));
        }
    }
}
