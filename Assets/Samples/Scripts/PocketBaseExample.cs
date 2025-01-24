using PocketBaseSdk;
using UnityEngine;

public class PocketBaseExample : MonoBehaviour
{
    [SerializeField] private string _pocketBaseUrl;

    private PocketBase _pocketBase;

    private void Start()
    {
        _pocketBase = new PocketBase(_pocketBaseUrl, authStore: AsyncAuthStore.PlayerPrefs);

        // _pocketBase.Collection("users").AuthRefresh().ContinueWithOnMainThread(t =>
        // {
        //     if (t.IsFaulted)
        //     {
        //         Debug.LogError(t.Exception);
        //     }
        //
        //     var user = t.Result;
        //     Debug.Log(user.Record.Email);
        // });

        _pocketBase.AuthStore.OnChange.Subscribe(e =>
        {
            Debug.Log(e.Record);
        });
    }

    [ContextMenu(nameof(Login))]
    private void Login()
    {
        _pocketBase.Collection("users")
            .AuthWithPassword("sduval@wonder-partners.com", "samuel91")
            .ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogError(t.Exception);
                }

                var user = t.Result;
                Debug.Log(user.Record.ToString());
            });
    }

    [ContextMenu(nameof(GetPost))]
    private void GetPost()
    {
        _pocketBase
            .Collection("Posts")
            .GetOne("3g28fsld3arvpaz")
            .ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogError(t.Exception);
                }

                var post = t.Result;
                Debug.Log(post["title"]);
            });
    }
}