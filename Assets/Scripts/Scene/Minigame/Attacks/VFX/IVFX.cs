using UnityEngine;
using System.Collections.Generic;
public interface IVFX
{
    public void Fire(Vector3 startPos, List<Enemy> targetPos, CardData card);
}