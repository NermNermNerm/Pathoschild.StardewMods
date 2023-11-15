using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Pathoschild.Stardew.TractorMod.Framework
{
    public class DerelictTractorTerrainFeature
        : TerrainFeature
    {
        private readonly Texture2D texture;
        private readonly Vector2 tile;

        public DerelictTractorTerrainFeature(Texture2D texture, Vector2 tile)
            : base(needsTick: true)
        {
            this.texture = texture;
            this.tile = tile;
        }

        public override bool isPassable(Character c)
        {
            return false;
        }

        public override bool performToolAction(Tool t, int damage, Vector2 tileLocation)
        {
            return base.performToolAction(t, damage, tileLocation);
        }

        public override void draw(SpriteBatch spriteBatch)
        {
            Rectangle tileSheetRect = Game1.getSourceRectForStandardTileSheet(this.texture, 0, 16, 16);
            tileSheetRect.Width = 32;
            tileSheetRect.Height = 32;
            spriteBatch.Draw( this.texture,
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
