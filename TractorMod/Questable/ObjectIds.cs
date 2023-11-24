using System;
using System.Collections.Generic;
using StardewValley.GameData.Objects;

namespace Pathoschild.Stardew.TractorMod.Questable
{
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
        public const string BustedSeeder = "Pathoschild.TractorMod_BustedSeeder";
        public const string WorkingSeeder = "Pathoschild.TractorMod_WorkingSeeder";

        internal static void EditAssets(IDictionary<string, ObjectData> objects)
        {
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
                BustedEngine,
                "funky looking engine that doesn't work", // TODO: 18n
                "Sebastian pulled this off of the rusty tractor.  I need to find someone to fix it.", // TODO: 18n
                0);
            addQuestItem(
                WorkingEngine,
                "working Junimo-powered engine", // TODO: 18n
                "The engine for the tractor!  I need to find someone to install it.", // TODO: 18n
                1);
            addQuestItem(
                BustedScythe,
                "core of the harvesting attachment for the tractor", // TODO: 18n
                "This looks like it was a tractor attachment for harvesting crops, but it doesn't seem to be all together.", // TODO: 18n
                2);
            addQuestItem(
                WorkingScythe,
                "harvesting attachment for the tractor", // TODO: 18n
                "Just need to bring this to the tractor garage to be able to use it with the tractor!", // TODO: 18n
                3);
            addQuestItem(
                ScythePart1,
                "crop shakerlooser", // TODO: 18n
                "One of the missing parts for the scythe attachment", // TODO: 18n
                4);
            addQuestItem(
                    ScythePart2,
                "fruity grabengetter", // TODO: 18n
                "One of the missing parts for the scythe attachment", // TODO: 18n
                5);
            addQuestItem(
                BustedWaterer,
                "broken watering attachment for the tractor", // TODO: 18n
                "This looks like it was a tractor attachment for watering crops.  Sure hope somebody can help me get it working again, watering can really be a drag.", // TODO: 18n
                6);
            addQuestItem(
                WorkingWaterer,
                "watering attachment for the tractor", // TODO: 18n
                "xx", // TODO: 18n
                7);
            addQuestItem(
                BustedLoader,
                "bent up and rusty front-end loader for the tractor", // TODO: 18n
                "This was the front-end loader attachment (for picking up rocks and sticks), but it's all bent up and rusted through in spots.  It needs to be fixed to be usable.", // TODO: 18n
                8);
            addQuestItem(
                WorkingLoader,
                "front-end loader attachment for my tractor", // TODO: 18n
                "This will allow me to clear rocks and sticks on my farm.  It needs to go into the tractor garage so I can use it.", // TODO: 18n
                9);
            addQuestItem(
                AlexesOldShoe,
                "pair of rather nice shoes", // TODO: 18n
                "Shoes that Alex threw away, certified 14EEE!", // TODO: 18n
                10);
            addQuestItem(
                DyedShoe,
                "cleverly repackaged pair of shoes", // TODO: 18n
                "Alex's old shoes, cleverly dyed.  Nobody will ever know.", // TODO: 18n
                11);
            addQuestItem(
                BustedSeeder,
                "broken fertilizer and seed Seeder.", // TODO: 18n
                "The old fertilizer and seed spread for the old tractor.  Needs a good bit of fiddling to make work.", // TODO: 18n
                12);
            addQuestItem(
                WorkingSeeder,
                "fertilizer and seed Seeder attachment for the tractor.", // TODO: 18n
                "Just needs to be brought back to the garage to use it on the tractor.", // TODO: 18n
                13);
        }
    }
}
