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
    internal class SeederQuestController
        : BaseQuestController<SeederQuestState, SeederQuest>
    {
        public SeederQuestController(QuestSetup mod) : base(mod) { }

        protected override string QuestCompleteMessage => "Awesome!  You've now got a way to plant and fertilize crops with your tractor!#$b#HINT: To use it, equip seeds or fertilizers while on the tractor.";

        protected override string ModDataKey => ModDataKeys.SeederQuestStatus;

        public override string WorkingAttachmentPartId => ObjectIds.WorkingSeeder;

        public override string BrokenAttachmentPartId => ObjectIds.BustedSeeder;

        public override string HintTopicConversationKey => ConversationKeys.SeederNotFound;

        protected override void HideStarterItemIfNeeded()
        {
            if (Game1.player.getFriendshipHeartLevelForNPC("George") >= SeederQuest.GeorgeSendsBrokenPartHeartLevel
                && RestoreTractorQuest.IsTractorUnlocked
                && !Game1.player.modData.ContainsKey(ModDataKeys.SeederQuestGeorgeSentMail))
            {
                Game1.player.mailbox.Add(MailKeys.GeorgeSeederMail);
                Game1.player.modData[ModDataKeys.SeederQuestGeorgeSentMail] = "sent";
            }
        }
    }
}
