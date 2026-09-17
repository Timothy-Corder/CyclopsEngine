using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CyclopsEngine
{
  /// <summary>
  /// An interface for creating components for use in game objects.
  /// </summary>
  public interface IComponent
  {
    /// <summary>
    /// The unique ID of this component.
    /// </summary>
    public Guid Guid { get; }
    /// <summary>
    /// The game object this component is attached to.
    /// </summary>
    public GameObject Parent { get; set; }
    /// <summary>
    /// Whether this component should receive update calls from its parent. Usually set in the constructor.
    /// </summary>
    public bool NeedsUpdate { get; set; }
    /// <summary>
    /// Whether this component should receive draw calls from its parent. Usually set in the constructor.
    /// </summary>
    public bool NeedsDraw { get; set; }
    /// <summary>
    /// Runs when this component's parent updates and <see cref="NeedsUpdate"/> is set to <see langword="true"/>.
    /// </summary>
    public void Update(GameTime gameTime);
    /// <summary>
    /// Runs when this component's parent is drawn and <see cref="NeedsDraw"/> is set to <see langword="true"/>.
    /// </summary>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch);
  }
  /// <summary>
  /// A basic class that implements <see cref="IComponent"/>. Use this as the base for any custom components you create.
  /// </summary>
  public class BaseComponent : IComponent
  {
    /// <summary>
    /// The unique ID of a component.
    /// </summary>
    public Guid Guid { get; }
    /// <summary>
    /// The game object this component is attached to.
    /// </summary>
    public GameObject Parent { get; set; }
    /// <summary>
    /// Whether this component should receive update calls from its parent. Usually set in the constructor.
    /// </summary>
    public bool NeedsUpdate { get; set; }
    /// <summary>
    /// Whether this component should receive draw calls from its parent. Usually set in the constructor.
    /// </summary>
    public bool NeedsDraw { get; set; }
    /// <summary>
    /// When calling this constructor from a derived class, make sure to set <paramref name="update"/> and <paramref name="draw"/> as needed.
    /// </summary>
    public BaseComponent(GameObject parent, bool update, bool draw)
    {
      Guid = Guid.NewGuid();
      Parent = parent;
      NeedsUpdate = update;
      NeedsDraw = draw;
    }
    /// <summary>
    /// Runs when this component's parent updates and <see cref="NeedsUpdate"/> is set to <see langword="true"/>.
    /// </summary>
    public virtual void Update(GameTime gameTime) {}
    /// <summary>
    /// Runs when this component's parent is drawn and <see cref="NeedsDraw"/> is set to <see langword="true"/>.
    /// </summary>
    public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch) { }
  }
  /// <summary>
  /// A component that renders a <see cref="Texture2D"/> within its parent's bounds using a color tint.
  /// </summary>
  public class SpriteComponent : BaseComponent
  {
    /// <summary>
    /// The <see cref="Texture2D"/> that this sprite component renders over its parent.
    /// </summary>
    public Texture2D Texture { get; set; }
    /// <summary>
    /// The color tint applied to this sprite component when rendering.
    /// </summary>
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
  /// <summary>
  /// A component that runs an <see cref="Action{T}"/> of <see cref="GameTime"/> when its parent updates.
  /// </summary>
  public class LambdaComponent : BaseComponent
  {
    /// <summary>
    /// The <see cref="Action{T}"/> of <see cref="GameTime"/> that this lambda component runs.
    /// </summary>
    public Action<GameTime> Script { get; set; }
    public LambdaComponent(GameObject parent, Action<GameTime> script) : base(parent, true, false)
    {
      Script = script;
    }
    public override void Update(GameTime gameTime)
    {
      Script(gameTime);
    }
  }
}
