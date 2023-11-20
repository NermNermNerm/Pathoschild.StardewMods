using System;
using System.Collections.Generic;
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
                __result = ItemRegistry.Create(QuestSetup.ObjectIds.BustedWaterer);
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
                || Game1.player.CurrentItem?.Name == ObjectIds.WorkingSpreader
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
            if (bustedLoader is not null)
            {
                e.Player.holdUpItemThenMessage(bustedLoader);
                var quest = new AxeAndPickQuest();
                e.Player.questLog.Add(quest);
            }

            var bustedScythe = e.Added.FirstOrDefault(i => i.ItemId == ObjectIds.BustedScythe);
            if (bustedScythe is not null)
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
            if (bustedWaterer is not null)
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
        }

        public static class ObjectIds
        {
            public const string BustedEngine = "Pathoschild.TractorMod_BustedEngine";
            public const string WorkingEngine = "Pathoschild.TractorMod_WorkingEngine";
            public const string BustedLoader = "Pathoschild.TractorMod_BustedLoader";
            public const string WorkingLoader = "Pathoschild.TractorMod_WorkingLoader";
            public const string BustedScythe = "Pathoschild.TractorMod_BustedScythe";
            public const string WorkingScythe = "Pathoschild.TractorMod_WorkingScythe";
            public const string ScythePart1 = "Pathoschild.TractorMod_ScythePart1";
            public const string ScythePart2 = "Pathoschild.TractorMod_ScythePart2";
            public const string AlexesOldShoe = "Pathoschild.TractorMod_AlexesShoe";
            public const string DyedShoe = "Pathoschild.TractorMod_DyedShoe";
            public const string BustedWaterer = "Pathoschild.TractorMod_BustedWaterer";
            public const string WorkingWaterer = "Pathoschild.TractorMod_WorkingWaterer";
            public const string BustedSpreader = "Pathoschild.TractorMod_BustedSpreader";
            public const string WorkingSpreader = "Pathoschild.TractorMod_WorkingSpreader";
        }

        public static class MailKeys
        {
            public const string BuildTheGarage = "QuestableTractorMod.BuildTheGarage";
            public const string FixTheEngine = "QuestableTractorMod.FixTheEngine";
            public const string FixTheWaterer = "QuestableTractorMod.FixTheEngine";
        };

        public static class ModDataKeys
        {
            public const string MainQuestStatus = "QuestableTractorMod.MainQuestStatus";
            public const string DerelictPosition = "QuestableTractorMod.DerelictPosition";
            public const string AxeAndPickQuestStatus = "QuestableTractorMod.AxeAndPickQuestStatus";
            public const string ScytheQuestStatus = "QuestableTractorMod.ScytheQuestStatus";
            public const string WateringQuestStatus = "QuestableTractorMod.WateringQuestStatus";
            public const string SpreadingQuestStatus = "QuestableTractorMod.SpreadingQuestStatus";
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
            WatererQuest.OnDayStart(this.Helper, this.Monitor, this.IsTractorUnlocked);
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
        }

        public static T? GetModConfig<T>(string key)
            where T: struct // <-- see how Enum.TryParse<T> is declared for evidence that's the best you can do.
        {
            return (Game1.player.modData.TryGetValue(key, out string value) && Enum.TryParse(value, out T result)) ? result : null;
        }

        public bool IsTractorUnlocked
        {
            get => GetModConfig<RestorationState>(ModDataKeys.MainQuestStatus) == RestorationState.Complete;
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

        internal GenericAttachmentConfig GetSpreaderConfig(GenericAttachmentConfig _)
        {
            return Disabled<GenericAttachmentConfig>();
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
                    IDictionary<string, ObjectData> objects = editor.AsDictionary<string, ObjectData>().Data;
                    void addQuestItem(string id, string displayName, string description, int spriteIndex)
                    {
                        objects[id] = new()
                        {
                            Name = id,
                            DisplayName = displayName,
                            Description = description,
                            Type = "Litter",
                            Category = -999,
                            Price = 0,
                            Texture = "Mods/PathosChild.TractorMod/QuestSprites",
                            SpriteIndex = spriteIndex,
                            ContextTags = new() { "not_giftable", "not_placeable", "prevent_loss_on_death" },
                        };
                    };
                    addQuestItem(
                        ObjectIds.BustedEngine,
                        "funky looking engine that doesn't work", // TODO: 18n
                        "Sebastian pulled this off of the rusty tractor.  I need to find someone to fix it.", // TODO: 18n
                        0);
                    addQuestItem(
                        ObjectIds.WorkingEngine,
                        "working Junimo-powered engine", // TODO: 18n
                        "The engine for the tractor!  I need to find someone to install it.", // TODO: 18n
                        1);
                    addQuestItem(
                        ObjectIds.BustedScythe,
                        "nonfunctional harvesting attachment for the tractor", // TODO: 18n
                        "This looks like it was a tractor attachment for harvesting crops, but it doesn't seem to be all together.", // TODO: 18n
                        2);
                    addQuestItem(
                        ObjectIds.WorkingScythe,
                        "harvesting attachment for the tractor", // TODO: 18n
                        "Just need to bring this to the tractor garage to be able to use it with the tractor!", // TODO: 18n
                        3);
                    addQuestItem(
                        ObjectIds.ScythePart1,
                        "crop shakerlooser", // TODO: 18n
                        "One of the missing parts for the scythe attachment", // TODO: 18n
                        4);
                    addQuestItem(
                        ObjectIds.ScythePart2,
                        "fruity grabengetter", // TODO: 18n
                        "One of the missing parts for the scythe attachment", // TODO: 18n
                        5);
                    addQuestItem(
                        ObjectIds.BustedWaterer,
                        "broken watering attachment for the tractor", // TODO: 18n
                        "This looks like it was a tractor attachment for watering crops.  Sure hope somebody can help me get it working again, watering can really be a drag.", // TODO: 18n
                        6);
                    addQuestItem(
                        ObjectIds.WorkingWaterer,
                        "watering attachment for the tractor", // TODO: 18n
                        "xx", // TODO: 18n
                        7);
                    addQuestItem(
                        ObjectIds.BustedLoader,
                        "bent up and rusty front-end loader for the tractor", // TODO: 18n
                        "This was the front-end loader attachment (for picking up rocks and sticks), but it's all bent up and rusted through in spots.  It needs to be fixed to be usable.", // TODO: 18n
                        8);
                    addQuestItem(
                        ObjectIds.WorkingLoader,
                        "front-end loader attachment for my tractor", // TODO: 18n
                        "This will allow me to clear rocks and sticks on my farm.  It needs to go into the tractor garage so I can use it.", // TODO: 18n
                        9);
                    addQuestItem(
                        ObjectIds.AlexesOldShoe,
                        "pair of rather nice shoes", // TODO: 18n
                        "Shoes that Alex threw away, certified 14EEE!", // TODO: 18n
                        10);
                    addQuestItem(
                        ObjectIds.DyedShoe,
                        "cleverly repackaged pair of shoes", // TODO: 18n
                        "Alex's old shoes, cleverly dyed.  Nobody will ever know.", // TODO: 18n
                        11);
                    addQuestItem(
                        ObjectIds.BustedSpreader,
                        "broken fertilizer and seed spreader.", // TODO: 18n
                        "The old fertilizer and seed spread for the old tractor.  Needs a good bit of fiddling to make work.", // TODO: 18n
                        12);
                    addQuestItem(
                        ObjectIds.WorkingSpreader,
                        "fertilizer and seed spreader attachment for the tractor.", // TODO: 18n
                        "Just needs to be brought back to the garage to use it on the tractor.", // TODO: 18n
                        13);
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
                    var mailItems = editor.AsDictionary<string, string>().Data;
                    // TODO: i18n
                    mailItems[MailKeys.BuildTheGarage] = "Hey there!^I talked with Sebastian about your tractor and he has agreed to work on it, but only if he's got a decent place to work.  I understand that you're just starting out here and don't have a lot of money laying around, so I'm willing to do it at-cost, providing you can come up with the materials.  Come by my shop for a full list of materials.  See you soon!^  - Robin";
                    mailItems[MailKeys.FixTheEngine] = "I got everything working except this engine.  I've never seen anything like it.  I mean, it's like it doesn't even need gas!^I don't know what you're gonna need to do to make it work, but I know I'm out of my area here.^If you manage to figure it out, bring it back up to my place and I'll see about getting it installed.^  - Sebastian"
                                                      + $"%item object {ObjectIds.BustedEngine} 1%%";
                    mailItems[MailKeys.FixTheWaterer] = "Thanks for letting me work on this!  I even let my Dad do some of the work on it so that he got to feel like maybe he finally did make good on his promise to your Granddad all those years ago.  But me, well, I just like gadgets!  If it ever breaks down, let me know, I have a 10-year warranty on all my work :)"
                                                      + $"%item object {ObjectIds.WorkingWaterer} 1%%";
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
