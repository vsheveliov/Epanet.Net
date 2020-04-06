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

using Epanet.Enums;
using Epanet.Network.Structures;
using Epanet.Util;

using EpanetNetwork = Epanet.Network.Network;

namespace Epanet.Hydraulic.Structures {


    public class SimulationNode {
        public SimulationNode(Node @ref, int idx) {
            Node = @ref;
            Index = idx;
        }

        public int Index { get; }

        public Node Node { get; }

        public string Id => Node.Name;

        public NodeType Type => Node.NodeType;

        public double Elevation => Node.Elevation;

        public List<Demand> Demand => Node.Demands;

        public double Ke => Node.Ke;

        ///<summary>Epanet 'H[n]' variable, node head.</summary>
        public double SimHead { get; set; }

        ///<summary>Epanet 'D[n]' variable, node demand.</summary>
        public double SimDemand { get; set; }

        ///<summary>Epanet 'E[n]' variable, emitter flows</summary>
        public double SimEmitter { get; set; }

        /// <summary>Completes calculation of nodal flow imbalance (X) flow correction (F) arrays.</summary>
        internal void ComputeNodeCoeff(SparseMatrix smat, LsVariables ls) {
            ls.x[Index] -= SimDemand;
            ls.f[smat.GetRow(Index)] += ls.x[Index];
        }

        /// <summary>Computes matrix coeffs. for emitters.</summary>
        /// <remarks>
        /// Emitters consist of a fictitious pipe connected to
        /// a fictitious reservoir whose elevation equals that
        /// of the junction. The headloss through this pipe is
        /// Ke*(Flow)^Qexp, where Ke = emitter headloss coeff.
        /// </remarks>
        internal void ComputeEmitterCoeff(EpanetNetwork net, SparseMatrix smat, LsVariables ls)
        {
            if (Node.Ke.IsZero()) return;

            double ke = Math.Max(Constants.CSMALL, Node.Ke);
            double q = SimEmitter;
            double z = ke * Math.Pow(Math.Abs(q), net.QExp);
            double p = net.QExp * z / Math.Abs(q);

            p = p < net.RQtol ? 1.0 / net.RQtol : 1.0 / p;

            double y = q.Sign() * z * p;
            ls.aii[smat.GetRow(Index)] += p;
            ls.f[smat.GetRow(Index)] += y + p * Node.Elevation;
            ls.x[Index] -= q;
        }


        /// <summary>Computes flow change at an emitter node.</summary>
        public double EmitFlowChange(EpanetNetwork net) {
            double ke = Math.Max(Constants.CSMALL, Ke);
            double p = net.QExp * ke * Math.Pow(Math.Abs(SimEmitter), net.QExp - 1.0);
            p = p < net.RQtol ? 1.0d / net.RQtol : 1.0d / p;
            return (SimEmitter / net.QExp - p * (SimHead - Elevation));
        }

    }

}