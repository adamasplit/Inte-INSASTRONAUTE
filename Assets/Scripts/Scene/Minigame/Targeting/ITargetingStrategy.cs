using System.Collections.Generic;
public interface ITargetingBehaviour
{
    List<Enemy> GetTargets(Column column);
}
