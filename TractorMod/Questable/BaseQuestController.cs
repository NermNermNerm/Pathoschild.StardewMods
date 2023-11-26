using System;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Pathoschild.Stardew.TractorMod.Questable
{
    public abstract class BaseQuestController
    {
        protected const string QuestCompleteStateMagicWord = "Complete";

        public abstract void OnDayStarted();
        public abstract void OnDayEnding();

        protected abstract string ModDataKey { get; }
        public abstract string WorkingAttachmentPartId { get; }
        public abstract string BrokenAttachmentPartId { get; }
        public abstract string HintTopicConversationKey { get; }
        public bool IsStarted => Game1.player.modData.ContainsKey(this.ModDataKey);

        public abstract void WorkingAttachmentBroughtToGarage();

        public static void Spout(NPC n, string message)
        {
            Game1.DrawDialogue(new Dialogue(n, null, message));
            //n.CurrentDialogue.Clear();
            //n.CurrentDialogue.Push(new Dialogue(n, null, message));
            //Game1.drawDialogue(n);
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

        public void PlayerGotBrokenPart(Item brokenPart)
        {
            if (Game1.player.questLog.OfType<TQuest>().Any())
            {
                this.mod.Monitor.Log($"Player found a broken attachment, {brokenPart.ItemId}, when the quest was active?!");
                return;
            }

            this.AnnounceGotBrokenPart(brokenPart);
            var quest = new TQuest();
            Game1.player.questLog.Add(quest);
            this.OnQuestStarted();
            this.MonitorInventoryForItem(this.WorkingAttachmentPartId, this.PlayerGotWorkingPart);
            this.StopMonitoringInventoryFor(this.BrokenAttachmentPartId);
        }

        public void PlayerGotWorkingPart(Item workingPart)
        {
            var quest = Game1.player.questLog.OfType<TQuest>().FirstOrDefault();
            if (quest is null)
            {
                this.mod.Monitor.Log($"Player found a working attachment, {workingPart.ItemId}, when the quest was not active?!", LogLevel.Warn);
                // consider recovering by creating the quest?
                return;
            }

            quest.GotWorkingPart(workingPart);
            this.StopMonitoringInventoryFor(this.WorkingAttachmentPartId);
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

        private readonly Dictionary<string, Action<Item>> itemsToWatch = new();
        private bool isWatchingInventory;

        protected void MonitorInventoryForItem(string itemId, Action<Item> onItemAdded)
        {
            this.itemsToWatch[itemId] = onItemAdded;
            if (!this.isWatchingInventory)
            {
                this.mod.Helper.Events.Player.InventoryChanged += this.Player_InventoryChanged;
                this.isWatchingInventory = true;
            }
        }

        protected void StopMonitoringInventoryFor(string itemId)
        {
            this.itemsToWatch.Remove(itemId);
            if (!this.itemsToWatch.Any() && this.isWatchingInventory)
            {
                this.mod.Helper.Events.Player.InventoryChanged -= this.Player_InventoryChanged;
                this.isWatchingInventory = false;
            }
        }

        public override void OnDayStarted()
        {
            if (!Game1.player.modData.TryGetValue(this.ModDataKey, out string stateAsString))
            {
                // Quest is not started.
                this.HideStarterItemIfNeeded();
                this.MonitorInventoryForItem(this.BrokenAttachmentPartId, this.PlayerGotBrokenPart);
            }
            else if (stateAsString != QuestCompleteStateMagicWord)
            {
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
                    this.MonitorInventoryForItem(this.WorkingAttachmentPartId, this.PlayerGotWorkingPart);
                    this.MonitorQuestItems();
                }
            }
        }

        /// <summary>
        ///  Called once a day when the quest is active to ensure that we're monitoring for items even after reload
        /// </summary>
        protected virtual void MonitorQuestItems() { }

        private void Player_InventoryChanged(object? sender, StardewModdingAPI.Events.InventoryChangedEventArgs e)
        {
            foreach (var item in e.Added)
            {
                if (this.itemsToWatch.TryGetValue(item.ItemId, out var handler))
                {
                    if (!e.Player.IsMainPlayer)
                    {
                        e.Player.holdUpItemThenMessage(item, true);
                        Spout("This item is for unlocking the tractor - only the host can advance this quest.  Give this item to the host.");
                    }
                    else
                    {
                        handler(item);
                    }
                }
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
            var existing = farm.objects.Values.FirstOrDefault(o => o.ItemId == this.BrokenAttachmentPartId);
            if (existing is not null)
            {
                this.mod.Monitor.VerboseLog($"{this.BrokenAttachmentPartId} is already placed at {existing.TileLocation.X},{existing.TileLocation.Y}");
                return;
            }

            var position = this.FindPlaceToPutItem(preferredResourceClumpToHideUnder);
            if (position != default)
            {
                var o = ItemRegistry.Create<StardewValley.Object>(this.BrokenAttachmentPartId);
                o.Location = Game1.getFarm();
                o.TileLocation = position;
                this.mod.Monitor.VerboseLog($"{this.BrokenAttachmentPartId} placed at {position.X},{position.Y}");
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
