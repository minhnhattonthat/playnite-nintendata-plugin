﻿using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NintendoMetadata
{
    public class NintendoMetadataSettings : ObservableObject
    {
        private StoreRegion storeRegion = StoreRegion.US;

        public StoreRegion StoreRegion { get => storeRegion; set => SetValue(ref storeRegion, value); }

    }

    public class NintendoMetadataSettingsViewModel : ObservableObject, ISettings
    {
        private readonly NintendoMetadata plugin;
        private NintendoMetadataSettings editingClone { get; set; }

        private NintendoMetadataSettings settings;
        public NintendoMetadataSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public NintendoMetadataSettingsViewModel(NintendoMetadata plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<NintendoMetadataSettings>();

            // LoadPluginSettings returns null if no saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new NintendoMetadataSettings();
            }
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = editingClone;
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }
    }

    public enum StoreRegion
    {
        [Description("North America")]
        US,
        [Description("Europe")]
        UK,
        [Description("Japan")]
        Japan,
    }
}