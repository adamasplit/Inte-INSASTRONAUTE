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

            // On WebGL, Unity Auth restores a cached session from IndexedDB on page reload.
            // Calling SignInAnonymouslyAsync when already signed in throws an exception that
            // can corrupt the auth state and cause Economy/CloudSave to fail downstream.
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Debug.Log($" Signed in! PlayerId = {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            Debug.LogError($" UGS Auth failed: {e}");
        }
    }
}
