using System;
using System.Linq;
using StardewValley;
using StardewValley.Quests;

namespace Pathoschild.Stardew.TractorMod.Questable
{

    public abstract class BaseQuest<TStateEnum> : Quest
        where TStateEnum : struct, Enum
    {
        private TStateEnum state;

        protected BaseQuest(TStateEnum state)
        {
            this.state = state;
            this.SetObjective();
        }

        public TStateEnum State
        {
            get => this.state;
            set
            {
                this.state = value;
                this.SetObjective();
            }
        }

        protected abstract void SetObjective();

        public abstract void GotWorkingPart(Item workingPart);

        public static void Spout(NPC n, string message) => BaseQuestController.Spout(n, message);

        public static void Spout(string message) => BaseQuestController.Spout(message);

        public virtual string Serialize() => this.state.ToString();

        protected void AddItemToInventory(string itemId)
        {
            // TODO: Make it scatter the item to litter if no room in inventory
            _ = Game1.player.addItemToInventory(new StardewValley.Object(itemId, 1));
        }

        protected bool TryTakeItemsFromPlayer(string itemId, int count = 1)
        {
            var stack = Game1.player.Items.FirstOrDefault(i => i?.ItemId == itemId && i.stack.Value >= count);
            if (stack == null)
            {
                return false;
            }
            else if (stack.Stack == count)
            {
                Game1.player.removeItemFromInventory(stack);
                return true;
            }
            else
            {
                stack.Stack -= 3;
                return true;
            }
        }

        protected bool TryTakeItemsFromPlayer(string item1Id, int count1, string item2Id, int count2)
        {
            var stack1 = Game1.player.Items.FirstOrDefault(i => i?.ItemId == item1Id && i.stack.Value >= count1);
            var stack2 = Game1.player.Items.FirstOrDefault(i => i?.ItemId == item2Id && i.stack.Value >= count2);
            if (stack1 is null || stack2 is null)
            {
                return false;
            }

            if (stack1.Stack == count1)
            {
                Game1.player.removeItemFromInventory(stack1);
            }
            else
            {
                stack1.Stack -= 3;
            }

            if (stack2.Stack == count2)
            {
                Game1.player.removeItemFromInventory(stack2);
            }
            else
            {
                stack2.Stack -= 3;
            }

            return true;
        }

        public virtual void AdvanceStateForDayPassing() {}
    }
}
