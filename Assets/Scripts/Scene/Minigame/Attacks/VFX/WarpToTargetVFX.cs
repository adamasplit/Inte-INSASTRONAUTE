using UnityEngine;
using System.Collections.Generic;
public class WarpToTargetVFX : MonoBehaviour, IVFX
{
    public Vector3 offset;
    public void Fire(Vector3 startPos, List<Enemy> targetPos, CardData card)
    {
        if (targetPos.Count > 0)
        {
            transform.position = targetPos[0].transform.position;
            transform.position += offset;
        }
    }
}