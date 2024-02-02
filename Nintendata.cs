using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Nintendata
{
    public class Nintendata : MetadataPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private NintendataSettingsViewModel settings { get; set; }

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
        };

        public override string Name => "Nintendata";

        public Nintendata(IPlayniteAPI api) : base(api)
        {
            settings = new NintendataSettingsViewModel(this);
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new NintendataProvider(options, this);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new NintendataSettingsView();
        }
    }
}