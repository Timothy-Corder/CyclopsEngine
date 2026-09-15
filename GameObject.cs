using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CyclopsEngine;

public class GameObject : IDisposable
{
    private bool _disposed;
    private float _rotationDegrees;

    protected Game Game => Runtime.GameInstance;

    public Guid Id { get; } = Guid.NewGuid();

    public int LocalId { get; internal set; }

    public Vector2 Position { get; set; }

    public Vector2 Velocity { get; set; }

    public Vector2 Scale { get; set; }

    public bool Enabled { get; set; } = true;

    public bool Hidden { get; set; }

    public int RenderLayer => GetRenderLayer();

    public Vector2 ScaleMultiplier => GetScaleMult();

    public virtual int GetRenderLayer() => 0;

    public virtual Vector2 GetScaleMult() => Vector2.One;

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
    public Rectangle Box
    {
        get => Bounds;
        set => Bounds = value;
    }

    public float RotationDegrees
    {
        get => _rotationDegrees;
        set
        {
            float normalized = value % 360f;
            _rotationDegrees = normalized < 0f ? normalized + 360f : normalized;
        }
    }

    public float RotationRadians
    {
        get => MathHelper.ToRadians(_rotationDegrees);
        set => RotationDegrees = MathHelper.ToDegrees(value);
    }

    public GameObject(Vector2 position, Vector2? velocity = null, Vector2? scale = null)
    {
        Position = position;
        Velocity = velocity ?? Vector2.Zero;
        Scale = scale ?? Vector2.One;
        Runtime.GameObjects.Add(this);
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (Enabled && !Hidden)
        {
            OnDraw(gameTime, spriteBatch);
        }
    }

    public virtual void OnDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
    }

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

    public void DrawBounds(GameTime gameTime, SpriteBatch spriteBatch) => DrawBox(gameTime, spriteBatch);

    public virtual void Update(GameTime gameTime)
    {
        Position += Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public bool IsColliding(GameObject other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Bounds.Intersects(other.Bounds);
    }

    public bool Contains(GameObject other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Bounds.Contains(other.Bounds);
    }

    public bool Contains(Point point) => Bounds.Contains(point);

    public void Click(MouseButton button)
    {
        if (Enabled)
        {
            OnClick(button);
        }
    }

    public void DoubleClick(MouseButton button)
    {
        if (Enabled)
        {
            OnDoubleClick(button);
        }
    }

    public virtual void OnClick(MouseButton button)
    {
    }

    public virtual void OnDoubleClick(MouseButton button)
    {
    }

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
}
