using System.Linq;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Pathoschild.Stardew.TractorMod.Questable
{
    internal class LoaderQuestController
        : BaseQuestController<LoaderQuestState, LoaderQuest>
    {
        public LoaderQuestController(QuestSetup mod)
            : base(mod)
        {

        }

        protected override string QuestCompleteMessage => "Sweet!  You've now got a front-end loader attachment for your tractor to clear out debris!#$b#HINT: To use it, equip the pick or the axe while on the tractor.";
        protected override string ModDataKey => ModDataKeys.LoaderQuestStatus;
        public override string WorkingAttachmentPartId => ObjectIds.WorkingLoader;
        public override string BrokenAttachmentPartId => ObjectIds.BustedLoader;
        public override string HintTopicConversationKey => ConversationKeys.LoaderNotFound;
        protected override void OnQuestStarted()
        {
            var mines = Game1.locations.FirstOrDefault(l => l.Name == "Mines");
            // Place shoes in the dwarf cavern

        }

        protected override void HideStarterItemIfNeeded()
        {
            this.PlaceBrokenPartUnderClump(ResourceClump.boulderIndex);
        }

        // Shenanigans to do...
        // Plant the shoes, but only if the quest is active and has reached the stage where we know we want shoes.
        // Know if the player has gotten alex's old shoes
        // register for trashcan stuff
        //   Looks like you can add items, and maybe even on-demand, from Data\\GarbageCans of type GarbageCanData where the id of the
        //   mullner's can is either "evelyn" or "6"
    }
}
