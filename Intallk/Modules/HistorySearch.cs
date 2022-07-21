using Intallk.Config;

using OneBot.CommandRoute.Attributes;
using OneBot.CommandRoute.Services;

using RestSharp;
using Sora.Entities;
using Sora.Entities.Segment;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Intallk.Modules;

// Powered by Eric🌹🌹🌹
class HistorySearch : IOneBotController
{
    public static string? token,host,api;
    public HistorySearch()
    {
        /**JObject j = JObject.Parse(File.ReadAllText("C:\\Intallk\\history_search.json"));
        token = j["token"]!.ToString();
        host = j["host"]!.ToString();
        api = j["api"]!.ToString();**/
    }
    public bool Search(GroupMessageEventArgs e,string filter,string pagesize = "200")
    {
        RestResponse response = new RestClient(host!).Execute(
                                    new RestRequest(api).AddParameter("filter", filter)
                                                        .AddParameter("page[size]", pagesize)
                                                        .AddHeader("token", token!)
                                                        .AddHeader("Accept", "application/vnd.api+json")
                                                             );
        if (!response.IsSuccessful || response.Content == null)
        {
            e.Reply("查询失败(" + response.StatusCode + ")");
            return false;
        }

        JObject j = JObject.Parse(response.Content!);
        
        return true;
    }

}

