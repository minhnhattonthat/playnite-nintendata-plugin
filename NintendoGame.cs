using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nintendata
{
    public class NintendoGame : GenericItemOption
    {
        private readonly ILogger logger = LogManager.GetLogger();

        private readonly string title;
        public string Title { get { return title; } }

        private readonly string fullDescription;
        public string FullDescription { get { return fullDescription; } }

        private ReleaseDate? releaseDate;
        public ReleaseDate? ReleaseDate { get { return releaseDate; } }

        private readonly List<MetadataProperty> developers;
        public List<MetadataProperty> Developers { get { return developers; } }

        private readonly List<MetadataProperty> publishers;
        public List<MetadataProperty> Publishers { get { return publishers; } }

        private readonly List<MetadataProperty> genres;
        public List<MetadataProperty> Genres { get { return genres; } }

        private readonly List<MetadataProperty> series;
        public List<MetadataProperty> Series { get { return series; } }

        private readonly List<MetadataProperty> ageRatings;
        public List<MetadataProperty> AgeRatings { get { return ageRatings; } }

        private readonly List<Link> links;
        public List<Link> Links { get { return links; } }

        private readonly MetadataFile image;
        public MetadataFile Image { get { return image; } }

        public Game LibraryGame;

        public NintendoGame()
        {
            this.developers = new List<MetadataProperty>();
            this.publishers = new List<MetadataProperty>();
            this.genres = new List<MetadataProperty>();
            this.series = new List<MetadataProperty>();
            this.ageRatings = new List<MetadataProperty>();
            this.links = new List<Link>();
        }

        public NintendoGame(JObject data) : this()
        {
            logger.Info($"JOBject {data}");

            this.title = NormalizeTitle((string)data["title"]);

            this.fullDescription = (string)data["description"];

            var releaseDateTime = (DateTime)data["releaseDate"];
            this.releaseDate = new ReleaseDate(releaseDateTime);

            var dev = (string)data["softwareDeveloper"];

            if (!string.IsNullOrEmpty(dev))
            {
                this.developers.Add(new MetadataNameProperty(dev));
            }

            this.publishers.Add(new MetadataNameProperty((string)data["softwarePublisher"]));

            foreach (dynamic genre in data["genres"])
            {
                this.genres.Add(new MetadataNameProperty((string)genre));
            }

            foreach (dynamic franchise in data["franchises"])
            {
                this.series.Add(new MetadataNameProperty((string)franchise));
            }

            var esrbRating = $"ESRB {(string)data["esrbRating"]}";
            this.ageRatings.Add(new MetadataNameProperty(esrbRating));

            this.links.Add(new Link("My Nintendo Store", $"https://www.nintendo.com{(string)data["url"]}"));

            var imageUrl = $"https://assets.nintendo.com/image/upload/ar_16:9,b_auto:border,c_lpad/b_white/f_auto/q_auto/dpr_1.5/c_scale,w_700/{(string)data["productImage"]}";
            this.image = new MetadataFile(imageUrl);

            this.Name = this.title;
            this.Description = $"{releaseDateTime.Year}-{releaseDateTime.Month}-{releaseDateTime.Day} | {publishers.First()}";
        }

        private string NormalizeTitle(string title)
        {
            return title.Replace("™", "");
        }

    }
}
