using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.RemoteConfig;
using UnityEngine.UIElements;

public class EventsMenuController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private MainUIBinder ui;                 // pour ShowNotification/ShowConfirmation
    [SerializeField] private Transform buttonsParent;
    [SerializeField] private GameObject eventButtonPrefab;

    [Header("Page")]
    [SerializeField] private EventPageController eventPage;   // ta page/panel EventPage
    [SerializeField] private GameObject eventPageRoot;        // panel root à activer si besoin

    [Header("Optional banner mapping")]
    [SerializeField] private List<BannerMapping> bannerMappings = new();

    private readonly List<GameObject> _spawned = new();

    [Serializable]
    public class BannerMapping
    {
        public string bannerUrl;   // on l'utilise comme clé (même si ce n'est pas une URL réelle en V1)
        public Sprite sprite;
    }

    public async Task RefreshEventsAsync()
    {
        try
        {
            ClearButtons();

            var events = await EventsRemoteConfig.GetEventsAsync();
            if (events.Length == 0)
            {
                ui.ShowNotification("Aucun événement pour le moment.");
                return;
            }

            foreach (var e in events)
            {
                var viewObj = Instantiate(eventButtonPrefab, buttonsParent);
                viewObj.SetActive(true);
                _spawned.Add(viewObj);

                var tag = e.type == "PARI" ? "[PARI]" : "[INFO]";
                var sprite = ResolveBannerSprite(e.bannerUrl);

                var view = viewObj.GetComponent<EventButtonView>();
                if (view != null)
                {
                    view.Bind(
                        title: e.title,
                        tag: tag,
                        banner: sprite,
                        onClick: () => OpenEventPage(e)
                    );
                    viewObj.SetActive(true);
                }
                else
                {
                    Debug.LogError("[Event] EventButtonView component not found on prefab!");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            ui.ShowNotification("Erreur lors du chargement des événements.");
        }
    }

    private void OpenEventPage(EventDto e)
    {
        if (eventPageRoot) eventPageRoot.SetActive(true);
        eventPage.Show(e);
    }

    private Sprite ResolveBannerSprite(string bannerUrl)
    {
        if (string.IsNullOrWhiteSpace(bannerUrl)) return null;
        foreach (var m in bannerMappings)
            if (m != null && m.bannerUrl == bannerUrl) return m.sprite;
        return null;
    }

    private void ClearButtons()
    {
        foreach (var go in _spawned)
            if (go) Destroy(go);
        _spawned.Clear();
    }
}
