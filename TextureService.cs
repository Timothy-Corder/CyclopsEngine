using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CyclopsEngine;

public sealed class TextureService : IDisposable
{
    private readonly Dictionary<string, Texture2D> _textures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Game _game;
    private bool _disposed;

    public TextureCategory Sprites { get; }

    public Texture2D this[string assetName] => Get(assetName);

    public TextureService(Game game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        Sprites = Category("sprites");
    }

    public TextureService()
        : this(Runtime.GameInstance)
    {
    }

    public TextureCategory Category(string name) => new(this, name);

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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ClearCache(disposeTextures: true);
        _disposed = true;
    }

    public sealed class TextureCategory
    {
        private readonly TextureService _parent;

        internal TextureCategory(TextureService parent, string name)
        {
            _parent = parent;
            Name = name.Trim('/');
        }

        public string Name { get; }

        public Texture2D this[string assetName] => _parent.Get($"{Name}/{assetName}");
    }
}
