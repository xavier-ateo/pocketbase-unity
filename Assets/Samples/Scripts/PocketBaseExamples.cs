using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PocketBaseSdk;
using TMPro;
using UnityEngine;

public class PocketBaseExamples : MonoBehaviour
{
    [SerializeField] private TextAsset _config;
    [SerializeField] private TMP_InputField _otpCode;

    private string _pocketBaseUrl;
    private string _email;
    private string _password;

    private PocketBase _pb;
    private string _otpId;

    private void Awake()
    {
        var config = JObject.Parse(_config.text);
        _email = config["email"]?.ToString();
        _password = config["password"]?.ToString();
        _pocketBaseUrl = config["pocketBaseUrl"]?.ToString();

        _pb = new PocketBase(_pocketBaseUrl, authStore: AsyncAuthStore.PlayerPrefs);
    }

    [ContextMenu(nameof(ConnectToCollectionSSE))]
    private async void ConnectToCollectionSSE()
    {
        await _pb.Collection("example").Subscribe("*", e =>
        {
            Debug.Log(e.Action);
            Debug.Log(e.Record);
        });
    }

    [ContextMenu(nameof(HealthCheck))]
    public void HealthCheck()
    {
        _pb.Health.Check().ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted)
            {
                Debug.LogError(t.Exception);
            }

            HealthCheck health = t.Result;
            Debug.Log(health);
        });
    }

    [ContextMenu(nameof(ListAuthMethods))]
    public async void ListAuthMethods()
    {
        try
        {
            AuthMethodsList authMethods = await _pb.Collection("users").ListAuthMethods();
            Debug.Log(authMethods);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    [ContextMenu(nameof(RequestOTP))]
    public async void RequestOTP()
    {
        try
        {
            OTPResponse response = await _pb.Collection("users").RequestOTP(_email);
            _otpId = response.OtpId;

            Debug.Log(response);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    [ContextMenu(nameof(AuthWithOTP))]
    public async void AuthWithOTP()
    {
        try
        {
            RecordAuth auth = await _pb.Collection("users").AuthWithOTP(
                otpId: _otpId,
                password: _otpCode.text
            );

            Debug.Log(auth);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    [ContextMenu(nameof(LoginWithGoogleAsync))]
    public async void LoginWithGoogleAsync()
    {
        try
        {
            // Start the OAuth2 flow
            RecordAuth auth = await _pb.Collection("users").AuthWithOAuth2("google", url =>
            {
                // Launch the OAuth2 URL in a web view or browser.
                // This is where the user will log in with the provider.
                Application.OpenURL(url.AbsoluteUri);
            });

            Debug.Log($"Success! {auth.Record}");
        }
        catch (ClientException e)
        {
            Debug.LogError(e.OriginalError);
        }
    }

    [ContextMenu(nameof(Login))]
    private void Login()
    {
        _pb.Collection("users")
            .AuthWithPassword(_email, _password)
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

    [ContextMenu(nameof(GetFullList))]
    private void GetFullList()
    {
        _pb.Collection("Posts")
            .GetFullList(10)
            .ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted && t.Exception?.InnerException is AggregateException ae)
                {
                    foreach (var e in ae.InnerExceptions)
                    {
                        Debug.LogError(e.Message);
                    }
                }

                List<RecordModel> posts = t.Result;

                foreach (var post in posts)
                {
                    Debug.Log(post);
                }
            });
    }

    [ContextMenu(nameof(GetOne))]
    private void GetOne()
    {
        _pb.Collection("Posts")
            .GetOne("3g28fsld3arvpaz")
            .ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted && t.Exception?.InnerException is AggregateException ae)
                {
                    foreach (var e in ae.InnerExceptions)
                    {
                        Debug.LogError(e.Message);
                    }
                }

                Post post = Post.FromRecord(t.Result);
                var status = (int)t.Result["status"];
                var nested = t.Result["expand"]?["user"]?.ToObject<RecordModel>();
                var nested2 = t.Result["expand"]?["user"]?["title"]?.ToString();
                Debug.Log(post.Title);
            });
    }

    [ContextMenu(nameof(GetOneSimple))]
    private async void GetOneSimple()
    {
        try
        {
            var record = await _pb.Collection("examples").GetOne("hz4l808h4w32713", expand: "other");
            Debug.Log(record.Id);
            Debug.Log(record.CollectionId);
            Debug.Log(record.CollectionName);
            Debug.Log(record.GetStringValue("title"));
            Debug.Log(record.GetStringValue("expand.other.title"));

            Debug.Log(record);
        }
        catch (ClientException e)
        {
            Debug.LogException(e);
        }
    }
}