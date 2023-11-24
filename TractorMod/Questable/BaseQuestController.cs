using System;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using Microsoft.Xna.Framework;

namespace Pathoschild.Stardew.TractorMod.Questable
{
    public abstract class BaseQuestController
    {
        protected const string QuestCompleteStateMagicWord = "Complete";

        public abstract void OnDayStart();
        public abstract void OnDayEnding();

        protected abstract string ModDataKey { get; }
        public abstract string WorkingAttachmentPartId { get; }
        public abstract string BrokenAttachmentPartId { get; }
        public abstract string HintTopicConversationKey { get; }
        public bool IsStarted => Game1.player.modData.ContainsKey(this.ModDataKey);

        public abstract void WorkingAttachmentBroughtToGarage();

        public abstract void PlayerGotBrokenPart(Farmer player, StardewValley.Item brokenPart);
        public abstract void PlayerGotWorkingPart(Farmer player, StardewValley.Item brokenPart);

        public static void Spout(NPC n, string message)
        {
            n.CurrentDialogue.Push(new Dialogue(n, null, message));
            Game1.drawDialogue(n);
        }

        public static void Spout(string message)
        {
            Game1.DrawDialogue(new Dialogue(null, null, message));
        }

    }

    public abstract class BaseQuestController<TStateEnum, TQuest> : BaseQuestController
        where TStateEnum : struct, Enum
        where TQuest : BaseQuest<TStateEnum>, new()
    {
        protected readonly QuestSetup mod;

        protected BaseQuestController(QuestSetup mod)
        {
            this.mod = mod;
        }

        public virtual void AnnounceGotBrokenPart(Item brokenPart)
        {
            Game1.player.holdUpItemThenMessage(brokenPart);
        }

        /// <summary>
        ///  Called when the main player gets the starter item for the quest.  Implmenetations should disable spawning any more quest starter items.
        /// </summary>
        protected virtual void OnQuestStarted() { }

        public override void PlayerGotBrokenPart(Farmer player, Item brokenPart)
        {
            if (!player.IsMainPlayer)
            {
                Spout("This item is for unlocking the tractor - only the host can advance this quest.  Give this item to the host.");
                return;
            }

            if (Game1.player.questLog.OfType<TQuest>().Any())
            {
                this.mod.Monitor.Log($"Player found a broken attachment, {brokenPart.ItemId}, when the quest was active?!");
                return;
            }

            this.AnnounceGotBrokenPart(brokenPart);
            var quest = new TQuest();
            player.questLog.Add(quest);
        }

        public override void PlayerGotWorkingPart(Farmer player, StardewValley.Item workingPart)
        {
            if (!player.IsMainPlayer)
            {
                Spout("This item is for unlocking the tractor - only the host can advance this quest.  Give this item to the host.");
                return;
            }

            var quest = Game1.player.questLog.OfType<TQuest>().FirstOrDefault();
            if (quest is null)
            {
                this.mod.Monitor.Log($"Player found a working attachment, {workingPart.ItemId}, when the quest was not active?!", LogLevel.Warn);
                // consider recovering by creating the quest?
                return;
            }

            quest.GotWorkingPart(workingPart);
        }

        public override void WorkingAttachmentBroughtToGarage()
        {
            var activeQuest = Game1.player.questLog.OfType<TQuest>().FirstOrDefault();
            activeQuest?.questComplete();
            if (activeQuest is null)
            {
                this.mod.Monitor.Log($"An active {nameof(TQuest)} should exist, but doesn't?!", LogLevel.Warn);
            }
            Game1.player.modData[this.ModDataKey] = QuestCompleteStateMagicWord;
            Game1.player.removeFirstOfThisItemFromInventory(this.WorkingAttachmentPartId);
            Game1.DrawDialogue(new Dialogue(null, null, this.QuestCompleteMessage));
        }

        protected abstract string QuestCompleteMessage { get; }
        protected virtual void HideStarterItemIfNeeded() { }


        protected virtual TQuest? Deserialize(string storedValue)
        {
            if (!Enum.TryParse(storedValue, out TStateEnum parsedValue))
            {
                this.mod.Monitor.Log($"Invalid value for moddata key, '{this.ModDataKey}': '{storedValue}' - quest state will revert to not started.", LogLevel.Error);
                return null;
            }
            else
            {
                return new TQuest { State = parsedValue };
            }
        }

        public override void OnDayStart()
        {
            if (!Game1.player.modData.TryGetValue(this.ModDataKey, out string stateAsString))
            {
                // Quest is not started.
                this.HideStarterItemIfNeeded();
                return;
            }

            if (stateAsString == QuestCompleteStateMagicWord)
            {
                // Quest is complete
                return;
            }

            var newQuest = this.Deserialize(stateAsString);
            if (newQuest is null)
            {
                // Try to recover from the fault by blowing away the data.  This means that the
                // next day, we'll hit the item-re-plant logic.
                Game1.player.modData.Remove(this.ModDataKey);
            }
            else
            {
                newQuest.MarkAsViewed();
                newQuest.AdvanceStateForDayPassing();
                Game1.player.questLog.Add(newQuest);
            }
        }

        public override void OnDayEnding()
        {
            string? questState = Game1.player.questLog.OfType<TQuest>().FirstOrDefault()?.Serialize();
            if (questState is not null)
            {
                Game1.player.modData[this.ModDataKey] = questState;
            }
            Game1.player.questLog.RemoveWhere(q => q is TQuest);
        }


        protected void PlaceBrokenPartUnderClump(int preferredResourceClumpToHideUnder)
        {
            var farm = Game1.getFarm();
            if (farm.objects.Values.Any(o => o.ItemId == this.BrokenAttachmentPartId))
            {
                // Already placed - nothing to do.
                return;
            }

            var position = this.FindPlaceToPutItem(preferredResourceClumpToHideUnder);
            if (position != default)
            {
                var o = ItemRegistry.Create<StardewValley.Object>(this.BrokenAttachmentPartId);
                o.Location = Game1.getFarm();
                o.TileLocation = position;
                o.IsSpawnedObject = true;
                farm.objects[o.TileLocation] = o;
            }
        }

        private Vector2 FindPlaceToPutItem(int preferredResourceClumpToHideUnder)
        {
            var farm = Game1.getFarm();
            var bottomMostResourceClump = farm.resourceClumps.Where(tf => tf.parentSheetIndex.Value == preferredResourceClumpToHideUnder).OrderByDescending(tf => tf.Tile.Y).FirstOrDefault();
            if (bottomMostResourceClump is not null)
            {
                return bottomMostResourceClump.Tile;
            }

            this.mod.Monitor.Log($"Couldn't find the preferred location ({preferredResourceClumpToHideUnder}) for the {this.BrokenAttachmentPartId}", LogLevel.Warn);
            bottomMostResourceClump = farm.resourceClumps.OrderByDescending(tf => tf.Tile.Y).FirstOrDefault();
            if (bottomMostResourceClump is not null)
            {
                return bottomMostResourceClump.Tile;
            }

            this.mod.Monitor.Log($"The farm contains no resource clumps under which to stick the {this.BrokenAttachmentPartId}", LogLevel.Warn);

            // We're probably dealing with an old save,  Try looking for any clear space.
            //  This technique is kinda dumb, but whatev's.  This mod is pointless on a fully-developed farm.
            for (int i = 0; i < 1000; ++i)
            {
                Vector2 positionToCheck = new Vector2(Game1.random.Next(farm.map.DisplayWidth / 64), Game1.random.Next(farm.map.DisplayHeight / 64));
                if (farm.CanItemBePlacedHere(positionToCheck))
                {
                    return positionToCheck;
                }
            }

            this.mod.Monitor.Log($"Couldn't find any place at all to put the {this.BrokenAttachmentPartId}", LogLevel.Error);
            return default;
        }
    }
}
