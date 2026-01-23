using Unity.Services.Economy;
using System.Threading.Tasks;

public static class CurrencyService
{
    public static async Task AddTokensAsync(int amount)
    {
        await EconomyService.Instance.PlayerBalances
            .IncrementBalanceAsync("TOKEN", amount);
        PlayerProfileStore.TOKEN += amount;

    }
}
