using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Economy;
using UnityEditor.UIElements;

public class PlayerStatusController : MonoBehaviour
{
    public UpdateDataUI[] uIElements;

    void Start()
    {
        uIElements = FindObjectsByType<UpdateDataUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        // Initial refresh
        _ = RefreshStatusAsync();
    }

    public async Task RefreshStatusAsync()
    {
        // 1) Balance TOKEN
        var currencies = await EconomyService.Instance.PlayerBalances.GetBalancesAsync();
        var tokenBal = currencies.Balances.FirstOrDefault(b => b.CurrencyId == "TOKEN");
        var tokens = tokenBal?.Balance ?? 0;

        // 2) Inventaire: compter PACK
        var inv = await EconomyService.Instance.PlayerInventory.GetInventoryAsync();
        var packs = inv.PlayersInventoryItems.Count(i => i.InventoryItemId == "PACK");

        Debug.Log($"TOKENS={tokens} | PACKS={packs}");

        // Mettre à jour les valeurs statiques
        PlayerProfileStore.TOKEN = tokens;
        PlayerProfileStore.PACK = packs;

        // Mettre à jour l'UI
        foreach (var ui in uIElements)
        {
            ui.RefreshDataUI();
        }
    }
}