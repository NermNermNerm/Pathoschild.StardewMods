using System;
using System.Linq;
using StardewValley;
using StardewValley.Quests;

namespace Pathoschild.Stardew.TractorMod.Questable
{
    internal class BorrowHarpoonQuest
        : Quest
    {
        private enum HarpoonQuestState
        {
            GetThePole,
            CatchTheBigOne,
            ReturnThePole
        }

        private HarpoonQuestState state;

        public BorrowHarpoonQuest()
            : this(HarpoonQuestState.GetThePole)
        {
            this.ShouldDisplayAsNew();
        }

        private BorrowHarpoonQuest(HarpoonQuestState state)
        {
            this.questTitle = "We need a bigger pole";
            this.questDescription = "There's something big down at the bottom of the farm pond.  Maybe Willy can loan me something to help get it out.";
            this.State = state;
        }

        private HarpoonQuestState State
        {
            get => this.state;
            set
            {
                this.state = value;
                switch (this.state)
                {
                    case HarpoonQuestState.GetThePole:
                        this.currentObjective = "Go find Willy in his shop";
                        break;
                    case HarpoonQuestState.CatchTheBigOne:
                        this.currentObjective = "Use Willy's harpoon to haul in whatever's at the bottom of the pond";
                        break;
                    case HarpoonQuestState.ReturnThePole:
                        this.currentObjective = "Give the Harpoon back to Willy";
                        break;
                }
            }
        }

        public override bool checkIfComplete(NPC n, int number1, int number2, Item item, string str, bool probe)
        {
            var harpoon = Game1.player.Items.FirstOrDefault(i => i?.ItemId == WatererQuestController.HarpoonToolId);
            if (n?.Name == "Willy" && this.state == HarpoonQuestState.ReturnThePole && harpoon is not null)
            {
                this.Spout(n, "Ya reeled that ol water tank on wheels in, did ya laddy!$3#$b#Aye I do believe this'll be the talk of the Stardrop for many Fridays to come!$1");
                Game1.player.removeItemFromInventory(harpoon);
                Game1.player.changeFriendship(240, n);
                n.doEmote(20); // hearts
                this.questComplete();
                return true;
            }
            else if (n?.Name == "Willy" && this.state == HarpoonQuestState.GetThePole && Game1.player.currentLocation.Name == "FishShop")
            {
                this.Spout(n, "Ah laddy...  I do think I know what you mighta hooked into and yer right that ya need a lot more pole than what you got.#$b#Here's a wee bit o' fishin' kit that my great great grandpappy used to land whales back before we knew better.#$b#I think ya will find it fit for tha purpose.");

                // TODO: Worry about full inventory
                _ = Game1.player.addItemToInventory(ItemRegistry.Create(WatererQuestController.HarpoonToolId));
                this.State = HarpoonQuestState.CatchTheBigOne;
            }
            else if (n?.Name == "Willy" && this.state == HarpoonQuestState.GetThePole && Game1.player.currentLocation.Name != "FishShop")
            {
                this.Spout(n, "Ah laddy...  I do think I know what you mighta hooked into and yer right that ya need a lot more pole than what you got.#$b#Come visit me in my shop and I'll show you something that might work");
            }
            return false;
        }

        internal static void OnDayStart(QuestSetup questSetup)
        {
            Game1.player.modData.TryGetValue(ModDataKeys.BorrowHarpoonQuestStatus, out string statusAsString);
            if (statusAsString is null || !Enum.TryParse(statusAsString, out HarpoonQuestState state))
            {
                return;
            }

            var quest = new BorrowHarpoonQuest(state);
            quest.MarkAsViewed();
            Game1.player.questLog.Add(quest);
        }

        internal static void OnDayEnding()
        {
            var quest = Game1.player.questLog.OfType<BorrowHarpoonQuest>().FirstOrDefault();
            if (quest is not null)
            {
                Game1.player.modData[ModDataKeys.BorrowHarpoonQuestStatus] = quest.state.ToString();
            }
            else
            {
                Game1.player.modData.Remove(ModDataKeys.BorrowHarpoonQuestStatus);
            }
            Game1.player.questLog.RemoveWhere(q => q is BorrowHarpoonQuest);
        }

        internal static void StartQuest()
        {
            if (!Game1.player.questLog.OfType<BorrowHarpoonQuest>().Any())
            {
                Game1.addHUDMessage(new HUDMessage("Whoah, I snagged onto something big down there, but this line's nowhere near strong enough to yank it up!", HUDMessage.newQuest_type));
                Game1.player.questLog.Add(new BorrowHarpoonQuest());
            }
        }

        internal static void GotTheBigOne()
        {
            var quest = Game1.player.questLog.OfType<BorrowHarpoonQuest>().FirstOrDefault();
            if (quest is null)
            {
                QuestSetup.Instance.Monitor.Log("BorrowHarpoon quest was not open when player caught waterer", StardewModdingAPI.LogLevel.Warn);
            }
            else
            {
                quest.State = HarpoonQuestState.ReturnThePole;
            }
        }

        public void Spout(NPC n, string message)
        {
            // COPYPASTA from QuestBase.  Maybe one day make this a subclass of QuestBase?

            // Conversation keys and location specific dialogs take priority.  We can't fix the location-specific
            // stuff, but we can nix conversation topics.

            // Forces it to see if there are Conversation Topics that can be pulled down.
            // Pulling them down toggles their "only show this once" behavior.
            n.checkForNewCurrentDialogue(Game1.player.getFriendshipHeartLevelForNPC(n.Name));

            // TODO: Only nix topics that are for this mod.
            // Can (maybe) be culled off of the tail end of 'n.CurrentDialogue.First().TranslationKey'
            n.CurrentDialogue.Clear();

            n.CurrentDialogue.Push(new Dialogue(n, null, message));
            Game1.drawDialogue(n); // <- Push'ing or perhaps the clicking on the NPC causes this to happen anyway. so not sure if it actually helps.
        }
    }
}
