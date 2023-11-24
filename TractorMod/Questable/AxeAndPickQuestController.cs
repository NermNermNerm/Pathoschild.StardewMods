using StardewValley.TerrainFeatures;

namespace Pathoschild.Stardew.TractorMod.Questable
{
    internal class AxeAndPickQuestController
        : BaseQuestController<AxeAndPickQuestState, AxeAndPickQuest>
    {
        public AxeAndPickQuestController(QuestSetup mod)
            : base(mod)
        {
        }

        protected override string QuestCompleteMessage => "Sweet!  You've now got a front-end loader attachment for your tractor to clear out debris!#$b#HINT: To use it, equip the pick or the axe while on the tractor.";
        protected override string ModDataKey => ModDataKeys.AxeAndPickQuestStatus;
        public override string WorkingAttachmentPartId => ObjectIds.WorkingLoader;
        public override string BrokenAttachmentPartId => ObjectIds.BustedLoader;
        public override string HintTopicConversationKey => ConversationKeys.LoaderNotFound;
        protected override void HideStarterItemIfNeeded() => base.PlaceBrokenPartUnderClump(ResourceClump.boulderIndex);
    }
}
