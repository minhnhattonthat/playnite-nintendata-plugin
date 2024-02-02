using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nintendata
{
    public class NintendoEshopClient
    {
        private readonly ILogger logger = LogManager.GetLogger();
        /** Algolia Key for getting US games */
        private const string UsAlgoliaId = "U3B6GR4UA3";
        private const string UsAlgoliaKey = "a29c6927638bfd8cee23993e51e721c9";
        private readonly string baseUrl = $"https://{UsAlgoliaId}-2.algolia.net/1/indexes/*/queries";

        private RestClient client;
        protected MetadataRequestOptions options;

        public NintendoEshopClient(MetadataRequestOptions options)
        {
            client = new RestClient(baseUrl);
            client.UserAgent = "Playnite";
            this.options = options;
        }

        public JObject ExecuteRequest(RestRequest request)
        {
            var fullUrl = client.BuildUri(request);
            logger.Info(fullUrl.ToString());

            var response = client.Execute(request);

            if (response.ErrorException != null)
            {
                const string message = "Error retrieving response.  Check inner details for more info.";
                var e = new Exception(message, response.ErrorException);
                throw e;
            }
            var content = response.Content;

            return JObject.Parse(content);
        }

        public List<GenericItemOption> SearchGames(string normalizedSearchName)
        {
            List<GenericItemOption> results = new List<GenericItemOption>();
            logger.Info(normalizedSearchName);

            var request = new RestRequest("/", Method.POST);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Accept", "*/*");
            request.AddHeader("X-Algolia-API-Key", UsAlgoliaKey);
            request.AddHeader("X-Algolia-Application-Id", UsAlgoliaId);

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
                    NintendoGame g = new NintendoGame(game);
                    results.Add(g);
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error performing search");
            }

            return results.OrderBy(game => NameStringCompare(normalizedSearchName, game.Name)).ToList();
        }

        // https://en.wikibooks.org/wiki/Algorithm_Implementation/Strings/Levenshtein_distance#C.23
        private int NameStringCompare(string a, string b)
        {

            if (string.IsNullOrEmpty(a))
            {
                if (!string.IsNullOrEmpty(b))
                {
                    return b.Length;
                }
                return 0;
            }

            if (string.IsNullOrEmpty(b))
            {
                if (!string.IsNullOrEmpty(a))
                {
                    return a.Length;
                }
                return 0;
            }

            int cost;
            int[,] d = new int[a.Length + 1, b.Length + 1];
            int min1;
            int min2;
            int min3;

            for (int i = 0; i <= d.GetUpperBound(0); i += 1)
            {
                d[i, 0] = i;
            }

            for (int i = 0; i <= d.GetUpperBound(1); i += 1)
            {
                d[0, i] = i;
            }

            for (int i = 1; i <= d.GetUpperBound(0); i += 1)
            {
                for (int j = 1; j <= d.GetUpperBound(1); j += 1)
                {
                    cost = (a[i - 1] != b[j - 1]) ? 1 : 0;

                    min1 = d[i - 1, j] + 1;
                    min2 = d[i, j - 1] + 1;
                    min3 = d[i - 1, j - 1] + cost;
                    d[i, j] = Math.Min(Math.Min(min1, min2), min3);
                }
            }

            return d[d.GetUpperBound(0), d.GetUpperBound(1)];
        }
    }
}
