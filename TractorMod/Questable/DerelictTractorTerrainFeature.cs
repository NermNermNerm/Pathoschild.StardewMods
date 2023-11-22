using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Monsters;
using StardewValley.TerrainFeatures;
using static Pathoschild.Stardew.TractorMod.Questable.QuestSetup;

namespace Pathoschild.Stardew.TractorMod.Questable
{
    public class DerelictTractorTerrainFeature
        : TerrainFeature
    {
        private readonly Texture2D texture;
        private readonly Vector2 tile;

        public DerelictTractorTerrainFeature(Texture2D texture, Vector2 tile)
            : base(needsTick: false)
        {
            this.texture = texture;
            this.tile = tile;
        }

        public static void PlaceInField(IModHelper helper, IMonitor monitor)
        {
            Game1.player.modData.TryGetValue(ModDataKeys.DerelictPosition, out string? positionAsString);
            if (positionAsString is null || !TryParse(positionAsString, out Vector2 position))
            {
                if (positionAsString is not null)
                {
                    monitor.Log($"Invalid value for {ModDataKeys.DerelictPosition}: {positionAsString} -- finding a new position", LogLevel.Error);
                }

                position = GetClearSpotForTractor(helper, monitor);
                if (position == new Vector2())
                {
                    // Hope for better luck tomorrow
                    monitor.Log("No clear spot could be found to place the derelict tractor.", LogLevel.Error);
                    return;
                }

                Game1.player.modData[ModDataKeys.DerelictPosition] = FormattableString.Invariant($"{position.X},{position.Y}");
            }
            Place(helper, position);
        }

        public static Vector2 GetClearSpotForTractor(IModHelper helper, IMonitor monitor)
        {
            // Find a spot under a tree on the West side of the map
            var farm = Game1.getFarm();
            foreach (var eastSideTree in farm.terrainFeatures.Values.OfType<Tree>().Where(t => t.growthStage == Tree.treeStage).OrderBy(tf => tf.Tile.X))
            {
                bool anyCollisions = false;
                List<Vector2> tilesToClear = new List<Vector2>();
                // note that trees are 3-wide, and this only looks at the leftmost pair, so we're leaving a little money on the table.
                foreach (var offset in new Vector2[] { new Vector2(0, -1), new Vector2(1, -1) })
                {
                    var posToCheck = eastSideTree.Tile + offset;
                    var objAtOffset = farm.getObjectAtTile((int)posToCheck.X, (int)posToCheck.Y);
                    if (objAtOffset is null)
                    {
                        if (!farm.CanItemBePlacedHere(posToCheck))
                        {
                            anyCollisions = true;
                            break;
                        }
                        // Else it's clear
                    }
                    else if (objAtOffset.Category == -999)
                    {
                        tilesToClear.Add(posToCheck);
                    }
                    else
                    {
                        anyCollisions = true;
                        break;
                    }
                }

                if (!anyCollisions)
                {
                    foreach (var tileToClear in tilesToClear)
                    {
                        // Not calling farm.removeObject because it does things that don't make sense when you're
                        // really just un-making a spot.
                        farm.objects.Remove(tileToClear);
                    }

                    return eastSideTree.Tile + new Vector2(0, -1);
                }
            }

            // No tree is around.  We're probably dealing with an old save,  Try looking for any clear space.
            //  This technique is kinda dumb, but whatev's.  This mod is going to suck with a fully-developed farm.
            for (int i = 0; i < 10000; ++i)
            {
                Vector2 positionToCheck = new Vector2(Game1.random.Next(farm.map.DisplayWidth / 64), Game1.random.Next(farm.map.DisplayHeight / 64));
                if (farm.CanItemBePlacedHere(positionToCheck) && farm.CanItemBePlacedHere(positionToCheck + new Vector2(1, 0)))
                {
                    return positionToCheck;
                }
            }

            return new Vector2();
        }

        public static void PlaceInGarage(IModHelper helper, IMonitor monitor, Stable garage)
        {
            Place(helper, new Vector2(garage.tileX.Value + 1, garage.tileY.Value));
        }

        private static void Place(IModHelper helper, Vector2 position)
        {
            var derelictTractorTexture = helper.ModContent.Load<Texture2D>("assets/rustyTractor.png");

            var tf = new DerelictTractorTerrainFeature(derelictTractorTexture, position);
            Game1.getFarm().terrainFeatures.Add(position, tf);
            Game1.getFarm().terrainFeatures.Add(position + new Vector2(1, 0), tf);
        }

        private static bool TryParse(string s, out Vector2 position)
        {
            string[] split = s.Split(",");
            if (split.Length == 2
                && int.TryParse(split[0], out int x)
                && int.TryParse(split[1], out int y))
            {
                position = new Vector2(x, y);
                return true;
            }
            else
            {
                position = new Vector2();
                return false;
            }
        }


        public override Rectangle getBoundingBox()
        {
            var r = new Rectangle((int)this.tile.X * 64, (int)this.tile.Y * 64, 64*2, 64);
            return r;
        }

        public override bool isPassable(Character c)
        {
            return false;
        }

        public override bool performToolAction(Tool t, int damage, Vector2 tileLocation)
        {
            if (!Game1.player.questLog.Any(q => q is RestoreTractorQuest))
            {
                Game1.drawObjectDialogue("This looks like an old tractor.  Perhaps it could help you out around the farm, but it's been out in the weather a long time.  It'll need some fixing.  Maybe somebody in town can help?");
                var q = new RestoreTractorQuest();
                Game1.player.questLog.Add(q);
            }

            return base.performToolAction(t, damage, tileLocation);
        }

        public override void draw(SpriteBatch spriteBatch)
        {
            Rectangle tileSheetRect = Game1.getSourceRectForStandardTileSheet(this.texture, 0, 16, 16);
            tileSheetRect.Width = 32;
            tileSheetRect.Height = 32;
            spriteBatch.Draw(this.texture,
                              Game1.GlobalToLocal(Game1.viewport, (this.tile - new Vector2(0,1)) * 64f),
                              tileSheetRect,
                              color: Color.White,
                              rotation: 0f,
                              origin: Vector2.Zero,
                              scale: 4f,
                              effects: SpriteEffects.None,
                              layerDepth: this.tile.Y * 64f / 10000f + this.tile.X / 100000f);
        }
    }
}
