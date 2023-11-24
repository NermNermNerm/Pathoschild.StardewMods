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
                        ObjectIds.BustedSeeder,
                        "broken fertilizer and seed Seeder.", // TODO: 18n
                        "The old fertilizer and seed spread for the old tractor.  Needs a good bit of fiddling to make work.", // TODO: 18n
                        12);
                    addQuestItem(
                        ObjectIds.WorkingSeeder,
                        "fertilizer and seed Seeder attachment for the tractor.", // TODO: 18n
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
                    mailItems[MailKeys.WatererRepaired] = "Thanks for letting me work on this!  I even let my Dad do some of the work on it so that he got to feel like maybe he finally did make good on his promise to your Granddad all those years ago.  But me, well, I just like gadgets!  If it ever breaks down, let me know, I have a 10-year warranty on all my work :)"
                                                        + $"%item object {ObjectIds.WorkingWaterer} 1%%";
                    mailItems[MailKeys.GeorgeSeederMail] = "I hear you're restoring that old tractor.  Here's the seeder - it spreads fertilizer and seeds.  Your Grandpa gave it to me to fix after he decided to try going organic and put chicken droppings in it. "
                                                         + "I got it cleaned up, but it needed some iron bars.  Your Grandpa took his time rounding those up, and, well, my accident and his decline put an end to it.  But you're a young man, you can find somebody to fix it. "
                                                         + "I'm too old to be of any use anymore."
                                                         + $"%item object {ObjectIds.BustedSeeder} 1%%";
                    mailItems[MailKeys.EvelynPointsToAlex] = "Hello Dear,^I gave what you said a lot of thought and I think I have a way.  George would be really uncomfortable with you doing work he would want to do.^But George has had to get used to letting Alex do a lot of things for him already, so it might be easier.^I talked to him already, but he seems uneasy about it.  Maybe someone nearer his own age who he trusted talked to him he would come around.^Good luck!^ - Granny";
                    mailItems[MailKeys.HaleysWorkIsDone] = "@ - I think you'll find Alex is a little more... inspired... today.^ - Haley";
                    mailItems[MailKeys.AlexThankYouMail] = "Hey!^Grandpa and I got the seeder working last night!  You can pick it up from Grandpa whenever.  Now that it's done, I'm really happy you rooked me into doing this. "
                                                         + "I really love my grandpa, and well, sometimes it's hard to find things to do together.  And hey, maybe if the Gridball thing doesn't work out I can get a job repairing "
                                                         + "farm equipment!^ - Your friend,^   Alex";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Abigail"))
            {
                e.Edit(editor =>
                {
                    var topics = editor.AsDictionary<string, string>().Data;
                    topics[ConversationKeys.TractorNotFound] = "Have you run across the old tractor yet?  It's off on the west side of the farm, in behind the some trees in an old lean-to.  It's just about one with the vegetation now, but it hasn't rusted away completely.  It sure would be fun to see it run!";
                    topics[ConversationKeys.ScytheNotFound] = "You know I used to like to tromp around your old farm.  I loved the empty haunted feel to it...$2#$b#Anyway...  I saw some things that probably work with the tractor, over on the South side of your farm near Marnie's ranch.#$b#One of them is buried under and old log and one is wedged into a boulder.#$b#Hey, if you get them working, does this mean I can drive the tractor?$4";
                    topics[ConversationKeys.LoaderNotFound] = "You know I used to like to tromp around your old farm.  I loved the empty haunted feel to it...$2#$b#Anyway...  I saw some things that probably work with the tractor, over on the South side of your farm near Marnie's ranch.#$b#One of them is buried under and old log and one is wedged into a boulder.#$b#Hey, if you get them working, does this mean I can drive the tractor?$4";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Caroline"))
            {
                e.Edit(editor =>
                {
                    var topics = editor.AsDictionary<string, string>().Data;
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Demetrius"))
            {
                e.Edit(editor =>
                {
                    var topics = editor.AsDictionary<string, string>().Data;
                    topics[ConversationKeys.TractorNotFound] = "Have you run across the old tractor yet?  I doubt it still works, but maybe it could be restored.";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Dwarf"))
            {
                e.Edit(editor =>
                {
                    var topics = editor.AsDictionary<string, string>().Data;
                    topics[ConversationKeys.DwarfShoesTaken] = "Hey I saw you take those shoes.  I would have charged gold for them if I thought anybody would be stupid enough to want them.#$b#They don't fit. That's why I chucked them over there.  I'm glad you hauled off that junk.";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Evelyn"))
            {
                e.Edit(editor =>
                {
                    var topics = editor.AsDictionary<string, string>().Data;
                    topics[ConversationKeys.LoaderNotFound]
                     = topics[ConversationKeys.ScytheNotFound]
                     = "You granddad... Bless his heart.  He loved that tractor of his, but you could never tell that judging by the dents and broken off parts!#$b#He left little bits of that thing scattered all over the farm, I'm afraid.  You'll probably come across bits of it here and there!";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/George"))
            {
                e.Edit(editor =>
                {
                    var topics = editor.AsDictionary<string, string>().Data;
                    topics[ConversationKeys.TractorNotFound + "2"] = "Have you run across the old tractor yet?  Your Grandpa kept it stored in a lean to out on the West side.#$b#It was a piece of junk when he bought it.  No idea how he kept it running.  He had no mechanical ability at all.  None.";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Gus"))
            {
                e.Edit(editor =>
                {
                    var topics = editor.AsDictionary<string, string>().Data;
                    topics[ConversationKeys.WatererNotFound] = "I hear you found that old tractor - you should get Robin to tell you the tale of the irrigator.";
                    topics[ConversationKeys.SeederNotFound] = "You know your Grandad and George were really good friends; it hit George pretty hard when your Granddad passed.$2#$b#You might do well to be nice to George, he might know a few secrets about your farm.";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Emily"))
            {
                e.Edit(editor =>
                {
                    var topics = editor.AsDictionary<string, string>().Data;
                    topics[ConversationKeys.SeederNotFound] = "You know your Grandad and George were really good friends; it hit George pretty hard when your Granddad passed.$2#$b#You might do well to be nice to George, he might know a few secrets about your farm.";

                    // Just a little color to go with the shoes quest...  I picked this day to replace because the existing dialog is pretty weak.
                    topics["winter_Sun"] = "I saw in one of Haley's magazines where it says that women unconsciously rate men based on their shoes...#$b#Hah!  I must be doing it wrong then.  If I ever did look at a man's shoes and I saw scuffed up old work boots, I'd be more attracted to him, not less!";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Jaz")
                  || e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Vincent"))
            {
                e.Edit(editor =>
                {
                    var topics = editor.AsDictionary<string, string>().Data;
                    topics[ConversationKeys.ScytheNotFound]
                      = topics[ConversationKeys.LoaderNotFound + "2"]
                      = "Hey, wanna know a secret about your farm?  Down in the brambles near Marnie's house, there's Greebles.  They've made some wierd machines too.$3#$b#No...  I've never seen a Greeble, but cats can!";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Jodi"))
            {
                e.Edit(editor =>
                {
                    var topics = editor.AsDictionary<string, string>().Data;
                    topics[ConversationKeys.LoaderNotFound]
                     = topics[ConversationKeys.ScytheNotFound]
                     = "The kids used to play out in the south field near Marnie's house.  They often came home with tales of high adventure!$1#$b#You might poke around down there sometime when you need a break, who knows what you'll find!";
                    topics[ConversationKeys.SeederNotFound] = "You know your Grandad and George were really good friends; it hit George pretty hard when your Granddad passed.$2#$b#You might do well to be nice to George, he might know a few secrets about your farm.";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Lewis"))
            {
                e.Edit(editor =>
                {
                    var topics = editor.AsDictionary<string, string>().Data;
                    topics[ConversationKeys.WatererNotFound] = "I hear you found that old tractor - you should get Robin to tell you the tale of the irrigator.";
                    topics[ConversationKeys.LoaderNotFound] = "One day your granddad came to ask me for help because he had wedged the front-end loader under a boulder he was moving on the south side of the farm.#$b#I told him that maybe his little tractor wasn't up to moving such a big rock...  He seemed to take that kinda personal; he did love that little tractor.$2#$b#But then his eyes lit up and he ran into Pierre's.$1#$b#I'm not sure what happened after that.";
                    topics[ConversationKeys.SeederNotFound] = "You know your Grandad and George were really good friends; it hit George pretty hard when your Granddad passed.$2#$b#You might do well to be nice to George, he might know a few secrets about your farm.";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Marnie"))
            {
                e.Edit(editor =>
                {
                    var topics = editor.AsDictionary<string, string>().Data;
                    topics[ConversationKeys.LoaderNotFound]
                     = topics[ConversationKeys.ScytheNotFound]
                     = "You know the kids used to play at your farm.  I've asked them to stay clear now that you're back.#$b#It's hard enough to run a farm with chickens running around, let alone kids!$1#$b#Ask me how I know...$2";
                    topics[ConversationKeys.SeederNotFound] = "You know your Grandad and George were really good friends; it hit George pretty hard when your Granddad passed.$2#$b#You might do well to be nice to George, he might know a few secrets about your farm.";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Maru"))
            {
                e.Edit(editor =>
                {
                    var topics = editor.AsDictionary<string, string>().Data;
                    topics[ConversationKeys.WatererNotFound] = "I hear you found the tractor!  Just a word of advice - might be best not to bring up the subject around my dad...$2#$b#It's a bit of a sore subject.#$b#You can talk to my Mom about it though - your only trouble will be getting her to stop!$1";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Pierre"))
            {
                e.Edit(editor =>
                {
                    var topics = editor.AsDictionary<string, string>().Data;
                    topics[ConversationKeys.TractorNotFound] = "Have you run across the old tractor yet?  If you could get it running, it'd sure help you get more crops in the ground.#$b#If you need help finding it, Abigail might know where it is.";
                    topics[ConversationKeys.WatererNotFound] = "I hear you found that old tractor - I wonder where all the attachments are?  One that's not a mystery is the irrigation rig.#$b#That's somewhere at the bottom of the farm pond, along with a big chunk of Demetrius' pride!#1";
                    topics[ConversationKeys.LoaderNotFound] = "Did you ever find the front-end loader?  One day your Granddad came running into the store, bought a whole bunch of bombs and ran back out again.#$b#Lewis tells me he wedged the loader under a big rock and was trying to get it out...  So if you do find it, it might be in pieces!#1#$b#I'm just glad your Granddad didn't end up in pieces.$2#$b#That's why I don't sell explosives anymore.$2";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Robin"))
            {
                e.Edit(editor =>
                {
                    var topics = editor.AsDictionary<string, string>().Data;
                    topics[ConversationKeys.TractorNotFound] = "Heh, have you run across the old tractor yet?  That thing has some stories to tell, let me tell you.  It might make a good decoration for your front yard someday.";
                    topics[ConversationKeys.WatererNotFound] = "Oh you want to find the tractor's irrigator?  Ha!  Good luck with that!$b#$b#Who knows, maybe you can fish it out of there.$1#$b#Demetrius tried to winch it out and ended up, well, let's just say the creases on his trousers weren't quite as crisp as usual after that attempt!$1";
                    topics[ConversationKeys.SeederNotFound] = "You know your Grandad and George were really good friends; it hit George pretty hard when your Granddad passed.$2#$b#You might do well to be nice to George, he might know a few secrets about your farm.";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Characters/Dialogue/Willy"))
            {
                e.Edit(editor =>
                {
                    var topics = editor.AsDictionary<string, string>().Data;
                    topics[ConversationKeys.TractorNotFound] = "Have you been out on the west side of your property yet?  Your Granddad had a tractor that he kept under a lean-to that always teetered on the brink of collapse.#$b#Know how he got it?  Me and Pappy boated it in from Knopperville.$l#$b#I didn't understand why he bought it -- the engine was had thrown a rod bent the crankshaft and cracked the case.$s#$b#But somehow he got it to live again!  Funny that.  He wasn't much of a mechanic...";
                    topics[ConversationKeys.WatererNotFound] = "Ah, but yaknow, the biggest catch on your farm isn't the fish!  Nay laddy, it's the watering wagon for the tractor!$3#$b#That ol' thing sunk into the depths of the pond ne'er to be seen again!$1#$b#You'll probably lose a lure or two to it if you fish on your pond.$3";
                    topics[ConversationKeys.SeederNotFound] = "You know your Grandad and George were really good friends; it hit George pretty hard when your Granddad passed.$2#$b#You might do well to be nice to George, he might know a few secrets about your farm.";
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
