using System;

namespace Epanet.Network.Structures {

    public enum ElementType { Node, Link, Pattern, Curve, Control, Rule };

    /// <summary>Base class for all epanet elements - Links, Nodes etc.</summary>
    public abstract class Element:IComparable<Element>, IEquatable<Element> {
        private readonly string _name;

        protected Element(string name) {
            _name = name.Length > Constants.MAXID 
                ? name.Substring(0, Constants.MAXID) 
                : name;

            // this.Comment = string.Empty;
            // this.Tag = string.Empty;
        }

        public string Name { get { return _name; } }

        public string Tag { get; set; }

        /// <summary>Element comment (parsed from INP or excel file)</summary>
        public string Comment { get; set; }

        public abstract ElementType ElementType { get; }

        #region Overrides of Object

        public override string ToString() {
            return GetType().FullName + ":Name=" + (_name ?? string.Empty);
        }

        #endregion

        #region Implementation of IComparable<Element>, IEquatable<Element>

        public int CompareTo(Element other) {
            return other == null ? 1 : string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public bool Equals(Element other) {
            return other != null && string.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode() {
            return string.IsNullOrEmpty(_name)
                ? 0
                : _name.GetHashCode();
        }
        
#endregion

    }

}
