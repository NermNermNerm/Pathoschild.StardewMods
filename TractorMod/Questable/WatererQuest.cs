using System;
using System.Linq;
using StardewValley;

namespace Pathoschild.Stardew.TractorMod.Questable
{
    public class WatererQuest
        : BaseQuest<WatererQuestState>
    {
        private WatererQuestState investigationState;

        private const int goldBarCount = 10;

        public WatererQuest()
            : this(WatererQuestState.NoCluesYet)
        {
            this.showNew.Value = true;
        }

        private WatererQuest(WatererQuestState questState)
            : base(questState)
        {
            this.questTitle = "Fix the waterer";
            this.questDescription = "I found the watering attachment for the tractor, but it's in bad shape, I should ask around town.";
        }

        private bool pesteredMaruToday = false;

        public override bool checkIfComplete(NPC? n, int number1, int number2, Item? item, string str, bool probe)
        {
            if (n?.Name == "Maru" && item is null && this.investigationState == WatererQuestState.WaitForMaruDay1 && !this.pesteredMaruToday)
            {
                Spout(n, "I'm still working on the irrigation system; you should have it day after tomorrow.");
                this.pesteredMaruToday = true;
                return false;
            }
            else if (n?.Name == "Maru" && item is null && this.investigationState == WatererQuestState.WaitForMaruDay2 && !this.pesteredMaruToday)
            {
                Spout(n, "I'm working through the bugs on the irrigation system; you should have it tomorrow if nothing goes haywire.$3");
                this.pesteredMaruToday = true;
                return false;
            }

            if (n is not null && new string[] { "Clint", "Lewis", "Pierre", "Abigail", "Pam", "Marnie", "Willy", "Linus", "Gus", "George", "Caroline" }.Contains(n.Name))
            {
                Spout(n, "Oh, that isn't...  It is!  It's your grandpa's legendary irrigation attachment!  And you *fished* it up you say?  Hah!  Well you would'a had to, wouldn'tya!$1#$b#. . . #$b#Sorry, I gotta compose myself.  You'll want to take this up to the mountain.  Show it to Robin, she can give you the first-hand account.$1#$b#It might not be safe to show it to Demetrius.  I think he's still working through the afteraffects...$s");

                // If you show it to Willy first, you get half a heart with him due to your devotion to the art o' fishin'
                if (n.Name == "Willy" && this.investigationState == WatererQuestState.NoCluesYet)
                {
                    Game1.player.changeFriendship(120, n);
                    n.doEmote(20);
                }

                if (this.investigationState == WatererQuestState.NoCluesYet)
                {
                    this.investigationState = WatererQuestState.RobinFingered;
                    this.SetObjective();
                }
            }
            else if (n?.Name == "Demetrius")
            {
                Spout(n, "Oh my!  Is that the irrigation system?  It is!  None of us expected to see that again, not after that . . .$3#$b#Wait, did Robin put you up to this?#$b#Nope, I don't want to know.  Well, yes, I got very wet, but it wasn't any big deal.  Not nearly as much as she plays it up to be.  Not at all...#4#$b#But you just want the thing fixed, don't you.  Well, it seems like a complicated device, but I bet Maru would have no trouble with it.  Why don't you show it to her?");
                if (this.investigationState < WatererQuestState.MaruFingered)
                {
                    this.investigationState = WatererQuestState.MaruFingered;
                    this.SetObjective();
                    Game1.player.changeFriendship(-60, n);
                    n.doEmote(12); // grumpy
                }
            }
            else if (n?.Name == "Robin")
            {
                Spout(n, "Oh you didn't!!  You fished up the watering doohickey?  Oh my I'll never forget that day!  Your granddad had the idea that instead of using the pump to fill it up he could just back his tractor into the pond.  Suffice it to say the tractor came out, but the irrigator did not!  He huffed up to the mountain, half soaked, thinking that Demetrius would have a net.  Well Demetrius was feeling especially can-do that day and offered to come help and, well, Maru was quite small and just had to go with her Dad everywhere and so I came along to ride herd.  Long story short, Demetrius ended covered in mud right up to his starched buttoned up collar.  Maru decided to go rescue him, I went chasing after her, and, well, we all ended up wet, but Demetrius, well, his self-image took a hit that day, heh.#$b#Heh, and you know what happened to the irrigator, donchanow!$l#$b#You should take it to Maru and, heh, youknow, best not to bring it up with Demetrius!$4");
                if (this.investigationState < WatererQuestState.MaruFingered)
                {
                    if (this.investigationState < WatererQuestState.MaruFingered)
                    {
                        this.investigationState = WatererQuestState.MaruFingered;
                        this.SetObjective();
                        Game1.player.changeFriendship(60, n);
                        n.doEmote(32); // smily
                    }
                }
                return false;
            }
            else if (n?.Name == "Maru")
            {
                switch (this.investigationState)
                {
                    case WatererQuestState.NoCluesYet:
                    case WatererQuestState.RobinFingered:
                    case WatererQuestState.MaruFingered:
                        Spout(n, $"Sure, I'd love to have a go at fixing it, afterall, it's practically a family heirloom!$4#$b#But let's have a look at it...  Hm...$2#$b#Yeah, if you can get me {goldBarCount} gold bars, I can get it working again.");
                        this.investigationState = WatererQuestState.GetGoldBars;
                        this.SetObjective();
                        break;
                    case WatererQuestState.GetGoldBars:
                        if (this.TryTakeItemsFromPlayer("336", goldBarCount)) //336=gold bar
                        {
                            Spout(n, "Alrighty, I'll get to work on it and have it back to you in a couple days.  I'll just drop it in the mail for you.");
                            this.investigationState = WatererQuestState.WaitForMaruDay1;
                            this.SetObjective();
                            return false;
                        }
                        else
                        {
                            Spout(n, "Have you found some gold bars yet?  Gotta go pretty deep in the mines to get it, but I'm sure you're up for it.");
                        }
                        break;
                }
            }

            return false;
        }

        protected override void SetObjective()
        {
            switch (this.investigationState)
            {
                case WatererQuestState.NoCluesYet:
                    this.currentObjective = "Ask the people in town about this thing.";
                    break;
                case WatererQuestState.RobinFingered:
                    this.currentObjective = "Ask Robin about this thing.";
                    break;
                case WatererQuestState.MaruFingered:
                    this.currentObjective = "Take it to Maru to see if she'll fix it.";
                    break;
                case WatererQuestState.GetGoldBars:
                    this.currentObjective = $"Bring the watering can and {goldBarCount} gold bars to Maru.";
                    break;
                case WatererQuestState.WaitForMaruDay1:
                case WatererQuestState.WaitForMaruDay2:
                    this.currentObjective = "Just gotta wait - should be in the mail any day now.";
                    break;
                case WatererQuestState.InstallPart:
                    this.currentObjective = "Bring the fixed waterer to the garage.";
                    break;
            }
        }

        public override void AdvanceStateForDayPassing()
        {
            if (this.State == WatererQuestState.WaitForMaruDay1)
            {
                this.State = WatererQuestState.WaitForMaruDay2;
                Game1.player.mailForTomorrow.Add(MailKeys.WatererRepaired);
            }
        }

        public override void GotWorkingPart(Item workingPart)
        {
            Spout("Maru came through!  Time to take it to the garage and water some crops!");
            this.State = WatererQuestState.InstallPart;
        }
    }
}
