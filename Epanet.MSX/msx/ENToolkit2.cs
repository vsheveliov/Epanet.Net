/*
 * Copyright (C) 2012  Addition, Lda. (addition at addition dot pt)
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
using System.Diagnostics;
using System.IO;
using org.addition.epanet.hydraulic.io;
using org.addition.epanet.network;
using org.addition.epanet.network.structures;
using org.addition.epanet.util;

namespace org.addition.epanet.msx {


    ///<summary>Bridge between the hydraulic network properties and the multi-species simulation MSX class.</summary>
    public class ENToolkit2 {


        public const int EN_INITVOLUME = 14;
        public const int EN_MIXMODEL = 15;
        public const int EN_MIXZONEVOL = 16;


        public const int EN_DIAMETER = 0;
        public const int EN_LENGTH = 1;
        public const int EN_ROUGHNESS = 2;


        public const int EN_DURATION = 0; // Time parameters
        public const int EN_HYDSTEP = 1;
        public const int EN_QUALSTEP = 2;
        public const int EN_PATTERNSTEP = 3;
        public const int EN_PATTERNSTART = 4;
        public const int EN_REPORTSTEP = 5;
        public const int EN_REPORTSTART = 6;
        public const int EN_STATISTIC = 8;
        public const int EN_PERIODS = 9;

        public const int EN_NODECOUNT = 0; // Component counts
        public const int EN_TANKCOUNT = 1;
        public const int EN_LINKCOUNT = 2;
        public const int EN_PATCOUNT = 3;
        public const int EN_CURVECOUNT = 4;
        public const int EN_CONTROLCOUNT = 5;

        public const int EN_JUNCTION = 0; // Node types
        public const int EN_RESERVOIR = 1;
        public const int EN_TANK = 2;


        private readonly List<Link> links;
        private readonly List<Node> nodes;
        private readonly network.Network net;

        private HydraulicReader dseek;

        public AwareStep getStep(int htime) {
            try {
                return dseek.getStep(htime);
            }
            catch (IOException e) {
                Debug.Print(e.ToString());
            }
            return null;
        }

        public ENToolkit2(network.Network net) {
            this.net = net;
            links = new List<Link>(net.getLinks());
            nodes = new List<Node>(net.getNodes());
        }

        public void open(string hydFile) {
            dseek = new HydraulicReader(new BinaryReader(File.OpenRead(hydFile)));
        }

        public void close() {
            dseek.close();
        }

        public string ENgetlinkid(int j) {
            return links[j - 1].getId();
        }

        public string ENgetnodeid(int j) {
            return nodes[j - 1].getId();
        }

        public int ENgetnodeindex(string s, out int tmp) {
            Node n = net.getNode(s);
            tmp = nodes.IndexOf(n) + 1;

            if (tmp == 0)
                return (203);

            return 0;
        }

        public int ENgetlinkindex(string s, out int tmp) {
            Link l = net.getLink(s);
            tmp = links.IndexOf(l) + 1;
            if (tmp == 0)
                return (204);
            return 0;
        }

        public EnumTypes.FlowUnitsType ENgetflowunits() {
            try {
                return (EnumTypes.FlowUnitsType)net.getPropertiesMap().getFlowflag();
            }
            catch (ENException e) {
                Debug.Print(e.ToString());
            }
            return 0;
        }

        public int ENgetnodetype(int i) {
            var n = nodes[i - 1];
            var tank = n as Tank;
            if (tank != null) {
                return tank.getArea() == 0 ? EN_RESERVOIR : EN_TANK;
            }

            return EN_JUNCTION;
        }

        public float ENgetlinkvalue(int index, int code) {
            FieldsMap fMap = net.getFieldsMap();

            double v;

            if (index <= 0 || index > links.Count)
                throw new ENException(ErrorCode.Err204);

            var link = links[index - 1];

            switch (code) {
                case EN_DIAMETER:
                    if (link is Pump)
                        v = 0.0;
                    else
                        v = fMap.revertUnit(FieldsMap.Type.DIAM, link.getDiameter());
                    break;

                case EN_LENGTH:
                    v = fMap.revertUnit(FieldsMap.Type.ELEV, link.getLenght());
                    break;

                case EN_ROUGHNESS:
                    if (link.getType() <= Link.LinkType.PIPE) {
                        if (net.getPropertiesMap().getFormflag() == PropertiesMap.FormType.DW)
                            v = fMap.revertUnit(FieldsMap.Type.ELEV, link.getRoughness()*1000.00);
                        else
                            v = link.getRoughness();
                    }
                    else
                        v = 0.0;
                    break;
                default:
                    throw new ENException(ErrorCode.Err251);
            }
            return ((float)v);
        }

        public int ENgetcount(int code) {
            switch (code) {
                case EN_NODECOUNT:
                    return nodes.Count;
                case EN_TANKCOUNT:
                    return net.getTanks().Length;
                case EN_LINKCOUNT:
                    return links.Count;
                case EN_PATCOUNT:
                    return net.getPatterns().Length;
                case EN_CURVECOUNT:
                    return net.getCurves().Length;
                case EN_CONTROLCOUNT:
                    return net.getControls().Length;
                default:
                    return 0;
            }
        }

        public long ENgettimeparam(int code) {
            long value = 0;
            if (code < EN_DURATION || code > EN_STATISTIC) //EN_PERIODS)
                return (251);
            try {
                switch (code) {
                    case EN_DURATION:
                        value = net.getPropertiesMap().getDuration();
                        break;
                    case EN_HYDSTEP:
                        value = net.getPropertiesMap().getHstep();
                        break;
                    case EN_QUALSTEP:
                        value = net.getPropertiesMap().getQstep();
                        break;
                    case EN_PATTERNSTEP:
                        value = net.getPropertiesMap().getPstep();
                        break;
                    case EN_PATTERNSTART:
                        value = net.getPropertiesMap().getPstart();
                        break;
                    case EN_REPORTSTEP:
                        value = net.getPropertiesMap().getRstep();
                        break;
                    case EN_REPORTSTART:
                        value = net.getPropertiesMap().getRstart();
                        break;
                    case EN_STATISTIC:
                        value = (long)net.getPropertiesMap().getTstatflag();
                        break;
                    case EN_PERIODS:
                        throw new NotSupportedException();
                            //value = dseek.getAvailableSteps().size();                 break;
                }
            }
            catch (ENException) {

            }
            return (value);
        }

        public float ENgetnodevalue(int index, int code) {
            double v;

            FieldsMap fMap = net.getFieldsMap();

            if (index <= 0 || index > nodes.Count)
                return (203);

            switch (code) {
                case EN_INITVOLUME:
                    v = 0.0;
                    if (nodes[index - 1] is Tank)
                        v = fMap.revertUnit(FieldsMap.Type.VOLUME, ((Tank)nodes[index - 1]).getV0());
                    break;

                case EN_MIXMODEL:
                    v = (double)Tank.MixType.MIX1;
                    if (nodes[index - 1] is Tank)
                        v = (double)((Tank)nodes[index - 1]).getMixModel();
                    break;


                case EN_MIXZONEVOL:
                    v = 0.0;
                    if (nodes[index - 1] is Tank)
                        v = fMap.revertUnit(FieldsMap.Type.VOLUME, ((Tank)nodes[index - 1]).getV1max());
                    break;

                default:
                    throw new ENException(ErrorCode.Err251);
            }
            return (float)v;
        }

        public int[] ENgetlinknodes(int index) {
            if (index < 1 || index > links.Count)
                throw new ENException(ErrorCode.Err204);

            Link l = links[index - 1];

            return new int[] {nodes.IndexOf(l.getFirst()) + 1, nodes.IndexOf(l.getSecond()) + 1};
        }
    }
}