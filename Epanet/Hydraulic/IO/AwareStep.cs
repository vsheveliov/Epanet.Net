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
using System.IO;
using org.addition.epanet.hydraulic.structures;
using org.addition.epanet.network;
using org.addition.epanet.network.structures;
using org.addition.epanet.quality;
using org.addition.epanet.quality.structures;
using org.addition.epanet.util;

namespace org.addition.epanet.hydraulic.io {



///<summary>Aware compatible hydraulic step snapshot</summary>
public class AwareStep {
    private double[] QN;
    private double[] QL;
    private double[] D;
    private double[] H;
    private double[] Q;
    private double[] DH;
    private long hydTime;
    private long hydStep;


    public const int FORMAT_VERSION = 1;

    public class HeaderInfo {
        public int version;
        public int nodes;
        public int links;
        public long rstart;
        public long rstep;
        public long duration;
    }

    public static void writeHeader(BinaryWriter outStream, HydraulicSim hydraulicSim, long rstart, long rstep, long duration) {
        outStream.Write((int)FORMAT_VERSION);
        outStream.Write((int)hydraulicSim.getnNodes().Count);
        outStream.Write((int)hydraulicSim.getnLinks().Count);
        outStream.Write((long)rstart);
        outStream.Write((long)rstep);
        outStream.Write((long)duration);
    }

    public static HeaderInfo readHeader(BinaryReader @in)  {
        var headerInfo = new HeaderInfo
        {
            version = @in.ReadInt32(),
            nodes = @in.ReadInt32(),
            links = @in.ReadInt32(),
            rstart = @in.ReadInt64(),
            rstep = @in.ReadInt64(),
            duration = @in.ReadInt64()
        };
        return headerInfo;
    }


    public static void write(BinaryWriter outStream, HydraulicSim hydraulicSim, long hydStep) {

        List<SimulationNode> nodes = hydraulicSim.getnNodes();
        List<SimulationLink> links = hydraulicSim.getnLinks();
        long hydTime = hydraulicSim.getHtime();

        int nNodes = nodes.Count;
        int nLinks = links.Count;

        // int baSize = (nNodes * 3 + nLinks * 3) * sizeof(double)  + sizeof(long) * 2;
        

        foreach (SimulationNode node  in  nodes) {
            outStream.Write((double)node.getSimDemand());
            outStream.Write((double)node.getSimHead());
            outStream.Write((double)0);
        }

        foreach (SimulationLink link  in  links) {
            outStream.Write((double)(link.getSimStatus() <= Link.StatType.CLOSED ? 0d : link.getSimFlow()));
            outStream.Write((double)(link.getFirst().getSimHead() - link.getSecond().getSimHead()));
            outStream.Write((double)0);
        }

        outStream.Write((long)hydStep);
        outStream.Write((long)hydTime);
    }

    public static void writeHydAndQual(BinaryWriter outStream, HydraulicSim hydraulicSim, QualitySim qualitySim, long step, long time) {
        List<QualityNode> qNodes = qualitySim != null ? qualitySim.getnNodes() : null;
        List<QualityLink> qLinks = qualitySim != null ? qualitySim.getnLinks() : null;
        List<SimulationNode> nodes = hydraulicSim.getnNodes();
        List<SimulationLink> links = hydraulicSim.getnLinks();

        int nNodes = nodes.Count;
        int nLinks = links.Count;

        // int baSize = (nNodes * 3 + nLinks * 3) * sizeof(double) + sizeof(long) * 2;
        
        int count = 0;
        foreach (SimulationNode node  in  nodes) {
            outStream.Write((double)node.getSimDemand());
            outStream.Write((double)node.getSimHead());
            outStream.Write((double)(qualitySim != null ? qNodes[count++].getQuality() : 0.0));
        }

        count = 0;
        foreach (SimulationLink link  in  links) {
            outStream.Write((double)(link.getSimStatus() <= Link.StatType.CLOSED ? 0d : link.getSimFlow()));
            outStream.Write((double)(link.getFirst().getSimHead() - link.getSecond().getSimHead()));
            outStream.Write((double)(qualitySim != null ? qLinks[count++].getAverageQuality(null) : 0));
        }

        outStream.Write((long)step);
        outStream.Write((long)time);

    }

#if COMMENTED
     public static void writeHybrid(BinaryWriter outStream,HydraulicSim hydraulicSim, double [] qN, double [] qL , long step, long time) {

        List<SimulationNode> nodes = hydraulicSim.getnNodes();
        List<SimulationLink> links = hydraulicSim.getnLinks();

        int nNodes = nodes.Count;
        int nLinks = links.Count;

        // int baSize = (nNodes * 3 + nLinks * 3) * sizeof(double) + sizeof(long) * 2;
        
        int count= 0;
        foreach (SimulationNode node  in  nodes) {
            outStream.Write((double)node.getSimDemand());
            outStream.Write((double)node.getSimHead());
            outStream.Write((double)qN[count++]);
        }

        count = 0;
        foreach (SimulationLink link  in  links) {
            outStream.Write((double)(link.getSimStatus() <= Link.StatType.CLOSED ? 0d : link.getSimFlow()));
            outStream.Write((double)(link.getFirst().getSimHead() - link.getSecond().getSimHead()));
            outStream.Write((double)qL[count++]);
        }

        outStream.Write((long)step);
        outStream.Write((long)time);

    } 

#endif

    public AwareStep(BinaryReader inStream, HeaderInfo headerInfo) {
        int nNodes = headerInfo.nodes;
        int nLinks = headerInfo.links;

        D = new double[nNodes];
        H = new double[nNodes];
        Q = new double[nLinks];
        DH = new double[nLinks];

        QN = new double[nNodes];
        QL = new double[nLinks];

        //int baSize = (nNodes * 3 + nLinks * 3) * sizeof(double) + sizeof(long) * 2;
        //byte[] ba = new byte[baSize];
        //inStream.readFully(ba);
        //ByteBuffer buf = ByteBuffer.wrap(ba);

        for (int i = 0; i < nNodes; i++) {
            D[i] = inStream.ReadDouble();
            H[i] = inStream.ReadDouble();
            QN[i] = inStream.ReadDouble();
        }

        for (int i = 0; i < nLinks; i++) {
            Q[i] = inStream.ReadDouble();
            DH[i] = inStream.ReadDouble();
            QL[i] = inStream.ReadDouble();
        }

        hydStep = inStream.ReadInt64();
        hydTime = inStream.ReadInt64();
    }


    public double getNodeDemand(int id, Node node, FieldsMap fMap) {
        try {
            return fMap != null ? fMap.revertUnit(FieldsMap.Type.DEMAND, D[id]) : D[id];
        } catch (ENException) {
            return 0;
        }
    }

    public double getNodeHead(int id, Node node, FieldsMap fMap) {
        try {
            return fMap != null ? fMap.revertUnit(FieldsMap.Type.HEAD, H[id]) : H[id];
        } catch (ENException e) {
            return 0;
        }
    }

    public double getNodePressure(int id, Node node, FieldsMap fMap) {
        try {
            double P = (getNodeHead(id, node, null) - node.getElevation());

            return fMap != null ? fMap.revertUnit(FieldsMap.Type.PRESSURE, P) : P;
        } catch (ENException e) {
            return 0;
        }
    }

    public double getLinkFlow(int id, Link link, FieldsMap fMap) {
        try {
            return fMap != null ? fMap.revertUnit(FieldsMap.Type.FLOW, Q[id]) : Q[id];
        } catch (ENException e) {
            return 0;
        }
    }


    public double getLinkVelocity(int id, Link link, FieldsMap fMap) {
        try {
            double V;
            double flow = getLinkFlow(id, link, null);
            if (link is Pump)
                V = 0;
            else
                V = (Math.Abs(flow) / (Constants.PI * Math.Pow(link.getDiameter(), 2) / 4.0));

            return fMap != null ? fMap.revertUnit(FieldsMap.Type.VELOCITY, V) : V;
        } catch (ENException e) {
            return 0;
        }
    }

    public double getLinkHeadLoss(int id, Link link, FieldsMap fMap) {
        try {
            if (getLinkFlow(id, link, null) == 0) {
                return 0.0;
            } else {
                double h = DH[id];
                if (!(link is Pump))
                    h = Math.Abs(h);

                if (link.getType() <= Link.LinkType.PIPE)
                    return (1000 * h / link.getLenght());
                else
                    return fMap != null ? fMap.revertUnit(FieldsMap.Type.HEADLOSS, h) : h;
            }
        } catch (ENException e) {
            return 0;
        }
    }


    public double getLinkFriction(int id, Link link, FieldsMap fMap) {
        try {
            double F;

            double flow = getLinkFlow(id, link, null);
            if (link.getType() <= Link.LinkType.PIPE && Math.Abs(flow) > Constants.TINY) {


                double h = Math.Abs(DH[id]);
                F = 39.725 * h * Math.Pow(link.getDiameter(), 5) / link.getLenght() /
                        (flow * flow);
            } else
                F = 0;

            return fMap != null ? fMap.revertUnit(FieldsMap.Type.FRICTION, F) : F;
        } catch (ENException e) {
            return 0;
        }
    }

    public double getLinkAvrQuality(int id) {
        return QL[id];
    }

    public double getNodeQuality(int id) {
        return QN[id];
    }

    public long getStep() {
        return hydStep;
    }

    public long getTime() {
        return hydTime;
    }


}
}