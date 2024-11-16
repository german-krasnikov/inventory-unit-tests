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

            foreach (var item in items)
            {
                AddItem(item);
            }
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
            if (item == null || Contains(item) || !CheckGridRange(posX, posY)) return false;
            CheckItemSize(item);

            for (var x = posX; x < posX + item.Size.x; x++)
            {
                for (var y = posY; y < posY + item.Size.y; y++)
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

        public bool AddItem(Item item, in int posX, in int posY)
        {
            if (!CanAddItem(item, posX, posY)) return false;
            CheckItemSize(item);
            if (posX + item.Size.x > Width || posY + item.Size.y > Height) return false;

            IterateByItemPositions(item, posX, posY, (x, y) => _grid[x, y] = item);
            var position = new Vector2Int(posX, posY);
            _items.Add(item, position);
            OnAdded?.Invoke(item, position);
            return true;
        }

        /// <summary>
        /// Checks for adding an item on a free position
        /// </summary>
        public bool CanAddItem(in Item item)
        {
            if (Contains(item)) return false;
            return AddItem(item);
        }

        /// <summary>
        /// Adds an item on a free position
        /// </summary>
        public bool AddItem(in Item item)
        {
            if (item == null) return false;
            CheckItemSize(item);

            if (FindFreePosition(item.Size, out var freePosition))
            {
                return AddItem(item, freePosition);
            }

            return false;
        }

        /// <summary>
        /// Returns a free position for a specified item
        /// </summary>
        public bool FindFreePosition(Vector2Int size, out Vector2Int freePosition)
        {
            CheckItemSize(size);

            if (size.x > Width || size.y > Height)
            {
                freePosition = default;
                return false;
            }

            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    if (IsFreeSection(x, y, size))
                    {
                        freePosition = new Vector2Int(x, y);
                        return true;
                    }
                }
            }

            freePosition = default;
            return false;
        }

        private bool IsFreeSection(int posX, int posY, Vector2Int size)
        {
            var result = true;
            IterateByRangePositions(posX, posY, size.x, size.y, (x, y) =>
            {
                if (!IsFree(x, y))
                {
                    result = false;
                }
            });
            return result;
        }

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
            if (!CheckGridRange(posX, posY)) return false;

            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    var item = _grid[x, y];
                    if (item == null) continue;
                    if (x == posX && y == posY) return false;
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

            IterateByItemPositions(item, position.x, position.y, (x, y) => _grid[x, y] = null);
            _items.Remove(item);
            OnRemoved.Invoke(item, position);
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
            if (!CheckGridRange(posX, posY)) throw new IndexOutOfRangeException();

            var item = _grid[posX, posY];
            if (item == null) throw new NullReferenceException();
            return item;
        }

        public bool TryGetItem(in Vector2Int position, out Item item)
            => TryGetItem(position.x, position.y, out item);

        public bool TryGetItem(in int x, in int y, out Item item)
        {
            try
            {
                item = GetItem(x, y);
            }
            catch (Exception e)
            {
                if (e is IndexOutOfRangeException or NullReferenceException)
                {
                    item = null;
                    return false;
                }

                throw;
            }

            return true;
        }

        /// <summary>
        /// Returns matrix positions of a specified item 
        /// </summary>
        public Vector2Int[] GetPositions(in Item item)
        {
            if (item == null) throw new NullReferenceException(nameof(item));

            var position = _items[item];
            var result = new List<Vector2Int>();

            IterateByItemPositions(item, position.x, position.y, (x, y) => result.Add(new Vector2Int(x, y)));

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
            if (_items.Count == 0) return;
            _items.Clear();
            IterateByGridPositions((x, y) => _grid[x, y] = null);
            OnCleared?.Invoke();
        }

        /// <summary>
        /// Returns a count of items with a specified name
        /// </summary>
        public int GetItemCount(string name) => _items.Count(it => it.Key.Name == name);

        /// <summary>
        /// Moves a specified item to a target position if it exists
        /// </summary>
        public bool MoveItem(Item item, in Vector2Int newPosition)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (!Contains(item)) return false;
            var position = _items[item];
            IterateByItemPositions(item, position.x, position.y, (x, y) => _grid[x, y] = null);

            if (!IsFreeSection(newPosition.x, newPosition.y, item.Size))
            {
                IterateByItemPositions(item, position.x, position.y, (x, y) => _grid[x, y] = item);
                return false;
            }

            IterateByItemPositions(item, newPosition.x, newPosition.y, (x, y) => _grid[x, y] = item);
            _items[item] = newPosition;
            OnMoved.Invoke(item, newPosition);
            return true;
        }

        /// <summary>
        /// Reorganizes inventory space to make the free area uniform
        /// </summary>
        public void ReorganizeSpace()
        {
            var sortedList = GetSortedList();
            Clear();

            foreach (var item in sortedList)
            {
                AddItem(item);
            }
        }

        private List<Item> GetSortedList()
        {
            var items = _items.Keys.ToList();
            items.Sort(Compare);
            return items;
        }

        private int Compare(Item item1, Item item2)
        {
            var item1Size = item1.Size.x * item1.Size.y;
            var item2Size = item2.Size.x * item2.Size.y;
            int result = item2Size.CompareTo(item1Size);
            if (result != 0) return result;
            int maxSide1 = Math.Max(item1.Size.x, item1.Size.y);
            int maxSide2 = Math.Max(item2.Size.x, item2.Size.y);
            result = maxSide2.CompareTo(maxSide1);
            if (result != 0) return result;
            return 0;
        }

        /// <summary>
        /// Copies inventory items to a specified matrix
        /// </summary>
        public void CopyTo(Item[,] matrix)
        {
            IterateByGridPositions((x, y) => matrix[x, y] = _grid[x, y]);
        }

        public IEnumerator<Item> GetEnumerator()
            => _items.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _items.Keys.GetEnumerator();

        private void IterateByItemPositions(Item item, int posX, int posY, Action<int, int> callback)
            => IterateByRangePositions(posX, posY, item.Size.x, item.Size.y, callback);

        private void IterateByGridPositions(Action<int, int> callback)
            => IterateByRangePositions(0, 0, Width, Height, callback);

        private void IterateByRangePositions(int posX, int posY, int width, int height, Action<int, int> callback)
        {
            for (var x = posX; x < posX + width; x++)
            {
                for (var y = posY; y < posY + height; y++)
                {
                    callback(x, y);
                }
            }
        }

        private void BreakableIterateByRangePositions(int posX, int posY, int width, int height, Func<int, int, bool> callback)
        {
            for (var x = posX; x < posX + width; x++)
            {
                for (var y = posY; y < posY + height; y++)
                {
                    if (!callback(x, y)) return;
                }
            }
        }

        private bool CheckGridRange(int x, int y) => x.IsInRange(0, Width - 1) && y.IsInRange(0, Height - 1);

        private void CheckItemSize(Item item) => CheckItemSize(item.Size);

        private void CheckItemSize(Vector2Int size)
        {
            if (size.x <= 0 || size.y <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        }
    }
}