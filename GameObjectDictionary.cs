using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CyclopsEngine;

/// <summary>
/// The runtime's global dictionary of game objects. It updates automatically as game objects are created or destroyed.
/// </summary>
public class GameObjectDictionary : Dictionary<Guid, GameObject>
{
    internal const int RenderLayerSize = 1000;
    private readonly HashSet<int> _occupiedIds = [];

    /// <summary>
    /// The local IDs currently assigned to game objects in this dictionary.
    /// </summary>
    public IReadOnlyCollection<int> OccupiedIds => _occupiedIds;

    /// <summary>
    /// Gets a game object by its unique ID using the supplied game object as the key.
    /// </summary>
    public GameObject this[GameObject gameObject]
    {
        get => this[gameObject.Id];
        private set => this[gameObject.Id] = value;
    }

    /// <summary>
    /// Gets the lowest unoccupied local ID in the specified render layer.
    /// </summary>
    public int GetLowestUnoccupied(int renderLayer)
    {
        int localId = checked(renderLayer * RenderLayerSize);
        while (_occupiedIds.Contains(localId))
        {
            localId++;
        }

        return localId;
    }

    /// <summary>
    /// Adds a game object and assigns it the lowest available local ID in its render layer.
    /// </summary>
    public void Add(GameObject gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        if (ContainsKey(gameObject.Id))
        {
            throw new ArgumentException("The game object has already been added.", nameof(gameObject));
        }

        gameObject.LocalId = GetLowestUnoccupied(gameObject.RenderLayer);
        this[gameObject] = gameObject;
        _occupiedIds.Add(gameObject.LocalId);
    }

    /// <summary>
    /// Removes a game object and settles the remaining local IDs within each render layer.
    /// </summary>
    public bool Remove(GameObject gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);

        if (!base.Remove(gameObject.Id))
        {
            return false;
        }

        _occupiedIds.Remove(gameObject.LocalId);
        Settle();
        return true;
    }

    /// <summary>
    /// Removes the game object with the specified unique ID.
    /// </summary>
    public new bool Remove(Guid id) => TryGetValue(id, out GameObject? gameObject) && Remove(gameObject);

    /// <summary>
    /// Adds a game object under a key that must match the object's unique ID.
    /// </summary>
    public new void Add(Guid id, GameObject gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);
        if (id != gameObject.Id)
        {
            throw new ArgumentException("The key must match the game object's ID.", nameof(id));
        }

        Add(gameObject);
    }

    /// <summary>
    /// Removes all game objects and releases all occupied local IDs.
    /// </summary>
    public new void Clear()
    {
        base.Clear();
        _occupiedIds.Clear();
    }

    /// <summary>
    /// Gets the game objects ordered from lowest to highest local ID.
    /// </summary>
    public List<GameObject> GetSorted() => Values.OrderBy(gameObject => gameObject.LocalId).ToList();

    /// <summary>
    /// Gets the game objects ordered from highest to lowest local ID.
    /// </summary>
    public List<GameObject> GetReverseSorted() => Values.OrderByDescending(gameObject => gameObject.LocalId).ToList();

    /// <summary>
    /// Updates each enabled game object in ascending local-ID order.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        foreach (GameObject gameObject in GetSorted())
        {
            if (gameObject.Enabled)
            {
                gameObject.Update(gameTime);
            }
        }
    }

    /// <summary>
    /// Gets all game objects whose names exactly match the specified name.
    /// </summary>
    public GameObject[] GetByName(string name)
    {
    return this.Values.Where(go => go.Name == name).ToArray();
    }

    /// <summary>
    /// Draws game objects in ascending local-ID order and optionally draws the bounds of enabled, visible objects.
    /// </summary>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, bool drawBounds = false)
    {
        foreach (GameObject gameObject in GetSorted())
        {
            gameObject.Draw(gameTime, spriteBatch);
        }

        if (!drawBounds)
        {
            return;
        }

        foreach (GameObject gameObject in GetSorted())
        {
            if (gameObject.Enabled && !gameObject.Hidden)
            {
                gameObject.DrawBox(gameTime, spriteBatch);
            }
        }
    }

    private void Settle()
    {
        GameObject[] gameObjects = Values.ToArray();
        _occupiedIds.Clear();

        foreach (IGrouping<int, GameObject> layer in gameObjects.GroupBy(gameObject => gameObject.RenderLayer))
        {
            int nextId = checked(layer.Key * RenderLayerSize);
            foreach (GameObject gameObject in layer.OrderBy(gameObject => gameObject.LocalId))
            {
                gameObject.LocalId = nextId++;
                _occupiedIds.Add(gameObject.LocalId);
            }
        }
    }
}
