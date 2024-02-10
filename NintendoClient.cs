using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NintendoMetadata
{
    interface INintendoClient
    {
        List<NintendoGame> SearchGames(string name);

        NintendoGame GetGameDetails(NintendoGame game);
    }

    public abstract class NintendoClient: INintendoClient, IDisposable
    {
        protected readonly ILogger logger = LogManager.GetLogger();

        protected RestClient restClient;

        protected MetadataRequestOptions options;

        protected NintendoMetadataSettings settings;

        protected abstract string BaseUrl { get; }

        public NintendoClient(MetadataRequestOptions options, NintendoMetadataSettings settings)
        {
            this.settings = settings;
            InitRestClient();
            this.options = options;
        }

        public abstract List<NintendoGame> SearchGames(string normalizedSearchName);

        public virtual NintendoGame GetGameDetails(NintendoGame game)
        {
            return game;
        }

        public virtual void Dispose()
        {
            restClient?.Dispose();
        }

        protected virtual void InitRestClient()
        {
            restClient = new RestClient(BaseUrl);
        }

        protected JObject ExecuteRequest(RestRequest request)
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

        // https://en.wikibooks.org/wiki/Algorithm_Implementation/Strings/Levenshtein_distance#C.23
        protected int NameStringCompare(string a, string b)
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

    public static class NintendoClientExtensions
    {
        public static string[] SplitToWords(this string input)
        {
            return new Regex(@"(?!\\.)\W").Split(input);
        }

        public static string NormalizeGameName(this string input)
        {
            // to lower case
            string output = input.ToLower();

            // remove all accents
            var bytes = Encoding.GetEncoding("Cyrillic").GetBytes(output);
            output = Encoding.ASCII.GetString(bytes);
            
            // remove invalid chars           
            output = Regex.Replace(output, @"[^a-z0-9\s-]", "");

            // convert multiple spaces into one space   
            output = Regex.Replace(output, @"\s+", " ").Trim();
            
            return output;
        }

        public static List<NintendoGame> OrderByRelevant(this List<NintendoGame> list, string normalizedSearchName)
        {
            var words = new Regex(@"(?!\\.)\W").Split(normalizedSearchName);
            string regex = string.Join("|", words);
            return list
                .Select(game => new
                {
                    MatchCount = Regex.Matches(game.Name.NormalizeGameName(), regex).Count,
                    Game = game,
                })
                .Where(item => item.MatchCount > 0)
                .OrderByDescending(item => item.MatchCount)
                .Select(item => item.Game)
                .ToList();
        }
    }
}
