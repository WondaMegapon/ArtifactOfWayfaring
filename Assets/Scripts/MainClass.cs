using BepInEx;
using RoR2;
using RoR2.ContentManagement;
using R2API;
using R2API.Utils;
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Path = System.IO.Path;

namespace Wonda.ArtifactOfWayfaring
{
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(guid, modName, version)]
    [R2APISubmoduleDependency(new string[]
    {
        "LanguageAPI",
        "ArtifactAPI"
    })]
    public class MainClass : BaseUnityPlugin
    {
        // Setting up variables and waking the program up.
        //

        // Cool info B)
        const string guid = "com.Wonda.ArtifactOfWayfaring";
        const string modName = "ArtifactOfWayfaring";
        const string version = "1.0.3";

        // The Artifact~
        private ArtifactDef artiWayfaring;

        // Stage List
        private List<SceneDef> sceneListing = new List<SceneDef>();
        private int currStageIndex = 0;

        // Config stuff
        private WayfaringConfig _config;

        public void Awake()
        {
            _config = new WayfaringConfig(Config);
            Assets.PopulateAssets();
            ContentPackProvider.Initialize();
            SetupArtifacts();
            SetupHooks();
            SetupLanguage();
        }

        /// <summary>
        /// Prepares Artifact variables for the mod to interface with.
        /// </summary>
        private void SetupArtifacts()
        {
            artiWayfaring = Assets.mainAssetBundle.LoadAsset<ArtifactDef>("WayfarerDef");
        }

        /// <summary>
        /// Sets up any necessary LanguageAPI tokens.
        /// </summary>
        private void SetupLanguage()
        {
            LanguageAPI.Add("WONDA_WAYFARING_NAME", "Artifact of Wayfaring");
            LanguageAPI.Add("WONDA_WAYFARING_DESC", "Loops now consist of every possible stage.");
        }

        /// <summary>
        /// Adds hooks for the mod to leech off of.
        /// </summary>
        private void SetupHooks()
        {
            On.RoR2.Run.Start += Run_Start;
            On.RoR2.Run.PickNextStageScene += Run_PickNextStageScene;
            On.RoR2.Run.OnServerSceneChanged += Run_OnServerSceneChanged;
            On.RoR2.BazaarController.SetUpSeerStations += BazaarController_SetUpSeerStations;
        }        

        private void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            if (RunArtifactManager.instance.IsArtifactEnabled(artiWayfaring.artifactIndex))
            {
                Logger.LogInfo("Starting Run!");

                // Filling sceneListing with our new stages.
                PopulateSceneList();
                Logger.LogInfo("Setting stages per loop!");

                // Setting a loop to include all of the new stages.
                if(_config.AdjustLoopCount) Run.stagesPerLoop = SceneCatalog.allStageSceneDefs.Count() - 1;
                Logger.LogInfo("Setting next stage for first stage!");

                // Resetting the index, in-case this is a new run.
                currStageIndex = 0;
            }
            orig(self);
        }

        private void Run_PickNextStageScene(On.RoR2.Run.orig_PickNextStageScene orig, Run self, SceneDef[] choices)
        {
            if (RunArtifactManager.instance.IsArtifactEnabled(artiWayfaring.artifactIndex))
            {
                Logger.LogInfo("Setting next stage!");

                // Repopulating our scene list if this is the last stage.
                if (currStageIndex > 0 && (currStageIndex + 1) % sceneListing.Count == 0) PopulateSceneList();

                // Incrementing the stage index if the current scene is in our scene list.
                if (CurrentSceneMatchesCurrentStage()) currStageIndex++;

                // Setting the next stage.
                Run.instance.nextStageScene = sceneListing[currStageIndex % (sceneListing.Count)];
                return;
            }
            orig(self, choices);
        }

        private void Run_OnServerSceneChanged(On.RoR2.Run.orig_OnServerSceneChanged orig, Run self, string sceneName)
        {
            orig(self, sceneName);
            if (RunArtifactManager.instance.IsArtifactEnabled(artiWayfaring.artifactIndex))
            {
                Invoke("TestAndSetTeleporterlessStage", 2f);
            }
        }

        private void BazaarController_SetUpSeerStations(On.RoR2.BazaarController.orig_SetUpSeerStations orig, BazaarController self)
        {
            orig(self);
            if (RunArtifactManager.instance.IsArtifactEnabled(artiWayfaring.artifactIndex) && _config.RemoveSeerStations)
            {
                foreach (SeerStationController seerStationController in self.seerStations)
                {
                    // Removing the seer if it contains a stage in our loop.
                    if (sceneListing.Contains(SceneCatalog.GetSceneDef(seerStationController.NetworktargetSceneDefIndex))) seerStationController.GetComponent<PurchaseInteraction>().SetAvailable(false);
                }
            }
        }

        /// <summary>
        /// Updates sceneListing's contents. 
        /// </summary>
        private void PopulateSceneList()
        {
            Logger.LogInfo("Starting Populate!");

            // A random number to drive everything! (TODO: Replace this with Ror2's seed system?)
            System.Random r = new System.Random();

            // Grabbing the current stages in the game.
            sceneListing = SceneCatalog.allStageSceneDefs.ToList();

            // Adding in our additional stages.
            if (r.NextDouble() <= _config.GoldshoresSpawnChance) sceneListing.Add(PrepareSceneDef("goldshores", _config.GoldshoresStageOrder));
            if (r.NextDouble() <= _config.VoidfieldsSpawnChance) sceneListing.Add(PrepareSceneDef("arena", _config.VoidfieldsStageOrder));

            // Shuffling the list, then ordering the list by stage order.
            sceneListing = sceneListing.OrderBy(x => r.Next()).OrderBy(wh => wh.stageOrder).ToList();

            // Removing Commencement, as it'll cause conflicts.
            sceneListing.Remove(sceneListing.Find(wh => wh.nameToken == SceneCatalog.GetSceneDefFromSceneName("moon2").nameToken));

            foreach(var item in sceneListing)
            {
                Logger.LogInfo(item.nameToken + " chosen!");
            }
        }

        /// <summary>
        /// Runs PickNextStageScene if there is no teleporter on the current stage.
        /// </summary>
        private void TestAndSetTeleporterlessStage()
        {
            // If there's no teleporter to set the scene, and this scene *is* in our loop.
            if (CurrentSceneMatchesCurrentStage() && TeleporterInteraction.instance == null)
            {
                Logger.LogInfo("Scene has no stage! Picking next stage anyway.");
                // Picking the first scene in the catalog as our new scene. It doesn't really matter what goes in here.
                Run.instance.PickNextStageScene(new SceneDef[] { SceneCatalog.allStageSceneDefs.ToList().First() });
            }
        }

        /// <summary>
        /// Returns a SceneDef that has been re-ordered for Wayfaring's shuffle.
        /// </summary>
        /// <param name="name">The name of the scene.</param>
        /// <param name="stageOrder">The order in which the scene will appear.</param>
        /// <returns>A new SceneDef with the modified scene.</returns>
        private SceneDef PrepareSceneDef(string name, int stageOrder)
        {
            SceneDef sceneDef = SceneCatalog.GetSceneDefFromSceneName(name);
            if(sceneDef == null)
            {
                Logger.LogError("Null SceneDef! Returning Null!");
                return null;
            }
            sceneDef.stageOrder = stageOrder;
            return sceneDef;
        }

        /// <summary>
        /// Returns true if the current stage matches sceneListing's current stage.
        /// </summary>
        private bool CurrentSceneMatchesCurrentStage()
        {
            if(sceneListing[currStageIndex % sceneListing.Count] && SceneCatalog.GetSceneDefForCurrentScene())
                return sceneListing[currStageIndex % sceneListing.Count].cachedName == SceneCatalog.GetSceneDefForCurrentScene().cachedName;
            return false;
        }
    }

    public static class Assets
    {
        public static AssetBundle mainAssetBundle = null;
        //the filename of your assetbundle
        internal static string assetBundleName = "WayfarerAssets";

        public static void PopulateAssets()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            mainAssetBundle = AssetBundle.LoadFromFile(Path.Combine(path, assetBundleName));
            ContentPackProvider.serializedContentPack = mainAssetBundle.LoadAsset<SerializableContentPack>(ContentPackProvider.contentPackName);
        }
    }

    public class ContentPackProvider : IContentPackProvider
    {
        public static SerializableContentPack serializedContentPack;
        public static ContentPack contentPack;
        //Should be the same names as your SerializableContentPack in the asset bundle
        public static string contentPackName = "WayfarerContent";

        public string identifier
        {
            get
            {
                //If I see this name while loading a mod I will make fun of you
                return "ArtifactOfWayfaring";
            }
        }

        internal static void Initialize()
        {
            contentPack = serializedContentPack.CreateContentPack();
            ContentManager.collectContentPackProviders += AddCustomContent;
        }

        private static void AddCustomContent(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
        {
            addContentPackProvider(new ContentPackProvider());
        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(contentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }
    }
}
