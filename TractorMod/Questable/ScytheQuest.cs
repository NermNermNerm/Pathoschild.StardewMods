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
    internal class ScytheQuest
        : Quest
    {
        private ScytheQuestState investigationState;
        private bool jazTradeKnown;
        private bool vincentTradeKnown;
        private bool jazPartGot;
        private bool vincentPartGot;

        public ScytheQuest()
            : this(ScytheQuestState.NoCluesYet, false, false, false, false)
        {
            this.showNew.Value = true;
        }

        private ScytheQuest(ScytheQuestState questState, bool jazTradeKnown, bool vincentTradeKnown, bool jazPartGot, bool vincentPartGot)
        {
            this.questTitle = "Fix the harvester";
            this.questDescription = "I found the harvester attachment for the tractor, but won't work like it is now.  I should ask around town about it.";
            this.investigationState = questState;
            this.jazPartGot = jazPartGot;
            this.vincentPartGot = vincentPartGot;
            this.jazTradeKnown = jazTradeKnown;
            this.vincentTradeKnown = vincentTradeKnown;
            this.SetObjective();
        }

        public void ReadyToInstall()
        {
            this.investigationState = ScytheQuestState.InstallPart;
            this.SetObjective();
        }

        private static void Spout(NPC n, string message)
        {
            n.CurrentDialogue.Push(new Dialogue(n, null, message));
            Game1.drawDialogue(n);
        }

        public override bool checkIfComplete(NPC? n, int number1, int number2, Item? item, string str, bool probe)
        {
            if (this.vincentTradeKnown && n?.Name == "Vincent")
            {
                var crayfishStack = Game1.player.Items.FirstOrDefault(i => i.ItemId == "716" && i.stack.Value >= 3); // Crayfish
                if (crayfishStack is not null)
                {
                    if (crayfishStack.Stack == 3)
                    {
                        Game1.player.removeItemFromInventory(crayfishStack);
                    }
                    else
                    {
                        crayfishStack.Stack -= 3;
                    }

                    _ = Game1.player.addItemToInventory(new StardewValley.Object(ObjectIds.ScythePart1, 1));
                    // TODO: if addItemToInventory fails, it returns the item.  Could make the item into loose litter in that case.

                    Spout(n, "Ooh!  Oh these 3 crawdads would be great!  Thanks!  Here's your thingamajig.  Hope it works!$l#$b#Sure, I won't tell Mom that you gave them to me if you want...  Why?$s");
                    this.vincentPartGot = true;
                    this.SetObjective();
                    return false;
                }
            }

            if (this.jazTradeKnown && n?.Name == "Jas")
            {
                // It'd be nice if there was a way to make this a little more interactive with Jaz having, like a random taste-of-the-day and
                //  only one gem will be shiny enough on that day.  I don't see a way to make that happen right now.
                foreach (string shinyItemId in new string[] { "80" /* quartz */, "82" /* fire quartz */, "68" /* topaz */, "66" /* amethyst */, "60" /* Emerald */, "62" /* Aquamarine */, "70" /* jade */})
                {
                    var shinyThing = Game1.player.Items.FirstOrDefault(i => i?.ItemId == shinyItemId);
                    if (shinyThing is not null)
                    {
                        if (shinyThing.Stack == 1)
                        {
                            Game1.player.removeItemFromInventory(shinyThing);
                        }
                        else
                        {
                            shinyThing.Stack -= 1;
                        }

                        _ = Game1.player.addItemToInventory(new StardewValley.Object(ObjectIds.ScythePart2, 1));
                        // TODO: if addItemToInventory fails, it returns the item.  Could make the item into loose litter in that case.

                        Spout(n, $"Ooh!  Oh this {shinyThing.DisplayName} is very sparkly!  Thanks!  Here's your thingamajig.  I sure hope it works!  I really want to ride on your tractor some day!$l");
                        this.jazPartGot = true;
                        this.SetObjective();
                        return false;
                    }
                }
            }


            if (item?.ItemId != QuestSetup.ObjectIds.BustedScythe || n is null)
            {
                return false;
            }

            switch (n.Name)
            {
                case "Clint":
                    Spout(n, "Let's see what you go there...$s#$b#You know, there's no real metalwork to do here.  It basically needs a good cleaning and oiling.$h#$b#But something seems wrong about it still, like are you sure you got all of it out of the weeds?");
                    this.SetMissingPartsKnown();
                    break;
                case "Robin":
                    Spout(n, "Oh yeah, that's the old harvester!  I think your Grandpa broke every other thing, but I don't recall any misadventures with that part.  If you can't get it to work, you might ask Demetrius or Maru.  They're good at metalworking.");
                    break;
                case "Maru":
                case "Demetrius":
                    Spout(n, "Oh yeah, that's the old harvester!  You know, I think it's okay, but I don't think it's quite complete.  Looks like there might be some parts missing.#$b#You're getting pretty good around the farm, I'm sure you could get it to work again if you can find the rest of the parts.  Let me know if you need help with it, though, I'd be happy to help.");
                    this.SetMissingPartsKnown();
                    break;
                case "Lewis":
                    Spout(n, "Oh I'm not much for farm equipment.  You say that's a harvester?  Well, I'll have to take your word for it!$h#$b#Yaknow he broke a lot of stuff on that tractor, but I don't recall that being one of them.  Maybe it just needs cleaning and oiling?");
                    break;
                case "Marnie":
                    Spout(n, "Oh that's the old harvester!  Or most of one anyway...$s#$b#Well, did you look around where you found that piece?  Might be more pieces under that log.#$b#You might enlist Jaz and Vincent to help - they used to play out in your south pasture, but when you moved back in, I asked them to keep clear so you could get your work done.  But they know that area pretty well.");
                    this.SetMissingPartsKnown();
                    break;
                case "Abigail":
                    Spout(n, "Oh that's a *harvester*?  I never would have guessed!  Yep, I saw it on your farm and always wondered about it...#$b#Well, unless I was prepared to accept Jaz and Vincent's explanation that it was a machine made by Greebles!$l");
                    this.SetJazAndVincentFingered();
                    break;
                case "Penny":
                    if (this.investigationState == ScytheQuestState.NoCluesYet)
                    {
                        Spout(n, "That's the old harvester for the tractor?  I guess it looks like that.");
                    }
                    else
                    {
                        Spout(n, "That's the old harvester for the tractor?  I guess it looks like that.  You think it's incomplete?$s#$b#You might ask Jaz and Vincent to help look for other parts like that - they told me they used to play out in your south pasture, but they don't anymore because Marnie shooed them away.");
                        this.SetJazAndVincentFingered();
                    }
                    break;
                case "Jodi":
                    if (this.investigationState == ScytheQuestState.NoCluesYet)
                    {
                        Spout(n, "What is that thing?  A harvester?  Sure, if you say so...  I tell you though, something about that thing seems familiar to me.$s#$b#Well, it looks like a mess, and I've cleaned up a lot messes.  It must be that!$l");
                    }
                    else
                    {
                        Spout(n, "That's the old harvester for the tractor?  I guess it looks like that.  You think it's incomplete?$s#$b#You might ask Jaz and Vincent to help look for other parts like that - they told me they used to play out in your south pasture, but Marnie and I asked them not to since you moved in.#$b#You've got enough on your hands without those two hooligans running through the corn!$l");
                        this.SetJazAndVincentFingered();
                    }
                    break;
                case "Wizard":
                    if (Game1.Date.DayOfWeek == DayOfWeek.Friday || Game1.Date.DayOfWeek == DayOfWeek.Saturday)
                    {
                        Spout(n, "Aha!!!!  I FORSEE THAT YOU HAVE NEED OF A SCRY!$a#$b#And you've come at the perfect time.  I happen to have need to flex my muscles from time to time, to keep in tip top form, you know.  Let's have a look#$b#Aahh through the mists I see two people, NO, CHILDREN, in your farm playing...#$b#They have taken parts of off and headed off...  But wait, there's more...  Yes...#$b#They will each need to be placated to get them to give the parts back.  The boy will want 3 crayfish and the girl will want a gemstone, but her tastes are not yet developed, so a cheap one will do...");
                        this.jazTradeKnown = true;
                        this.vincentTradeKnown = true;
                        this.SetJazAndVincentFingered();
                    }
                    else
                    {
                        Spout(n, "BAH!  Begone with this mundane contrivance!$a");
                    }
                    break;
                case "Jas":
                    if (this.jazPartGot)
                    {
                        Spout(n, "I love how my jumprope sparkles in the sun now!  Did you have any luck getting it to fit back together?");
                    }
                    else if (this.jazTradeKnown)
                    {
                        Spout(n, "Did you bring me something shiny?  Lemme see!");
                    }
                    else
                    {
                        Spout(n, "Ooh!  You found the Greeble machine!$h#$b#Vincent and I used to play games with that, but we had to stop because Aunt Marnie told us we couldn't go into your pasture anymore unless you invite us.#$b#There are parts missing?  Well of course there are!  Vincent and I kept some of the shinier bits, see, one's on my jumprope!  It's so shiny, it sparkles when I jump!  Oh, and FINDERS KEEPERS!$l#$b#Well, I suppose I could give it back to you...  BUT ONLY IF YOU TRADE ME SOMETHING REALLY SHINY!$h");
                        this.jazTradeKnown = true;
                        this.SetJazAndVincentFingered();
                    }
                    break;

                case "Vincent":
                    if (this.vincentPartGot)
                    {
                        Spout(n, "Did you have any luck getting it to fit back together?  I could probably help you put it back together, it sure came apart easy!$l");
                    }
                    else if (this.vincentTradeKnown)
                    {
                        Spout(n, "Did you get the bugs?  Big ones?");
                    }
                    else
                    {
                        Spout(n, "The Greeble machine!$h#$b#Jaz and I used to play games with that, but we had to stop because Marnie told us we couldn't go into your pasture anymore unless you invite us.#$b#There are parts missing?  Sure there are!  Me and Jaz took a couple of pieces.  I used mine in a trap I was building to trap the Greebles under my bed!  It didn't work.  Hey, have you got any good bugs on your farm?  I'll find it for you if you can bring me some really big crawly ones!$h");
                        this.vincentTradeKnown = true;
                        this.SetJazAndVincentFingered();
                    }
                    break;
                default:
                    Spout(n, "What is that thing?  A harvester?  Sure, if you say so...$s");
                    break;
            }

            return false;
        }

        private void SetMissingPartsKnown()
        {
            if (this.investigationState == ScytheQuestState.NoCluesYet)
            {
                this.investigationState = ScytheQuestState.MissingParts;
                this.SetObjective();
            }
        }

        private void SetJazAndVincentFingered()
        {
            this.investigationState = ScytheQuestState.JazAndVincentFingered;
            this.SetObjective();
        }

        public void WorkingAttachmentBroughtToGarage()
        {
            this.questComplete();
            Game1.player.modData[QuestSetup.ModDataKeys.ScytheQuestStatus] = "Complete";
            Game1.player.removeFirstOfThisItemFromInventory(ObjectIds.WorkingScythe);
            Game1.DrawDialogue(new Dialogue(null, null, "Sweet!  You've now got a harvester attachment for your tractor!#$b#HINT: To use it, equip the scythe while on the tractor."));
        }

        private void SetObjective()
        {
            switch (this.investigationState)
            {
                case ScytheQuestState.NoCluesYet:
                    this.currentObjective = "Ask the people in town about this thing.";
                    break;
                case ScytheQuestState.MissingParts:
                    this.currentObjective = "It seems some parts are missing, but I know there's nothing else out there in that field, at least not that's easy to see.  Maybe somebody else who noses around the farm would know about it.";
                    break;
                case ScytheQuestState.JazAndVincentFingered:
                    if (!this.jazTradeKnown || !this.vincentTradeKnown)
                    {
                        this.currentObjective = "Ask Jaz and Vincent about the harvester";
                    }
                    else if (this.jazPartGot && this.vincentPartGot)
                    {
                        this.currentObjective = "Fix the scythe attachment (HINT: craft it from the parts)";
                    }
                    else if (this.jazTradeKnown && this.vincentTradeKnown)
                    {
                        this.currentObjective = "Get a 'shiny thing' for Jaz (perhaps a gem?) and 3 big bugs for Vincent.  (Hm.  The bugs in the mines seem TOO big...  Maybe a lobster?  Hm.  Again maybe too big...)";
                    }
                    else
                    {
                        this.currentObjective = "Find a way to get the kids to give me the parts";
                    }
                    break;
                case ScytheQuestState.InstallPart:
                    this.currentObjective = "Take the fixed scythe attachment to the tractor garage.";
                    break;
            }
        }

        public string Serialize()
            => FormattableString.Invariant($"{this.investigationState},{this.jazTradeKnown},{this.vincentTradeKnown},{this.jazPartGot},{this.vincentPartGot}");

        private static bool TryParseQuestStatus(string? s, out ScytheQuestState state, out bool[] flags)
        {
            if (s is null)
            {
                state = ScytheQuestState.NotStarted;
                flags = new bool[0];
                return true;
            }

            string[] splits = s.Split(',');
            if (!Enum.TryParse<ScytheQuestState>(splits[0], out state) || (splits.Length != 1 && splits.Length != 5))
            {
                flags = new bool[0];
                return false;
            }

            flags = new bool[splits.Length-1];
            for (int i = 1; i < splits.Length; i++)
            {
                if (!bool.TryParse(splits[i], out flags[i-1]))
                {
                    return false;
                }
            }

            return true;
        }

        internal static void OnDayStart(IModHelper helper, IMonitor monitor)
        {
            Game1.player.modData.TryGetValue(ModDataKeys.ScytheQuestStatus, out string? statusAsString);
            if (!TryParseQuestStatus(statusAsString, out ScytheQuestState state, out bool[] flags))
            {
                monitor.Log($"Invalid value for {ModDataKeys.ScytheQuestStatus}: {statusAsString} -- reverting to NotStarted", LogLevel.Error);
                state = ScytheQuestState.NotStarted;
            }

            if (state == ScytheQuestState.NotStarted)
            {
                var farm = Game1.getFarm();
                if (!farm.objects.Values.Any(o => o.ItemId == ObjectIds.BustedScythe))
                {
                    var bottomMostLog = farm.resourceClumps.Where(tf => tf.parentSheetIndex.Value == ResourceClump.hollowLogIndex).OrderByDescending(tf => tf.Tile.Y).FirstOrDefault();
                    if (bottomMostLog is null)
                    {
                        monitor.Log($"The farm contains no hollow logs under which to stick the scythe", LogLevel.Warn);

                        // Although I'm pretty sure all farms will have a log, fall back to any resource clump
                        bottomMostLog = farm.resourceClumps.OrderByDescending(tf => tf.Tile.Y).FirstOrDefault();
                        if (bottomMostLog is null)
                        {
                            monitor.Log($"The farm contains no resource clumps under which to stick the scythe", LogLevel.Error);
                            // TODO: Fall back to finding an open spot for it?  This would happen if the user enables this mod on an old save where the whole farm has been cleared.
                            return;
                        }
                    }

                    var o = ItemRegistry.Create<StardewValley.Object>(ObjectIds.BustedScythe);
                    o.Location = Game1.getFarm();
                    o.TileLocation = bottomMostLog.Tile;
                    o.IsSpawnedObject = true;
                    _ = farm.objects.TryAdd(o.TileLocation, o);
                }
            }

            if (state != ScytheQuestState.NotStarted && state != ScytheQuestState.Complete)
            {
                var q = new ScytheQuest(state, flags[0], flags[1], flags[2], flags[3]);
                q.MarkAsViewed();
                Game1.player.questLog.Add(q);
            }
        }
    }
}