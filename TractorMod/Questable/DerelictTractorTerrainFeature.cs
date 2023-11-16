using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.TerrainFeatures;

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
                RestoreTractorQuest.BeginQuest();
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
