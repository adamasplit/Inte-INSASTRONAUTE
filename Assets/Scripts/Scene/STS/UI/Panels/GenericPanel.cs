using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
public class GenericPanel : MonoBehaviour
{
    public TextMeshProUGUI title;
    public Transform container;
    public GameObject optionPrefab;
    public RectTransform panelRoot;

    public void Show(string titleText, List<PanelOption> options)
    {
        title.text = titleText;

        foreach (Transform child in container)
            Destroy(child.gameObject);

        foreach (var opt in options)
        {
            var obj = Instantiate(optionPrefab, container);

            var view = obj.GetComponent<PanelOptionView>();
            view.Init(opt);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRoot);
    }
}