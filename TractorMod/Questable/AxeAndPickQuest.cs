using System;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Quests;
using StardewValley.TerrainFeatures;
using static Pathoschild.Stardew.TractorMod.Questable.QuestSetup;

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
    internal class AxeAndPickQuest
        : BaseQuest<AxeAndPickQuestState>
    {
        private AxeAndPickQuest(AxeAndPickQuestState state)
            : base(state)
        {
            this.questTitle = "Fix the loader";
            this.questDescription = "I found the front end loader attachment for the tractor, but it's all bent up and rusted through in spots.  Looks like a the sort of thing a blacksmith could fix.";
        }

        public AxeAndPickQuest()
            : this(AxeAndPickQuestState.TalkToClint)
        {
            this.showNew.Value = true;
        }


        protected override void SetObjective()
        {
            switch (this.State)
            {
                case AxeAndPickQuestState.TalkToClint:
                    this.currentObjective = "Find someone to fix the loader.";
                    break;
                case AxeAndPickQuestState.InstallTheLoader:
                    this.currentObjective = "Take the fixed loader attachment to the tractor garage.";
                    break;
            }
        }

        public override void GotWorkingPart(Item workingPart)
        {
            this.State = AxeAndPickQuestState.InstallTheLoader;
        }

        public override bool checkIfComplete(NPC? n, int number1, int number2, Item? item, string str, bool probe)
        {
            if(n?.Name == "Clint" && item?.ItemId == ObjectIds.BustedLoader)
            {
                // TODO: Properly implement the quest
                Game1.player.removeItemFromInventory(item);
                _ = Game1.player.addItemToInventory(new StardewValley.Object(ObjectIds.WorkingLoader, 1));
            }

            return false;
        }
    }
}
