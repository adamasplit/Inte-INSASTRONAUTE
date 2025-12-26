using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class UgsAuthBootstrap : MonoBehaviour
{
    async void Awake()
    {
        await SignIn();
    }

    private async Task SignIn()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Debug.Log($"✅ Signed in! PlayerId = {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ UGS Auth failed: {e}");
        }
    }
}
