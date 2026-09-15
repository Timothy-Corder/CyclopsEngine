using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CyclopsEngine;

public class GameObjectDictionary : Dictionary<Guid, GameObject>
{
    private const int RenderLayerSize = 1000;
    private readonly HashSet<int> _occupiedIds = [];

    public IReadOnlyCollection<int> OccupiedIds => _occupiedIds;

    public GameObject this[GameObject gameObject]
    {
        get => this[gameObject.Id];
        private set => this[gameObject.Id] = value;
    }

    public int GetLowestUnoccupied(int renderLayer)
    {
        int localId = checked(renderLayer * RenderLayerSize);
        while (_occupiedIds.Contains(localId))
        {
            localId++;
        }

        return localId;
    }

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

    public new bool Remove(Guid id) => TryGetValue(id, out GameObject? gameObject) && Remove(gameObject);

    public new void Add(Guid id, GameObject gameObject)
    {
        ArgumentNullException.ThrowIfNull(gameObject);
        if (id != gameObject.Id)
        {
            throw new ArgumentException("The key must match the game object's ID.", nameof(id));
        }

        Add(gameObject);
    }

    public new void Clear()
    {
        base.Clear();
        _occupiedIds.Clear();
    }

    public List<GameObject> GetSorted() => Values.OrderBy(gameObject => gameObject.LocalId).ToList();

    public List<GameObject> GetReverseSorted() => Values.OrderByDescending(gameObject => gameObject.LocalId).ToList();

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
