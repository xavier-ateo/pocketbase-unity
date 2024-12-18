using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PocketBaseSdk;
using UnityEngine;

public class PocketBaseExamples : MonoBehaviour
{
    [SerializeField] private TextAsset _config;

    private string _pocketBaseUrl;
    private string _email;
    private string _password;

    private PocketBase _pb;

    private void Awake()
    {
        var config = JObject.Parse(_config.text);
        _email = config["email"]?.ToString();
        _password = config["password"]?.ToString();
        _pocketBaseUrl = config["pocketBaseUrl"]?.ToString();

        _pb = new PocketBase(_pocketBaseUrl, authStore: AsyncAuthStore.PlayerPrefs);
    }
    
    [ContextMenu(nameof(HealthCheck))]
    private void HealthCheck()
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
            .GetFullList<RecordModel>(10)
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

    [ContextMenu(nameof(GetPost))]
    private void GetPost()
    {
        _pb.Collection("Posts")
            .GetOne<RecordModel>("3g28fsld3arvpaza")
            .ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted && t.Exception?.InnerException is AggregateException ae)
                {
                    foreach (var e in ae.InnerExceptions)
                    {
                        Debug.LogError(e.Message);
                    }
                }

                var post = t.Result;
                Debug.Log(post["title"]);
            });
    }
}