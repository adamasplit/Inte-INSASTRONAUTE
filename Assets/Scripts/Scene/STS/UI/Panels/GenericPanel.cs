using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Linq;
public class GenericPanel : MonoBehaviour
{
    public TextMeshProUGUI title;
    public Transform container;
    public GameObject optionPrefab;
    public RectTransform panelRoot;

    private string currentTitle;
    private readonly List<PanelOption> currentOptions = new();

    public void Show(string titleText, List<PanelOption> options)
    {
        gameObject.SetActive(true);
        currentTitle = titleText;
        currentOptions.Clear();

        if (options != null)
        {
            currentOptions.AddRange(options);
        }

        Render();
    }

    public bool UpdateOption(string optionId, Action<PanelOption> mutator)
    {
        if (string.IsNullOrEmpty(optionId) || mutator == null)
        {
            return false;
        }

        for (int i = 0; i < currentOptions.Count; i++)
        {
            if (currentOptions[i] != null && currentOptions[i].id == optionId)
            {
                mutator(currentOptions[i]);
                Render();
                return true;
            }
        }

        return false;
    }

    public bool ReplaceOption(string optionId, PanelOption replacement)
    {
        if (string.IsNullOrEmpty(optionId) || replacement == null)
        {
            return false;
        }

        for (int i = 0; i < currentOptions.Count; i++)
        {
            if (currentOptions[i] != null && currentOptions[i].id == optionId)
            {
                currentOptions[i] = replacement;
                Render();
                return true;
            }
        }

        return false;
    }

    public bool ReplaceOptions(List<string> targetIds, List<PanelOption> replacements, string fallbackOptionId)
    {
        List<string> ids = targetIds != null
            ? targetIds.Where(id => !string.IsNullOrEmpty(id)).ToList()
            : new List<string>();

        if (ids.Count == 0 && !string.IsNullOrEmpty(fallbackOptionId))
        {
            ids.Add(fallbackOptionId);
        }

        if (ids.Count == 0)
        {
            return false;
        }

        List<PanelOption> snapshot = new List<PanelOption>(currentOptions);
        currentOptions.Clear();

        bool replacedAny = false;
        foreach (var option in snapshot)
        {
            if (option != null && ids.Contains(option.id))
            {
                if (replacements != null && replacements.Count > 0)
                {
                    currentOptions.AddRange(replacements);
                }

                replacedAny = true;
                continue;
            }

            currentOptions.Add(option);
        }

        if (!replacedAny)
        {
            currentOptions.Clear();
            currentOptions.AddRange(snapshot);
            return false;
        }

        Render();
        return true;
    }

    public bool ReplaceCurrentOption(PanelOption currentOption, List<PanelOption> replacements)
    {
        if (currentOption == null)
        {
            return false;
        }

        int index = currentOption.runtimeIndex;
        if (index < 0 || index >= currentOptions.Count || !ReferenceEquals(currentOptions[index], currentOption))
        {
            index = currentOptions.IndexOf(currentOption);
        }

        if (index < 0)
        {
            return false;
        }

        currentOptions.RemoveAt(index);

        if (replacements != null && replacements.Count > 0)
        {
            currentOptions.InsertRange(index, replacements);
        }

        Render();
        return true;
    }

    public void ReplaceOptions(List<PanelOption> options)
    {
        currentOptions.Clear();

        if (options != null)
        {
            currentOptions.AddRange(options);
        }

        Render();
    }

    private void Render()
    {
        title.text = currentTitle;

        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < currentOptions.Count; i++)
        {
            var opt = currentOptions[i];
            if (opt != null)
            {
                opt.runtimeIndex = i;
            }

            var obj = Instantiate(optionPrefab, container);

            var view = obj.GetComponent<PanelOptionView>();
            view.Init(opt);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRoot);
        UILayoutHelper.RebuildAfterFrame(this, panelRoot);
    }
}