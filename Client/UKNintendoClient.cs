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
    public class UKNintendoClient : NintendoClient
    {

        protected override string BaseUrl => "http://search.nintendo-europe.com/en/select";

        public UKNintendoClient(MetadataRequestOptions options, NintendoMetadataSettings settings) : base(options, settings)
        {
        }

        public override List<NintendoGame> SearchGames(string normalizedSearchName)
        {
            List<NintendoGame> results = new List<NintendoGame>();
            var platform = options.GetPlatform();
            var playableOnTxt = "HAC";
            if (platform == NintendoPlatform.Nintendo3DS)
            {
                playableOnTxt = "CTR";
            }

            var request = new RestRequest("/");
            var parameters = new
            {
                q = normalizedSearchName,
                fq = $@"type:GAME AND playable_on_txt:""{playableOnTxt}""",
                sort = "score desc, date_from desc",
                start = 0,
                rows = 24,
                wt = "json",
            };
            request.AddObject(parameters);

            try
            {
                JObject response = ExecuteRequest(request);

                if (!response.TryGetValue("response", out JToken resultsData))
                {
                    logger.Error($"Encountered API error: [{response["error"]["code"]}] {response["error"]["msg"]}");
                    return results;
                }

                var docs = resultsData["docs"];
                logger.Debug($"SearchGames {resultsData["numFound"]} results for {normalizedSearchName}");

                foreach (dynamic game in docs)
                {
                    NintendoGame g = NintendoGame.ParseUkGame(game);
                    results.Add(g);
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error performing search");
            }

            return results.OrderByRelevance(normalizedSearchName);
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
            var fullDescription = "";
            var descriptionNodes = doc.DocumentNode.SelectNodes(@"//section[@id='Overview']//div[contains(@class, 'row-content')]/div");
            foreach (var descriptionNode in descriptionNodes)
            {
                var header = descriptionNode.SelectSingleNode(@".//h2");
                if (header != null) 
                {
                    var text = new Regex(@" class=""(.*?)""").Replace(header.OuterHtml, "");
                    fullDescription += text;
                    continue; 
                }
                var paragraphs = descriptionNode.SelectNodes(@".//p");
                if (paragraphs != null)
                {
                    foreach (var paragraph in paragraphs)
                    {
                        var text = Regex.Replace(paragraph.OuterHtml, @"\s{2,}", "");
                        text = new Regex(@" class=""(.*?)""").Replace(text, "");
                        fullDescription += text;
                    }
                }
            }
            if (fullDescription.StartsWith(@"<p>"))
            {
                fullDescription = fullDescription.Substring(3, fullDescription.Length - 7);
            }
            game.FullDescription = fullDescription;
                logger.Info(game.FullDescription);

            return game;
        }
    }
}
