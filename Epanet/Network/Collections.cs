using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using org.addition.epanet.network.structures;

namespace org.addition.epanet.network
{
    public abstract class StringKeyedCollection<T> : KeyedCollection<string, T>
    {
        public StringKeyedCollection() : base(StringComparer.OrdinalIgnoreCase, 20) { }
        public StringKeyedCollection(int capacity) : base(StringComparer.OrdinalIgnoreCase, capacity) { }

        public void AddOrReplace(T item)
        {
            string key = this.GetKeyForItem(item);
            base.Remove(key);
            base.Add(item);
        }

        /*
    public new T this[string key] {
        get {

            if (key == null)
                throw new ArgumentNullException("key");

            if (this.Dictionary != null) {
                T value;
                this.Dictionary.TryGetValue(key, out value);
                return value;
            }

            var comparer = base.Comparer ?? StringComparer.OrdinalIgnoreCase;

            foreach (T item in this.Items) {
                if (comparer.Equals(item.ID, key))
                    return item;
            }

            return default(T);
        }
    }
    */

        public bool TryGetValue(string key, out T value)
        {

            if (key == null)
                throw new ArgumentNullException("key");

            if (this.Dictionary != null)
            {
                return this.Dictionary.TryGetValue(key, out value);
            }

            var comparer = base.Comparer ?? StringComparer.OrdinalIgnoreCase;

            foreach (T item in this.Items)
            {
                string itemKey = this.GetKeyForItem(item);

                if (comparer.Equals(itemKey, key))
                {
                    value = item;
                    return true;
                }
            }

            value = default(T);
            return false;
        }
    }

    internal sealed class CurveCollection : StringKeyedCollection<Curve>
    {
        protected override string GetKeyForItem(Curve item) { return item.getId(); }
    }

    internal sealed class NodeCollection : StringKeyedCollection<Node>
    {
        protected override string GetKeyForItem(Node item) { return item.getId(); }
    }

    internal sealed class LinkCollection : StringKeyedCollection<Link>
    {
        protected override string GetKeyForItem(Link item) { return item.getId(); }
    }

    internal sealed class PatternCollection : StringKeyedCollection<Pattern>
    {
        protected override string GetKeyForItem(Pattern item) { return item.getId(); }
    }

    internal sealed class RuleCollection : StringKeyedCollection<Rule>
    {
        protected override string GetKeyForItem(Rule item) { return item.getLabel(); }
    }

}
