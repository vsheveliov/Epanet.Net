using System;
using System.Collections.ObjectModel;

using Epanet.Network.Structures;

namespace Epanet.Network {

    internal class ElementCollection<TItem>:KeyedCollection<string, TItem> where TItem:Element {
        public ElementCollection():base(StringComparer.OrdinalIgnoreCase, 10) { }

        // public new void Add(TItem item) { base.Add(item); }

        public void AddOrReplace(TItem item) {
            string key = GetKeyForItem(item);
            base.Remove(key);
            base.Add(item);
        }

        /// <summary>Get item by Name.</summary>
        /// <param name="key">Item <see cref="Element.Name"/></param>
        /// <returns>Item with given key, null otherwise.</returns>
        public new TItem this[string key] {
            get {

                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                if (Dictionary != null) {
                    return Dictionary.TryGetValue(key, out TItem value) 
                        ? value 
                        : default(TItem);
                }

                var comparer = base.Comparer ?? StringComparer.OrdinalIgnoreCase;

                foreach (var item in Items)
                    if (comparer.Equals(item.Name, key))
                        return item;

                return default(TItem);
            }
        }

        
        public bool TryGetValue(string key, out TItem value) {
            if(key == null)
                throw new ArgumentNullException(nameof(key));

            if(base.Dictionary != null)
                return base.Dictionary.TryGetValue(key, out value);

            var comparer = Comparer ?? StringComparer.OrdinalIgnoreCase;

            foreach(var item in Items) {
                string itemKey = item.Name;

                if(comparer.Equals(itemKey, key)) {
                    value = item;
                    return true;
                }
            }

            value = default(TItem);
            return false;
        }

        protected override string GetKeyForItem(TItem item) => item.Name;
    }

}