using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StardewModdingAPI;
using StardewValley.TerrainFeatures;
using StardewValley;
using static Pathoschild.Stardew.TractorMod.Questable.QuestSetup;
using Microsoft.Xna.Framework;

namespace Pathoschild.Stardew.TractorMod.Questable
{
    public class AttachmentQuestBase
    {
        public static void PlaceQuestItemUnderClump(IMonitor monitor, int preferredClumpType, string questStarterObjectId)
        {
            var farm = Game1.getFarm();
            if (farm.objects.Values.Any(o => o.ItemId == questStarterObjectId))
            {
                // Already placed - nothing to do.
                return;
            }

            var position = FindPlaceToPutItem(monitor, preferredClumpType, questStarterObjectId);
            if (position != default)
            {
                var o = ItemRegistry.Create<StardewValley.Object>(questStarterObjectId);
                o.Location = Game1.getFarm();
                o.TileLocation = position;
                o.IsSpawnedObject = true;
                farm.objects[o.TileLocation] = o;
            }
        }

        private static Vector2 FindPlaceToPutItem(IMonitor monitor, int preferredClumpType, string questStarterObjectId)
        {
            var farm = Game1.getFarm();
            var bottomMostResourceClump = farm.resourceClumps.Where(tf => tf.parentSheetIndex.Value == preferredClumpType).OrderByDescending(tf => tf.Tile.Y).FirstOrDefault();
            if (bottomMostResourceClump is not null)
            {
                return bottomMostResourceClump.Tile;
            }

            monitor.Log($"Couldn't find the preferred location ({preferredClumpType}) for the {questStarterObjectId}", LogLevel.Warn);
            bottomMostResourceClump = farm.resourceClumps.OrderByDescending(tf => tf.Tile.Y).FirstOrDefault();
            if (bottomMostResourceClump is not null)
            {
                return bottomMostResourceClump.Tile;
            }

            monitor.Log($"The farm contains no resource clumps under which to stick the scythe", LogLevel.Warn);

            // We're probably dealing with an old save,  Try looking for any clear space.
            //  This technique is kinda dumb, but whatev's.  This mod is pointless on a fully-developed farm.
            for (int i = 0; i < 1000; ++i)
            {
                Vector2 positionToCheck = new Vector2(Game1.random.Next(farm.map.DisplayWidth / 64), Game1.random.Next(farm.map.DisplayHeight / 64));
                if (farm.CanItemBePlacedHere(positionToCheck))
                {
                    return positionToCheck;
                }
            }

            monitor.Log($"Couldn't find any place at all to put the {questStarterObjectId}", LogLevel.Error);
            return default;
        }
    }
}
