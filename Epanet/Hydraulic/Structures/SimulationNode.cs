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
        private readonly int _index;
        protected readonly Node node;

        ///<summary>Epanet 'H[n]' variable, node head.</summary>
        protected double head; 

        ///<summary>Epanet 'D[n]' variable, node demand.</summary>
        protected double demand; 

        ///<summary>Epanet 'E[n]' variable, emitter flows</summary>
        private double _emitter; 

        public SimulationNode(Node @ref, int idx) {
            node = @ref;
            _index = idx;
        }

        public int Index { get { return _index; } }

        public Node Node { get { return node; } }

        public string Id { get { return node.Name; } }

        public NodeType Type { get { return node.Type; } }

        public double Elevation { get { return node.Elevation; } }

        public List<Demand> Demand { get { return node.Demands; } }

        public double Ke { get { return node.Ke; } }

        ///<summary>Epanet 'H[n]' variable, node head.</summary>
        public double SimHead { get { return head; } set { head = value; } }

        ///<summary>Epanet 'D[n]' variable, node demand.</summary>
        public double SimDemand { get { return demand; } set { demand = value; } }

        ///<summary>Epanet 'E[n]' variable, emitter flows</summary>
        public double SimEmitter { get { return _emitter; } set { _emitter = value; } }

        /// <summary>Completes calculation of nodal flow imbalance (X) flow correction (F) arrays.</summary>
        public static void ComputeNodeCoeffs(List<SimulationNode> junctions, SparseMatrix smat, LsVariables ls) {
            foreach (SimulationNode node  in  junctions) {
                ls.AddNodalInFlow(node, -node.demand);
                ls.AddRhsCoeff(smat.GetRow(node.Index), +ls.GetNodalInFlow(node));
            }
        }

        /// <summary>Computes matrix coeffs. for emitters.</summary>
        /// <remarks>
        /// Emitters consist of a fictitious pipe connected to
        /// a fictitious reservoir whose elevation equals that
        /// of the junction. The headloss through this pipe is
        /// Ke*(Flow)^Qexp, where Ke = emitter headloss coeff.
        /// </remarks>
        public static void ComputeEmitterCoeffs(
            EpanetNetwork net,
            List<SimulationNode> junctions,
            SparseMatrix smat,
            LsVariables ls) {

            foreach (SimulationNode node  in  junctions) {
                if (node.Node.Ke == 0.0)
                    continue;

                double ke = Math.Max(Constants.CSMALL, node.Node.Ke);
                double q = node._emitter;
                double z = ke * Math.Pow(Math.Abs(q), net.QExp);
                double p = net.QExp * z / Math.Abs(q);

                p = p < net.RQtol ? 1.0 / net.RQtol : 1.0 / p;

                double y = Utilities.GetSignal(q) * z * p;
                ls.AddAii(smat.GetRow(node.Index), +p);
                ls.AddRhsCoeff(smat.GetRow(node.Index), +(y + p * node.Node.Elevation));
                ls.AddNodalInFlow(node, -q);
            }
        }

        /// <summary>Computes flow change at an emitter node.</summary>
        public double EmitFlowChange(EpanetNetwork net) {
            double ke = Math.Max(Constants.CSMALL, Ke);
            double p = net.QExp * ke * Math.Pow(Math.Abs(_emitter), (net.QExp - 1.0));
            p = p < net.RQtol ? 1.0d / net.RQtol : 1.0d / p;
            return (_emitter / net.QExp - p * (head - Elevation));
        }

    }

}