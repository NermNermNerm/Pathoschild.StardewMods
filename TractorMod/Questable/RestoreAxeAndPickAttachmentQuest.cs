using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Quests;
using static Pathoschild.Stardew.TractorMod.Questable.QuestSetup;

namespace Pathoschild.Stardew.TractorMod.Questable
{
    internal class RestoreAxeAndPickAttachmentQuest
        : Quest
    {
        private AxeAndPickQuestState state = AxeAndPickQuestState.NotStarted;
        private bool hasDoneStatusCheckToday = false;

        private RestoreAxeAndPickAttachmentQuest(AxeAndPickQuestState state)
        {
            this.questTitle = "Fix the loader";
            this.questDescription = "I found the front end loader attachment for the tractor, but it's all bent up and rusted through in spots.  Looks like a the sort of thing a blacksmith could fix.";
            this.SetState(state);
        }

        public RestoreAxeAndPickAttachmentQuest()
            : this(AxeAndPickQuestState.TalkToClint)
        {
            this.showNew.Value = true;
        }

        private void SetState(AxeAndPickQuestState state)
        {
            this.state = state;

            switch (state)
            {
                case AxeAndPickQuestState.TalkToClint:
                    this.currentObjective = "Find someone to fix the loader.";
                    break;
                case AxeAndPickQuestState.InstallTheLoader:
                    this.currentObjective = "Take the fixed loader attachment to the tractor garage.";
                    break;
            }
        }

        public static void Spout(NPC n, string message)
        {
            n.CurrentDialogue.Push(new Dialogue(n, null, message));
            Game1.drawDialogue(n);
        }

        public string Serialize() => this.state.ToString();

        public override bool checkIfComplete(NPC? n, int number1, int number2, Item? item, string str, bool probe)
        {
            if(n?.Name == "Clint" && item?.ItemId == QuestSetup.ObjectIds.BustedLoader)
            {
                // TODO: Properly implement the quest
                Game1.player.removeItemFromInventory(item);
                _ = Game1.player.addItemToInventory(new StardewValley.Object(QuestSetup.ObjectIds.WorkingLoader, 1));
                this.SetState(AxeAndPickQuestState.InstallTheLoader);
            }

            return false;
        }

        public void WorkingAttachmentBroughtToGarage()
        {
            this.questComplete();
            Game1.player.modData[QuestSetup.ModDataKeys.AxeAndPickQuestStatus] = AxeAndPickQuestState.Complete.ToString();
            Game1.player.removeFirstOfThisItemFromInventory(ObjectIds.WorkingLoader);
            Game1.DrawDialogue(new Dialogue(null, null, "Sweet!  You've now got a front-end loader attachment for your tractor to clear out debris!#$b#HINT: To use it, equip the pick or the axe while on the tractor."));
        }

        internal static void OnDayStart(IModHelper helper, IMonitor monitor)
        {

            if (!Game1.player.modData.TryGetValue(ModDataKeys.AxeAndPickQuestStatus, out string? statusAsString)
                || !Enum.TryParse(statusAsString, true, out AxeAndPickQuestState axeAndPickQuestStatus))
            {
                if (statusAsString is not null)
                {
                    monitor.Log($"Invalid value for {ModDataKeys.AxeAndPickQuestStatus}: {statusAsString} -- reverting to NotStarted", LogLevel.Error);
                }
                axeAndPickQuestStatus = AxeAndPickQuestState.NotStarted;
            }

            if (axeAndPickQuestStatus == AxeAndPickQuestState.NotStarted)
            {
                if (!Game1.getFarm().objects.Values.Any(o => o.ItemId == ObjectIds.BustedLoader))
                {
                    // TODO: Pick a spot randomly
                    var o = ItemRegistry.Create<StardewValley.Object>(ObjectIds.BustedLoader);
                    o.Location = Game1.getFarm();
                    o.TileLocation = new Vector2(56, 38);
                    o.IsSpawnedObject = true;
                    _ = Game1.getFarm().objects.TryAdd(new Vector2(56, 38), o);
                }
            }

            if (axeAndPickQuestStatus != AxeAndPickQuestState.Complete && axeAndPickQuestStatus != AxeAndPickQuestState.NotStarted)
            {
                var q = new RestoreAxeAndPickAttachmentQuest(axeAndPickQuestStatus);
                q.MarkAsViewed();
                Game1.player.questLog.Add(q);
            }
        }
    }
}
