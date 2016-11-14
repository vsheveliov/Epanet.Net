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
using org.addition.epanet.network;
using org.addition.epanet.network.structures;
using org.addition.epanet.util;

namespace org.addition.epanet.hydraulic.structures {


public class SimulationNode {
    protected readonly int index;
    protected readonly Node node;

    protected double head;   // Epanet 'H[n]' variable, node head.
    protected double demand; // Epanet 'D[n]' variable, node demand.
    protected double emitter;// Epanet 'E[n]' variable, emitter flows

    public static SimulationNode createIndexedNode(Node _node, int idx){
        SimulationNode ret;
        if(_node is Tank)
            ret = new SimulationTank(_node,idx);
        else
            ret = new SimulationNode(_node,idx);

        return ret;
    }
    public SimulationNode(Node @ref, int idx) {
        this.node =@ref;
        index = idx;
    }

    public int Index { get { return this.index; } }

    public Node Node { get { return this.node; } }

    public string Id { get { return this.node.Id; } }

    public Node.NodeType Type { get { return this.node.Type; } }

    public double getElevation() {
        return this.node.Elevation;
    }

    public List<Demand> getDemand() {
        return this.node.Demand;
    }

    //public Source getSource() {
    //    return node.getSource();
    //}


    public double [] getC0() {
        return this.node.C0;
    }


    public double getKe() {
        return this.node.Ke;
    }

    //public bool getRptFlag() {
    //    return node.isRptFlag();
    //}

    ////

    public double getSimHead() {
        return head;
    }

    public void setSimHead(double value)
    {
        this.head = value;
    }

    public double getSimDemand(){
        return demand;
    }

    public void setSimDemand(double value){
        demand = value;
    }

    public double getSimEmitter(){
        return emitter;
    }

    public void setSimEmitter(double value){
        emitter = value;
    }

    ////

    // Completes calculation of nodal flow imbalance (X) flow correction (F) arrays
    public static void computeNodeCoeffs(List<SimulationNode> junctions, SparseMatrix smat, LSVariables ls){
        foreach (SimulationNode node  in  junctions)
        {
            ls.addNodalInFlow(node, - node.demand);
            ls.addRHSCoeff(smat.getRow(node.Index),+ls.getNodalInFlow(node));
        }
    }

    // computes matrix coeffs. for emitters
    // Emitters consist of a fictitious pipe connected to
    // a fictitious reservoir whose elevation equals that
    // of the junction. The headloss through this pipe is
    // Ke*(Flow)^Qexp, where Ke = emitter headloss coeff.

    public static void computeEmitterCoeffs(PropertiesMap pMap,
                                            List<SimulationNode> junctions,
                                            SparseMatrix smat, LSVariables ls) {
        foreach (SimulationNode node  in  junctions) {
            if (node.Node.Ke == 0.0)
                continue;

            double ke = Math.Max(Constants.CSMALL, node.Node.Ke);
            double q = node.emitter;
            double z = ke * Math.Pow(Math.Abs(q), pMap.Qexp);
            double p = pMap.Qexp* z / Math.Abs(q);

            if (p < pMap.RQtol)
                p = 1.0 / pMap.RQtol;
            else
                p = 1.0 / p;

            double y = Utilities.GetSignal(q) * z * p;
            ls.addAii(smat.getRow(node.Index),+ p);
            ls.addRHSCoeff(smat.getRow(node.Index), + (y + p * node.Node.Elevation));
            ls.addNodalInFlow(node, - q);
        }
    }

    // Computes flow change at an emitter node
    public double emitFlowChange(PropertiesMap pMap) {
        double ke = Math.Max(Constants.CSMALL, getKe());
        double p = pMap.Qexp * ke * Math.Pow(Math.Abs(emitter), (pMap.Qexp - 1.0));
        if (p < pMap.RQtol)
            p = 1.0d / pMap.RQtol;
        else
            p = 1.0d / p;
        return (emitter / pMap.Qexp - p * (head-getElevation()));
    }

}
}