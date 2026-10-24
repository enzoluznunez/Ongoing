using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

// Everything the app reads ships in StreamingAssets: the map, the sheets and
// the Gemini key. Nothing is asked of a server.
public static class StreamingFiles {

    // A file shipped in StreamingAssets, as a URL a web request can read: on
    // Android it already is one, elsewhere it is a path on disk. Each segment
    // of 'fileName' is escaped, so a sheet named for a metro with spaces or
    // punctuation in it still reads.
    public static string Url(string fileName) {
        string escaped = string.Join("/", fileName.Split('/').Select(Uri.EscapeDataString));
        string path = Application.streamingAssetsPath.TrimEnd('/') + "/" + escaped;
        return path.Contains("://") ? path : "file://" + path;
    }

    // A StreamingAssets file's whole text, or null when it cannot be read.
    // Started on the main thread: UnityWebRequest is Unity's.
    public static Task<string> ReadText(string fileName) {
        var done = new TaskCompletionSource<string>();
        var req = UnityWebRequest.Get(Url(fileName));
        req.SendWebRequest().completed += _ => {
            done.SetResult(req.result == UnityWebRequest.Result.Success ? req.downloadHandler.text : null);
            req.Dispose();
        };
        return done.Task;
    }

    // The one line a StreamingAssets file holds, trimmed: configuration such
    // as a key. Null when the file cannot be read.
    public static async Task<string> ReadLine(string fileName) => (await ReadText(fileName))?.Trim();
}
