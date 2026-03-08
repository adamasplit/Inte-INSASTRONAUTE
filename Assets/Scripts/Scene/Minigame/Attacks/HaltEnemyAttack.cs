using UnityEngine;
using System.Collections.Generic;
using System.Collections;
public class HaltEnemyAttack : MonoBehaviour, IAttackBehaviour
{
    public float haltDuration = 2f;
    public IVFX vfx;

    public void ExecuteAttack(Tower tower, Column column, List<Enemy> targets, CardData card)
    {
        Debug.Log("Executing HaltEnemyAttack on " + targets.Count + " targets.");
        foreach (Enemy enemy in targets)
        {
            StartCoroutine(HaltEnemy(enemy));
        }
        if (targets.Count > 0&& vfx != null)
        {
            vfx.Fire(transform.position, targets,card);
        }
    }

    private IEnumerator HaltEnemy(Enemy enemy)
    {
        enemy.Halt();
        yield return new WaitForSeconds(haltDuration);
        enemy.Resume();
    }
}