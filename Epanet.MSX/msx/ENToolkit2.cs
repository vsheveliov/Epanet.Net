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

using Epanet.Enums;
using Epanet.Hydraulic.IO;
using Epanet.Network;
using Epanet.Network.Structures;

using EpanetNetwork = Epanet.Network.Network;

namespace Epanet.MSX {

    ///<summary>Bridge between the hydraulic network properties and the multi-species simulation MSX class.</summary>
    public class EnToolkit2 {

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


        private readonly IList<Link> _links;
        private readonly IList<Node> _nodes;
        private readonly EpanetNetwork _net;

        private HydraulicReader _dseek;

        public AwareStep GetStep(int htime) {
            try {
                return _dseek.GetStep(htime);
            }
            catch (IOException e) {
                Debug.Print(e.ToString());
            }
            return null;
        }

        public EnToolkit2(EpanetNetwork net) {
            _net = net;
            _links = net.Links;
            _nodes = net.Nodes;
        }

        public void Open(string hydFile) { _dseek = new HydraulicReader(new BinaryReader(File.OpenRead(hydFile))); }

        public void Close() { _dseek.Close(); }

        public string ENgetlinkid(int j) { return _links[j - 1].Name; }

        public string ENgetnodeid(int j) { return _nodes[j - 1].Name; }

        public int ENgetnodeindex(string s, out int tmp) {
            Node n = _net.GetNode(s);
            tmp = _nodes.IndexOf(n) + 1;

            return tmp == 0 ? 203 : 0;
        }

        public int ENgetlinkindex(string s, out int tmp) {
            Link l = _net.GetLink(s);
            tmp = _links.IndexOf(l) + 1;
            return tmp == 0 ? 204 : 0;
        }

        public FlowUnitsType ENgetflowunits() {
            return (FlowUnitsType)_net.FlowFlag;
        }

        public int ENgetnodetype(int i) {
            var n = _nodes[i - 1];

            switch (n.Type) {
            case NodeType.TANK:
                return EN_TANK;
            case NodeType.RESERV:
                return EN_RESERVOIR;
            default:
                return EN_JUNCTION;
            }

        }

        public float ENgetlinkvalue(int index, int code) {
            FieldsMap fMap = _net.FieldsMap;

            double v;

            if (index <= 0 || index > _links.Count)
                throw new ENException(ErrorCode.Err204);

            var link = _links[index - 1];

            switch (code) {
            case EN_DIAMETER:
                v = link is Pump
                    ? 0.0 
                    : fMap.RevertUnit(FieldType.DIAM, link.Diameter);
                break;

            case EN_LENGTH:
                v = fMap.RevertUnit(FieldType.ELEV, link.Lenght);
                break;

            case EN_ROUGHNESS:
                if (link.Type <= LinkType.PIPE) {
                    v = _net.FormFlag == FormType.DW
                        ? fMap.RevertUnit(FieldType.ELEV, link.Kc * 1000.00)
                        : link.Kc;
                }
                else
                    v = 0.0;
                break;
            default:
                throw new ENException(ErrorCode.Err251);
            }
            return (float)v;
        }

        public int ENgetcount(int code) {
            switch (code) {
            case EN_NODECOUNT:
                return _nodes.Count;
            case EN_TANKCOUNT:
                return _net.Tanks.Count() + _net.Reservoirs.Count();
            case EN_LINKCOUNT:
                return _links.Count;
            case EN_PATCOUNT:
                return _net.Patterns.Count;
            case EN_CURVECOUNT:
                return _net.Curves.Count;
            case EN_CONTROLCOUNT:
                return _net.Controls.Count;
            default:
                return 0;
            }
        }

        public long ENgettimeparam(int code) {
            long value = 0;
            if (code < EN_DURATION || code > EN_STATISTIC) //EN_PERIODS)
                return 251;
            try {
                switch (code) {
                case EN_DURATION:
                    value = _net.Duration;
                    break;
                case EN_HYDSTEP:
                    value = _net.HStep;
                    break;
                case EN_QUALSTEP:
                    value = _net.QStep;
                    break;
                case EN_PATTERNSTEP:
                    value = _net.PStep;
                    break;
                case EN_PATTERNSTART:
                    value = _net.PStart;
                    break;
                case EN_REPORTSTEP:
                    value = _net.RStep;
                    break;
                case EN_REPORTSTART:
                    value = _net.RStart;
                    break;
                case EN_STATISTIC:
                    value = (long)_net.TstatFlag;
                    break;
                case EN_PERIODS:
                    throw new NotSupportedException();
                //value = dseek.getAvailableSteps().size();                 break;
                }
            }
            catch (ENException) {}
            return value;
        }

        public float ENgetnodevalue(int index, int code) {
            double v;

            FieldsMap fMap = _net.FieldsMap;

            if (index <= 0 || index > _nodes.Count)
                return 203;

            Tank tank;
            switch (code) {
            case EN_INITVOLUME:
                v = 0.0;
                tank = _nodes[index - 1] as Tank;
                if (tank != null)
                    v = fMap.RevertUnit(FieldType.VOLUME, tank.V0);
                break;

            case EN_MIXMODEL:
                v = (double)Enums.MixType.MIX1;
                tank = _nodes[index - 1] as Tank;
                if (tank != null)
                    v = (double)tank.MixModel;
                break;


            case EN_MIXZONEVOL:
                v = 0.0;
                tank = _nodes[index - 1] as Tank;
                if (tank != null)
                    v = fMap.RevertUnit(FieldType.VOLUME, tank.V1Max);
                break;

            default:
                throw new ENException(ErrorCode.Err251);
            }
            return (float)v;
        }

        public void ENgetlinknodes(int index, out int n1, out int n2) {
            if (index < 1 || index > _links.Count)
                throw new ENException(ErrorCode.Err204);

            Link l = _links[index - 1];

            n1 = _nodes.IndexOf(l.FirstNode) + 1;
            n2 = _nodes.IndexOf(l.SecondNode) + 1;
        }
    }

}