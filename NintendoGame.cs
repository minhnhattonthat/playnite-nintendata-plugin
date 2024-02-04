using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NintendoMetadata
{
    public class NintendoGame : GenericItemOption
    {

        public string Title { get; set; }

        public string FullDescription { get; set; }

        public ReleaseDate? ReleaseDate { get; set; }

        public List<MetadataProperty> Developers { get; set; }

        public List<MetadataProperty> Publishers { get; set; }

        public List<MetadataProperty> Genres { get; set; }

        public List<MetadataProperty> Series { get; set; }

        public List<MetadataProperty> AgeRatings { get; set; }

        public List<Link> Links { get; set; }

        public MetadataFile Image { get; set; }

        public MetadataFile LandscapeImage { get; set; }

        public Game LibraryGame;

        public NintendoGame()
        {
            this.Developers = new List<MetadataProperty>();
            this.Publishers = new List<MetadataProperty>();
            this.Genres = new List<MetadataProperty>();
            this.Series = new List<MetadataProperty>();
            this.AgeRatings = new List<MetadataProperty>();
            this.Links = new List<Link>();
        }

        public static NintendoGame ParseUsGame(JObject data)
        {
            var result = new NintendoGame
            {
                Title = ((string)data["title"]).Replace("™", ""),
                FullDescription = (string)data["description"],
                ReleaseDate = new ReleaseDate((DateTime)data["releaseDate"]),
            };

            var developer = (string)data["softwareDeveloper"];

            if (!string.IsNullOrEmpty(developer))
            {
                var developers = developer.Split(',').Select(i => i.Trim());
                foreach(var d in developers)
                {
                    result.Developers.Add(new MetadataNameProperty(d));
                }
            }

            result.Publishers.Add(new MetadataNameProperty((string)data["softwarePublisher"]));

            IList<string> genreList = data["genres"]?.Select(v => (string)v)?.ToList() ?? new List<string>();
            foreach (string genre in genreList)
            {
                result.Genres.Add(new MetadataNameProperty(genre));
            }

            foreach (dynamic franchise in data["franchises"])
            {
                result.Series.Add(new MetadataNameProperty((string)franchise));
            }

            var esrbRating = $"ESRB {(string)data["esrbRating"]}";
            result.AgeRatings.Add(new MetadataNameProperty(esrbRating));

            result.Links.Add(new Link("My Nintendo Store", $"https://www.nintendo.com{(string)data["url"]}"));

            var imageUrl = $"https://assets.nintendo.com/image/upload/ar_16:9,b_auto:border,c_lpad/b_white/f_auto/q_auto/dpr_1/c_scale,w_800/{(string)data["productImage"]}";
            result.Image = new MetadataFile(imageUrl);

            var landscapeImageUrl = $"https://assets.nintendo.com/image/upload/ar_16:9,b_auto:border,c_lpad/b_white/f_auto/q_auto/dpr_1/c_scale,w_1920/{(string)data["productImage"]}";
            result.LandscapeImage = new MetadataFile(landscapeImageUrl);

            result.Name = result.Title;
            result.Description = $"{result.ReleaseDate?.Year}-{result.ReleaseDate?.Month}-{result.ReleaseDate?.Day} | {result.Publishers.First()}";

            return result;
        }

        public static NintendoGame ParseUkGame(JObject data)
        {
            var result = new NintendoGame
            {
                Title = (string)data["title"],
                FullDescription = (string)data["product_catalog_description_s"],
                ReleaseDate = new ReleaseDate((DateTime)data["dates_released_dts"][0]),
            };
            
            var developer = (string)data["softwareDeveloper"];
            if (!string.IsNullOrEmpty(developer))
            {
                result.Developers.Add(new MetadataNameProperty(developer));
            }
            
            result.Publishers.Add(new MetadataNameProperty((string)data["publisher"]));

            IList<string> genreList = data["pretty_game_categories_txt"]?.Select(v => (string)v)?.ToList() ?? new List<string>();
            foreach (string genre in genreList)
            {
                result.Genres.Add(new MetadataNameProperty(genre));
            }

            result.AgeRatings.Add(new MetadataNameProperty((string)data["pretty_agerating_s"]));

            result.Links.Add(new Link("My Nintendo Store", $"https://www.nintendo.co.uk/{(string)data["url"]}"));
            
            result.Image = new MetadataFile((string)data["image_url_sq_s"]);
            
            result.LandscapeImage = new MetadataFile(((string)data["image_url_h2x1_s"]).Replace("500w", "1600w"));

            result.Name = result.Title;
            result.Description = $"{result.ReleaseDate?.Year}-{result.ReleaseDate?.Month}-{result.ReleaseDate?.Day} | {result.Publishers.First()}";
            
            return result;
        }

        public static NintendoGame ParseJapanGame(JObject data)
        {
            var result = new NintendoGame
            {
                Title = (string)data["title"],
                FullDescription = (string)data["text"],
                ReleaseDate = new ReleaseDate((DateTime)data["dsdate"]),
            };
            
            result.Publishers.Add(new MetadataNameProperty((string)data["maker"]));

            IList<string> genreList = data["genre"]?.Select(v=> (string)v)?.ToList() ?? new List<string>();
            foreach (string genre in genreList)
            {
                JapanGenreMap.TryGetValue(genre, out string g);
                result.Genres.Add(new MetadataNameProperty(g ?? genre));
            }
            
            result.Links.Add(new Link("My Nintendo Store", (string)data["url"]));

            // 1920x1080 $"https://img-eshop.cdn.nintendo.net/i/{data["iurl"]}.jpg"
            // sw $"https://store-jp.nintendo.com/dw/image/v2/BFGJ_PRD/on/demandware.static/-/Sites-all-master-catalog/ja_JP/dwe8af036b/products/D{(string)data["nsuid"]}/heroBanner/{(string)data["iurl"]}.jpg?sw=1024&strip=false"
            var imageUrl = $"https://store-jp.nintendo.com/dw/image/v2/BFGJ_PRD/on/demandware.static/-/Sites-all-master-catalog/ja_JP/dwe8af036b/products/D{(string)data["nsuid"]}/squareHeroBanner/{(string)data["siurl"]}.jpg?sw=512&strip=false";
            result.Image = new MetadataFile(imageUrl);

            var landscapeImageUrl = $"https://img-eshop.cdn.nintendo.net/i/{data["iurl"]}.jpg";
            result.LandscapeImage = new MetadataFile(landscapeImageUrl);

            result.Name = result.Title;
            result.Description = $"{result.ReleaseDate?.Year}-{result.ReleaseDate?.Month}-{result.ReleaseDate?.Day} | {result.Publishers.First()}";
            
            return result;
        }

        public static Dictionary<string, string> JapanGenreMap = new Dictionary<string, string>()
        {
            { "アクション", "Action" },
            { "アドベンチャー", "Adventure" },
            { "アーケード", "Arcade" },
            { "格闘", "Fighting" },
            { "音楽", "Music" },
            { "パーティー", "Party" },
            { "パズル", "Puzzle" },
            { "レース", "Race" },
            { "ロールプレイング", "Role-playing (RPG)"},
            { "シューティング", "Shooting" },
            { "シミュレーション", "Simulation" },
            { "スポーツ", "Sports" },
            { "ストラテジー", "Strategy" },
            { "学習", "Study" },
            { "テーブル", "Table" },
            { "トレーニング", "Training" },
            { "実用", "Utility" },
            { "その他", "Other" },
        };
    }
}
