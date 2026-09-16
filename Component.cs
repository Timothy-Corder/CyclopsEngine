using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyclopsEngine
{
  public interface IComponent
  {
    public Guid Guid { get; }
    public GameObject Parent { get; set; }
    public bool NeedsUpdate { get; set; }
    public bool NeedsDraw { get; set; }
    public void Update(GameTime gameTime);
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch);
  }
  public class BaseComponent : IComponent
  {
    public Guid Guid { get; }
    public GameObject Parent { get; set; }
    public bool NeedsUpdate { get; set; }
    public bool NeedsDraw { get; set; }
    public BaseComponent(GameObject parent, bool update, bool draw)
    {
      Guid = Guid.NewGuid();
      Parent = parent;
      NeedsUpdate = update;
      NeedsDraw = draw;
    }
    public virtual void Update(GameTime gameTime) {}
    public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch) { }
  }
  public class SpriteComponent : BaseComponent
  {
    public Texture2D Texture { get; set; }
    public Color Color { get; set; }
    public SpriteComponent (GameObject parent, Texture2D texture, Color? color = null) : base(parent, false, true)
    {
      Texture = texture;
      Color = color ?? Color.White;
    }
    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
      spriteBatch.Draw(Texture, Parent.Bounds, Color);
    }
  }
}
