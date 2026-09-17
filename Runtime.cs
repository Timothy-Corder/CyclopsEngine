using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CyclopsEngine;

/// <summary>
/// Provides global access to the active game and the engine's core services.
/// </summary>
public static class Runtime
{
    private static Game? _gameInstance;
    private static InputService? _input;
    private static TextureService? _textures;

    /// <summary>
    /// Whether the runtime has been initialized with a game instance.
    /// </summary>
    public static bool IsInitialized => _gameInstance is not null;

    /// <summary>
    /// The active game instance associated with this runtime.
    /// </summary>
    public static Game GameInstance => _gameInstance
        ?? throw new InvalidOperationException("Runtime.Initialize must be called before accessing the game instance.");

    /// <summary>
    /// The runtime's global input service.
    /// </summary>
    public static InputService Input => _input
        ?? throw new InvalidOperationException("Runtime.Initialize must be called before accessing input.");

    /// <summary>
    /// The runtime's global texture service.
    /// </summary>
    public static TextureService Textures => _textures
        ?? throw new InvalidOperationException("Runtime.Initialize must be called before accessing textures.");

    /// <summary>
    /// The runtime's global collection of game objects.
    /// </summary>
    public static GameObjectDictionary GameObjects { get; } = new();

    /// <summary>
    /// The shared random-number generator used by the runtime.
    /// </summary>
    public static Random Random { get; set; } = new();

    /// <summary>
    /// A one-pixel white texture used to draw primitive shapes after content has been loaded.
    /// </summary>
    public static Texture2D? Pixel { get; private set; }

    /// <summary>
    /// Initializes the runtime and its global services for a game instance.
    /// </summary>
    public static void Initialize(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        if (_gameInstance is not null)
        {
            if (ReferenceEquals(_gameInstance, game))
            {
                return;
            }

            throw new InvalidOperationException("The runtime is already initialized for another game instance.");
        }

        _gameInstance = game;
        _input = new InputService(game);
        _textures = new TextureService(game);
        _input.MouseClick += DispatchClick;
        _input.MouseDoubleClick += DispatchDoubleClick;
    }

    /// <summary>
    /// Creates graphics-device resources. Call this from Game.LoadContent.
    /// </summary>
    public static void LoadContent()
    {
        if (Pixel is not null)
        {
            return;
        }

        Pixel = new Texture2D(GameInstance.GraphicsDevice, 1, 1);
        Pixel.SetData([Color.White]);
    }

    /// <summary>
    /// Updates input state and all registered game objects.
    /// </summary>
    public static void Update(GameTime gameTime)
    {
        Input.Update(gameTime);
        GameObjects.Update(gameTime);
    }

    /// <summary>
    /// Draws all registered game objects and optionally their bounds.
    /// </summary>
    public static void Draw(GameTime gameTime, SpriteBatch spriteBatch, bool drawBounds = false)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        GameObjects.Draw(gameTime, spriteBatch, drawBounds);
    }

    /// <summary>
    /// Clears runtime state, detaches input handlers, and disposes runtime-owned graphics resources.
    /// </summary>
    public static void Shutdown()
    {
        if (_input is not null)
        {
            _input.MouseClick -= DispatchClick;
            _input.MouseDoubleClick -= DispatchDoubleClick;
        }

        GameObjects.Clear();
        _textures?.Dispose();
        Pixel?.Dispose();

        Pixel = null;
        _textures = null;
        _input = null;
        _gameInstance = null;
    }

    private static void DispatchClick(int x, int y, MouseButton button)
    {
        GameObjects.GetReverseSorted()
            .FirstOrDefault(gameObject => gameObject.Enabled && gameObject.Contains(new Point(x, y)))
            ?.Click(button);
    }

    private static void DispatchDoubleClick(int x, int y, MouseButton button)
    {
        GameObjects.GetReverseSorted()
            .FirstOrDefault(gameObject => gameObject.Enabled && gameObject.Contains(new Point(x, y)))
            ?.DoubleClick(button);
    }
}
