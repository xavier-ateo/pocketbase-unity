# PocketBase Unity SDK

Unofficial Multi-platform Unity C# SDK for interacting with the [PocketBase Web API](https://pocketbase.io/docs).

- [Supported Unity versions and platforms](#supported-unity-versions-and-platforms)
- [Installation](#installation)
- [Usage](#usage)
- [Caveats](#caveats)
  - [File upload](#file-upload)
  - [RecordModel](#recordmodel)
  - [Error handling](#error-handling)
  - [AuthStore](#authstore)
  - [Binding filter parameters](#binding-filter-parameters)
  - [Extension methods](#extension-methods)
- [Services](#services)
- [Development](#development)

## Supported Unity versions and platforms

This package runs on Unity **2022.3 or later**. It has been tested on the following platforms:

- Windows
- MacOS
- Linux
- Android
- iOS
- WebGL

## Supported PocketBase versions

Some versions of PocketBase may not be compatible with some versions of this SDK. Please check the following table:

| Unity SDK Version | PocketBase Version |
| ----------------- | ------------------ |
| 0.22.x            | 0.22.x             |
| Not yet released  | 0.23.x             |

## Installation

Open the *Package Manager* window, and click the *+* icon, then click on *Add package from git url*. Copy and paste the
following url and click *Add*:

```bash
https://github.com/Sov3rain/pocketbase-unity.git?path=/Assets/pocketbase-unity#0.22.1
```

This will tag the package with the version `0.22.1`.

You can also install the SDK by downloading the `.unitypackage` from the [releases page](https://github.com/Sov3rain/pocketbase-unity/releases) and importing it into your project.

## Usage

```csharp
using PocketBaseSdk;
using UnityEngine;

public class PocketBaseExample : MonoBehaviour
{
    private PocketBase pb;

    private async void Start()
    {
        _pocketBase = new PocketBase("http://127.0.0.1:8090");

        // Authenticate as regular user
        var userData = await pb.Collection("users").AuthWithPassword("user@example.com", "password");

        // List and filter "example" collection records
        var result = await pb.Collection("example").GetList<RecordModel>(
            page: 1,
            perPage: 20,
            filter: "status = true && created >= \"2022-08-01\"",
            sort: "-created",
            expand: "someRelField"
        );

        // Susbscribe to realtime "example" collection changes
        pb.Collection("example").Subscribe<RecordModel>("*", e =>
        {
            Debug.Log(e.Action); // "create", "update", "delete"
            Debug.Log(e.Record); // The changed record
        }, filter: "someField > 10");
    }
}
```

## Caveats

### File upload

PocketBase Unity SDK handles file upload seamlessly by using [
`IMultipartFormSection`](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Networking.IMultipartFormSection.html)
list.

Here is an example of uploading a single text file together with some other regular fields:

```csharp
using PocketBaseSdk;
using UnityEngine;

public class PocketBaseExample : MonoBehaviour
{
    private PocketBase _pocketBase;

    private async void Start()
    {
        pb= new PocketBase("http://127.0.0.1:8090");

        var record = await pb.Collection("example").Create<RecordModel>(
            body: new()
            {
                title = "Hello, World!"
            },
            files: new()
            {
                new MultipartFormFileSection(
                    name: "document", // The name of the file field
                    data: Encoding.UTF8.GetBytes("Hello, World!"), // The file data
                    fileName: "example_document.txt",
                    contentType: "text/plain")
            }
        );

        Debug.Log(record.Id);
    }
}
```

### RecordModel

The SDK comes with several helpers to make it easier working with the `RecordService` and `RecordModel` DTO. Below is an example on how to access and cast record data values with the `RecordModel[string]` indexer:

```csharp
var record = await pb.Collection("example").GetOne("RECORD_ID");


var options = record["options"]?.ToObject<List<string>>();
var email = (string)record["email"];
var status = (int)record["status"];
var price = (float)record["price"];
var nested1 = record["expand"]?["user"]?.ToObject<RecordModel>();
var nested2 = record["expand"]?["user"]?["title"]?.ToString() ?? "N/A";
```

Alternatively, you can also create your own typed DTO data classes and use a static factory method to populate your object, eg:

```csharp
using PocketBaseSdk;
using UnityEngine;

public class Post : RecordModel
{
    public string Title { get; set; }
    public string Content { get; set; }

    public static Post FromRecord(RecordModel record) => 
        JsonConvert.DeserializeObject<Post>(record.ToString());
}
```

And here is an example of how to use it:

```csharp
// Fetch your raw record
var record = await pb.Collection("posts").GetOne("POST_ID");

var post = Post.FromRecord(record);
```

### Error handling

All services return a standard Task object that can be awaited, so the error handling is pretty straightforward.

```csharp
// If you are using the async/await syntax:
try
{
    var userData = await pb.Collection("users").AuthWithPassword("user@example.com", "password");
}
catch (ClientException e)
{
    // Handle error
}

// Or if you are using the ContinueWithOnMainThread syntax:
pb.Collection("users").AuthWithPassword("user@example.com", "password").ContinueWithOnMainThread(task => 
{
    if (task.IsFaulted)
    {
        // Handle error
    }
    else if (task.IsCompleted)
    {
        var user = task.Result;
    }
});
```

All responses errors are wrapped in a `ClientException` object, which contains the following properties:

```csharp
public class ClientException : Exception
{
    public string Url { get; }
    public int StatusCode { get; }
    public Dictionary<string, object> Response { get; }
    public object OriginalError { get; }
}
```

### AuthStore

The SDK keeps track of the authenticated token and auth record for you via the `PocketBase.AuthStore` service. The
default AuthStore class has the following public properties:

```csharp
public class AuthStore
{
    public string Token { get; }
    public RecordModel Model { get; }
    public bool IsValid();
    public void Save(string newToken, RecordModel newModel);
    public void Clear();
}
```

To *"logout"* an authenticated record, you can just call `PocketBase.AuthStore.Clear()`.

To *"listen"* for changes to the AuthStore, you can subscribe to the `PocketBase.AuthStore.OnChange` event:

```csharp
pocketBase.AuthStore.OnChange.Subscribe(e =>
{
    Debug.Log(e.Token);
    Debug.Log(e.Model);
});
```

**The default `AuthStore` is NOT persistent!**

If you want to persist the `AuthStore`, you can inherit from the default store and pass a new custom instance as
constructor argument to the client.
To make is slightly more convenient, the SDK has a built-in `AsyncAuthStore` that you can combine with any async
persistent layer. Here is an example using Unity's `PlayerPrefs`:

```csharp
AsyncAuthStore store => new(
    save: data =>
    {
        UnityEngine.PlayerPrefs.SetString("pb_auth", data);
        return Task.CompletedTask; // Mandatory since SetString() is synchronous
    },
    initial: UnityEngine.PlayerPrefs.GetString("pb_auth", string.Empty)
);

var pb = new PocketBase(
    "http://127.0.0.1:8090",
    authStore: store
);
```

You can also use the `AsyncAuthStore.PlayerPrefs` static property, which will automatically save the AuthStore to the
PlayerPrefs:

```csharp
var pb = new PocketBase(
    "http://127.0.0.1:8090",
    authStore: AsyncAuthStore.PlayerPrefs
);
```

### Binding filter parameters

The SDK comes with a helper `PocketBase.Filter(expr, params)` method to generate a filter string with placeholder
parameters (`{paramName}`) populated from a `Dictionary<string, object>`.

```csharp
// the same as: "title ~ 'exa\\'mple' && created = '2023-10-18 18:20:00.123Z'"
var filter = PocketBase.Filter(
    "title ~ {:title} && created >= {:created}", 
    new Dictionary<string, object>
    {
        ["title"] = "exa'mple",
        ["created"] = DateTime.UtcNow
    }
);

var record = await pb.Collection("example").GetList<RecordModel>(filter: filter);
```

### Extension Methods

The SDK provides some helper methods to help with common tasks. 

One of them is the `ContinueWithOnMainThread` method, which allows you to run a continuation task on the main thread. This is useful when you need to update the UI from a background thread, as Unity does not allow you to do this on any other thread:

```csharp
private Text _title;

// This will run on the main thread
pb.Collection("users").AuthWithPassword("user@example.com", "password").ContinueWithOnMainThread(task => 
{
    if (task.IsFaulted)
    {
        // Handle error
    }
    else if (task.IsCompleted)
    {
        var user = task.Result;
        _title.text = user.Email; // Will throw an exception if called on a background thread
    }
});
```

## Services

See the [API documentation](https://pocketbase.io/docs/) for more information on the available services.
You can also check out the [Dart SDK](https://github.com/pocketbase/dart-sdk/tree/v0.18.1) documentation for more
information on the available methods, as this SDK is a port of the Dart SDK.

## Development

Clone the repository and open the project in Unity 2022.3. You can safely use any patch version of Unity 2022.3.

The SDK code is located in the `Assets/pocketbase-unity/` folder. This project uses the format of
the [Unity Package Manager](https://docs.unity3d.com/Manual/upm-ui-giturl.html) to distribute the SDK.

The only dependency is the [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/) package, which is included
in the Unity Package Manager. The rest of the code is written in fully managed C# for maximum platform compatibility.
