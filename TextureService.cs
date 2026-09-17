using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CyclopsEngine;

/// <summary>
/// Loads, caches, categorizes, and disposes textures for a game instance.
/// </summary>
public sealed class TextureService : IDisposable
{
    private readonly Dictionary<string, Texture2D> _textures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Game _game;
    private bool _disposed;

    /// <summary>
    /// A texture category rooted at the <c>sprites</c> asset path.
    /// </summary>
    public TextureCategory Sprites { get; }

    /// <summary>
    /// Gets a cached or newly loaded texture by asset name.
    /// </summary>
    public Texture2D this[string assetName] => Get(assetName);

    /// <summary>
    /// Creates a texture service for the specified game instance.
    /// </summary>
    public TextureService(Game game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        Sprites = Category("sprites");
    }

    /// <summary>
    /// Creates a texture service for the active runtime game instance.
    /// </summary>
    public TextureService()
        : this(Runtime.GameInstance)
    {
    }

    /// <summary>
    /// Creates a category that prefixes texture asset names with the specified path.
    /// </summary>
    public TextureCategory Category(string name) => new(this, name);

    /// <summary>
    /// Gets a texture from the cache or loads it through the content manager, falling back to a PNG file.
    /// </summary>
    public Texture2D Get(string assetName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(assetName);

        string normalizedName = assetName.Replace('\\', '/').TrimStart('/');
        if (_textures.TryGetValue(normalizedName, out Texture2D? texture))
        {
            return texture;
        }

        try
        {
            texture = _game.Content.Load<Texture2D>(normalizedName);
        }
        catch (ContentLoadException)
        {
            string filePath = Path.Combine(
                AppContext.BaseDirectory,
                _game.Content.RootDirectory,
                normalizedName.Replace('/', Path.DirectorySeparatorChar) + ".png");

            using FileStream stream = File.OpenRead(filePath);
            texture = Texture2D.FromStream(_game.GraphicsDevice, stream);
        }

        _textures.Add(normalizedName, texture);
        return texture;
    }

    /// <summary>
    /// Removes and disposes a cached texture with the specified asset name.
    /// </summary>
    public bool Unload(string assetName)
    {
        string normalizedName = assetName.Replace('\\', '/').TrimStart('/');
        if (!_textures.Remove(normalizedName, out Texture2D? texture))
        {
            return false;
        }

        texture.Dispose();
        return true;
    }

    /// <summary>
    /// Clears the texture cache and optionally disposes the cached textures.
    /// </summary>
    public void ClearCache(bool disposeTextures = false)
    {
        if (disposeTextures)
        {
            foreach (Texture2D texture in _textures.Values)
            {
                texture.Dispose();
            }
        }

        _textures.Clear();
    }

    /// <summary>
    /// Disposes all cached textures and prevents further texture loading.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ClearCache(disposeTextures: true);
        _disposed = true;
    }

    /// <summary>
    /// Provides access to textures beneath a shared asset-path prefix.
    /// </summary>
    public sealed class TextureCategory
    {
        private readonly TextureService _parent;

        internal TextureCategory(TextureService parent, string name)
        {
            _parent = parent;
            Name = name.Trim('/');
        }

        /// <summary>
        /// The normalized asset-path prefix for this category.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets a texture whose asset name is relative to this category.
        /// </summary>
        public Texture2D this[string assetName] => _parent.Get($"{Name}/{assetName}");
    }
}
