using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NintendoMetadata
{
    public class NintendoClient
    {
        private readonly ILogger logger = LogManager.GetLogger();
        
        private const string UsAlgoliaId = "U3B6GR4UA3";
        private const string UsAlgoliaKey = "a29c6927638bfd8cee23993e51e721c9";
        private readonly string UsBaseUrl = $"https://{UsAlgoliaId}-2.algolia.net/1/indexes/*/queries";

        private readonly string UkBaseUrl = "http://search.nintendo-europe.com/en/select";

        private readonly string JapanBaseUrl = "https://search.nintendo.jp/nintendo_soft/search.json";

        private RestClient restClient;
        protected MetadataRequestOptions options;

        private StoreRegion storeRegion;
        private NintendoMetadataSettings settings;

        public StoreRegion StoreRegion
        {
            get { return storeRegion; }
            set
            {
                restClient?.Dispose();
                switch (value)
                {
                    case StoreRegion.US:
                        restClient = new RestClient(UsBaseUrl)
                            .AddDefaultHeader("Content-Type", "application/json")
                            .AddDefaultHeader("Accept", "*/*")
                            .AddDefaultHeader("X-Algolia-API-Key", UsAlgoliaKey)
                            .AddDefaultHeader("X-Algolia-Application-Id", UsAlgoliaId);
                        break;
                    case StoreRegion.UK:
                        restClient = new RestClient(UkBaseUrl);
                        break;
                    case StoreRegion.Japan:
                        restClient = new RestClient(JapanBaseUrl);
                        break;
                }
                storeRegion = value;
            }
        }

        public NintendoClient(MetadataRequestOptions options, NintendoMetadataSettings settings)
        {
            this.settings = settings;
            StoreRegion = settings.StoreRegion;
            this.options = options;
        }

        private JObject ExecuteRequest(RestRequest request)
        {
            var fullUrl = restClient.BuildUri(request);
            logger.Info(fullUrl.ToString());

            var response = restClient.Execute(request);

            if (response.ErrorException != null)
            {
                const string message = "Error retrieving response.  Check inner details for more info.";
                var e = new Exception(message, response.ErrorException);
                throw e;
            }
            var content = response.Content;

            return JObject.Parse(content);
        }

        public List<GenericItemOption> SearchUsGames(string normalizedSearchName)
        {
            List<GenericItemOption> results = new List<GenericItemOption>();

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

        private List<GenericItemOption> SearchUkGames(string normalizedSearchName)
        {
            List<GenericItemOption> results = new List<GenericItemOption>();

            var request = new RestRequest("/", Method.Get)
                .AddQueryParameter("q", normalizedSearchName)
                .AddQueryParameter("fq", "type:GAME AND system_type:nintendoswitch*")
                .AddQueryParameter("sort", "score desc, date_from desc")
                .AddQueryParameter("start", 0)
                .AddQueryParameter("rows", 24)
                .AddQueryParameter("wt", "json");

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

            return results.OrderBy(game => NameStringCompare(normalizedSearchName, game.Name)).ToList();
        }

        private List<GenericItemOption> SearchJapanGames(string normalizedSearchName)
        {
            List<GenericItemOption> results = new List<GenericItemOption>();

            var request = new RestRequest("/", Method.Get)
                .AddQueryParameter("q", normalizedSearchName)
                .AddQueryParameter("limit", 24)
                .AddQueryParameter("page", 1)
                .AddQueryParameter("sort", "hards asc,score,titlek asc")
                .AddQueryParameter("fq", "hard_s:1_HAC AND (sform_s:HAC_DL OR sform_s:HAC_DOWNLOADABLE)")
                .AddQueryParameter("spt", "B");

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

                foreach (JObject game in items)
                {
                    NintendoGame g = NintendoGame.ParseJapanGame(game);
                    results.Add(g);
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error performing search");
            }

            return results.OrderBy(game => NameStringCompare(normalizedSearchName, game.Name)).ToList();
        }

        public List<GenericItemOption> SearchGames(string normalizedSearchName)
        {
            logger.Info(normalizedSearchName);

            switch (storeRegion)
            {
                case StoreRegion.US:
                    return SearchUsGames(normalizedSearchName);
                case StoreRegion.UK:
                    return SearchUkGames(normalizedSearchName);
                case StoreRegion.Japan:
                    return SearchJapanGames(normalizedSearchName);
                default:
                    return SearchUsGames(normalizedSearchName);
            }
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
