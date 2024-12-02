using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using PocketBaseSdk;

public class PocketBaseExample : MonoBehaviour
{
    public string RecordIdToDelete;

    private PocketBase _pocketBase;

    private void Start()
    {
        _pocketBase = new PocketBase(
            "https://pocketbase-production-891b.up.railway.app",
            authStore: AsyncAuthStore.PlayerPrefs
        );

        _pocketBase.AuthStore.OnChange.Subscribe(eventData =>
        {
            if (eventData.Model is UserModel user)
            {
                Debug.Log("User data: " + JsonConvert.SerializeObject(user, Formatting.Indented));
            }
        });

        _pocketBase.Collection("post").Subscribe<Post>("*", post =>
        {
            Debug.Log(post.Action);
            Debug.Log(post.Record.Content);
        });
    }

    [ContextMenu(nameof(ListAuthMethods))]
    private async void ListAuthMethods()
    {
        try
        {
            var result = await _pocketBase.Collection("users").ListAuthMethods();
            Debug.Log(JsonConvert.SerializeObject(result, Formatting.Indented));
        }
        catch (ClientException e)
        {
            Debug.LogError(e);
        }
    }

    [ContextMenu(nameof(RequestVerification))]
    private async void RequestVerification()
    {
        try
        {
            await _pocketBase.Collection("users").RequestVerification(((UserModel)_pocketBase.AuthStore.Model).Email);
        }
        catch (ClientException e)
        {
            Debug.LogError(e);
        }
    }

    [ContextMenu(nameof(RequestPasswordReset))]
    private async void RequestPasswordReset()
    {
        try
        {
            await _pocketBase.Collection("users").RequestPasswordReset(((UserModel)_pocketBase.AuthStore.Model).Email);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    [ContextMenu(nameof(InvalidRequest))]
    private async void InvalidRequest()
    {
        try
        {
            await _pocketBase.Collection("invalid").GetFullList<RecordModel>();
        }
        catch (ClientException ex)
        {
            Debug.LogError(ex.ToString());
        }
    }

    [ContextMenu(nameof(AuthWithPassword))]
    private async void AuthWithPassword()
    {
        try
        {
            await _pocketBase.Collection("users").AuthWithPassword(
                "sduval@wonder-partners.com",
                "samuel91"
            );

            Debug.Log(_pocketBase.AuthStore.Model.Id);
        }
        catch (ClientException e)
        {
            Debug.LogException(e);
        }
    }

    [ContextMenu(nameof(Logout))]
    private void Logout()
    {
        _pocketBase.AuthStore.Clear();
    }

    [ContextMenu(nameof(GetPost))]
    private async void GetPost()
    {
        try
        {
            var result = await _pocketBase.Collection("post").GetOne<Post>("rssao1gsgwiuc6m", fields: "content");
            Debug.Log(JsonConvert.SerializeObject(result, Formatting.Indented));
        }
        catch (ClientException e)
        {
            Debug.LogError(
                "uri: " + e.URL + "\n" +
                "status: " + e.StatusCode + "\n" +
                "response: " + string.Join("\n", e.Response) + "\n" +
                "original: " + e.OriginalError
            );
        }
    }

    [ContextMenu(nameof(GetSecured))]
    private async void GetSecured()
    {
        try
        {
            var result = await _pocketBase.Collection("secured").GetOne<RecordModel>("83twzdyokfc3epn");
            Debug.Log(JsonConvert.SerializeObject(result, Formatting.Indented));
        }
        catch (ClientException e)
        {
            Debug.LogException(e);
        }
    }

    [ContextMenu(nameof(UpdatePost))]
    private async void UpdatePost()
    {
        string newContent = "<p>Hello, World! Updated</p>";

        try
        {
            var result = await _pocketBase.Collection(collectionIdOrName: "post").Update<Post>(
                id: "rssao1gsgwiuc6m",
                body: new Post
                {
                    Content = newContent
                },
                files: new()
                {
                    new MultipartFormFileSection("cover", new byte[] { 1, 2, 3, 4, 5 }, "new_cover.png", "image/png")
                }
            );
            Debug.Log(JsonConvert.SerializeObject(result, Formatting.Indented));
        }
        catch (ClientException e)
        {
            Debug.LogException(e);
        }
    }

    [ContextMenu(nameof(CreatePost))]
    private async void CreatePost()
    {
        try
        {
            var record = await _pocketBase.Collection("post").Create<Post>(
                body: new Post
                {
                    Content = "<p>Hello, World!</p>"
                },
                files: new()
                {
                    new MultipartFormFileSection(
                        name: "cover",
                        data: new byte[] { 1, 2, 3, 4, 5 },
                        fileName: "cover.png",
                        contentType: "image/png")
                }
            );
            Debug.Log(JsonConvert.SerializeObject(record, Formatting.Indented));
        }
        catch (ClientException e)
        {
            Debug.LogException(e);
        }
    }

    [ContextMenu(nameof(CreatePostWithDictionary))]
    private async void CreatePostWithDictionary()
    {
        try
        {
            var record = await _pocketBase.Collection("post").Create<Post>(new Dictionary<string, string>
            {
                { "content", "<p>Hello, From dictionary!</p>" },
                { "test", "test" } // This should not be added in the database as it's not part of the db schema.
            });
            Debug.Log(JsonConvert.SerializeObject(record, Formatting.Indented));
        }
        catch (ClientException e)
        {
            Debug.LogException(e);
        }
    }

    [ContextMenu(nameof(DeletePost))]
    private async void DeletePost()
    {
        try
        {
            await _pocketBase.Collection("post").Delete(RecordIdToDelete);
            Debug.Log("Record deleted successfully.");
        }
        catch (ClientException e)
        {
            Debug.LogException(e);
        }
    }

    [ContextMenu(nameof(GetList))]
    private async void GetList()
    {
        try
        {
            var result = await _pocketBase.Collection("post").GetList<Post>();
            Debug.Log("List fetch success: \n" + JsonConvert.SerializeObject(result, Formatting.Indented));

            foreach (Post post in result.Items)
            {
                Debug.Log(post.Id);
            }
        }
        catch (ClientException e)
        {
            Debug.LogException(e);
        }
    }

    [ContextMenu(nameof(GetFullList))]
    public async void GetFullList()
    {
        try
        {
            var fullList = await _pocketBase.Collection("post").GetFullList<Post>();

            foreach (Post post in fullList)
            {
                Debug.Log(post.Id);
            }
        }
        catch (ClientException e)
        {
            Debug.LogException(e);
        }
    }
}