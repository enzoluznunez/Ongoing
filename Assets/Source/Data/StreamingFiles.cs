using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class StreamingFiles {

    public static string Url(string fileName) {
        string escaped = string.Join("/", fileName.Split('/').Select(Uri.EscapeDataString));
        string path = Application.streamingAssetsPath.TrimEnd('/') + "/" + escaped;
        return path.Contains("://") ? path : "file://" + path;
    }

    public static Task<string> ReadText(string fileName) {
        var done = new TaskCompletionSource<string>();
        var req = UnityWebRequest.Get(Url(fileName));
        req.SendWebRequest().completed += _ => {
            done.SetResult(req.result == UnityWebRequest.Result.Success ? req.downloadHandler.text : null);
            req.Dispose();
        };
        return done.Task;
    }

    public static async Task<string> ReadLine(string fileName) => (await ReadText(fileName))?.Trim();
}
