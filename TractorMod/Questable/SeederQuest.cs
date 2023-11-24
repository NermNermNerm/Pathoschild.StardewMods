using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Quests;
using StardewValley.TerrainFeatures;
using static Pathoschild.Stardew.TractorMod.Questable.QuestSetup;

namespace Pathoschild.Stardew.TractorMod.Questable
{
    internal class SeederQuest
        : Quest
    {
        private const int georgeSendsCanHeartLevel = 3;
        private const int evelynWillingToHelpLevel = 3;
        private const int alexWillingToHelpLevel = 2;

        // Story:
        //   Before George's accident, Grandpa gave it to George to repair, and George had given him a parts list,
        //   but Grandpa had left it for a long time, then George had the accident and Grandpa didn't ask for it
        //   and then Grandpa died, and here we are.
        //
        //   After getting 4 hearts with George, he sends you the seeder and says it's all good except for the
        //   iron bars which Grandpa never came up with.
        //
        //   If you bring it back to George, he gets all grumpy and says he mailed it to you on-purpose and he's
        //   too old and broke down to fix it.  Quest directs you to talk to his "old friends", and everybody
        //   points you to Lewis.
        //
        //   Lewis says something like "Evelyn can be a big help if you earn her trust.  Talking to Evelyn gives
        //   you a grumpy response until you get a few hearts with her and then she confides that his hands are
        //   too shaky to do fine work anymore.
        //
        //   If you talk to Alex with less than 3 hearts, he blows you off with a "these hands were made for
        //   gridball, not farm work."
        //
        //   After talking to Alex once, talking to Granny and anybody else about it will say that Alex has a
        //   hard time trusting people and you should build up some friendship with him.
        //
        //   After Alex gets to 3 hearts, he'll confide that he just doesn't feel confident and he'll just screw
        //   up and make his grandpa mad.
        //
        //   If you talk to Lewis, Emily or Penny, they'll tell you to talk to Haley.
        //
        //   Haley will (regardless of your heart level with her), declare that confident men are more attractive
        //   and that she knows for sure his hands are plenty nimble enough to do the job and tells you she'll
        //   fix it for you.  A couple days later, you get a mail from her saying that she thinks she's got
        //   him on-side.
        //
        //   After that, you ask Alex and he says he'll do it.  Give him the item and the iron and he asserts
        //   that they'll take care of it.  After a couple days, you get a mail from him saying that the job's
        //   done and tells you to pick it up from George.
        //
        //   When you pick it up from George, you get a friendship heart for him, Alex, Evelyn and half a heart
        //   for Lewis.

        private SeederQuestState state;

        private const int ironBarCount = 10;

        public SeederQuest()
            : this(SeederQuestState.GotPart)
        {
            this.showNew.Value = true;
        }

        private SeederQuest(SeederQuestState questState)
        {
            this.questTitle = "Fix the seeder";
            this.questDescription = "Turns out George had the seeder attachment, maybe he can be talked into fixing it.";
            this.state = questState;
            this.SetObjective();
        }

        public static bool IsStarted => GetModConfig<SeederQuestState>(ModDataKeys.SeederQuestStatus) != SeederQuestState.NotStarted;


        public void ReadyToInstall()
        {
            this.state = SeederQuestState.InstallPart;
            this.SetObjective();
        }

        private static void Spout(NPC n, string message)
        {
            n.CurrentDialogue.Push(new Dialogue(n, null, message));
            Game1.drawDialogue(n);
        }

        private bool pesteredGeorgeToday = false;
        private bool pesteredAlexToday = false;
        private bool pesteredHaleyToday = false;
        private bool pesteredEvelynToday = false;

        public override bool checkIfComplete(NPC? n, int number1, int number2, Item? item, string str, bool probe)
        {

            if (n?.Name == "George" && this.state == SeederQuestState.GotPart && (item is null || item.ItemId == ObjectIds.BustedSeeder) && !this.pesteredGeorgeToday)
            {
                Spout(n, "Kids these days just don't listen.  I told you, I'm too old to repair your equipment.$a");
                Game1.player.changeFriendship(-120, n);
                n.doEmote(12); // Grumpiness emote
                this.pesteredGeorgeToday = true;
                this.SetObjective();
            }
            else if (n?.Name == "Maru" && item?.ItemId == ObjectIds.BustedSeeder)
            {
                Spout(n, "I'm honored that you're asking me to look at this, but given what you've said, and what I know about George, it'd really be better if George did the work...#$b#But I know it won't be easy for him.$s#$b#But if he actually did it, well, it'd do him a lot of good.#$b#You should talk to Lewis.  He and George go back a long way.  He'll know what to do.#$b#But if it all goes wrong, come back to me and I'll try to fix it.");
            }
            else if (n?.Name == "Lewis" && this.state == SeederQuestState.GotPart && (item is null || item.ItemId == ObjectIds.BustedSeeder))
            {
                Spout(n, "Oh dear oh dear oh dear...$2#$b#Hm...$2#$b#George isn't altogether wrong here.  Physically, he's not in good shape.  Mentally, though, he's still as sharp as ever.  Alas, not like your Grandpa, towards the end.$s#$b#But if he can somehow do this, or at least help in doing it, it'll do him so much good.#$b#We're gonna need some help here...  What we need is Evelyn, and in particular, YOU have to get her to cajole George into trying this.  I can't be seen to be involved for, err...  reasons.#$b#You need to build some trust with her before you broach the topic, however.  Tread carefully.");
                this.state = SeederQuestState.GetEvelynOnSide;
                this.SetObjective();
            }
            else if (n?.Name == "Evelyn" && this.state == SeederQuestState.GetEvelynOnSide && !this.pesteredEvelynToday)
            {
                if (Game1.player.getFriendshipHeartLevelForNPC("Evelyn") >= evelynWillingToHelpLevel)
                {
                    Spout(n, "Ohhh...  I know George *could* repair that thing...  But he could also fail miserably, you see, it's not just his legs that have let him down.  Have you seen his hands, how they shake?  It's not so bad, but it really frustrates him.#$b#Oh deary, I'm old too...  I need time to think about this.");
                    this.pesteredEvelynToday = true;
                    Game1.player.mailForTomorrow.Add(MailKeys.EvelynPointsToAlex);
                    this.state = SeederQuestState.WaitForEvelyn;
                    this.SetObjective();
                }
                else
                {
                    Spout(n, "Sorry, what did you say?  I didn't quite hear...");
                    this.pesteredEvelynToday = true;
                }
            }
            else if (n?.Name == "Alex" && this.state == SeederQuestState.TalkToAlex1 && (item is null || item.ItemId == ObjectIds.BustedSeeder) && !this.pesteredAlexToday)
            {
                this.pesteredAlexToday = true;
                if (Game1.player.getFriendshipHeartLevelForNPC("Alex") < alexWillingToHelpLevel)
                {
                    Spout(n, "Look, I got my life to live, and it doesn't involve fixing farm equipment.");
                }
                else
                {
                    // Spout(n, "Yeah...  I know.  I should help my Granddad do this.  I love him so much.  But I'll just screw it up and he'll get all mad and bothered and it'll just all go wrong.  You're good with this sort of thing, you should talk Grandpa into showing you how.  Or get Maru, she's good with this sort of thing...");
                    Spout(n, "Nah man, these hands were made for Gridball.  You're good with this sort of thing, you should talk Grandpa into showing you how.  Or get Maru, she's good at mechanical stuff...");
                    this.state = SeederQuestState.GetHaleyOnSide;
                    this.SetObjective();
                }
            }
            else if (n?.Name == "Lewis" && this.state == SeederQuestState.GetHaleyOnSide && (item is null || item.ItemId == ObjectIds.BustedSeeder))
            {
                Spout(n, "Heh, isn't easy is it.  Keep it up, maybe you'll learn how to be mayor someday!$1#$b#Evelyn was probably right, in that somebody his own age could do it.  Somebody he hangs out with alot.");
            }
            else if ((n?.Name == "Sebastian" || n?.Name == "Abigail" || n?.Name == "Sam") && this.state == SeederQuestState.GetHaleyOnSide && (item is null || item.ItemId == ObjectIds.BustedSeeder))
            {
                Spout(n, "You think *I* have a clue what goes on inside Alex's head??$5#$b#Oh wait, I *DO* know...  Absolutely nothing.#4");
            }
            else if (n?.Name == "Emily" && this.state == SeederQuestState.GetHaleyOnSide && (item is null || item.ItemId == ObjectIds.BustedSeeder))
            {
                Spout(n, "Oh I hate to admit it, but the answer to your problem is Haley.  She can make any boy do any thing, ESPECIALLY Alex.^She might seem vaccuous, but trust me, if you need some guy manipulated, she can do it.  I'm not saying it's a good thing all the time, but this sounds like a good cause.");
            }
            else if (n?.Name == "Haley" && this.state == SeederQuestState.GetHaleyOnSide && (item is null || item.ItemId == ObjectIds.BustedSeeder) && !this.pesteredHaleyToday)
            {
                Spout(n, "Hah.  Alex does more with his hands than just play Gridball.$3#$b#Tell me everything. every. little. detail.$7#$b#Okay.  I'll take care of this for you, afterall, confidence is so very attractive in a man.  Just give me a couple days.  I'll let you know.");
                this.state = SeederQuestState.WaitForHaleyDay1;
                this.SetObjective();
                this.pesteredHaleyToday = true;
                Game1.player.mailForTomorrow.Add(MailKeys.HaleysWorkIsDone);
            }
            else if (n?.Name == "Alex" && this.state == SeederQuestState.TalkToAlex2 && (item is null || item.ItemId == ObjectIds.BustedSeeder))
            {
                Spout(n, $"Okay, I can do this.  Grandpa seems fired up too.  Give me the {ironBarCount} iron bars Grandpa said we need and the broken seeder and we'll get on it.");
                this.state = SeederQuestState.GiveAlexStuff;
                this.SetObjective();
            }
            else if (n?.Name == "Alex" && this.state == SeederQuestState.GiveAlexStuff && (item is null || item.ItemId == ObjectIds.BustedSeeder))
            {
                var ironStack = Game1.player.Items.FirstOrDefault(i => i?.ItemId == "335" && i.stack.Value >= ironBarCount);
                var seederStack = Game1.player.Items.FirstOrDefault(i => i?.ItemId == ObjectIds.BustedSeeder);
                if (ironStack is not null && seederStack is not null)
                {
                    if (ironStack.Stack == ironBarCount)
                    {
                        Game1.player.removeItemFromInventory(ironStack);
                    }
                    else
                    {
                        ironStack.Stack -= ironBarCount;
                    }

                    Game1.player.removeItemFromInventory(seederStack);
                    this.state = SeederQuestState.WaitForAlexDay1;
                    Spout(n, "Thanks, that's all the stuff.  Well, I'm off the the garage with Gramps.  I'll send mail or something after we get it working.");
                }
                else
                {
                    Spout(n, $"We'll need the old seeder and {ironBarCount} iron bars.  Bring 'em by when you can.");
                }
            }
            else if (n?.Name == "George" && this.state == SeederQuestState.GetPartFromGeorge && item is null)
            {
                Game1.player.addItemToInventory(new StardewValley.Object(ObjectIds.WorkingSeeder, 1));
                Spout(n, "There you go.  Fixed it myself.  Alex helped a little; he's a good kid.#$b#The seeder is as good as new.  Don't try and sprinkle chicken manure with the thing.  I don't want to see this thing back here again.");
                Game1.player.changeFriendship(240, n);
                n.doEmote(20); //hearts
                var evelyn = Game1.getCharacterFromName("Evelyn");
                Game1.player.changeFriendship(240, evelyn);
                evelyn?.doEmote(20);
                var alex = Game1.getCharacterFromName("Alex");
                Game1.player.changeFriendship(240, alex);
                alex?.doEmote(20);
                var lewis = Game1.getCharacterFromName("Lewis");
                Game1.player.changeFriendship(120, alex);
                lewis?.doEmote(32); // smiley
                this.state = SeederQuestState.InstallPart;
                this.SetObjective();
            }

            return false;
        }

        public void WorkingAttachmentBroughtToGarage()
        {
            this.questComplete();
            Game1.player.modData[ModDataKeys.SeederQuestStatus] = "Complete";
            Game1.player.removeFirstOfThisItemFromInventory(ObjectIds.WorkingSeeder);
            Game1.DrawDialogue(new Dialogue(null, null, "Awesome!  You've now got a way to plant and fertilize crops with your tractor!#$b#HINT: To use it, equip seeds or fertilizers while on the tractor."));
        }

        private void SetObjective()
        {
            switch (this.state)
            {
                case SeederQuestState.GotPart:
                    this.currentObjective = "Hm.  I wonder what I should do, I certainly can't fix it, and it'd cost a fortune to send it to Zuza city.";
                    break;
                case SeederQuestState.GetEvelynOnSide:
                    this.currentObjective = $"Get Evelyn to help (Talk to her after getting her to {evelynWillingToHelpLevel} hearts)";
                    break;
                case SeederQuestState.WaitForEvelyn:
                    this.currentObjective = "Granny said she'll think about it.  I guess we'll wait to hear from her.";
                    break;
                case SeederQuestState.TalkToAlex1:
                    this.currentObjective = "Granny's mail said I should gain Alex's trust (2 hearts) and try and talk him into it.";
                    break;
                case SeederQuestState.GetHaleyOnSide:
                    this.currentObjective = "Alex still seems resistant to the idea.  Maybe I need to get somebody else to give me another angle.";
                    break;
                case SeederQuestState.WaitForHaleyDay1:
                    this.currentObjective = "Wait for Haley to cajole Alex into helping.";
                    break;
                case SeederQuestState.TalkToAlex2:
                    this.currentObjective = "Talk to Alex";
                    break;
                case SeederQuestState.GiveAlexStuff:
                    this.currentObjective = $"Bring Alex the old seeder and {ironBarCount} iron bars";
                    break;
                case SeederQuestState.WaitForAlexDay1:
                case SeederQuestState.WaitForAlexDay2:
                    this.currentObjective = "Hopefully Alex and George are getting on with it.  Alex said he'd send mail when it's working.";
                    break;
                case SeederQuestState.GetPartFromGeorge:
                    this.currentObjective = "Alex's mail says the seeder is done and I just need to get it from George.";
                    break;
                case SeederQuestState.InstallPart:
                    this.currentObjective = "Bring the fixed seeder to the tractor garage.";
                    break;
            }
        }

        public string Serialize() => this.state.ToString();

        private static bool TryParseQuestStatus(string? s, out SeederQuestState state)
        {
            if (s is null)
            {
                state = SeederQuestState.NotStarted;
                return true;
            }

            return Enum.TryParse(s, out state);
        }

        internal static void OnDayStart(ModEntry mod)
        {
            if (Game1.player.getFriendshipHeartLevelForNPC("George") >= georgeSendsCanHeartLevel && RestoreTractorQuest.IsTractorUnlocked && !Game1.player.modData.ContainsKey(ModDataKeys.SeederQuestGeorgeSentMail))
            {
                Game1.player.mailbox.Add(MailKeys.GeorgeSeederMail);
                Game1.player.modData[ModDataKeys.SeederQuestGeorgeSentMail] = "sent";
            }

            Game1.player.modData.TryGetValue(ModDataKeys.SeederQuestStatus, out string? statusAsString);
            if (!TryParseQuestStatus(statusAsString, out SeederQuestState state))
            {
                mod.Monitor.Log($"Invalid value for {ModDataKeys.SeederQuestStatus}: {statusAsString} -- reverting to NotStarted", LogLevel.Error);
                state = SeederQuestState.NotStarted;
            }

            if (state == SeederQuestState.WaitForEvelyn)
            {
                state = SeederQuestState.TalkToAlex1;
            }
            else if (state == SeederQuestState.WaitForHaleyDay1)
            {
                state = SeederQuestState.TalkToAlex2;
            }
            else if (state == SeederQuestState.WaitForAlexDay1)
            {
                state = SeederQuestState.WaitForAlexDay2;
                Game1.player.mailForTomorrow.Add(MailKeys.AlexThankYouMail);
            }
            else if (state == SeederQuestState.WaitForAlexDay2)
            {
                state = SeederQuestState.GetPartFromGeorge;
            }

            if (state != SeederQuestState.NotStarted && state != SeederQuestState.Complete)
            {
                var q = new SeederQuest(state);
                q.MarkAsViewed();
                Game1.player.questLog.Add(q);
            }
        }
    }
}
