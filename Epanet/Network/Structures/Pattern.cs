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

        #region Overrides of Element

        public override ElementType ElementType => ElementType.PATTERN;

        #endregion

        // public IList<double> Factors => _factors;

        #region partial implementation of IList<double>

        public void Add(double factor) => _factors.Add(factor);

        public int Count => _factors.Count;

        public double this[int index] => _factors[index];

        public IEnumerator<double> GetEnumerator() => _factors.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _factors.GetEnumerator();

        #endregion
    }

}
