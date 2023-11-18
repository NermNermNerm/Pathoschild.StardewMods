using System;
using System.Collections.Generic;
using System.Linq;
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
using StardewValley.Tools;

namespace Pathoschild.Stardew.TractorMod.Questable
{
    public class QuestSetup
    {
        // Mirrored from ModEntry  IMO, this is how it should be declared there.  Doing it this way for least-intrusion.
        public const string GarageBuildingId = "Pathoschild.TractorMod_Stable";
        public const string PublicAssetBasePath = "Mods/Pathoschild.TractorMod";

        private ModConfig Config { get; }
        private IModHelper Helper { get; }
        private IMonitor Monitor { get; }

        internal QuestSetup(IMonitor monitor, ModConfig config, IModHelper helper)
        {
            this.Config = config;
            this.Helper = helper;
            this.Monitor = monitor;
        }

        public static class ObjectIds
        {
            public const string BustedEngine = "Pathoschild.TractorMod_BustedEngine";
            public const string WorkingEngine = "Pathoschild.TractorMod_WorkingEngine";
        }

        public static class MailKeys
        {
            public const string BuildTheGarage = "QuestableTractorMod.BuildTheGarage";
            public const string FixTheEngine = "QuestableTractorMod.FixTheEngine";
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

            if (!Game1.player.modData.TryGetValue(ModDataKeys.MainQuestStatus, out string? statusAsString)
                || !Enum.TryParse(statusAsString, true, out RestorationState mainQuestStatusAtDayStart))
            {
                if (statusAsString is not null)
                {
                    this.Monitor.Log($"Invalid value for {ModDataKeys.MainQuestStatus}: {statusAsString} -- reverting to NotStarted", LogLevel.Error);
                }
                mainQuestStatusAtDayStart = RestorationState.NotStarted;
            }

            var mainQuestStatus = RestoreTractorQuest.AdvanceProgress(garage, mainQuestStatusAtDayStart);

            if (mainQuestStatus.IsDerelictInTheFields() || mainQuestStatus.IsDerelictInTheGarage())
            {
                Vector2 position;
                if (mainQuestStatus.IsDerelictInTheFields() || garage is null || garage.isUnderConstruction())
                {
                    Game1.player.modData.TryGetValue(ModDataKeys.DerelictPosition, out string? positionAsString);
                    if (positionAsString is null || !TryParse(positionAsString, out position))
                    {
                        if (positionAsString is not null)
                        {
                            this.Monitor.Log($"Invalid value for {ModDataKeys.MainQuestStatus}: {statusAsString} -- finding a new position", LogLevel.Error);
                        }

                        // TODO: Properly find a position.
                        position = new Vector2(75, 14);

                        Game1.player.modData[ModDataKeys.DerelictPosition] = FormattableString.Invariant($"{position.X},{position.Y}");
                    }
                }
                else
                {
                    position = new Vector2(garage.tileX.Value + 1, garage.tileY.Value);
                }

                var derelictTractorTexture = this.Helper.ModContent.Load<Texture2D>("assets/rustyTractor.png");

                var tf = new DerelictTractorTerrainFeature(derelictTractorTexture, position);
                Game1.getFarm().terrainFeatures.Add(position, tf);
                Game1.getFarm().terrainFeatures.Add(position + new Vector2(0, 1), tf);
                Game1.getFarm().terrainFeatures.Add(position + new Vector2(1, 1), tf);
                Game1.getFarm().terrainFeatures.Add(position + new Vector2(1, 0), tf);
            }

            if (mainQuestStatus != RestorationState.Complete && mainQuestStatus != RestorationState.NotStarted)
            {
                var q = new RestoreTractorQuest(mainQuestStatus);
                q.MarkAsViewed();
                Game1.player.questLog.Add(q);
            }
            else if (mainQuestStatus == RestorationState.Complete && mainQuestStatusAtDayStart != RestorationState.Complete)
            {
                var q = new RestoreTractorQuest(mainQuestStatus);
                Game1.player.questLog.Add(q);
                q.questComplete();
                Game1.player.modData[ModDataKeys.MainQuestStatus] = RestorationState.Complete.ToString();
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
            return Disabled<ScytheConfig>();
        }

        internal GenericAttachmentConfig GetScytheConfig(GenericAttachmentConfig configured)
        {
            return Disabled<GenericAttachmentConfig>();
        }

        internal GenericAttachmentConfig GetWateringCanConfig(GenericAttachmentConfig configured)
        {
            return Disabled<GenericAttachmentConfig>();
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
                    objects[ObjectIds.BustedEngine] = new()
                    {
                        Name = ObjectIds.BustedEngine,
                        DisplayName = "A funky looking engine that doesn't work", // TODO: 18n
                        Description = "Sebastian pulled this off of the rusty tractor.  I need to find someone to fix it.", // TODO: 18n
                        Type = "Litter",
                        Category = -999,
                        Price = 0,
                        Texture = "Mods/PathosChild.TractorMod/QuestSprites",
                        SpriteIndex = 0,
                        ContextTags = new() { "not_giftable", "not_placeable", "prevent_loss_on_death" },
                    };
                    objects[ObjectIds.WorkingEngine] = new()
                    {
                        Name = ObjectIds.WorkingEngine,
                        DisplayName = "A working Junimo-powered engine", // TODO: 18n
                        Description = "The engine for the tractor!  I need to find someone to install it.", // TODO: 18n
                        Type = "Litter",
                        Category = -999,
                        Price = 0,
                        Texture = "Mods/PathosChild.TractorMod/QuestSprites",
                        SpriteIndex = 1,
                        ContextTags = new() { "not_giftable", "not_placeable", "prevent_loss_on_death" },
                    };
                });
            }
            //else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            //{
            //    e.Edit(editor =>
            //    {
            //        IDictionary<string, string> recipes = editor.AsDictionary<string, string>().Data;
            //        recipes["TractorMod.TempTractorRecipe"] = $"388 2/Field/{this.TractorChunkObjectId}/false/default/";
            //    });
            //}
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Mail"))
            {
                e.Edit(editor =>
                {
                    var mailItems = editor.AsDictionary<string, string>().Data;
                    // TODO: i18n
                    mailItems[MailKeys.BuildTheGarage] = "Hey there!^I talked with Sebastian about your tractor and he has agreed to work on it, but only if he's got a decent place to work.  I understand that you're just starting out here and don't have a lot of money laying around, so I'm willing to do it at-cost, providing you can come up with the materials.  Come by my shop for a full list of materials.  See you soon!^  - Robin";
                    mailItems[MailKeys.FixTheEngine] = $"I got everything working except this engine.  I've never seen anything like it.  I mean, it's like it doesn't even need gas!^I don't know what you're gonna need to do to make it work, but I know I'm out of my area here.^If you manage to figure it out, bring it back up to my place and I'll see about getting it installed.^  - Sebastian"
                                                      + $"%item object {ObjectIds.BustedEngine} 1%%";
                });
            }
        }


        private static bool TryParse(string s, out Vector2 position)
        {
            string[] split = s.Split(",");
            if (split.Length == 2
                && int.TryParse(split[0], out int x)
                && int.TryParse(split[1], out int y))
            {
                position = new Vector2(x, y);
                return true;
            }
            else
            {
                position = new Vector2();
                return false;
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
