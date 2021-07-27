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
        const string version = "1.0.0";

        // The Artifact~
        private ArtifactDef artiWayfaring;

        // Stage List
        private List<SceneDef> sceneListing = new List<SceneDef>();
        private int currStageIndex = 0;

        public void Awake()
        {
            Assets.PopulateAssets();
            ContentPackProvider.Initialize();
            SetupArtifacts();
            SetupHooks();
            SetupLanguage();
        }

        private void SetupArtifacts()
        {
            artiWayfaring = Assets.mainAssetBundle.LoadAsset<ArtifactDef>("WayfarerDef");
        }

        private void SetupLanguage()
        {
            LanguageAPI.Add("WONDA_WAYFARING_NAME", "Artifact of Wayfaring");
            LanguageAPI.Add("WONDA_WAYFARING_DESC", "Travel through every stage.");
        }

        private void SetupHooks()
        {
            On.RoR2.Run.Start += Run_Start;
            On.RoR2.Run.PickNextStageScene += Run_PickNextStageScene;
        }

        private void Run_Start(On.RoR2.Run.orig_Start orig, Run self)
        {
            if (RunArtifactManager.instance.IsArtifactEnabled(artiWayfaring.artifactIndex))
            {
                Logger.LogInfo("Starting Run!");
                PopulateSceneList();
                Logger.LogInfo("Setting stages per loop!");
                Run.stagesPerLoop = sceneListing.Count;
                Logger.LogInfo("Setting next stage for first stage!");
                currStageIndex = 0;
            }
            orig(self);
        }

        private void Run_PickNextStageScene(On.RoR2.Run.orig_PickNextStageScene orig, Run self, SceneDef[] choices)
        {
            if (RunArtifactManager.instance.IsArtifactEnabled(artiWayfaring.artifactIndex))
            {
                Logger.LogInfo("Setting next stage!");
                if (currStageIndex > 0 && (currStageIndex + 1) % sceneListing.Count == 0) PopulateSceneList();
                if (SceneCatalog.allStageSceneDefs.Contains(SceneCatalog.GetSceneDefForCurrentScene())) currStageIndex++;
                Run.instance.nextStageScene = sceneListing[currStageIndex % (sceneListing.Count)];
                Logger.LogInfo("Current Loop Count: " + Run.instance.loopClearCount);
                return;
            }
            orig(self, choices);
        }

        private void PopulateSceneList()
        {
            Logger.LogInfo("Starting Populate!");
            System.Random r = new System.Random();
            sceneListing = SceneCatalog.allStageSceneDefs.OrderBy(x => r.Next()).OrderBy(wh => wh.stageOrder).ToList();
            sceneListing.Remove(sceneListing.Find(wh => wh.nameToken == SceneCatalog.GetSceneDefFromSceneName("moon2").nameToken));
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
