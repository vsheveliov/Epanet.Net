using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Epanet.Network.Structures {

    ///<summary>Temporal pattern.</summary>
    [DebuggerDisplay("{Name}:{_factors}")]
    public class Pattern:Element, IEnumerable<double> {
        ///<summary>Pattern factors list.</summary>
        private readonly List<double> _factors = new List<double>();

        public Pattern(string name):base(name) { }

        // public IList<double> FactorsList { get { return this.factors; } }


        public override ElementType ElementType { 
            get { return ElementType.Pattern; }
        }


        #region partial implementation of IList<double>

        public void Add(double factor) { _factors.Add(factor); }

        public int Count {
            get { return _factors.Count; }
        }

        public double this[int index] { get { return _factors[index]; } }

        public IEnumerator<double> GetEnumerator() { return _factors.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return _factors.GetEnumerator(); }

        #endregion
    }

}
