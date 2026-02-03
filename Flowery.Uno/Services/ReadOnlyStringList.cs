// CsWinRT1028: Generic collections implementing WinRT interfaces - inherent to Uno Platform, not fixable via code
#pragma warning disable CsWinRT1028

using System.Collections;
using System.Collections.Generic;

namespace Flowery.Services
{
    /// <summary>
    /// A lightweight, read-only wrapper around a string collection.
    /// Uses ArrayList internally to avoid generic collection WinRT projection issues.
    /// </summary>
    public sealed partial class ReadOnlyStringList : IReadOnlyList<string>
    {
        private readonly ArrayList _items;

        /// <summary>
        /// Creates an empty ReadOnlyStringList.
        /// </summary>
        public ReadOnlyStringList()
        {
            _items = [];
        }

        /// <summary>
        /// Creates a ReadOnlyStringList from the specified items.
        /// </summary>
        /// <param name="items">The source items to copy. Null items are converted to empty strings.</param>
        public ReadOnlyStringList(IEnumerable<string> items)
        {
            _items = [];
            if (items == null)
                return;

            foreach (var item in items)
                _items.Add(item ?? string.Empty);
        }

        /// <summary>
        /// Gets the number of items in the list.
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// Gets the item at the specified index.
        /// </summary>
        public string this[int index] => _items[index] as string ?? string.Empty;

        /// <summary>
        /// Returns an enumerator that iterates through the list.
        /// </summary>
        public IEnumerator<string> GetEnumerator()
        {
            foreach (var item in _items)
                yield return item as string ?? string.Empty;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
