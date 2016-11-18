/*
 * Copyright (C) 2016 Vyacheslav Shevelyov (slavash at aha dot ru)
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see http://www.gnu.org/licenses/.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using Epanet.Network.Structures;

namespace Epanet.Network {

    public abstract class StringKeyedCollection<T>:KeyedCollection<string, T> {
        public StringKeyedCollection():base(StringComparer.OrdinalIgnoreCase, 20) { }
        public StringKeyedCollection(int capacity):base(StringComparer.OrdinalIgnoreCase, capacity) { }

        public void AddOrReplace(T item) {
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

        public bool TryGetValue(string key, out T value) {

            if (key == null)
                throw new ArgumentNullException("key");

            if (this.Dictionary != null) {
                return this.Dictionary.TryGetValue(key, out value);
            }

            var comparer = base.Comparer ?? StringComparer.OrdinalIgnoreCase;

            foreach (T item in this.Items) {
                string itemKey = this.GetKeyForItem(item);

                if (comparer.Equals(itemKey, key)) {
                    value = item;
                    return true;
                }
            }

            value = default(T);
            return false;
        }
    }

    internal sealed class CurveCollection:StringKeyedCollection<Curve> {
        protected override string GetKeyForItem(Curve item) { return item.Id; }
    }

    internal sealed class NodeCollection:StringKeyedCollection<Node> {
        protected override string GetKeyForItem(Node item) { return item.Id; }
    }

    internal sealed class LinkCollection:StringKeyedCollection<Link> {
        protected override string GetKeyForItem(Link item) { return item.Id; }
    }

    internal sealed class PatternCollection:StringKeyedCollection<Pattern> {
        protected override string GetKeyForItem(Pattern item) { return item.Id; }
    }

    internal sealed class RuleCollection:StringKeyedCollection<Rule> {
        protected override string GetKeyForItem(Rule item) { return item.Label; }
    }

}
