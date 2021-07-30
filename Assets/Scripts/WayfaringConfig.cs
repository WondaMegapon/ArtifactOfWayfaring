using System;
using BepInEx;
using BepInEx.Configuration;

namespace Wonda.ArtifactOfWayfaring
{
    public class WayfaringConfig
    {
        // Private Vars
        private readonly ConfigEntry<bool> _adjustLoopCount;
        private readonly ConfigEntry<bool> _removeSeerStations;

        private readonly ConfigEntry<float> _goldshoresSpawnChance;
        private readonly ConfigEntry<int> _goldshoresStageOrder;

        private readonly ConfigEntry<float> _voidfieldsSpawnChance;
        private readonly ConfigEntry<int> _voidfieldsStageOrder;

        // Public Vars
        public bool AdjustLoopCount { get => _adjustLoopCount.Value; }
        public bool RemoveSeerStations { get => _removeSeerStations.Value; }

        public float GoldshoresSpawnChance { get => _goldshoresSpawnChance.Value; }
        public int GoldshoresStageOrder { get => _goldshoresStageOrder.Value; }

        public float VoidfieldsSpawnChance { get => _voidfieldsSpawnChance.Value; }
        public int VoidfieldsStageOrder { get => _voidfieldsStageOrder.Value; }

        // Constructor
        public WayfaringConfig(ConfigFile config) {
            _adjustLoopCount = config.Bind("Mechanics", "AdjustLoopCount", true, "Adjusts how many stages are considered a loop. (5 to 10, by default.)");
            _removeSeerStations = config.Bind("Mechanics", "RemoveSeerStations", true, "Removes the Bazaar's Seer stations, if they have a stage from the main loop.");

            _goldshoresSpawnChance = config.Bind("Intermission Stages", "GoldshoresSpawnChance", 0.01f, "Sets the chance for Gilded Coast to occur in the Wayfarer loop. (0 to disable, 1 to guarantee occurance.)");
            _goldshoresStageOrder = config.Bind("Intermission Stages", "GoldshoresStageOrder", 3, "Sets the pool that Gilded Coast will appear alongside.");

            _voidfieldsSpawnChance = config.Bind("Intermission Stages", "VoidfieldsSpawnChance", 0.01f, "Sets the chance for Void Fields to occur in the Wayfarer loop. (0 to disable, 1 to guarantee occurance.)");
            _voidfieldsStageOrder = config.Bind("Intermission Stages", "VoidfieldsStageOrder", 5, "Sets the pool that Void Fields will appear alongside.");
        }
    }
}
