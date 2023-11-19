using System;
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

                // TODO: Properly find a position.
                position = new Vector2(75, 14);

                Game1.player.modData[ModDataKeys.DerelictPosition] = FormattableString.Invariant($"{position.X},{position.Y}");
            }
            Place(helper, position);
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
            Game1.getFarm().terrainFeatures.Add(position + new Vector2(0, 1), tf);
            Game1.getFarm().terrainFeatures.Add(position + new Vector2(1, 1), tf);
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
            var r = new Rectangle((int)this.tile.X * 64, (int)this.tile.Y * 64, 64*2, 64*2);
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
                var q = new RestoreTractorQuest(RestorationState.TalkToLewis);
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
                              Game1.GlobalToLocal(Game1.viewport, this.tile * 64f),
                              tileSheetRect,
                              color: Color.White,
                              rotation: 0f,
                              origin: Vector2.Zero,
                              scale: 4f,
                              effects: SpriteEffects.None,
                              layerDepth: (this.tile.Y + 1f) * 64f / 10000f + this.tile.X / 100000f);
        }
    }
}
