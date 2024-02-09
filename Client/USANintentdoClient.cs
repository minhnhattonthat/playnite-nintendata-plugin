using Playnite.SDK.Plugins;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;
using Playnite.SDK.Models;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;

namespace NintendoMetadata.Client
{
    public class USANintendoClient : NintendoClient
    {
        private const string UsAlgoliaId = "U3B6GR4UA3";
        private const string UsAlgoliaKey = "a29c6927638bfd8cee23993e51e721c9";

        protected override string BaseUrl => $"https://{UsAlgoliaId}-2.algolia.net/1/indexes/*/queries";

        public USANintendoClient(MetadataRequestOptions options, NintendoMetadataSettings settings) : base(options, settings)
        {
        }

        protected override void InitRestClient()
        {
            base.InitRestClient();
            restClient
                    .AddDefaultHeader("Content-Type", "application/json")
                    .AddDefaultHeader("Accept", "*/*")
                    .AddDefaultHeader("X-Algolia-API-Key", UsAlgoliaKey)
                    .AddDefaultHeader("X-Algolia-Application-Id", UsAlgoliaId);
        }

        public override List<NintendoGame> SearchGames(string normalizedSearchName)
        {
            List<NintendoGame> results = new List<NintendoGame>();

            var request = new RestRequest("/", Method.Post);
            var body = $@"{{""requests"": [{{""indexName"": ""store_game_en_us"",""query"": ""{normalizedSearchName}"",""params"": ""hitsPerPage=10""}}]}}";
            request.RequestFormat = DataFormat.Json;
            request.AddBody(body);

            try
            {
                JObject response = ExecuteRequest(request);

                if (!response.TryGetValue("results", out JToken resultsData))
                {
                    logger.Error($"Encountered API error: [{response["status"]}] {response["message"]}");
                    return results;
                }

                var result = resultsData[0];
                logger.Debug($"SearchGames {result["nbHits"]} results for {normalizedSearchName}");

                foreach (dynamic game in result["hits"])
                {
                    NintendoGame g = NintendoGame.ParseUsGame(game);
                    results.Add(g);
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error performing search");
            }

            return results.OrderBy(game => NameStringCompare(normalizedSearchName, game.Name)).ToList();
        }

        public override NintendoGame GetGameDetails(NintendoGame game)
        {
            var link = game.Links.FirstOrDefault(l => l.Name == "My Nintendo Store");

            if (link == null)
            {
                return game;
            }

            var web = new HtmlWeb();
            var doc = web.Load(link.Url);
            var descriptionNode = doc.DocumentNode.SelectSingleNode(@"//div[@class='ProductDetailstyles__Grid-sc-4l5ex7-4 hKLOzA']//p");
            if (descriptionNode != null)
            {
                game.FullDescription = descriptionNode.InnerHtml;
                logger.Info(game.FullDescription);
            }
            else
            {
                var nextData = doc.DocumentNode.SelectSingleNode(@"//script[@id='__NEXT_DATA__']");
                if (nextData != null)
                {
                    var matches = Regex.Matches(nextData.InnerText, @"""description"":""(.*?)""");
                    if (matches.Count > 1 && matches[matches.Count - 1].Success)
                    {
                        string fullDescription = Regex.Unescape(matches[matches.Count - 1].Groups[1].Value);
                        fullDescription = fullDescription.Substring(3, fullDescription.Length - 7);
                        game.FullDescription = fullDescription;
                    }
                    //var json = JObject.Parse(nextData.InnerText);
                }
            }

            return game;
        }
    }
}
