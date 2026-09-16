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
    public SpriteObject(Texture2D sprite, Color? color, Vector2 position, Vector2? velocity = null, Vector2? scale = null) : base(position, velocity, scale)
    {
      Components.Add(new SpriteComponent(this, sprite, color));
    }
    public override void OnClick(MouseButton button)
    {
      base.OnClick(button);
      new SpriteObject(Runtime.Pixel, Color.White, Position, Velocity + new Vector2(1, 1), Scale);
    }
  }
}
