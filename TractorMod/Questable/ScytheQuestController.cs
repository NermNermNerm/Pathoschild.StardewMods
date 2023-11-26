using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Pathoschild.Stardew.TractorMod.Questable
{
    public class ScytheQuestController : BaseQuestController<ScytheQuestState, ScytheQuest>
    {
        public ScytheQuestController(QuestSetup mod)
            : base(mod)
        { }

        public override string WorkingAttachmentPartId => ObjectIds.WorkingScythe;
        public override string BrokenAttachmentPartId => ObjectIds.BustedScythe;
        public override string HintTopicConversationKey => ConversationKeys.ScytheNotFound;
        protected override string QuestCompleteMessage => "Sweet!  You've now got a harvester attachment for your tractor!#$b#HINT: To use it, equip the scythe while on the tractor.";
        protected override string ModDataKey => ModDataKeys.ScytheQuestStatus;
        protected override void HideStarterItemIfNeeded() => base.PlaceBrokenPartUnderClump(ResourceClump.hollowLogIndex);


        protected override ScytheQuest? Deserialize(string statusAsString)
        {
            if (!TryParseQuestStatus(statusAsString, out ScytheQuestState state, out bool[] flags))
            {
                this.mod.Monitor.Log($"Invalid value for {ModDataKeys.ScytheQuestStatus}: {statusAsString} -- reverting to NotStarted", LogLevel.Error);
                return null;
            }

            return new ScytheQuest(state, flags[0], flags[1], flags[2], flags[3]);
        }


        private static bool TryParseQuestStatus(string s, out ScytheQuestState state, out bool[] flags)
        {
            string[] splits = s.Split(',');
            if (!Enum.TryParse(splits[0], out state) || (splits.Length != 1 && splits.Length != 5))
            {
                flags = new bool[0];
                return false;
            }

            flags = new bool[splits.Length - 1];
            for (int i = 1; i < splits.Length; i++)
            {
                if (!bool.TryParse(splits[i], out flags[i - 1]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}