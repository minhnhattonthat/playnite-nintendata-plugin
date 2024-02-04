using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace NintendoMetadata
{
    public class NintendoMetadata : MetadataPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private NintendoMetadataSettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("5213fe24-bc90-4578-ae00-0039fac67ed8");

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.Name,
            MetadataField.Description,
            MetadataField.ReleaseDate,
            MetadataField.Developers,
            MetadataField.Publishers,
            MetadataField.Genres,
            MetadataField.Series,
            MetadataField.AgeRating,
            MetadataField.Links,
            MetadataField.CoverImage,
            MetadataField.BackgroundImage,
        };

        public override string Name => "Nintendo";

        public NintendoMetadata(IPlayniteAPI api) : base(api)
        {
            settings = new NintendoMetadataSettingsViewModel(this);
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new NintendoMetadataProvider(options, this);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new NintendoMetadataSettingsView();
        }
    }
}