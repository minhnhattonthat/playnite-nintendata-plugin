using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nintendata
{
    public class NintendataProvider : OnDemandMetadataProvider
    {
        private readonly MetadataRequestOptions options;
        private readonly Nintendata plugin;
        private readonly IPlayniteAPI playniteApi;

        private NintendoEshopClient client;
        private NintendoGame game;
        private static readonly ILogger logger = LogManager.GetLogger();
        private List<MetadataField> availableFields;
        public override List<MetadataField> AvailableFields
        {
            get
            {
                if (availableFields == null)
                {
                    availableFields = GetAvailableFields();
                }

                return availableFields;
            }
        }

        public NintendataProvider(MetadataRequestOptions options, Nintendata plugin)
        {
            this.options = options;
            this.plugin = plugin;
            this.playniteApi = plugin.PlayniteApi;
            this.client = new NintendoEshopClient(options);
        }

        private List<MetadataField> GetAvailableFields()
        {
            if (this.game == null)
            {
                GetNintendoGameMetadata();
            }

            var fields = new List<MetadataField> { MetadataField.Links };

            fields.Add(MetadataField.Name);

            if (!string.IsNullOrEmpty(game.FullDescription))
            {
                fields.Add(MetadataField.Description);
            }

            if (game.ReleaseDate != null && game.ReleaseDate?.Year != null)
            {
                fields.Add(MetadataField.ReleaseDate);
            }

            if (game.Developers.Count > 0)
            {
                fields.Add(MetadataField.Developers);
            }

            if (game.Publishers.Count > 0)
            {
                fields.Add(MetadataField.Publishers);
            }

            if (game.Genres.Count > 0)
            {
                fields.Add(MetadataField.Genres);
            }

            if (game.Series.Count > 0)
            {
                fields.Add(MetadataField.Series);
            }

            if (game.Links.Count > 0)
            {
                fields.Add(MetadataField.Links);
            }

            if (game.Image != null && !string.IsNullOrEmpty(game.Image.Path))
            {
                fields.Add(MetadataField.CoverImage);
            }

            if (game.AgeRatings.Count > 0)
            {
                fields.Add(MetadataField.AgeRating);
            }

            return fields;
        }

        private void GetNintendoGameMetadata()
        {
            if (this.game != null)
            {
                return;
            }

            if (!options.IsBackgroundDownload)
            {
                logger.Debug("not background");
                var item = plugin.PlayniteApi.Dialogs.ChooseItemWithSearch(null, (a) =>
                {
                    return client.SearchGames(NormalizeSearchString(a));
                }, options.GameData.Name);

                if (item != null)
                {
                    this.game = (NintendoGame)item;
                }
                else
                {
                    this.game = new NintendoGame();
                    logger.Warn($"Cancelled search");
                }
            }
            else
            {
                try
                {
                    List<GenericItemOption> results = client.SearchGames(NormalizeSearchString(options.GameData.Name));

                    switch (results.Count)
                    {
                        case 0:
                            this.game = new NintendoGame();
                            break;
                        case 1:
                            this.game = (NintendoGame)results.First();
                            break;
                        default:
                            var words = SplitStringToWords(NormalizeSearchString(options.GameData.Name));
                            var nameFullyMatchedResult = results.FirstOrDefault(game => words.All(w => game.Name.ToLower().Contains(w)));
                            if (nameFullyMatchedResult != null)
                            {
                                this.game = (NintendoGame)nameFullyMatchedResult;
                            }
                            else
                            {
                                this.game = new NintendoGame();
                            }
                            break;
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e, "Failed to get Nintendo game metadata.");
                    this.game = new NintendoGame();
                }
            }

        }
        private string NormalizeSearchString(string search)
        {
            return new Regex(@"\[.*\]").Replace(search, "").Replace("-", " ").Replace(":", "").ToLower();
        }

        private string[] SplitStringToWords(string normalizedString)
        {
            return new Regex(@"\W").Split(normalizedString);
        }

        public override string GetName(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.Name))
            {
                return this.game.Title;
            }

            return base.GetName(args);
        }

        public override string GetDescription(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.Description))
            {
                return this.game.FullDescription;
            }

            return base.GetDescription(args);
        }

        public override ReleaseDate? GetReleaseDate(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.ReleaseDate))
            {
                return this.game.ReleaseDate;
            }

            return base.GetReleaseDate(args);
        }

        public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.Publishers))
            {
                return this.game.Publishers;
            }

            return base.GetPublishers(args);
        }

        public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.Developers))
            {
                return this.game.Developers;
            }

            return base.GetDevelopers(args);
        }

        public override IEnumerable<MetadataProperty> GetGenres(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.Genres))
            {
                return this.game.Genres;
            }

            return base.GetGenres(args);
        }

        public override IEnumerable<MetadataProperty> GetSeries(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.Series))
            {
                return this.game.Series;
            }

            return base.GetSeries(args);
        }

        public override IEnumerable<MetadataProperty> GetAgeRatings(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.AgeRating))
            {
                return this.game.AgeRatings;
            }
            return base.GetAgeRatings(args);
        }

        public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.Links))
            {
                return this.game.Links;
            }

            return base.GetLinks(args);
        }

        public override MetadataFile GetCoverImage(GetMetadataFieldArgs args)
        {
            if (AvailableFields.Contains(MetadataField.CoverImage))
            {
                return this.game.Image;
            }
            return base.GetCoverImage(args);
        }
    }
}