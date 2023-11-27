using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;

namespace Pathoschild.Stardew.TractorMod.Questable
{
    /// <summary>
    ///  Implements the quest line for the Axe and Pick tools, which we're treating as a single thing.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   The story line for this attachment is intended to be simple, with low costs and nothing that
    ///   would preclude getting it running pretty much immediately.  In fact, the player is pretty
    ///   likely to complete it before the tractor itself.  The story begins with finding the attachment
    ///   underneath a rock.  Villager chat will reveal that one day Grandpa attempted to remove a large
    ///   rock, but the thing became stuck.  He went to town to ask Lewis what he should do.  Lewis said
    ///   that maybe his tractor was just too small to move a rock of that size.  Grandpa jumped to the
    ///   conclusion that Lewis meant he should break up the rock, and went to Pierre's to buy some
    ///   explosives, which he did, but the explosives failed to break the rock and broke the tractor
    ///   instead.  Pierre resolved to not stock explosives from that point forward.
    ///  </para>
    ///  <para>
    ///   The part needs some work from Clint, who's willing to do the work, but at the moment the
    ///   farmer wants to talk about the work, Clint is fixated on his shoes.  He's read some stuff that
    ///   convinces him that he'll never be able to attract a ladyfriend with his current footwear.
    ///   He thinks the farmer would be just the person to fix this for him because he/she has lived
    ///   in the city and thus knows all the trends.  He explains that his shoes are size 14EEE and
    ///   sets the farmer on their way.
    ///  </para>
    ///  <para>
    ///   Talking to Alex will directly reveal something that he mentions in his gossip - that he throws
    ///   away shoes that aren't all that worn.  At some point, Alex's chatter will include "What, you
    ///   want to know my shoe size??  14EEE, but why do you ask?"
    ///  </para>
    ///  <para>
    ///   While the quest is active, there'll be a 25% chance of finding "Alex's old shoes" in the trash.
    ///   A pair can also be found sitting beside the dwarf.  If you chat with the dwarf while on the
    ///   quest, he'll say "Oh yes.  I got these shoes while rummaging around in town.  They're broken.
    ///   They don't fit."  (somehow we need to word the text so that it's apparent that the dwarf thinks
    ///   that any shoe that doesn't fit him is utterly worthless to anybody because he believes everybody
    ///   has the same shoe size.)
    ///  </para>
    ///  <para>
    ///    If you talk to Linus and you have 3 hearts, he'll offer to look out for them.  If you don't
    ///    find the shoes yourself in 3 days, he'll mail a pair.
    ///  </para>
    ///  <para>
    ///   But having Alex's shoes isn't good enough, and you have to dye them.  Once you dye them, you
    ///   can present them to Clint along with 10 copper bars and he'll get to work on it.
    ///  </para>
    /// </remarks>
    internal class LoaderQuest
        : BaseQuest<LoaderQuestState>
    {
        private static readonly Vector2 placeInMineForShoes = new Vector2(48, 8);

        public LoaderQuest()
            : base(LoaderQuestState.TalkToClint)
        {
            this.questTitle = "Fix the loader";
            this.questDescription = "I found the front end loader attachment for the tractor, but it's all bent up and rusted through in spots.";
        }

        protected override void SetObjective()
        {
            switch (this.State)
            {
                case LoaderQuestState.TalkToClint:
                    this.currentObjective = "This looks like a the sort of thing a blacksmith could fix.";
                    break;
                case LoaderQuestState.FindSomeShoes:
                    this.currentObjective = "Somehow I've been railroaded into finding Clint some new shoes.  Maybe some of the younger townspeople know where I could get some for cheap.";
                    break;
                case LoaderQuestState.SnagAlexsOldShoes:
                    this.currentObjective = "Somehow I've been maneuvered into finding Clint some new shoes.  Alex wears the same shoe size and tosses out shoes regularly...  Hm...  Ew...  Hm...";
                    break;
                case LoaderQuestState.LinusSniffing1:
                case LoaderQuestState.LinusSniffing2:
                case LoaderQuestState.LinusSniffing3:
                case LoaderQuestState.LinusSniffing4:
                case LoaderQuestState.LinusSniffing5:
                    this.currentObjective = "I've got Linus recruited to look out for shoes in Alex's trash can, but he warns me that there's something else that might be looking as well...  Who could that be?";
                    break;
                case LoaderQuestState.DisguiseTheShoes:
                    this.currentObjective = "Somehow I need to disguise these shoes so that Alex doesn't recognize them...  And maybe do something about that smudge.  Perhaps Sam or Sebastian would loan me some shoe polish.";
                    break;
                case LoaderQuestState.GiveShoesToClint:
                    this.currentObjective = "Give the shoes and the old loader to Clint";
                    break;
                case LoaderQuestState.InstallTheLoader:
                    this.currentObjective = "Take the fixed loader attachment to the tractor garage.";
                    break;
                case LoaderQuestState.WaitForClint1:
                case LoaderQuestState.WaitForClint2:
                    this.currentObjective = "Wait for Clint to finish repairing the loader.";
                    break;
                case LoaderQuestState.PickUpLoader:
                    this.currentObjective = "Clint should be done repairing the loader by now.";
                    break;
            }
        }

        public override void GotWorkingPart(Item workingPart)
        {
            this.State = LoaderQuestState.InstallTheLoader;
        }

        public override void CheckIfComplete(NPC? n, Item? item)
        {
            if (n?.Name == "Clint" && this.State == LoaderQuestState.TalkToClint)
            {
                Spout(n, "Shoes.  That's my problem.  I wear these dusty old work boots all over the place.$2#$b#What??  Oh.  Sorry.  Just been a bit distracted because I saw on TV that women judge a man by their shoes and look at these...  No wonder I've got no luck with the ladies.$3#$b#What?  You want me to fix that thing?  Sure, looks like it'd be just a bit of reforging, some welds here and there...#$b#Wait!  You're from the city, you know all about shoes!  Tell you what, you get me a nice pair of shoes and I'll fix your loader.  Deal??#$b#GREAT!  I wear 14EEE.");
                this.State = LoaderQuestState.FindSomeShoes;
            }
            else if (n?.Name == "Sam" && this.State < LoaderQuestState.SnagAlexsOldShoes)
            {
                Spout(n, "Shoes, yeah man, they cost a fortune.  My gig at the library barely pays, so I roll around in these supercheapies from Joja.  I color mine every once in a while so they look fresh.");
            }
            else if (n?.Name == "Abigail" && this.State < LoaderQuestState.SnagAlexsOldShoes)
            {
                Spout(n, "Cheap shoes?  And you somehow deduce that I'm authority on such matters!  Hah, you're not far off.#$b#Back before the Jojamart we'd order them online, but now, I've learned the art of Thrift Stores.  I'm actually kindof glad it happened, I really like shopping at thrift stores.#$b#Cheaper than that?  Welp, you could always dumpster-dive!");
            }
            else if (n?.Name == "Haley" && this.State < LoaderQuestState.SnagAlexsOldShoes)
            {
                Spout(n, "Ladies' shoes I know.  Men's shoes I don't.");
            }
            else if (n?.Name == "Emily" && this.State < LoaderQuestState.SnagAlexsOldShoes)
            {
                Spout(n, "Well, I mostly get my shoes from secondhand stores, but I don't really know about men's shoes.  Have you asked Sam, or Alex?");
            }
            else if (n?.Name == "Sebastian" && this.State < LoaderQuestState.SnagAlexsOldShoes)
            {
                Spout(n, "Shoes, yeah man, they cost a fortune.  My gig at the library barely pays, so I roll around in these supercheapies from Joja.  I use colored shoe-polish on mine every once in a while so they look fresh.");
            }
            else if (n?.Name == "Alex" && this.State < LoaderQuestState.SnagAlexsOldShoes)
            {
                this.PlantShoesNextToDwarf();
                Spout(n, "I got these new shoes yesterday 'cuz my old pair had a brown smudge.#$b#I just threw them into the garbage. I would've donated them but I don't like the idea of some weirdo wearing my shoes, ya know?#$b#What size do I wear?  14EEE. . . .  Wait, why do you ask?");
                this.State = LoaderQuestState.SnagAlexsOldShoes;
            }
            else if (n?.Name == "Linus" && Game1.player.getFriendshipHeartLevelForNPC("Linus") >= 2 && this.State == LoaderQuestState.SnagAlexsOldShoes)
            {
                // The event where you catch linus dumpster diving is at ~.25 hearts, so at a level of 2, we can assume the player knows the sort of things Linus gets up to during the night...
                Spout(n, ". . . So...  what I think I'm hearing you say is you want me to scout the Mullner's trash can for shoes...#$b#You promise this is for a good cause?  Hm...  Okay.  I'll let you know if I come across them.#$b#I don't want to disturb you, but I'm not the only one nosing around town late at night.  I might not find them first.");
                this.State = LoaderQuestState.LinusSniffing1;
            }
            else if ((n?.Name == "Sam" || n?.Name == "Sebastian") && this.TryTakeItemsFromPlayer(ObjectIds.AlexesOldShoe))
            {
                string treat = (n.Name == "Sam" ? "Pizza" : "Sashimi");
                Spout(n, $"You want to borrow my shoe polish?  That's kindof an odd request but, you know what?  Sure.  Knock yourself out.#$b#There better be some {treat} in this for me somewhere down the road.");
                this.AddItemToInventory(ObjectIds.DisguisedShoe);
            }
            else if (n?.Name == "Clint" && this.TryTakeItemsFromPlayer(ObjectIds.BustedLoader, 1, ObjectIds.DisguisedShoe, 1))
            {
                Spout(n, "Ah, these shoes look great!  Fit good too.  But somehow I still don't quite feel like a ladykiller.  Well, time will tell!#$b#I'll get this back to you in a couple days.  Look for it in the mail.  I can at least ship it to you since you did all this running around for me.");
                this.State = LoaderQuestState.WaitForClint1;
            }
            else if (n?.Name == "Clint" && this.State == LoaderQuestState.PickUpLoader)
            {
                Spout(n, "Here's your front-end loader, all fixed up.  Stick to small rocks, right?#$b#If you need to move big ones, get some explosives for the job.  Oh, and let me know when you're doing it.  I'll bring beer.");
                this.AddItemToInventory(ObjectIds.WorkingLoader);
            }
        }

        public override void AdvanceStateForDayPassing()
        {
            switch (this.State)
            {
                case LoaderQuestState.LinusSniffing1:
                    this.State = LoaderQuestState.LinusSniffing2;
                    break;
                case LoaderQuestState.LinusSniffing2:
                    this.State = LoaderQuestState.LinusSniffing3;
                    break;
                case LoaderQuestState.LinusSniffing3:
                    this.State = LoaderQuestState.LinusSniffing4;
                    break;
                case LoaderQuestState.LinusSniffing4:
                    this.State = LoaderQuestState.LinusSniffing5;
                    Game1.player.mailForTomorrow.Add(MailKeys.LinusFoundShoes);
                    break;
                case LoaderQuestState.WaitForClint1:
                    this.State = LoaderQuestState.WaitForClint2;
                    break;
                case LoaderQuestState.WaitForClint2:
                    this.State = LoaderQuestState.PickUpLoader;
                    break;
            }
        }

        private void PlantShoesNextToDwarf()
        {
            var mines = Game1.locations.FirstOrDefault(l => l.Name == "Mine");
            if (mines is null)
            {
                QuestSetup.Instance.Monitor.Log("Couldn't find the Mine?!", StardewModdingAPI.LogLevel.Warn);
                return;
            }

            var alreadyPlaced = mines.getObjectAtTile((int)placeInMineForShoes.X, (int)placeInMineForShoes.Y);
            if (alreadyPlaced is null)
            {
                var o = ItemRegistry.Create<StardewValley.Object>(ObjectIds.AlexesOldShoe);
                o.Location = mines;
                o.TileLocation = placeInMineForShoes;
                QuestSetup.Instance.Monitor.VerboseLog($"{ObjectIds.AlexesOldShoe} placed at {placeInMineForShoes.X},{placeInMineForShoes.Y}");
                o.IsSpawnedObject = true;
                mines.objects[o.TileLocation] = o;
            }
        }

        private void RemoveShoesNearDwarf()
        {
            var mines = Game1.locations.FirstOrDefault(l => l.Name == "Mine");
            mines?.removeObject(placeInMineForShoes, showDestroyedObject: false);
        }
    }
}
