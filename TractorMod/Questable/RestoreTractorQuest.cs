using System;
using System.Collections;
using System.Linq;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Quests;

namespace Pathoschild.Stardew.TractorMod.Questable
{
    public class RestoreTractorQuest
        : Quest
    {
        private RestorationState state = RestorationState.NotStarted;

        public RestoreTractorQuest(RestorationState state)
        {
            this.questTitle = "Investigate the tractor";
            this.questDescription = "You've found a rusty old tractor in the fields.  It sure would be nice if we can restore it.  Perhaps the townspeople can help.";
            this.SetState(state);
        }

        private void SetState(RestorationState state)
        {
            this.state = state;

            switch (state)
            {
                case RestorationState.TalkToLewis:
                    this.currentObjective = "Talk to mayor Lewis";
                    break;

                case RestorationState.TalkToSebastian:
                    this.currentObjective = "Ask Sebastian to help restore the tractor";
                    break;

                case RestorationState.TalkToLewisAgain:
                    this.currentObjective = "Welp, Sebastian was a bust.  Maybe Mayor Lewis knows somebody else who could be more helpful.";
                    break;

                case RestorationState.WaitingForMailFromRobinDay1:
                    this.currentObjective = "Wait for Lewis to work his magic";
                    break;

                default:
                    this.currentObjective = "TODO";
                    break;
            }
        }

        public static void BeginQuest()
        {
            var q = new RestoreTractorQuest(RestorationState.TalkToLewis);
            Game1.player.questLog.Add(q);
        }

        public static void RestoreQuest(RestorationState state)
        {
            if (state == RestorationState.Complete || state == RestorationState.NotStarted)
            {
                return;
            }

            var q = new RestoreTractorQuest(RestorationState.TalkToLewis);
            q.MarkAsViewed();
            Game1.player.questLog.Add(q);
        }

        public static void Spout(NPC n, params string[] dialogitems)
        {
            new DialogueBox(dialogitems.ToList());
        }

        public override bool checkIfComplete(NPC? n, int number1, int number2, Item? item, string str, bool probe)
        {
            if (n?.Name == "Lewis" && this.state == RestorationState.TalkToLewis)
            {
                n.CurrentDialogue.Push(new Dialogue(n, null, "An old tractor you say?#$b#I know your Grandfather had one - I thought he had sold it off before he died.  He never could keep it on the furrows.$h#$b#If you want to get it fixed, I suggest you talk to Robin's son, Sebastian; he's actually quite the gearhead.  Maybe he can help."));
                this.SetState(RestorationState.TalkToSebastian);
            }
            else if (n?.Name == "Sebastian" && this.state == RestorationState.TalkToSebastian)
            {
                n.CurrentDialogue.Push(new Dialogue(n, null, "Let me get this straight - I barely know who you are and I'm supposed to fix your rusty old tractor?$a#$b#Sorry, but I've got a lot of stuff going on and can't really spare the time."));
                this.SetState(RestorationState.TalkToLewisAgain);
            }
            else if (n?.Name == "Lewis" && this.state == RestorationState.TalkToLewisAgain)
            {
                n.CurrentDialogue.Clear();
                n.CurrentDialogue.Push(new Dialogue(n, null, "He said that?$a#$b#Well, I can't say I'm really surprised...  A bit disappointed, tho.$s#$b#Hm. . .$u#$b#Welp, I guess this is why they pay me the big money, eh?  I'll see if I can make this happen for you, but it might take a couple days."));
                //n.CurrentDialogue.Clear();
                //n.CurrentDialogue.Push(new Dialogue(n, null, "He said that?#$b#Well, I can't say I'm really surprised...  A bit disappointed, tho.#$b#Hm. . .#$b#Welp, I guess this is why they pay me the big money, eh?  I'll see if I can make this happen for you, but it might take a couple days."));
                this.SetState(RestorationState.WaitingForMailFromRobinDay1);
            }

            return base.checkIfComplete(n, number1, number2, item, str, probe);
        }

        internal string Serialize() => this.state.ToString();
    }
}
