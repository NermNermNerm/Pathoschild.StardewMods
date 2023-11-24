using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using HarmonyLib;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.TractorMod.Framework;
using Pathoschild.Stardew.TractorMod.Framework.Config;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Objects;
using StardewValley.Network;
using StardewValley.Tools;

namespace Pathoschild.Stardew.TractorMod.Questable
{
    public class QuestSetup
    {
        // Mirrored from ModEntry  IMO, this is how it should be declared there.  Doing it this way for least-intrusion.
        public const string GarageBuildingId = "Pathoschild.TractorMod_Stable";
        public const string PublicAssetBasePath = "Mods/Pathoschild.TractorMod";

        private ModEntry mod;

        private IModHelper Helper => this.mod.Helper;
        private IMonitor Monitor => this.mod.Monitor;

        internal QuestSetup(ModEntry mod)
        {
            this.mod = mod;

            this.Helper.Events.Player.InventoryChanged += this.Player_InventoryChanged;
            this.Helper.Events.GameLoop.OneSecondUpdateTicked += this.GameLoop_OneSecondUpdateTicked;

            var harmony = new Harmony(mod.ModManifest.UniqueID);
            var farmType = typeof(Farm);
            var getFishMethod = farmType.GetMethod("getFish");
            harmony.Patch(getFishMethod, prefix: new HarmonyMethod(typeof(QuestSetup), nameof(Prefix_GetFish)));
        }

        private static bool Prefix_GetFish(ref Item __result)
        {
            if (Game1.random.NextDouble() < WatererQuest.chanceOfCatchingQuestItem)
            {
                __result = ItemRegistry.Create(ObjectIds.BustedWaterer);
                return false;
            }
            else
            {
                return true;
            }
        }

        private void GameLoop_OneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
        {
            // Hacky way to detect when the player when a quest is open
            if (Game1.player.currentLocation is not null && Game1.player.currentLocation == Game1.getFarm() &&
                (Game1.player.CurrentItem?.Name == ObjectIds.WorkingLoader
                || Game1.player.CurrentItem?.Name == ObjectIds.WorkingScythe
                || Game1.player.CurrentItem?.Name == ObjectIds.WorkingSeeder
                || Game1.player.CurrentItem?.Name == ObjectIds.WorkingWaterer))
            {
                if (Game1.player.currentLocation.buildings
                    .OfType<Stable>()
                    .Where(s => s.buildingType.Value == GarageBuildingId)
                    .Any(s => IsPlayerInGarage(Game1.player, s)))
                {
                    switch (Game1.player.CurrentItem?.Name)
                    {
                        case ObjectIds.WorkingLoader:
                            var loaderQuest = Game1.player.questLog.OfType<AxeAndPickQuest>().FirstOrDefault();
                            loaderQuest?.WorkingAttachmentBroughtToGarage();
                            break;
                        case ObjectIds.WorkingScythe:
                            var scytheQuest = Game1.player.questLog.OfType<ScytheQuest>().FirstOrDefault();
                            scytheQuest?.WorkingAttachmentBroughtToGarage();
                            break;
                        case ObjectIds.WorkingSeeder:
                            var seederQuest = Game1.player.questLog.OfType<SeederQuest>().FirstOrDefault();
                            seederQuest?.WorkingAttachmentBroughtToGarage();
                            break;
                        case ObjectIds.WorkingWaterer:
                            var watererQuest = Game1.player.questLog.OfType<WatererQuest>().FirstOrDefault();
                            watererQuest?.WorkingAttachmentBroughtToGarage();
                            break;
                    }
                    this.mod.UpdateConfig();
                }
            }
        }

        private static bool IsPlayerInGarage(Character c, Stable b)
        {
            Rectangle cPos = new Rectangle(new Point((int)c.Position.X, (int)c.Position.Y-128), new Point(64, 128));
            bool isIntersecting = b.intersects(cPos);
            return isIntersecting;
        }

        private void Player_InventoryChanged(object? sender, InventoryChangedEventArgs e)
        {
            var bustedLoader = e.Added.FirstOrDefault(i => i.ItemId == ObjectIds.BustedLoader);
            if (bustedLoader is not null && !e.Player.questLog.OfType<AxeAndPickQuest>().Any())
            {
                e.Player.holdUpItemThenMessage(bustedLoader);
                var quest = new AxeAndPickQuest();
                e.Player.questLog.Add(quest);
            }

            var bustedScythe = e.Added.FirstOrDefault(i => i.ItemId == ObjectIds.BustedScythe);
            if (bustedScythe is not null && !e.Player.questLog.OfType<ScytheQuest>().Any())
            {
                e.Player.holdUpItemThenMessage(bustedScythe);
                var quest = new ScytheQuest();
                e.Player.questLog.Add(quest);
            }

            var fixedScythe = e.Added.FirstOrDefault(i => i.ItemId == ObjectIds.WorkingScythe);
            if (fixedScythe is not null)
            {
                var q = e.Player.questLog.OfType<ScytheQuest>().FirstOrDefault();
                q?.ReadyToInstall();
                e.Player.holdUpItemThenMessage(fixedScythe, showMessage: false);
                Game1.DrawDialogue(new Dialogue(null, null, "A little wire brush, some oil, and of course the rest of the parts and job done!  Just need to take it to the garage now!"));
            }

            var bustedWaterer = e.Added.FirstOrDefault(i => i.ItemId == ObjectIds.BustedWaterer);
            if (bustedWaterer is not null && !e.Player.questLog.OfType<WatererQuest>().Any())
            {
                var quest = new WatererQuest();
                e.Player.questLog.Add(quest);
                Game1.DrawDialogue(new Dialogue(null, null, "Whoah that was heavy!  Looks like an irrigator attachment for a tractor!  I bet there's a story behind how it got here..."));
                WatererQuest.chanceOfCatchingQuestItem = 0;
            }

            var fixedWaterer = e.Added.FirstOrDefault(i => i.ItemId == ObjectIds.WorkingWaterer);
            if (fixedWaterer is not null)
            {
                var q = e.Player.questLog.OfType<WatererQuest>().FirstOrDefault();
                q?.ReadyToInstall();
                Game1.DrawDialogue(new Dialogue(null, null, "Maru came through!  Time to take it to the garage and water some crops!"));
            }

            var bustedSeeder = e.Added.FirstOrDefault(i => i.ItemId == ObjectIds.BustedSeeder);
            if (bustedSeeder is not null && !e.Player.questLog.OfType<SeederQuest>().Any())
            {
                Game1.player.questLog.Add(new SeederQuest());
            }
        }

        public void OnDayStarted(Stable? garage)
        {
            if (!Context.IsMainPlayer)
            {
                return;
            }

            RestoreTractorQuest.OnDayStart(this.Helper, this.Monitor, garage);
            AxeAndPickQuest.OnDayStart(this.Helper, this.Monitor);
            ScytheQuest.OnDayStart(this.Helper, this.Monitor);
            WatererQuest.OnDayStart(this.Helper, this.Monitor);
            SeederQuest.OnDayStart(this.mod);

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
                // we want to dribble out the clues, not spew them all at once, so see what's missing...
                List<string> possibleHintTopics = new List<string>();
                if (!AxeAndPickQuest.IsStarted)
                {
                    possibleHintTopics.Add(ConversationKeys.LoaderNotFound);
                }
                if (!ScytheQuest.IsStarted)
                {
                    possibleHintTopics.Add(ConversationKeys.ScytheNotFound);
                }
                if (!WatererQuest.IsStarted)
                {
                    possibleHintTopics.Add(ConversationKeys.WatererNotFound);
                }
                if (!SeederQuest.IsStarted)
                {
                    possibleHintTopics.Add(ConversationKeys.SeederNotFound);
                }

                if (possibleHintTopics.Any())
                {
                    Game1.player.activeDialogueEvents.Add(possibleHintTopics[Game1.random.Next(possibleHintTopics.Count)], 4);
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

            questState = Game1.player.questLog.OfType<AxeAndPickQuest>().FirstOrDefault()?.Serialize();
            if (questState is not null)
            {
                Game1.player.modData[ModDataKeys.AxeAndPickQuestStatus] = questState;
            }
            Game1.player.questLog.RemoveWhere(q => q is AxeAndPickQuest);

            questState = Game1.player.questLog.OfType<ScytheQuest>().FirstOrDefault()?.Serialize();
            if (questState is not null)
            {
                Game1.player.modData[ModDataKeys.ScytheQuestStatus] = questState;
            }
            Game1.player.questLog.RemoveWhere(q => q is ScytheQuest);

            questState = Game1.player.questLog.OfType<WatererQuest>().FirstOrDefault()?.Serialize();
            if (questState is not null)
            {
                Game1.player.modData[ModDataKeys.WateringQuestStatus] = questState;
            }
            Game1.player.questLog.RemoveWhere(q => q is WatererQuest);

            questState = Game1.player.questLog.OfType<SeederQuest>().FirstOrDefault()?.Serialize();
            if (questState is not null)
            {
                Game1.player.modData[ModDataKeys.SeederQuestStatus] = questState;
            }
            Game1.player.questLog.RemoveWhere(q => q is SeederQuest);
        }

        public static T GetModConfig<T>(string key)
            where T: struct // <-- see how Enum.TryParse<T> is declared for evidence that's the best you can do.
        {
            return (Game1.player.modData.TryGetValue(key, out string value) && Enum.TryParse(value, out T result)) ? result : default(T);
        }

        internal AxeConfig GetAxeConfig(AxeConfig configured)
        {
            return GetModConfig<AxeAndPickQuestState>(ModDataKeys.AxeAndPickQuestStatus) == AxeAndPickQuestState.Complete
                ? configured : Disabled<AxeConfig>();
        }

        internal PickAxeConfig GetPickConfig(PickAxeConfig configured)
        {
            return GetModConfig<AxeAndPickQuestState>(ModDataKeys.AxeAndPickQuestStatus) == AxeAndPickQuestState.Complete
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
