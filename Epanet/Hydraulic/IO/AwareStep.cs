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
using Epanet.Hydraulic.Structures;
using Epanet.Network;
using Epanet.Network.Structures;
using Epanet.Quality;
using Epanet.Quality.Structures;
using Epanet.Util;

namespace Epanet.Hydraulic.IO {



    ///<summary>Aware compatible hydraulic step snapshot</summary>
    public class AwareStep {
        private readonly double[] qn;
        private readonly double[] ql;
        private readonly double[] d;
        private readonly double[] h;
        private readonly double[] q;
        private readonly double[] dh;
        private readonly long hydTime;
        private readonly long hydStep;

        private const int FORMAT_VERSION = 1;

        public class HeaderInfo {
            public int Version;
            public int Nodes;
            public int Links;
            public long Rstart;
            public long Rstep;
            public long Duration;
        }

        // ReSharper disable RedundantCast

        public static void WriteHeader(
            BinaryWriter outStream,
            HydraulicSim hydraulicSim,
            long rstart,
            long rstep,
            long duration) {
            outStream.Write((int)FORMAT_VERSION);
            outStream.Write((int)hydraulicSim.Nodes.Count);
            outStream.Write((int)hydraulicSim.Links.Count);
            outStream.Write((long)rstart);
            outStream.Write((long)rstep);
            outStream.Write((long)duration);
        }



        public static HeaderInfo ReadHeader(BinaryReader @in) {
            var headerInfo = new HeaderInfo {
                Version = @in.ReadInt32(),
                Nodes = @in.ReadInt32(),
                Links = @in.ReadInt32(),
                Rstart = @in.ReadInt64(),
                Rstep = @in.ReadInt64(),
                Duration = @in.ReadInt64()
            };
            return headerInfo;
        }


        public static void Write(BinaryWriter outStream, HydraulicSim hydraulicSim, long hydStep) {

            List<SimulationNode> nodes = hydraulicSim.Nodes;
            List<SimulationLink> links = hydraulicSim.Links;
            long hydTime = hydraulicSim.Htime;

            // int nNodes = nodes.Count;
            // int nLinks = links.Count;
            // int baSize = (nNodes * 3 + nLinks * 3) * sizeof(double)  + sizeof(long) * 2;

            foreach (SimulationNode node  in  nodes) {
                outStream.Write((double)node.SimDemand);
                outStream.Write((double)node.SimHead);
                outStream.Write((double)0);
            }

            foreach (SimulationLink link  in  links) {
                outStream.Write((double)(link.SimStatus <= Link.StatType.CLOSED ? 0d : link.SimFlow));
                outStream.Write((double)(link.First.SimHead - link.Second.SimHead));
                outStream.Write((double)0.0);
            }

            outStream.Write((long)hydStep);
            outStream.Write((long)hydTime);
        }

        public static void WriteHydAndQual(
            BinaryWriter outStream,
            HydraulicSim hydraulicSim,
            QualitySim qualitySim,
            long step,
            long time) {
            List<QualityNode> qNodes = qualitySim != null ? qualitySim.NNodes : null;
            List<QualityLink> qLinks = qualitySim != null ? qualitySim.NLinks : null;
            List<SimulationNode> nodes = hydraulicSim.Nodes;
            List<SimulationLink> links = hydraulicSim.Links;

            // int nNodes = nodes.Count;
            // int nLinks = links.Count;
            // int baSize = (nNodes * 3 + nLinks * 3) * sizeof(double) + sizeof(long) * 2;

            int count = 0;
            foreach (SimulationNode node  in  nodes) {
                outStream.Write((double)node.SimDemand);
                outStream.Write((double)node.SimHead);
                outStream.Write((double)(qualitySim != null ? qNodes[count++].Quality : 0.0));
            }

            count = 0;
            foreach (SimulationLink link  in  links) {
                outStream.Write((double)(link.SimStatus <= Link.StatType.CLOSED ? 0d : link.SimFlow));
                outStream.Write((double)(link.First.SimHead - link.Second.SimHead));
                outStream.Write((double)(qualitySim != null ? qLinks[count++].GetAverageQuality(null) : 0));
            }

            outStream.Write((long)step);
            outStream.Write((long)time);

        }

#if COMMENTED
        public static void WriteHybrid(
            BinaryWriter outStream,
            HydraulicSim hydraulicSim,
            double[] qN,
            double[] qL,
            long step,
            long time) {

            List<SimulationNode> nodes = hydraulicSim.Nodes;
            List<SimulationLink> links = hydraulicSim.Links;

            // int nNodes = nodes.Count;
            // int nLinks = links.Count;
            // int baSize = (nNodes * 3 + nLinks * 3) * sizeof(double) + sizeof(long) * 2;

            int count = 0;
            foreach (SimulationNode node  in  nodes) {
                outStream.Write((double)node.SimDemand);
                outStream.Write((double)node.SimHead);
                outStream.Write((double)qN[count++]);
            }

            count = 0;
            foreach (SimulationLink link  in  links) {
                outStream.Write((double)(link.SimStatus <= Link.StatType.CLOSED ? 0d : link.SimFlow));
                outStream.Write((double)(link.First.SimHead - link.Second.SimHead));
                outStream.Write((double)qL[count++]);
            }

            outStream.Write((long)step);
            outStream.Write((long)time);

        }

#endif

        // ReSharper restore RedundantCast

        public AwareStep(BinaryReader inStream, HeaderInfo headerInfo) {
            int nNodes = headerInfo.Nodes;
            int nLinks = headerInfo.Links;

            this.d = new double[nNodes];
            this.h = new double[nNodes];
            this.q = new double[nLinks];
            this.dh = new double[nLinks];

            this.qn = new double[nNodes];
            this.ql = new double[nLinks];

            //int baSize = (nNodes * 3 + nLinks * 3) * sizeof(double) + sizeof(long) * 2;
            //byte[] ba = new byte[baSize];
            //inStream.readFully(ba);
            //ByteBuffer buf = ByteBuffer.wrap(ba);

            for (int i = 0; i < nNodes; i++) {
                this.d[i] = inStream.ReadDouble();
                this.h[i] = inStream.ReadDouble();
                this.qn[i] = inStream.ReadDouble();
            }

            for (int i = 0; i < nLinks; i++) {
                this.q[i] = inStream.ReadDouble();
                this.dh[i] = inStream.ReadDouble();
                this.ql[i] = inStream.ReadDouble();
            }

            this.hydStep = inStream.ReadInt64();
            this.hydTime = inStream.ReadInt64();
        }


        public double GetNodeDemand(int id, Node node, FieldsMap fMap) {
            try {
                return fMap != null ? fMap.RevertUnit(FieldsMap.FieldType.DEMAND, this.d[id]) : this.d[id];
            }
            catch (ENException) {
                return 0;
            }
        }

        public double GetNodeHead(int id, Node node, FieldsMap fMap) {
            try {
                return fMap != null ? fMap.RevertUnit(FieldsMap.FieldType.HEAD, this.h[id]) : this.h[id];
            }
            catch (ENException) {
                return 0;
            }
        }

        public double GetNodePressure(int id, Node node, FieldsMap fMap) {
            try {
                double p = (this.GetNodeHead(id, node, null) - node.Elevation);

                return fMap != null ? fMap.RevertUnit(FieldsMap.FieldType.PRESSURE, p) : p;
            }
            catch (ENException) {
                return 0;
            }
        }

        public double GetLinkFlow(int id, Link link, FieldsMap fMap) {
            try {
                return fMap != null ? fMap.RevertUnit(FieldsMap.FieldType.FLOW, this.q[id]) : this.q[id];
            }
            catch (ENException) {
                return 0;
            }
        }


        public double GetLinkVelocity(int id, Link link, FieldsMap fMap) {
            try {
                double v;
                double flow = this.GetLinkFlow(id, link, null);
                if (link is Pump)
                    v = 0;
                else
                    v = (Math.Abs(flow) / (Math.PI * Math.Pow(link.Diameter, 2) / 4.0));

                return fMap != null ? fMap.RevertUnit(FieldsMap.FieldType.VELOCITY, v) : v;
            }
            catch (ENException) {
                return 0;
            }
        }

        public double GetLinkHeadLoss(int id, Link link, FieldsMap fMap) {
            try {
                if (this.GetLinkFlow(id, link, null) == 0) {
                    return 0.0;
                }
                else {
                    double hh = this.dh[id];
                    if (!(link is Pump))
                        hh = Math.Abs(hh);

                    if (link.Type <= Link.LinkType.PIPE)
                        return (1000 * hh / link.Lenght);
                    else
                        return fMap != null ? fMap.RevertUnit(FieldsMap.FieldType.HEADLOSS, hh) : hh;
                }
            }
            catch (ENException) {
                return 0;
            }
        }


        public double GetLinkFriction(int id, Link link, FieldsMap fMap) {
            try {
                double f;

                double flow = this.GetLinkFlow(id, link, null);
                if (link.Type <= Link.LinkType.PIPE && Math.Abs(flow) > Constants.TINY) {


                    double hh = Math.Abs(this.dh[id]);
                    f = 39.725 * hh * Math.Pow(link.Diameter, 5) / link.Lenght /
                        (flow * flow);
                }
                else
                    f = 0;

                return fMap != null ? fMap.RevertUnit(FieldsMap.FieldType.FRICTION, f) : f;
            }
            catch (ENException) {
                return 0;
            }
        }

        public double GetLinkAvrQuality(int id) { return this.ql[id]; }

        public double GetNodeQuality(int id) { return this.qn[id]; }

        public long Step { get { return this.hydStep; } }

        public long Time { get { return this.hydTime; } }
    }

}