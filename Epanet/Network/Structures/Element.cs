using System;

namespace Epanet.Network.Structures {

    public enum ElementType { Node, Link, Pattern, Curve, Control, Rule };

    /// <summary>Base class for all epanet elements - Links, Nodes etc.</summary>
    public abstract class Element:IComparable<Element>, IEquatable<Element> {
        private readonly string _name;

        protected Element(string name) {
            this._name = name.Length > Constants.MAXID 
                ? name.Substring(0, Constants.MAXID) 
                : name;

            // this.Comment = string.Empty;
            // this.Tag = string.Empty;
        }

        public string Name { get { return this._name; } }

        public string Tag { get; set; }

        /// <summary>Element comment (parsed from INP or excel file)</summary>
        public string Comment { get; set; }

        public abstract ElementType ElementType { get; }

#region Implementation of IComparable<Element>, IEquatable<Element>

        public int CompareTo(Element other) {
            if(other == null)
                return 1;

            return string.Compare(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);            
        }

        public bool Equals(Element other) {
            if(other == null)
                return false;

            return string.Equals(this._name, other._name, StringComparison.OrdinalIgnoreCase);            
        }

        public override int GetHashCode() {
            return string.IsNullOrEmpty(this._name)
                ? 0
                : this._name.GetHashCode();
        }
        
#endregion

    }

}
