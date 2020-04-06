using System;

namespace Epanet.Network.Structures {

    public enum ElementType { NODE, LINK, PATTERN, CURVE, CONTROL, RULE }

    /// <summary>Base class for all epanet elements - Links, Nodes etc.</summary>
    public abstract class Element:IComparable<Element>, IEquatable<Element> {
        private readonly string _name;

        protected Element(string name) {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (name.Length == 0 || name.Length > Constants.MAXID)
                throw new ArgumentException(nameof(name));

            _name = name;
        }

        public abstract ElementType ElementType { get; }

        public string Name => _name;

        public string Tag { get; set; } = string.Empty;

        /// <summary>Element comment (parsed from INP or excel file)</summary>
        public string Comment { get; set; }

        #region Overrides of Object

        public override string ToString() {
            return string.Format("{0}{{{1}}}", GetType().Name, _name);
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
