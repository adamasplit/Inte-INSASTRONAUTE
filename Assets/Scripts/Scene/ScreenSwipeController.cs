using UnityEngine;
using System.Collections;
using Lean.Gui;
public class ScreenSwipeController : MonoBehaviour
{
    public LeanDrag leanDrag;

    void Update()
    {
        leanDrag.enabled = !NavigationLock.IsScreenSwipeLocked;
    }
}
