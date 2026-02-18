using System.Collections.Generic;
public interface IAttackBehaviour
{
    void ExecuteAttack(Tower tower, Column column, List<Enemy> targets, CardData card);
}
