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
using System.Collections.Generic;
using Epanet.Hydraulic.Structures;

namespace Epanet.Hydraulic {

    ///<summary>Linear system solving support class.</summary>
    public class SparseMatrix {

        ///<summary>Adjacent item</summary>
        private class AdjItem {
            private readonly int _node;
            private readonly int _link;

            public AdjItem(int node, int link) {
                _node = node;
                _link = link;
            }

            public int Node { get { return _node; } }

            public int Link { get { return _link; } }
        }

        ///<summary>Number of coefficients(number of links)</summary>
        private int _coeffsCount;

        ///<summary>Node-to-row of A.</summary>
        private readonly int[] _order;
        ///<summary>Row-to-node of A</summary>
        private readonly int[] _row;
        ///<summary>Index of link's coeff. in Aij</summary>
        private readonly int[] _ndx;
        ///<summary>Number of links adjacent to each node</summary>
        private readonly int[] _degree;


        public int GetOrder(int id) { return _order[id + 1] - 1; }
        public int GetRow(int id) { return _row[id + 1] - 1; }
        public int GetNdx(int id) { return _ndx[id + 1] - 1; }
        public int CoeffsCount { get { return _coeffsCount; } }

        ///<summary>Creates sparse representation of coeff. matrix.</summary>
        public SparseMatrix(ICollection<SimulationNode> nodes, ICollection<SimulationLink> links, int juncs) {

            _order = new int[nodes.Count + 1];
            _row = new int[nodes.Count + 1];
            _ndx = new int[links.Count + 1];
            _degree = new int[nodes.Count + 1];

            // For each node, builds an adjacency list that identifies all links connected to the node (see buildlists())
            List<AdjItem>[] adjList = new List<AdjItem>[nodes.Count + 1];
            for (int i = 0; i <= nodes.Count; i++) // <= is necessary due to the array start index being 1
                adjList[i] = new List<AdjItem>();

            BuildLists(adjList, nodes, links, true);
                // Build node-link adjacency lists with parallel links removed.
            XparaLinks(adjList); // Remove parallel links //,nodes.size()
            CountDegree(adjList, juncs); // Find degree of each junction

            _coeffsCount = links.Count;

            // Re-order nodes to minimize number of non-zero coeffs
            // in factorized solution matrix. At same time, adjacency
            // list is updated with links representing non-zero coeffs.
            ReorderNodes(adjList, juncs);

            StoreSparse(adjList, juncs); // Sort row indexes in NZSUB to optimize linsolve()
            OrderSparse(juncs);
            BuildLists(adjList, nodes, links, false);
                // Re-build adjacency lists without removing parallel links for use in future connectivity checking.
        }


        /// <summary>Builds linked list of links adjacent to each node.</summary>
        ///  <param name="adjlist">Nodes adjacency list.</param>
        /// <param name="nodes">Collecion of hydraulic simulation nodes.</param>
        /// <param name="links">Collection of hydraulic simulation links.</param>
        /// <param name="paraflag">Remove parallel links.</param>
        private void BuildLists(
            List<AdjItem>[] adjlist,
            IEnumerable<SimulationNode> nodes,
            IEnumerable<SimulationLink> links,
            bool paraflag) {

            bool pmark = false;

            foreach (SimulationLink link  in  links) {
                int k = link.Index + 1;
                int i = link.First.Index + 1;
                int j = link.Second.Index + 1;

                if (paraflag)
                    pmark = ParaLink(adjlist, i, j, k);

                // Include link in start node i's list
                AdjItem alink = new AdjItem(!pmark ? j : 0, k);

                adjlist[i].Insert(0, alink);

                // Include link in end node j's list
                alink = new AdjItem(!pmark ? i : 0, k);

                adjlist[j].Insert(0, alink);
            }
        }


        /// <summary>Checks for parallel links between nodes i and j.</summary>
        /// <param name="adjlist">Nodes adjacency list.</param>
        /// <param name="i">First node index.</param>
        /// <param name="j">Second node index.</param>
        /// <param name="k">Link index.</param>
        private bool ParaLink(List<AdjItem>[] adjlist, int i, int j, int k) {
            foreach (AdjItem alink  in  adjlist[i]) {
                if (alink.Node == j) {
                    _ndx[k] = alink.Link;
                    return true;
                }
            }
            _ndx[k] = k;
            return false;
        }

        ///<summary>Removes parallel links from nodal adjacency lists.</summary>
        ///<param name="adjlist">Nodes adjacency list.</param>
        private static void XparaLinks(List<AdjItem>[] adjlist) {
            for (int i = 1; i < adjlist.Length; i++) {
                adjlist[i].RemoveAll(x => x.Node == 0);
            }
        }

        /// <summary>Counts number of nodes directly connected to each node.</summary>
        /// <param name="adjlist">Nodes adjacency list.</param>
        /// <param name="njuncs">Number of junctions.</param>
        private void CountDegree(List<AdjItem>[] adjlist, int njuncs) {
            Array.Clear(_degree, 0, _degree.Length);

            for (int i = 1; i <= njuncs; i++) {
                foreach (AdjItem li  in  adjlist[i])
                    if (li.Node > 0) _degree[i]++;
            }
        }

        /// <summary>
        /// Re-orders nodes to minimize # of non-zeros that will appear in 
        /// factorized solution matrix.
        /// </summary>
        /// <param name="adjlist">Nodes adjacency list.</param>
        /// <param name="njuncs">Number of junctions.</param>
        private void ReorderNodes(List<AdjItem>[] adjlist, int njuncs) {
            for (int i = 1; i < adjlist.Length; i++) {
                _row[i] = i;
                _order[i] = i;
            }

            for (int i = 1; i <= njuncs; i++) {
                int m = MinDegree(i, njuncs);
                int knode = _order[m];
                GrowList(adjlist, knode);
                _order[m] = _order[i];
                _order[i] = knode;
                _degree[knode] = 0;
            }

            for (int i = 1; i <= njuncs; i++)
                _row[_order[i]] = i;
        }

        /// <summary>Finds active node with fewest direct connections.</summary>
        /// <param name="k">Junction id.</param>
        /// <param name="n">Number of junctions.</param>
        /// <returns>Node id.</returns>
        private int MinDegree(int k, int n) {
            int min = n,
                imin = n;

            for (int i = k; i <= n; i++) {
                int m = _degree[_order[i]];
                if (m < min) {
                    min = m;
                    imin = i;
                }
            }
            return imin;
        }

        ///<summary>Creates new entries in knode's adjacency list for all unlinked pairs of active nodes that are adjacent to knode.</summary>
        /// <param name="adjlist">Nodes adjacency list.</param>
        /// <param name="knode">Node id.</param>
        private void GrowList(List<AdjItem>[] adjlist, int knode) {
            for (int i = 0; i < adjlist[knode].Count; i++) {
                AdjItem alink = adjlist[knode][i];
                int node = alink.Node;
                if (_degree[node] > 0) {
                    _degree[node]--;
                    NewLink(adjlist, adjlist[knode], i);
                }
            }
        }

        /// <summary>
        ///  Links end of current adjacent link to end nodes 
        ///  of all links that follow it on adjacency list.
        ///  </summary>
        ///  <param name="adjList">Nodes adjacency list.</param>
        ///  <param name="list">Adjacent links</param>
        /// <param name="id">Link id.</param>  
        private void NewLink(List<AdjItem>[] adjList, List<AdjItem> list, int id) {
            int inode = list[id].Node;
            for (int i = id + 1; i < list.Count; i++) {
                AdjItem blink = list[i];
                int jnode = blink.Node;

                if (_degree[jnode] > 0) {
                    if (!Linked(adjList, inode, jnode)) {
                        _coeffsCount++;
                        AddLink(adjList, inode, jnode, _coeffsCount);
                        AddLink(adjList, jnode, inode, _coeffsCount);
                        _degree[inode]++;
                        _degree[jnode]++;
                    }
                }
            }
        }

        /// <summary>Checks if nodes i and j are already linked.</summary>
        ///  <param name="adjlist">Nodes adjacency list.</param>
        /// <param name="i">Node i index.</param>
        /// <param name="j">Node j index.</param>
        private static bool Linked(List<AdjItem>[] adjlist, int i, int j) {
            foreach (AdjItem alink  in  adjlist[i])
                if (alink.Node == j)
                    return true;

            return false;
        }

        /// <summary>Augments node i's adjacency list with node j.</summary>
        private static void AddLink(List<AdjItem>[] adjList, int i, int j, int n) {
            AdjItem alink = new AdjItem(j, n);
            adjList[i].Insert(0, alink);
        }

        ///<summary>Start position of each column in NZSUB.</summary>
        private int[] _xlnz;
        ///<summary>Row index of each coeff. in each column</summary>
        private int[] _nzsub;
        ///<summary>Position of each coeff. in Aij array</summary>
        private int[] _lnz;

        ///<summary>Stores row indexes of non-zeros of each column of lower triangular portion of factorized matrix.</summary>
        /// <param name="adjlist">Nodes adjacency list.</param>
        /// <param name="n">Junctions count.</param>
        private void StoreSparse(List<AdjItem>[] adjlist, int n) {
            _xlnz = new int[n + 2];
            _nzsub = new int[_coeffsCount + 2];
            _lnz = new int[_coeffsCount + 2];

            int k = 0;
            _xlnz[1] = 1;
            for (int i = 1; i <= n; i++) {
                int m = 0;
                int ii = _order[i];

                foreach (AdjItem alink  in  adjlist[ii]) {
                    int j = _row[alink.Node];
                    int l = alink.Link;
                    if (j > i && j <= n) {
                        m++;
                        k++;
                        _nzsub[k] = j;
                        _lnz[k] = l;
                    }
                }
                _xlnz[i + 1] = _xlnz[i] + m;
            }
        }

        ///<summary>Puts row indexes in ascending order in NZSUB.</summary>
        ///<param name="n">Number of junctions.</param>
        private void OrderSparse(int n) {
            int[] xlnzt = new int[n + 2];
            int[] nzsubt = new int[_coeffsCount + 2];
            int[] lnzt = new int[_coeffsCount + 2];
            int[] nzt = new int[n + 2];

            for (int i = 1; i <= n; i++) {
                for (int j = _xlnz[i]; j < _xlnz[i + 1]; j++)
                    nzt[_nzsub[j]]++;
            }

            xlnzt[1] = 1;

            for (int i = 1; i <= n; i++)
                xlnzt[i + 1] = xlnzt[i] + nzt[i];

            Transpose(n, _xlnz, _nzsub, _lnz, xlnzt, nzsubt, lnzt, nzt);
            Transpose(n, xlnzt, nzsubt, lnzt, _xlnz, _nzsub, _lnz, nzt);

        }

        /// <summary>Determines sparse storage scheme for transpose of a matrix.</summary>
        ///  <param name="n">Number of junctions.</param>
        /// <param name="il">sparse storage scheme for original matrix.</param>
        /// <param name="jl">sparse storage scheme for original matrix.</param>
        /// <param name="xl">sparse storage scheme for original matrix.</param>
        /// <param name="ilt">sparse storage scheme for transposed matrix.</param> 
        /// <param name="jlt">sparse storage scheme for transposed matrix.</param>
        /// <param name="xlt">sparse storage scheme for transposed matrix.</param>
        /// <param name="nzt">work array.</param>
        private static void Transpose(
            int n,
            int[] il,
            int[] jl,
            int[] xl,
            int[] ilt,
            int[] jlt,
            int[] xlt,
            int[] nzt) {

            for (int i = 1; i <= n; i++)
                nzt[i] = 0;

            for (int i = 1; i <= n; i++) {
                for (int k = il[i]; k < il[i + 1]; k++) {
                    int j = jl[k];
                    int kk = ilt[j] + nzt[j];
                    jlt[kk] = i;
                    xlt[kk] = xl[k];
                    nzt[j]++;
                }
            }
        }

        /// <summary>Solves sparse symmetric system of linear equations using Cholesky factorization.</summary>
        ///  <param name="n">Number of equations.</param>
        /// <param name="aii">Diagonal entries of solution matrix.</param>
        /// <param name="aij">Non-zero off-diagonal entries of matrix.</param>
        /// <param name="b">Right hand side coeffs, after solving it's also used as the solution vector.</param>
        /// <returns>0 if solution found, or index of equation causing system to be ill-conditioned.</returns>
        public int LinSolve(int n, double[] aii, double[] aij, double[] b) {
            int istop, istrt, isub;
            double bj;

            double[] temp = new double[n + 1];
            int[] link = new int[n + 1];
            int[] first = new int[n + 1];

            // Begin numerical factorization of matrix A into L
            // Compute column L(*,j) for j = 1,...n
            for (int j = 1; j <= n; j++) {
                // For each column L(*,k) that affects L(*,j):
                var diagj = 0.0;
                int newk = link[j];
                int k = newk;
                while (k != 0) {

                    // Outer product modification of L(*,j) by
                    // L(*,k) starting at first[k] of L(*,k).
                    newk = link[k];
                    int kfirst = first[k];
                    double ljk = aij[_lnz[kfirst] - 1];
                    diagj += ljk * ljk;
                    istrt = kfirst + 1;
                    istop = _xlnz[k + 1] - 1;
                    if (istop >= istrt) {

                        // Before modification, update vectors 'first'
                        // and 'link' for future modification steps.
                        first[k] = istrt;
                        isub = _nzsub[istrt];
                        link[k] = link[isub];
                        link[isub] = k;

                        // The actual mod is saved in vector 'temp'.
                        for (int i = istrt; i <= istop; i++) {
                            isub = _nzsub[i];
                            temp[isub] += aij[_lnz[i] - 1] * ljk;
                        }
                    }
                    k = newk;
                }

                // Apply the modifications accumulated
                // in 'temp' to column L(*,j).
                diagj = aii[j - 1] - diagj;
                if (diagj <= 0.0) // Check for ill-conditioning
                {
                    return j;
                }
                diagj = Math.Sqrt(diagj);
                aii[j - 1] = diagj;
                istrt = _xlnz[j];
                istop = _xlnz[j + 1] - 1;
                if (istop >= istrt) {
                    first[j] = istrt;
                    isub = _nzsub[istrt];
                    link[j] = link[isub];
                    link[isub] = j;
                    for (int i = istrt; i <= istop; i++) {
                        isub = _nzsub[i];
                        bj = (aij[_lnz[i] - 1] - temp[isub]) / diagj;
                        aij[_lnz[i] - 1] = bj;
                        temp[isub] = 0.0;
                    }
                }
            }

            // Foward substitution
            for (int j = 1; j <= n; j++) {
                bj = b[j - 1] / aii[j - 1];
                b[j - 1] = bj;
                istrt = _xlnz[j];
                istop = _xlnz[j + 1] - 1;
                if (istop >= istrt) {
                    for (int i = istrt; i <= istop; i++) {
                        isub = _nzsub[i];
                        b[isub - 1] -= aij[_lnz[i] - 1] * bj;
                    }
                }
            }

            // Backward substitution
            for (int j = n; j >= 1; j--) {
                bj = b[j - 1];
                istrt = _xlnz[j];
                istop = _xlnz[j + 1] - 1;
                if (istop >= istrt) {
                    for (int i = istrt; i <= istop; i++) {
                        isub = _nzsub[i];
                        bj -= aij[_lnz[i] - 1] * b[isub - 1];
                    }
                }
                b[j - 1] = bj / aii[j - 1];
            }

            return 0;
        }

    }

}