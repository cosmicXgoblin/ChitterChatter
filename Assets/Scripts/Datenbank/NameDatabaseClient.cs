using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class NameDatabaseClient : MonoBehaviour
{
    [Header("API Base URL (no trailing slash)")]
    [SerializeField] private string baseUrl = "http://localhost/ChitterChatter_api";

    [Serializable]
    private class SaveRequest { public string name; }

    [Serializable]
    private class SaveResponse { public bool ok; public int id; public string error; }

    [Serializable]
    private class PlayerRow { public int id; public string name; public string created_at; }

    [Serializable]
    private class GetNamesResponse { public bool ok; public PlayerRow[] players; public string error; }

    public void SaveName(string playerName)
    {
        StartCoroutine(SaveNameCoroutine(playerName));
    }

    public void FetchNames(Action<string[]> onResult)
    {
        StartCoroutine(GetNamesCoroutine(onResult));
    }

    private IEnumerator SaveNameCoroutine(string playerName)
    {
        var url = $"{baseUrl}/save_name.php";

        var reqObj = new SaveRequest { name = playerName };
        string json = JsonUtility.ToJson(reqObj);
        byte[] body = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"SaveName failed: {req.error}");
            yield break;
        }

        var res = JsonUtility.FromJson<SaveResponse>(req.downloadHandler.text);
        if (res == null || !res.ok)
        {
            Debug.LogError($"SaveName server error: {(res != null ? res.error : "Invalid JSON")}");
            yield break;
        }

        Debug.Log($"Saved name '{playerName}' with id={res.id}");
    }

    private IEnumerator GetNamesCoroutine(Action<string[]> onResult)
    {
        var url = $"{baseUrl}/get_names.php";

        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"FetchNames failed: {req.error}");
            onResult?.Invoke(Array.Empty<string>());
            yield break;
        }

        var res = JsonUtility.FromJson<GetNamesResponse>(req.downloadHandler.text);
        if (res == null || !res.ok || res.players == null)
        {
            Debug.LogError($"FetchNames server error: {(res != null ? res.error : "Invalid JSON")}");
            onResult?.Invoke(Array.Empty<string>());
            yield break;
        }

        string[] names = new string[res.players.Length];
        for (int i = 0; i < res.players.Length; i++)
            names[i] = res.players[i].name;

        onResult?.Invoke(names);
    }
}