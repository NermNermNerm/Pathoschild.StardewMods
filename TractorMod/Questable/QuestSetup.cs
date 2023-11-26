using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Pathoschild.Stardew.TractorMod.Framework;
using Pathoschild.Stardew.TractorMod.Framework.Config;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Tools;

namespace Pathoschild.Stardew.TractorMod.Questable
{
    public class QuestSetup
    {
        private readonly IReadOnlyCollection<BaseQuestController> QuestControllers;

        // Mirrored from ModEntry  IMO, this is how it should be declared there.  Doing it this way for least-intrusion.
        public const string GarageBuildingId = "Pathoschild.TractorMod_Stable";
        public const string PublicAssetBasePath = "Mods/Pathoschild.TractorMod";

        private ModEntry mod;

        public IModHelper Helper => this.mod.Helper;
        public IMonitor Monitor => this.mod.Monitor;

        public Harmony Harmony = null!;

        // TODO: See if we can get rid of this.
        public static QuestSetup Instance = null!;

        internal QuestSetup(ModEntry mod)
        {
            Instance = this;
            this.mod = mod;
            this.Harmony = new Harmony(mod.ModManifest.UniqueID);

            this.QuestControllers = new List<BaseQuestController> {
                new LoaderQuestController(this),
                new ScytheQuestController(this),
                new SeederQuestController(this),
                new WatererQuestController(this),
            };

            this.Helper.Events.GameLoop.OneSecondUpdateTicked += this.GameLoop_OneSecondUpdateTicked;
        }


        private void UpdateTractorModConfig()
        {
            this.mod.UpdateConfig();
        }

        private void GameLoop_OneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
        {
            var itemInHand = Game1.player?.CurrentItem;
            if (Game1.player is not null && Game1.player.currentLocation is not null && itemInHand is not null && Game1.player.currentLocation == Game1.getFarm()
                && Game1.player.currentLocation.buildings
                    .OfType<Stable>()
                    .Where(s => s.buildingType.Value == GarageBuildingId)
                    .Any(s => IsPlayerInGarage(Game1.player, s)))
            {
                foreach (var qc in this.QuestControllers.Where(qc => qc.WorkingAttachmentPartId == itemInHand.ItemId))
                {
                    qc.WorkingAttachmentBroughtToGarage();
                    this.UpdateTractorModConfig();
                }
            }
        }

        private static bool IsPlayerInGarage(Character c, Stable b)
        {
            Rectangle cPos = new Rectangle(new Point((int)c.Position.X, (int)c.Position.Y-128), new Point(64, 128));
            bool isIntersecting = b.intersects(cPos);
            return isIntersecting;
        }

        public void OnDayStarted(Stable? garage)
        {
            if (!Context.IsMainPlayer)
            {
                return;
            }

            foreach (var qc in this.QuestControllers)
            {
                qc.OnDayStarted();
            }

            RestoreTractorQuest.OnDayStarted(this.Helper, this.Monitor, garage);
            BorrowHarpoonQuest.OnDayStart(this);
            this.SetupMissingPartConversations();
        }

        private void SetupMissingPartConversations()
        {
            // Our stuff recurs every week for 4 days out of the week.  Delay until after the
            // first week so that the introductions quest runs to completion.  Perhaps it
            // would be better to delay until all the villagers we care about have been greeted.
            if (Game1.Date.DayOfWeek != DayOfWeek.Sunday || Game1.Date.TotalDays < 7)
            {
                return;
            }

            // A case could be made to having code that removes these conversation keys as
            // things get found, but maybe it'd be better to figure that it takes a while for
            // word to get around...  Although there might be some awkward dialogs with
            // townspeople directly involved in the quest.

            if (!RestoreTractorQuest.IsStarted)
            {
                Game1.player.activeDialogueEvents.Add(ConversationKeys.TractorNotFound, 4);
            }
            else
            {
                string[] possibleHintTopics = this.QuestControllers.Where(qc => !qc.IsStarted).Select(qc => qc.HintTopicConversationKey).ToArray();
                if (possibleHintTopics.Any())
                {
                    Game1.player.activeDialogueEvents.Add(possibleHintTopics[Game1.random.Next(possibleHintTopics.Length)], 4);
                }
            }
        }

        /// <summary>
        ///   Custom classes, like we're doing with the tractor and the quest, don't serialize without some help.
        ///   This method provides that help by converting the objects to player moddata and deleting the objects
        ///   prior to save.  <see cref="InitializeQuestable"/> restores them.
        /// </summary>
        public void OnDayEnding()
        {
            Game1.getFarm().terrainFeatures.RemoveWhere(p => p.Value is DerelictTractorTerrainFeature);

            string? questState = Game1.player.questLog.OfType<RestoreTractorQuest>().FirstOrDefault()?.Serialize();
            if (questState is not null)
            {
                Game1.player.modData[ModDataKeys.MainQuestStatus] = questState;
            }
            Game1.player.questLog.RemoveWhere(q => q is RestoreTractorQuest);

            foreach (var qc in this.QuestControllers)
            {
                qc.OnDayEnding();
            }

            BorrowHarpoonQuest.OnDayEnding();
        }

        public static T GetModConfig<T>(string key)
            where T: struct // <-- see how Enum.TryParse<T> is declared for evidence that's the best you can do.
        {
            return (Game1.player.modData.TryGetValue(key, out string value) && Enum.TryParse(value, out T result)) ? result : default(T);
        }

        internal AxeConfig GetAxeConfig(AxeConfig configured)
        {
            return GetModConfig<LoaderQuestState>(ModDataKeys.LoaderQuestStatus) == LoaderQuestState.Complete
                ? configured : Disabled<AxeConfig>();
        }

        internal PickAxeConfig GetPickConfig(PickAxeConfig configured)
        {
            return GetModConfig<LoaderQuestState>(ModDataKeys.LoaderQuestStatus) == LoaderQuestState.Complete
                ? configured : Disabled<PickAxeConfig>();
        }

        internal GenericAttachmentConfig GetSeederConfig(GenericAttachmentConfig configured)
        {
            return GetModConfig<SeederQuestState>(ModDataKeys.SeederQuestStatus) == SeederQuestState.Complete
                ? configured : Disabled<GenericAttachmentConfig>();
        }

        internal ScytheConfig GetScytheConfig(ScytheConfig configured)
        {
            // The harvester default config is pretty broad, but there's nothing unrealistic or out of hand about it.
            return GetModConfig<ScytheQuestState>(ModDataKeys.ScytheQuestStatus) == ScytheQuestState.Complete
                ? configured : Disabled<ScytheConfig>();
        }

        internal GenericAttachmentConfig GetScytheConfig(GenericAttachmentConfig configured)
        {
            return Disabled<GenericAttachmentConfig>();
        }

        internal GenericAttachmentConfig GetWateringCanConfig(GenericAttachmentConfig configured)
        {
            return GetModConfig<WatererQuestState>(ModDataKeys.WateringQuestStatus) == WatererQuestState.Complete
                ? configured : Disabled<GenericAttachmentConfig>();
        }

        internal HoeConfig GetHoeConfig(HoeConfig configured)
        {
            // By default, the Hoe has amazing powers.  This variant of the mod tones it down.
            HoeConfig limitedConfig = Disabled<HoeConfig>();
            limitedConfig.TillDirt = true;
            limitedConfig.ClearWeeds = configured.ClearWeeds; // <- if you run a real plow over a weed, it's a bad day for the weed... unless maybe it's a dandilion, then it only makes it stronger.
            return limitedConfig;
        }

        internal T GetUnsupportedConfig<T>(T configured)
            where T : new()
        {
            return Disabled<T>();
        }

        internal void OnAssetRequested(AssetRequestedEventArgs e, ModConfig config)
        {
            this.Monitor.Log($"OnAssetRequested({e.NameWithoutLocale.Name})");
            if (!config.QuestDriven)
            {
                return;
            }

            if (e.NameWithoutLocale.IsEquivalentTo("Data/Buildings"))
            {
                e.Edit(editor =>
                {
                    List<BuildingMaterial>? buildingMaterials = null;
                    int buildingCost = 0;

                    // TODO: It'd be nice if we could do  ' if (this.IsQuestReadyForTractorBuilding())'
                    //   but it looks like the game builds its list of buildings before a save is even
                    //   loaded, so we can't use any sort of context here.

                    // Note that the cost isn't configurable here because:
                    //  1. The whole idea of the quest is to tune it to other events in the game.
                    //  2. There are several other quest objectives that have requirements besides
                    //     the garage and doing them all would be kinda out of hand.
                    //  3. The requirements are designed to be very manageable.  People who just
                    //     want an easy button tractor should just nerf the requirements in non-quest
                    //     mode.
                    buildingCost = 350;

                    // Note that the practical length limit of this list is 3 - because of the size of
                    //   the shop-for-buildings dialog at Robin's shop.  It'd be nice if we could make
                    //   a bit of a story out of the cup of coffee.
                    buildingMaterials = new List<BuildingMaterial>
                    {
                        new BuildingMaterial() { ItemId = "(O)388", Amount = 3 }, // 3 Wood
                        new BuildingMaterial() { ItemId = "(O)390", Amount = 5 }, // 5 Stone
                        new BuildingMaterial() { ItemId = "(O)395", Amount = 1 }, // 1 cup of coffee
                    };

                    var data = editor.AsDictionary<string, BuildingData>().Data;
                    data[GarageBuildingId] = new BuildingData
                    {
                        Name = I18n.Garage_Name(),
                        Description = "A garage to store your tractor.", // TODO: i18n
                        Texture = $"{PublicAssetBasePath}/Garage",
                        BuildingType = typeof(Stable).FullName,
                        SortTileOffset = 1,

                        Builder = Game1.builder_robin,
                        BuildCost = buildingCost,
                        BuildMaterials = buildingMaterials,
                        BuildDays = 2,

                        Size = new Point(4, 2),
                        CollisionMap = "XXXX\nXOOX"
                    };
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(editor =>
                {
                    ObjectIds.EditAssets(editor.AsDictionary<string, ObjectData>().Data);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            {
                e.Edit(editor =>
                {
                    IDictionary<string, string> recipes = editor.AsDictionary<string, string>().Data;
                    recipes["TractorMod.ScytheAttachment"] = $"{ObjectIds.BustedScythe} 1 {ObjectIds.ScythePart1} 1 {ObjectIds.ScythePart2} 1/Field/{ObjectIds.WorkingScythe}/false/default/";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Mail"))
            {
                e.Edit(editor =>
                {
                    MailKeys.EditAssets(editor.AsDictionary<string, string>().Data);
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Tools"))
            {
                e.Edit(editor =>
                {
                    WatererQuestController.EditToolAssets(editor.AsDictionary<string, ToolData>().Data);
                });
            }
            else if (e.NameWithoutLocale.StartsWith("Characters/Dialogue/"))
            {
                e.Edit(editor =>
                {
                    var topics = editor.AsDictionary<string, string>().Data;
                    ConversationKeys.EditAssets(e.NameWithoutLocale, topics);
                });
            }
        }

        public static T Disabled<T>() where T : new()
        {
            var x = new T();
            foreach (var prop in typeof(T).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (prop.CanWrite && prop.PropertyType == typeof(bool))
                {
                    prop.SetValue(x, false, null);
                }
            }

            return x;
        }
    }
}
