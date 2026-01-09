using System.Linq;
using UnityEngine;
using Unity.Services.Economy;
using System.Threading.Tasks;

public class PlayerStatusController : MonoBehaviour
{
    public UpdateDataUI[] uIElements;

    private async void Start()
    {
        await PlayerProfileStore.LoadPackCollectionAsync();
        await PlayerProfileStore.LoadCardCollectionAsync();

        uIElements = FindObjectsByType<UpdateDataUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        await RefreshStatusAsync();
    }

    public async Task RefreshStatusAsync()
    {
        // TOKEN depuis Economy
        var currencies = await EconomyService.Instance.PlayerBalances.GetBalancesAsync();
        var tokenBal = currencies.Balances.FirstOrDefault(b => b.CurrencyId == "TOKEN");
        var tokens = tokenBal?.Balance ?? 0;

        // PACKS depuis Cloud Save (mémoire)
        long packs = PlayerProfileStore.PACK_COLLECTION.Values.Sum();

        Debug.Log($"TOKENS={tokens} | PACKS={packs}");

        PlayerProfileStore.TOKEN = tokens;
        PlayerProfileStore.PACK = packs;

        foreach (var ui in uIElements)
        {
            ui.RefreshDataUI();
        }
    }
}
