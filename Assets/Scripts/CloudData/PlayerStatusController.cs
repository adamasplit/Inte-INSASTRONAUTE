using System.Linq;
using UnityEngine;
using Unity.Services.Economy;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

public class PlayerStatusController : MonoBehaviour
{
    public UpdateDataUI[] uIElements;
    public GameObject loadingIndicator;

    private async void Start()
    {
        loadingIndicator.SetActive(true);
        await PlayerProfileStore.LoadPackCollectionAsync();
        await PlayerProfileStore.LoadCardCollectionAsync();

        uIElements = FindObjectsByType<UpdateDataUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
        //await Task.Delay(500); // Attendre un peu pour s'assurer que tout est prêt
        await RefreshStatusAsync();
        await FindFirstObjectByType<LeaderboardController>().RefreshLeaderboardAsync();
        await FindFirstObjectByType<ShopRemoteLoader>().UpdateShopFromRemoteTask();
        loadingIndicator.SetActive(false);
    }

    public async Task RefreshStatusAsync()
    {
        // TOKEN depuis Economy
        var currencies = await EconomyService.Instance.PlayerBalances.GetBalancesAsync();
        var tokenBal = currencies.Balances.FirstOrDefault(b => b.CurrencyId == "TOKEN");
        var tokens = tokenBal?.Balance ?? 0;
        var collectionPointsBal = currencies.Balances.FirstOrDefault(b => b.CurrencyId == "PC");
        var collectionPoints = collectionPointsBal?.Balance ?? 0;

        PlayerProfileStore.TOKEN = tokens;
        await PlayerProfileStore.ComputePC();

        foreach (var ui in uIElements)
        {
            ui.RefreshDataUI();
        }
    }
}
