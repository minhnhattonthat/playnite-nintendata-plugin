using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NintendoMetadata.Client
{
    public class JapanNintendoClient : NintendoClient
    {

        protected override string BaseUrl => "https://search.nintendo.jp/nintendo_soft/search.json";
        
        public JapanNintendoClient(MetadataRequestOptions options, NintendoMetadataSettings settings) : base(options, settings)
        {
        }

        public override List<NintendoGame> SearchGames(string normalizedSearchName)
        {
            List<NintendoGame> results = new List<NintendoGame>();

            var request = new RestRequest("/", Method.Get);
            var parameters = new
            {
                q = normalizedSearchName,
                limit = 24,
                page = 1,
                sort = "hards asc,score,titlek asc",
                fq = "hard_s:1_HAC AND (sform_s:HAC_DL OR sform_s:HAC_DOWNLOADABLE)",
                spt = "B",
            };
            request.AddObject(parameters);

            try
            {
                JObject response = ExecuteRequest(request);

                if (!response.TryGetValue("result", out JToken resultsData))
                {
                    logger.Error($"Encountered API error: [{response["error"]["code"]}] {response["error"]["msg"]}");
                    return results;
                }

                var items = resultsData["items"];
                logger.Debug($"SearchGames {resultsData["total"]} results for {normalizedSearchName}");

                foreach (JObject game in items.Cast<JObject>())
                {
                    NintendoGame g = NintendoGame.ParseJapanGame(game);
                    results.Add(g);
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error performing search");
            }

            return results.OrderBy(game => NintendoClientExtensions.NameStringCompare(normalizedSearchName, game.Name)).ToList();
        }

        public override NintendoGame GetGameDetails(NintendoGame game)
        {
            var link = game.Links.FirstOrDefault(l => l.Name == "My Nintendo Store");

            if (link == null)
            {
                return game;
            }

            var web = new HtmlWeb()
            {
                OverrideEncoding = Encoding.UTF8
            };
            var doc = web.Load(link.Url);
            var descriptionNode = doc.DocumentNode.SelectSingleNode(@"//div[@class='productDetail--catchphrase__longDescription']");
            if (descriptionNode != null)
            {
                game.FullDescription = Regex.Replace(descriptionNode.InnerHtml, @"\s{2,}", "");
                logger.Info(game.FullDescription);
            }

            return game;
        }
    }
}
