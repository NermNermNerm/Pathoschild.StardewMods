using StardewValley;

namespace Pathoschild.Stardew.TractorMod.Questable
{
    public class WatererQuestController
        : BaseQuestController<WatererQuestState, WatererQuest>
    {
        public WatererQuestController(QuestSetup mod)
            : base(mod)
        { }


        public static float chanceOfCatchingQuestItem = 0;

        protected override string QuestCompleteMessage => "Awesome!  You've now got a way to water your crops with your tractor!#$b#HINT: To use it, equip the watering can while on the tractor.";

        protected override string ModDataKey => ModDataKeys.WateringQuestStatus;

        public override string WorkingAttachmentPartId => ObjectIds.WorkingWaterer;

        public override string BrokenAttachmentPartId => ObjectIds.BustedWaterer;

        public override string HintTopicConversationKey => ConversationKeys.WatererNotFound;

        public override void AnnounceGotBrokenPart(Item brokenPart)
        {
            // We want to act a lot differently than we do in the base class, as we got the item through fishing, holding it up would look dumb
            BaseQuest<WatererQuestState>.Spout("Whoah that was heavy!  Looks like an irrigator attachment for a tractor!  I bet there's a story behind how it got here...");
        }

        protected override void OnQuestStarted()
        {
            chanceOfCatchingQuestItem = 0;
        }

        public override void OnDayStart()
        {
            chanceOfCatchingQuestItem = 0;
            if (this.IsStarted)
            {
                chanceOfCatchingQuestItem = 0; // No chance - already pulled it up.
            }
            else if (RestoreTractorQuest.IsTractorUnlocked)
            {
                chanceOfCatchingQuestItem = 0.01f + Game1.Date.TotalDays / 200f;
            }
            else
            {
                chanceOfCatchingQuestItem = .01f;
            }

            base.OnDayStart();
        }
    }
}
