using UnityEngine;
using System.Collections.Generic;
public class InstantDamageAttack : MonoBehaviour, IAttackBehaviour
{
    public void ExecuteAttack(Tower tower,Column column, List<Enemy> targets, CardData card)
    {
        Debug.Log("Executing InstantDamageAttack on " + targets.Count + " targets. Single Target VFX: " + card.singleTargetVfx + ", Damaging VFX: " + card.damagingVfx);
        foreach (Enemy target in targets)
            {
                if (target == null) continue;
                if (!card.damagingVfx)
                {
                    target.TakeDamage(card.baseDamage, card.element);
                }
                if (card.singleTargetVfx)
                {
                    PlayVFX(tower, column, new List<Enemy> { target }, card);
                }
            }
        if (!card.singleTargetVfx)
        {
            PlayVFX(tower, column, targets, card);
        }
            
    }

    void PlayVFX(Tower tower, Column column, List<Enemy> targets, CardData card)
    {
        if (!card.damagingVfx && !GameSettings.VFXEnabled)
        {
            Debug.Log("VFX not necessary and VFX disabled, skipping VFX instantiation.");
            return;
        }
        GameObject vfx=Resources.Load<GameObject>("VFX/"+card.cardId);
        if (vfx != null)
        {
            GameObject instance = Instantiate(vfx, tower.transform);
            IVFX ivfx=instance.GetComponent<IVFX>();
            ivfx?.Fire(tower.transform.position, targets, card);
        }
    }
}
