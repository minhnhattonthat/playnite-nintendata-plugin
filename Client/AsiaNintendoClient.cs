using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Playnite.SDK.Models;
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
    public class AsiaNintendoClient : NintendoClient
    {
        protected override string BaseUrl => "https://www.nintendo.com/sg";

        public AsiaNintendoClient(MetadataRequestOptions options, NintendoMetadataSettings settings) : base(options, settings)
        {
        }

        public override List<NintendoGame> SearchGames(string normalizedSearchName)
        {
            List<NintendoGame> results = new List<NintendoGame>();

            var request = new RestRequest("/api/v1/games/all", Method.Get);

            try
            {
                JObject response = ExecuteRequest(request);

                if (!response.TryGetValue("result", out JToken resultsData))
                {
                    logger.Error($"Encountered API error: [{response["error"]["code"]}] {response["error"]["msg"]}");
                    return results;
                }

                var items = resultsData["items"];
                logger.Debug($"SearchGames {items.Count()} results for {normalizedSearchName}");

                foreach (JObject game in items.Cast<JObject>())
                {
                    NintendoGame g = NintendoGame.ParseAsiaGame(game);
                    results.Add(g);
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error performing search");
            }

            var words = new Regex(@"(?!\\.)\W").Split(normalizedSearchName);
            string regex = string.Join("|", words);
            return results
                .Where(game => Regex.IsMatch(game.Name.ToLower(), regex))
                .OrderByDescending(game => Regex.Matches(game.Name.ToLower(), regex).Count)
                .ToList();
        }

        public override NintendoGame GetGameDetails(NintendoGame game)
        {
            var link = game.Links.FirstOrDefault();
            
            if (link == null)
            {
                return game;
            }

            if (link.Url.Contains(game.NSUID))
            {
                var web = new HtmlWeb();
                var doc = web.Load(link.Url);
                var nextDataNode = doc.DocumentNode.SelectSingleNode(@"//script[@id='__NEXT_DATA__']");
                var data = JObject.Parse(nextDataNode.InnerText);
                var text = ((string)data["props"]?["pageProps"]?["post"]?["text"]) ?? "";
                game.FullDescription = Regex.Replace(text, @"\r\n?|\n", @"<br>");
                var genres = data["props"]?["pageProps"]?["post"]?["common"]?["genre"]?.Select(v => (string)v)?.ToList() ?? new List<string>();
                foreach (var genre in genres)
                {
                    game.Genres.Add(new MetadataNameProperty(genre));
                }
            }
            else
            {
                var web = new HtmlWeb
                {
                    OverrideEncoding = Encoding.UTF8
                };
                var doc = web.Load(link.Url);
                var descriptionNode = doc.DocumentNode.SelectSingleNode(@"//div[@class='overview-content']");
                if (descriptionNode != null)
                {
                    var descHtml= new Regex(@" class=""(.*?)""").Replace(descriptionNode.InnerHtml, "");
                    game.FullDescription = Regex.Replace(descHtml, @"\s{2,}", "");
                }
                var genreNode = doc.DocumentNode.SelectSingleNode(@"//dl[not(@id)][contains(@class,'detail-foot-outline-desc--flex')]//p[@class='detail-foot-outline-txt']");
                if (genreNode != null)
                {
                    var genres = genreNode.SelectNodes(@"span").Select(n => n.InnerText.Replace(",", "").Trim()).ToList();
                    foreach (var genre in genres)
                    {
                        game.Genres.Add(new MetadataNameProperty(genre));
                    }
                }
                else
                {
                    var regex = Regex.Match(link.Url, @"switch/(.*?)/index.html");
                    string icode;
                    if (regex.Success)
                    {
                        icode = regex.Groups[1].Value;
                    }
                    else
                    {
                        throw new Exception("No icode found");
                    }
                    var request = new RestRequest($"/api/v1/switch/{icode}", Method.Get);
                    try
                    {
                        JObject response = ExecuteRequest(request);

                        if (!response.TryGetValue("detail", out JToken detailData))
                        {
                            throw new Exception($"Encountered API error: game with code {icode} not found");
                        }

                        IList<string> genreList = detailData["common"]["genre"]?.Select(v => (string)v)?.ToList() ?? new List<string>();
                        foreach (string genre in genreList)
                        {
                            game.Genres.Add(new MetadataNameProperty(genre));
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error(e, "Error performing get game details");
                    }
                }
            }
            return game;
        }
    }
}
