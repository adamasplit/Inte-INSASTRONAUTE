using System.Linq;
using UnityEngine;
using Unity.Services.Economy;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

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
        var collectionPointsBal = currencies.Balances.FirstOrDefault(b => b.CurrencyId == "CP");
        var collectionPoints = collectionPointsBal?.Balance ?? 0;

        PlayerProfileStore.TOKEN = tokens;
        PlayerProfileStore.PC = collectionPoints;

        foreach (var ui in uIElements)
        {
            ui.RefreshDataUI();
        }
    }
}
