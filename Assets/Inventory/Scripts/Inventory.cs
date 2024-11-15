using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ReSharper disable NotResolvedInText

namespace Inventories
{
    public sealed class Inventory : IEnumerable<Item>
    {
        public event Action<Item, Vector2Int> OnAdded;
        public event Action<Item, Vector2Int> OnRemoved;
        public event Action<Item, Vector2Int> OnMoved;
        public event Action OnCleared;

        public int Width => _grid.GetLength(0);
        public int Height => _grid.GetLength(1);
        public int Count => _items.Count;

        private readonly Item[,] _grid;
        private readonly Dictionary<Item, Vector2Int> _items;

        public Inventory(in int width, in int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            _grid = new Item[width, height];
            _items = new Dictionary<Item, Vector2Int>();
        }

        public Inventory(
            in int width,
            in int height,
            params KeyValuePair<Item, Vector2Int>[] items
        ) : this(width, height)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            foreach (var it in items)
            {
                AddItem(it.Key, it.Value);
            }
        }

        public Inventory(
            in int width,
            in int height,
            params Item[] items
        ) : this(width, height)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            foreach (var it in items)
            {
                AddItem(it);
            }
        }

        public Inventory(
            in int width,
            in int height,
            in IEnumerable<KeyValuePair<Item, Vector2Int>> items
        ) : this(width, height)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            foreach (var it in items)
            {
                AddItem(it.Key, it.Value);
            }
        }

        public Inventory(
            in int width,
            in int height,
            in IEnumerable<Item> items
        ) : this(width, height)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
        }

        /// <summary>
        /// Checks for adding an item on a specified position
        /// </summary>
        public bool CanAddItem(in Item item, in Vector2Int position)
        {
            return CanAddItem(item, position.x, position.y);
        }

        public bool CanAddItem(in Item item, in int posX, in int posY)
        {
            if (item == null || Contains(item) || !posY.IsInRange(0, Height - 1) || !posX.IsInRange(0, Width - 1)) return false;

            for (int x = posX; x < posX + item.Size.x; x++)
            {
                for (int y = posY; y < posY + item.Size.y; y++)
                {
                    if (!IsFree(x, y)) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Adds an item on a specified position if not exists
        /// </summary>
        public bool AddItem(in Item item, in Vector2Int position) => AddItem(item, position.x, position.y);

        public bool AddItem(in Item item, in int posX, in int posY)
        {
            if (!CanAddItem(item, posX, posY)) return false;
            if (posX + item.Size.x > Width || posY + item.Size.y > Height) return false;
            _grid[posX, posY] = item;
            _items.Add(item, new Vector2Int(posX, posY));
            return true;
        }

        /// <summary>
        /// Checks for adding an item on a free position
        /// </summary>
        public bool CanAddItem(in Item item)
            => throw new NotImplementedException();

        /// <summary>
        /// Adds an item on a free position
        /// </summary>
        public bool AddItem(in Item item)
            => throw new NotImplementedException();

        /// <summary>
        /// Returns a free position for a specified item
        /// </summary>
        public bool FindFreePosition(in Vector2Int size, out Vector2Int freePosition)
            => throw new NotImplementedException();

        /// <summary>
        /// Checks if a specified item exists
        /// </summary>
        public bool Contains(in Item item)
        {
            if (item == null) return false;
            return _items.ContainsKey(item);
        }

        /// <summary>
        /// Checks if a specified position is occupied
        /// </summary>
        public bool IsOccupied(in Vector2Int position)
            => IsOccupied(position.x, position.y);

        public bool IsOccupied(in int x, in int y)
            => !IsFree(x, y);

        /// <summary>
        /// Checks if a position is free
        /// </summary>
        public bool IsFree(in Vector2Int position)
            => IsFree(position.x, position.y);

        public bool IsFree(in int posX, in int posY)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var item = _grid[x, y];
                    if (item == null) continue;
                    if (x == posX && y == posY) return false;
                    if (posX.IsInRange(x, x + item.Size.x - 1) && posY.IsInRange(y, y + item.Size.y - 1)) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Removes a specified item if exists
        /// </summary>
        public bool RemoveItem(in Item item)
        {
            if (!Contains(item)) return false;

            var position = _items[item];
            _grid[position.x, position.y] = null;
            _items.Remove(item);
            OnRemoved(item, position);
            return true;
        }

        public bool RemoveItem(in Item item, out Vector2Int position)
        {
            if (!Contains(item))
            {
                position = default;
                return false;
            }

            position = _items[item];
            RemoveItem(item);
            return true;
        }

        /// <summary>
        /// Returns an item at specified position 
        /// </summary>
        public Item GetItem(in Vector2Int position)
            => GetItem(position.x, position.y);

        public Item GetItem(in int posX, in int posY)
        {
            foreach (var pair in _items)
            {
                var item = pair.Key;
                var pos = pair.Value;
                if (posX.IsInRange(pos.x, pos.x + item.Size.x) && posY.IsInRange(pos.y, pos.y + item.Size.y)) return item;
            }

            return null;
        }

        public bool TryGetItem(in Vector2Int position, out Item item)
            => TryGetItem(position.x, position.y, out item);

        public bool TryGetItem(in int x, in int y, out Item item)
        {
            item = GetItem(x, y);
            return item != null;
        }

        /// <summary>
        /// Returns matrix positions of a specified item 
        /// </summary>
        public Vector2Int[] GetPositions(in Item item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var position = _items[item];
            var result = new List<Vector2Int>();

            for (int x = position.x; x < position.x + item.Size.x; x++)
            {
                for (int y = position.y; y < position.y + item.Size.y; y++)
                {
                    result.Add(new Vector2Int(x, y));
                }
            }

            return result.ToArray();
        }

        public bool TryGetPositions(in Item item, out Vector2Int[] positions)
        {
            if (!Contains(item))
            {
                positions = null;
                return false;
            }

            positions = GetPositions(item);
            return true;
        }

        /// <summary>
        /// Clears all inventory items
        /// </summary>
        public void Clear()
        {
            _items.Clear();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _grid[x, y] = null;
                }
            }

            OnCleared?.Invoke();
        }

        /// <summary>
        /// Returns a count of items with a specified name
        /// </summary>
        public int GetItemCount(string name) => _items.Count(it => it.Key.Name == name);

        /// <summary>
        /// Moves a specified item to a target position if it exists
        /// </summary>
        public bool MoveItem(in Item item, in Vector2Int newPosition)
            => throw new NotImplementedException();

        /// <summary>
        /// Reorganizes inventory space to make the free area uniform
        /// </summary>
        public void ReorganizeSpace()
            => throw new NotImplementedException();

        /// <summary>
        /// Copies inventory items to a specified matrix
        /// </summary>
        public void CopyTo(in Item[,] matrix)
            => throw new NotImplementedException();

        public IEnumerator<Item> GetEnumerator()
            => _items.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _items.Keys.GetEnumerator();
    }
}