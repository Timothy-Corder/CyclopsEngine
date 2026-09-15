using CyclopsEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGame
{
  public class SpriteObject : GameObject
  {
    public Texture2D Sprite;
    public Color Color;
    public SpriteObject(Texture2D sprite, Color? color, Vector2 position, Vector2? velocity = null, Vector2? scale = null) : base(position, velocity, scale)
    {
      if (color is null) color = Color.White;
      Sprite = sprite;
      Color = (Color)color;
    }
    public override void OnDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
      spriteBatch.Draw(Sprite, Bounds, Color);
      base.OnDraw(gameTime, spriteBatch);
    }
    public override void OnClick(MouseButton button)
    {
      base.OnClick(button);
      new SpriteObject(Runtime.Pixel, Color.White, Position, Velocity + new Vector2(1, 1), Scale);
    }
  }
}
