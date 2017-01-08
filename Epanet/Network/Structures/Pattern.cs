using System.Collections;
using System.Collections.Generic;

namespace Epanet.Network.Structures {

    ///<summary>Temporal pattern.</summary>
    public class Pattern:Element, IEnumerable<double> {
        ///<summary>Pattern factors list.</summary>
        private readonly List<double> factors = new List<double>();

        public Pattern(string name):base(name) { }

        // public IList<double> FactorsList { get { return this.factors; } }


        public override ElementType ElementType { 
            get { return ElementType.Pattern; }
        }


        #region partial implementation of IList<double>

        public void Add(double factor) { this.factors.Add(factor); }

        public int Count {
            get { return this.factors.Count; }
        }

        public double this[int index] { get { return this.factors[index]; } }

        public IEnumerator<double> GetEnumerator() { return this.factors.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return this.factors.GetEnumerator(); }

        #endregion
    }

}
