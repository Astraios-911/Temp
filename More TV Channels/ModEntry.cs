using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using static StardewValley.GameLocation;

namespace MoreTVChannels
{
    public class ModEntry : Mod
    {
        private Api? _api;
        public static ModEntry? Instance { get; private set; }
        public static IMonitor ModMonitor { get; private set; }

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            _api = new Api();
            ModMonitor = this.Monitor;

            // Apply Harmony patches
            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.Patch(
                AccessTools.Method(typeof(GameLocation), nameof(GameLocation.createQuestionDialogue),
                    new[] { typeof(string), typeof(Response[]), typeof(afterQuestionBehavior), typeof(NPC) }),
                prefix: new HarmonyMethod(typeof(TvPatches), nameof(TvPatches.CreateQuestionDialoguePrefix))
            );
            harmony.Patch(
                AccessTools.Method(typeof(TV), nameof(TV.checkForAction)),
                postfix: new HarmonyMethod(typeof(TvPatches), nameof(TvPatches.CheckForActionPostfix))
            );
            harmony.Patch(
                AccessTools.Method(typeof(TV), nameof(TV.turnOffTV)),
                postfix: new HarmonyMethod(typeof(TvPatches), nameof(TvPatches.TurnOffTVPostfix))
            );

            // Initialize asset handlers
            AssetHandler<CustomChannelData>.Init(Monitor, ModManifest.UniqueID, "CustomChannels");
            AssetHandler<OverlayData>.Init(Monitor, ModManifest.UniqueID, "OverlaySprite");
            AssetHandler<EditChannelData>.Init(Monitor, ModManifest.UniqueID, "EditChannels");

            // Register events
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.Content.AssetsInvalidated += AssetHandler<CustomChannelData>.OnAssetsInvalidated;
            helper.Events.Content.AssetsInvalidated += AssetHandler<OverlayData>.OnAssetsInvalidated;
            helper.Events.Content.AssetsInvalidated += AssetHandler<EditChannelData>.OnAssetsInvalidated;
        }

        public override object GetApi() => _api!;

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // Load empty dictionaries for each asset type
            var baseName = this.ModManifest.UniqueID;
            if (e.NameWithoutLocale.IsEquivalentTo($"{baseName}/CustomChannels"))
                e.LoadFrom(() => new Dictionary<string, CustomChannelData>(), AssetLoadPriority.Exclusive);

            if (e.NameWithoutLocale.IsEquivalentTo($"{baseName}/OverlaySprite"))
                e.LoadFrom(() => new Dictionary<string, OverlayData>(), AssetLoadPriority.Exclusive);

            if (e.NameWithoutLocale.IsEquivalentTo($"{baseName}/EditChannels"))
                e.LoadFrom(() => new Dictionary<string, EditChannelData>(), AssetLoadPriority.Exclusive);
        }
    }

    /// <summary>
    /// Generic asset handler to reduce code duplication.
    /// </summary>
    public static class AssetHandler<T> where T : class
    {
        private static Dictionary<string, T>? _cache;
        private static IMonitor? _monitor;
        private static string? _assetName;
        private static string? _assetType;

        public static void Init(IMonitor monitor, string modUniqueId, string assetType)
        {
            _monitor = monitor;
            _assetType = assetType;
            _assetName = $"{modUniqueId}/{assetType}";
            _monitor?.Log($"[{assetType}] Initialized", LogLevel.Trace);
        }

        public static Dictionary<string, T> Data
        {
            get
            {
                if (_cache == null)
                {
                    _monitor?.Log($"[{_assetType}] Loading asset: {_assetName}", LogLevel.Trace);
                    try
                    {
                        _cache = Game1.content.Load<Dictionary<string, T>>(_assetName!);
                        _cache ??= new Dictionary<string, T>();

                        _monitor?.Log($"[{_assetType}] Loaded {_cache.Count} entries", LogLevel.Info);

                        if (_cache.Count > 0)
                            foreach (var key in _cache.Keys)
                                _monitor?.Log($"[{_assetType}] Key: {key}", LogLevel.Trace);
                    }
                    catch (Exception ex)
                    {
                        _monitor?.Log($"[{_assetType}] Error loading: {ex}", LogLevel.Error);
                        _cache = new Dictionary<string, T>();
                    }
                }
                return _cache;
            }
        }

        public static void OnAssetsInvalidated(object? sender, AssetsInvalidatedEventArgs e)
        {
            foreach (var name in e.NamesWithoutLocale)
            {
                if (name.IsEquivalentTo(_assetName))
                {
                    _monitor?.Log($"[{_assetType}] Asset invalidated, reloading", LogLevel.Info);
                    _cache = null;
                }
            }
        }
    }
}