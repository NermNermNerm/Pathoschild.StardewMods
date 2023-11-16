using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Netcode;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Quests;

namespace Pathoschild.Stardew.TractorMod.Questable
{
    public class RestoreTractorQuest
        : Quest
    {
        private RestorationState state = RestorationState.NotStarted;

        private static class MailKeys {
            public const string BuildTheGarage = "QuestableTractorMod.BuildTheGarage";
        };

        public RestoreTractorQuest(RestorationState state)
        {
            this.questTitle = "Investigate the tractor";
            this.questDescription = "There's a rusty old tractor in the fields; it sure would be nice if it could be restored.  Perhaps the townspeople can help.";
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
                case RestorationState.WaitingForMailFromRobinDay2:
                    this.currentObjective = "Wait for Lewis to work his magic";
                    break;

                case RestorationState.BuildTractorGarage:
                    this.currentObjective = "Get Robin to build you a garage to get the tractor out of the weather.";
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

            var newStateForToday = state;
            switch (state)
            {
                case RestorationState.WaitingForMailFromRobinDay1:
                    newStateForToday = RestorationState.WaitingForMailFromRobinDay2;
                    break;
                case RestorationState.WaitingForMailFromRobinDay2:
                    newStateForToday = RestorationState.BuildTractorGarage;
                    Game1.addMail(MailKeys.BuildTheGarage);
                    break;
                case RestorationState.BuildTractorGarage:
                    // TODO: If garage is built:
                    // newStateForToday = RestorationState.WaitingForSebastianDay1;
                    break;
                case RestorationState.WaitingForSebastianDay1:
                    newStateForToday = RestorationState.WaitingForSebastianDay2;
                    break;
                case RestorationState.WaitingForSebastianDay2:
                    // TODO: send mail with engine
                    break;

                case RestorationState.BringStuffToForest:
                    // TODO: Check if the player left the goodies in the forest and if so:
                    // newStateForToday = RestorationState.BringEngineToSebastian;
                    break;
            }

            var q = new RestoreTractorQuest(newStateForToday);
            q.MarkAsViewed();
            Game1.player.questLog.Add(q);
        }

        public string Serialize() => this.state.ToString();

        public static void Spout(NPC n, params string[] dialogitems)
        {
            new DialogueBox(dialogitems.ToList());
        }

        public override bool checkIfComplete(NPC? n, int number1, int number2, Item? item, string str, bool probe)
        {
            if (n?.Name == "Lewis" && this.state == RestorationState.TalkToLewis)
            {
                n.CurrentDialogue.Push(new Dialogue(n, null, "An old tractor you say?#$b#I know your Grandfather had one - I thought he had sold it off before he died.  He never could keep it on the furrows.$h#$b#If you want to get it fixed, I suggest you talk to Robin's son, Sebastian; he's actually quite the gearhead.  Maybe he can help."));
                Game1.drawDialogue(n);
                this.SetState(RestorationState.TalkToSebastian);
            }
            else if (n?.Name == "Sebastian" && this.state == RestorationState.TalkToSebastian)
            {
                n.CurrentDialogue.Push(new Dialogue(n, null, "Let me get this straight - I barely know who you are and I'm supposed to fix your rusty old tractor?$a#$b#Sorry, but I've got a lot of stuff going on and can't really spare the time."));
                Game1.drawDialogue(n);
                this.SetState(RestorationState.TalkToLewisAgain);
            }
            else if (n?.Name == "Lewis" && this.state == RestorationState.TalkToLewisAgain)
            {
                n.CurrentDialogue.Push(new Dialogue(n, null, "He said that?$a#$b#Well, I can't say I'm really surprised...  A bit disappointed, tho.$s#$b#Hm. . .$u#$b#Welp, I guess this is why they pay me the big money, eh?  I'll see if I can make this happen for you, but it might take a couple days."));
                Game1.drawDialogue(n);
                this.SetState(RestorationState.WaitingForMailFromRobinDay1);
            }

            return base.checkIfComplete(n, number1, number2, item, str, probe);
        }

        public bool CanBuildGarage => this.state.CanBuildGarage();


        public static void AddMailItems(IDictionary<string, string> mailItems)
        {
            mailItems[MailKeys.BuildTheGarage] = "Hey there!^I talked with Sebastian about your tractor and he has agreed to work on it, but only if he's got a decent place to work.  I understand that you're just starting out here and don't have a lot of money laying around, so I'm willing to do it at-cost, providing you can come up with the materials.  Come by my shop for a full list of materials.  See you soon!^  - Robin";
        }
    }
}
