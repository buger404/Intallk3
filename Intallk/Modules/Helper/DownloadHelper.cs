using RestSharp;

namespace Intallk.Modules;

public static class DownloadHelper
{
    public static async void DownLoad(this string url, string path)
    {
        byte[]? data = await new RestClient().DownloadDataAsync(new RestRequest(url, Method.Get));
        if (data != null) File.WriteAllBytes(path, data!); else throw new Exception("下载失败。");
    }
}
