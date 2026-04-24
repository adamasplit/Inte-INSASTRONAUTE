using UnityEngine;
using TMPro;

public class UpdateDataUI : MonoBehaviour
{
    public string dataKey;
    public long currentValue;

    private TextMeshProUGUI _text;

    void Awake() => _text = this.Require(GetComponent<TextMeshProUGUI>(), "TextMeshProUGUI");

    void OnEnable()
    {
        switch (dataKey)
        {
            case "TOKEN":
                PlayerProfileStore.OnTokenChanged += HandleLong;
                HandleLong(PlayerProfileStore.TOKEN);
                break;
            case "PC":
                PlayerProfileStore.OnPCChanged += HandleLong;
                HandleLong(PlayerProfileStore.PC);
                break;
            case "USERNAME":
                PlayerProfileStore.OnDisplayNameChanged += HandleString;
                HandleString(PlayerProfileStore.DISPLAY_NAME);
                break;
            default:
                Debug.LogWarning($"[UpdateDataUI] Clé inconnue : '{dataKey}' sur {gameObject.name}");
                break;
        }
    }

    void OnDisable()
    {
        PlayerProfileStore.OnTokenChanged       -= HandleLong;
        PlayerProfileStore.OnPCChanged          -= HandleLong;
        PlayerProfileStore.OnDisplayNameChanged -= HandleString;
    }

    private void HandleLong(long value)
    {
        currentValue = value;
        if (_text != null) _text.text = value.ToString();
    }

    private void HandleString(string value)
    {
        if (_text != null) _text.text = value;
    }

    // Compatibilité ascendante avec le code existant (GameManager, etc.)
    public void RefreshDataUI() => OnEnable();
    public void alterDataUI(long delta) => HandleLong(currentValue + delta);
    public void setDataUI(long value)   => HandleLong(value);
}
