using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CyclopsEngine;

/// <summary>
/// Represents an object that acts within the <see cref="Runtime"/>, owns components, and participates in updating,
/// drawing, and collision detection. New instances register themselves with <see cref="Runtime.GameObjects"/>.
/// </summary>
public class GameObject : IDisposable
{
    private bool _disposed;
    private float _rotationDegrees;

    /// <summary>
    /// The active game instance. Provides a global reference for services, game objects, and components.
    /// </summary>
    protected Game Game => Runtime.GameInstance;

    /// <summary>
    /// The string name of this game object. It can be used to locate the object with <see cref="GameObjectDictionary.GetByName(string)"/>.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The unique ID of this game object.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// The dynamic ID of this game object. It is used primarily for render-layer ordering and changes as other game objects are created and destroyed.
    /// </summary>
    public int LocalId { get; internal set; }

    /// <summary>
    /// The <see cref="Vector2"/> position of this game object.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// The movement velocity of this game object in units per second.
    /// </summary>
    public Vector2 Velocity { get; set; }

    /// <summary>
    /// The base width and height of this game object before applying its scale multiplier.
    /// </summary>
    public Vector2 Scale { get; set; }

    /// <summary>
    /// Whether this game object can update, draw, and receive click events.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether drawing this game object is suppressed while it remains enabled.
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// The render layer used to order this game object relative to other game objects.
    /// </summary>
    public int RenderLayer => GetRenderLayer();

    /// <summary>
    /// The multiplier applied to <see cref="Scale"/> when calculating <see cref="Bounds"/>.
    /// </summary>
    public Vector2 ScaleMultiplier => GetScaleMult();

    /// <summary>
    /// Gets the render layer for this game object. Override this method to place a derived object on another layer.
    /// </summary>
    public virtual int GetRenderLayer() => 0;

    /// <summary>
    /// Gets the scale multiplier for this game object. Override this method to alter its effective bounds.
    /// </summary>
    public virtual Vector2 GetScaleMult() => Vector2.One;

    /// <summary>
    /// The axis-aligned bounds of this game object, centered on <see cref="Position"/> and sized from its effective scale.
    /// </summary>
    public Rectangle Bounds
    {
        get
        {
            Vector2 size = Scale * GetScaleMult();
            return new Rectangle(
                (int)(Position.X - (size.X / 2f)),
                (int)(Position.Y - (size.Y / 2f)),
                (int)size.X,
                (int)size.Y);
        }
        set
        {
            Position = new Vector2(value.Center.X, value.Center.Y);
            Scale = new Vector2(value.Width, value.Height);
        }
    }

    // Kept as a convenient alias for code written against the original component.
    /// <summary>
    /// An alias for <see cref="Bounds"/> retained for compatibility with existing code.
    /// </summary>
    public Rectangle Box
    {
        get => Bounds;
        set => Bounds = value;
    }

    /// <summary>
    /// The clockwise rotation of this game object in degrees, normalized to the range from zero up to 360.
    /// </summary>
    public float RotationDegrees
    {
        get => _rotationDegrees;
        set
        {
            float normalized = value % 360f;
            _rotationDegrees = normalized < 0f ? normalized + 360f : normalized;
        }
    }

    /// <summary>
    /// The rotation of this game object in radians.
    /// </summary>
    public float RotationRadians
    {
        get => MathHelper.ToRadians(_rotationDegrees);
        set => RotationDegrees = MathHelper.ToDegrees(value);
    }

    private List<IComponent?>? _components;
    /// <summary>
    /// The components attached to this game object.
    /// </summary>
    public List<IComponent?> Components 
    { 
        get
        {
            if (_components is null) _components = new List<IComponent?>();

            return _components;
        }
        set
        {
            _components = value;
        }
    }

    /// <summary>
    /// Creates a game object and registers it with <see cref="Runtime.GameObjects"/>.
    /// </summary>
    public GameObject(Vector2 position, Vector2? velocity = null, Vector2? scale = null, string? name = null)
    {
        Id = Guid.NewGuid();
        Position = position;
        Velocity = velocity ?? Vector2.Zero;
        Scale = scale ?? Vector2.One;
        Name = name ?? $"{this.GetType().Name} {Id}";
        Runtime.GameObjects.Add(this);
    }

    /// <summary>
    /// Draws this game object when it is enabled and visible.
    /// </summary>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (Enabled && !Hidden)
        {
            OnDraw(gameTime, spriteBatch);
        }
    }

    /// <summary>
    /// Draws every attached component whose <see cref="IComponent.NeedsDraw"/> property is set.
    /// </summary>
    public void OnDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        foreach (var comp in Components)
        {
            if (!(comp?.NeedsDraw) ?? true) continue;
            comp?.Draw(gameTime, spriteBatch);
        }
    }

    /// <summary>
    /// Draws a red outline around this game object's bounds.
    /// </summary>
    public virtual void DrawBox(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Texture2D pixel = Runtime.Pixel
            ?? throw new InvalidOperationException("Runtime.LoadContent must be called before drawing bounds.");
        Rectangle bounds = Bounds;

        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, 1), Color.Red);
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Y, 1, bounds.Height), Color.Red);
        spriteBatch.Draw(pixel, new Rectangle(bounds.X, bounds.Bottom - 1, bounds.Width, 1), Color.Red);
        spriteBatch.Draw(pixel, new Rectangle(bounds.Right - 1, bounds.Y, 1, bounds.Height), Color.Red);
    }

    /// <summary>
    /// Draws this game object's bounds by calling <see cref="DrawBox(GameTime, SpriteBatch)"/>.
    /// </summary>
    public void DrawBounds(GameTime gameTime, SpriteBatch spriteBatch) => DrawBox(gameTime, spriteBatch);

    /// <summary>
    /// Updates eligible components and advances this game object's position according to its velocity.
    /// </summary>
    public virtual void Update(GameTime gameTime)
    {
        if (!Enabled) return;
        foreach (var comp in Components)
        {
            if (!(comp?.NeedsUpdate) ?? true) continue;
            comp?.Update(gameTime);
        }
        Position += Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    /// <summary>
    /// Determines whether this game object's bounds intersect another game object's bounds.
    /// </summary>
    public bool IsColliding(GameObject other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Bounds.Intersects(other.Bounds);
    }

    /// <summary>
    /// Determines whether this game object's bounds fully contain another game object's bounds.
    /// </summary>
    public bool Contains(GameObject other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Bounds.Contains(other.Bounds);
    }

    /// <summary>
    /// Determines whether this game object's bounds contain a point.
    /// </summary>
    public bool Contains(Point point) => Bounds.Contains(point);

    /// <summary>
    /// Dispatches a click to this game object when it is enabled.
    /// </summary>
    public void Click(MouseButton button)
    {
        if (Enabled)
        {
            OnClick(button);
        }
    }

    /// <summary>
    /// Dispatches a double-click to this game object when it is enabled.
    /// </summary>
    public void DoubleClick(MouseButton button)
    {
        if (Enabled)
        {
            OnDoubleClick(button);
        }
    }

    /// <summary>
    /// Handles a mouse click dispatched to this game object.
    /// </summary>
    public virtual void OnClick(MouseButton button)
    {
    }

    /// <summary>
    /// Handles a mouse double-click dispatched to this game object.
    /// </summary>
    public virtual void OnDoubleClick(MouseButton button)
    {
    }

    /// <summary>
    /// Removes this game object from the runtime and releases its registration.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Runtime.GameObjects.Remove(this);
        GC.SuppressFinalize(this);
    }

  /// <summary>
  /// Returns the name of this game object.
  /// </summary>
  public override string ToString()
  {
    return Name;
  }

    /// <summary>
    /// Determines whether this game object contains a component assignable to the specified component type.
    /// </summary>
    public bool HasComponentOfType<T>() where T : IComponent
    {
        return Components.Any(c => c is T);
    }
    /// <summary>
    /// Determines whether this game object contains a component with the exact specified runtime type.
    /// </summary>
    public bool HasComponentOfType(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new ArgumentException($"The type {componentType.Name} does not implement IComponent.", nameof(componentType));
        }
        return Components.Any(c => c?.GetType() == componentType);
  }

  /// <summary>
  /// Attaches a component to this game object and assigns this object as its parent.
  /// </summary>
  public void AddComponent(IComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);
      if (HasComponentOfType(component.GetType()))
    {
            throw new InvalidOperationException($"A component of type {component.GetType().Name} already exists in this GameObject.");
        }
        Components.Add(component);
        component.Parent = this;
    }

  /// <summary>
  /// Gets the first component assignable to the specified component type.
  /// </summary>
  public T GetComponentOfType<T>() where T : IComponent
    {
        var component = Components.FirstOrDefault(c => c is T);
        if (component == null)
        {
            throw new InvalidOperationException($"No component of type {typeof(T).Name} exists in this GameObject.");
        }
        return (T)component;
  }
  /// <summary>
  /// Gets the component with the specified unique ID, or <see langword="null"/> if no matching component exists.
  /// </summary>
  public IComponent? GetComponent(Guid guid) => Components.FirstOrDefault(c => c.Guid == guid);
}
