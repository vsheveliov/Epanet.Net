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
using System.Diagnostics;
using System.IO;
using System.Linq;
using Epanet.Hydraulic.IO;
using Epanet.Network;
using Epanet.Network.Structures;
using Epanet.Util;

namespace Epanet.MSX {

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


        private readonly IList<Link> links;
        private readonly IList<Node> nodes;
        private readonly Epanet.Network.Network net;

        private HydraulicReader dseek;

        public AwareStep GetStep(int htime) {
            try {
                return this.dseek.GetStep(htime);
            }
            catch (IOException e) {
                Debug.Print(e.ToString());
            }
            return null;
        }

        public ENToolkit2(Epanet.Network.Network net) {
            this.net = net;
            this.links = net.Links;
            this.nodes = net.Nodes;
        }

        public void Open(string hydFile) { this.dseek = new HydraulicReader(new BinaryReader(File.OpenRead(hydFile))); }

        public void Close() { this.dseek.Close(); }

        public string ENgetlinkid(int j) { return this.links[j - 1].Id; }

        public string ENgetnodeid(int j) { return this.nodes[j - 1].Id; }

        public int ENgetnodeindex(string s, out int tmp) {
            Node n = this.net.GetNode(s);
            tmp = this.nodes.IndexOf(n) + 1;

            return tmp == 0 ? (203) : 0;
        }

        public int ENgetlinkindex(string s, out int tmp) {
            Link l = this.net.GetLink(s);
            tmp = this.links.IndexOf(l) + 1;
            return tmp == 0 ? 204 : 0;
        }

        public EnumTypes.FlowUnitsType ENgetflowunits() {
            try {
                return (EnumTypes.FlowUnitsType)this.net.PropertiesMap.FlowFlag;
            }
            catch (ENException e) {
                Debug.Print(e.ToString());
            }
            return 0;
        }

        public int ENgetnodetype(int i) {
            var n = this.nodes[i - 1];
            var tank = n as Tank;
            if (tank != null) {
                return tank.IsReservoir ? EN_RESERVOIR : EN_TANK;
            }

            return EN_JUNCTION;
        }

        public float ENgetlinkvalue(int index, int code) {
            FieldsMap fMap = this.net.FieldsMap;

            double v;

            if (index <= 0 || index > this.links.Count)
                throw new ENException(ErrorCode.Err204);

            var link = this.links[index - 1];

            switch (code) {
            case EN_DIAMETER:
                if (link is Pump)
                    v = 0.0;
                else
                    v = fMap.RevertUnit(FieldsMap.FieldType.DIAM, link.Diameter);
                break;

            case EN_LENGTH:
                v = fMap.RevertUnit(FieldsMap.FieldType.ELEV, link.Lenght);
                break;

            case EN_ROUGHNESS:
                if (link.Type <= Link.LinkType.PIPE) {
                    v = this.net.PropertiesMap.FormFlag == PropertiesMap.FormType.DW
                        ? fMap.RevertUnit(FieldsMap.FieldType.ELEV, link.Roughness * 1000.00)
                        : link.Roughness;
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
                return this.nodes.Count;
            case EN_TANKCOUNT:
                return this.net.Tanks.Count();
            case EN_LINKCOUNT:
                return this.links.Count;
            case EN_PATCOUNT:
                return this.net.Patterns.Count;
            case EN_CURVECOUNT:
                return this.net.Curves.Count;
            case EN_CONTROLCOUNT:
                return this.net.Controls.Count;
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
                    value = this.net.PropertiesMap.Duration;
                    break;
                case EN_HYDSTEP:
                    value = this.net.PropertiesMap.HStep;
                    break;
                case EN_QUALSTEP:
                    value = this.net.PropertiesMap.QStep;
                    break;
                case EN_PATTERNSTEP:
                    value = this.net.PropertiesMap.PStep;
                    break;
                case EN_PATTERNSTART:
                    value = this.net.PropertiesMap.PStart;
                    break;
                case EN_REPORTSTEP:
                    value = this.net.PropertiesMap.RStep;
                    break;
                case EN_REPORTSTART:
                    value = this.net.PropertiesMap.RStart;
                    break;
                case EN_STATISTIC:
                    value = (long)this.net.PropertiesMap.TStatFlag;
                    break;
                case EN_PERIODS:
                    throw new NotSupportedException();
                //value = dseek.getAvailableSteps().size();                 break;
                }
            }
            catch (ENException) {}
            return (value);
        }

        public float ENgetnodevalue(int index, int code) {
            double v;

            FieldsMap fMap = this.net.FieldsMap;

            if (index <= 0 || index > this.nodes.Count)
                return (203);

            Tank tank;
            switch (code) {
            case EN_INITVOLUME:
                v = 0.0;
                tank = this.nodes[index - 1] as Tank;
                if (tank != null)
                    v = fMap.RevertUnit(FieldsMap.FieldType.VOLUME, tank.V0);
                break;

            case EN_MIXMODEL:
                v = (double)Tank.MixType.MIX1;
                tank = this.nodes[index - 1] as Tank;
                if (tank != null)
                    v = (double)tank.MixModel;
                break;


            case EN_MIXZONEVOL:
                v = 0.0;
                tank = this.nodes[index - 1] as Tank;
                if (tank != null)
                    v = fMap.RevertUnit(FieldsMap.FieldType.VOLUME, tank.V1Max);
                break;

            default:
                throw new ENException(ErrorCode.Err251);
            }
            return (float)v;
        }

        public void ENgetlinknodes(int index, out int n1, out int n2) {
            if (index < 1 || index > this.links.Count)
                throw new ENException(ErrorCode.Err204);

            Link l = this.links[index - 1];

            n1 = this.nodes.IndexOf(l.FirstNode) + 1;
            n2 = this.nodes.IndexOf(l.SecondNode) + 1;
        }
    }

}