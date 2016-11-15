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

using System.Collections.Generic;
using System.Linq;
using Epanet.Network.Structures;

namespace Epanet.Network {


    ///<summary>Hydraulic network structure.</summary>
    public class Network {

        ///<summary>Available files types.</summary>
        public enum FileType {
            EXCEL_FILE,
            INP_FILE,
            NULL_FILE,
            XML_FILE,
            XML_GZ_FILE,
        }

        ///<summary>Available section types.</summary>
        public enum SectType {
            TITLE = 0,
            JUNCTIONS = 1,
            RESERVOIRS = 2,
            TANKS = 3,
            PIPES = 4,
            PUMPS = 5,
            VALVES = 6,
            CONTROLS = 7,
            RULES = 8,
            DEMANDS = 9,
            SOURCES = 10,
            EMITTERS = 11,
            PATTERNS = 12,
            CURVES = 13,
            QUALITY = 14,
            STATUS = 15,
            ROUGHNESS = 16,
            ENERGY = 17,
            REACTIONS = 18,
            MIXING = 19,
            REPORT = 20,
            TIMES = 21,
            OPTIONS = 22,
            COORDINATES = 23,
            VERTICES = 24,
            LABELS = 25,
            BACKDROP = 26,
            TAGS = 27,
            END = 28,
        }

        private readonly List<Control> _controls;
        private readonly CurveCollection _curves;
        
        [System.NonSerialized]
        private readonly FieldsMap _fields;
        
        private readonly List<Label> _labels;
        private readonly LinkCollection _links;
        private readonly NodeCollection _nodes;
        private readonly PatternCollection _patterns;
        private readonly PropertiesMap _properties;
        private readonly RuleCollection _rules;
        private readonly List<string> _titleText;

        public Network() {
            this._titleText = new List<string>();
            this._patterns = new PatternCollection();
            this._nodes = new NodeCollection();
            this._links = new LinkCollection();

            this._curves = new CurveCollection();
            this._controls = new List<Control>();
            this._labels = new List<Label>();
            this._rules = new RuleCollection();

            this._fields = new FieldsMap();

            this._properties = new PropertiesMap();

            this._patterns.Add(new Pattern(string.Empty));
        }
        
        public IList<Control> Controls { get { return this._controls; } }

        public Curve GetCurve(string name) {
            Curve curve;
            return this._curves.TryGetValue(name, out curve) ? curve : null;
        }

        public IList<Curve> Curves { get { return this._curves; } }

        ///<summary>Fields map with report variables properties and conversion units.</summary>
        public FieldsMap FieldsMap { get { return this._fields; } }

        ///<summary>Transient colleciton of junctions.</summary>
        public IEnumerable<Node> Junctions {
            get {
                foreach(Node n in this._nodes) {
                    if(!(n is Tank))
                        yield return n;
                }
            }
        }

        public IList<Label> Labels { get { return this._labels; } }

        public Link GetLink(string name) {
            Link obj;
            return this._links.TryGetValue(name, out obj) ? obj : null;
        }

        public IList<Link> Links { get { return this._links; } }

        public Node GetNode(string name) {
            Node obj;
            return this._nodes.TryGetValue(name, out obj) ? obj : null;
        }

        public IList<Node> Nodes { get { return this._nodes; } }

        public Pattern GetPattern(string name) {
            Pattern obj;
            return this._patterns.TryGetValue(name, out obj) ? obj : null;
        }

        public IList<Pattern> Patterns { get { return this._patterns; } }

        ///<summary>Properties Map with simulation configuration properties.</summary>
        public PropertiesMap PropertiesMap { get { return this._properties; } }

        ///<summary>Transient colleciton of pumps.</summary>
        public IEnumerable<Pump> Pumps {
            get {
                foreach (Link l in this._links) {
                    var pump = l as Pump;
                    if (pump != null)
                        yield return pump;
                }
            }
        }

        public Rule GetRule(string ruleName) {
            Rule obj;
            return this._rules.TryGetValue(ruleName, out obj) ? obj : null;

        }

        public IList<Rule> Rules { get { return this._rules; } }

        ///<summary>Transient collection of tanks(and reservoirs)</summary>
        public IEnumerable<Tank> Tanks {
            get {
                foreach (Node n in this._nodes) {
                    var tank = n as Tank;
                    if (tank != null)
                        yield return tank;
                }
            }
        }

        public List<string> TitleText { get { return this._titleText; } }

        ///<summary>Transient colleciton of valves.</summary>
        public IEnumerable<Valve> Valves {
            get {
                foreach (Link l in this._links) {
                    var valve = l as Valve;
                    if (valve != null)
                        yield return valve;
                }
            }
        }

        public override string ToString() {
            string res = " Network\n";
            res += "  Nodes : " + this._nodes.Count + "\n";
            res += "  Links : " + this._links.Count + "\n";
            res += "  Pattern : " + this._patterns.Count + "\n";
            res += "  Curves : " + this._curves.Count + "\n";
            res += "  Controls : " + this._controls.Count + "\n";
            res += "  Labels : " + this._labels.Count + "\n";
            res += "  Rules : " + this._rules.Count + "\n";
            res += "  Tanks : " + this.Tanks.Count() + "\n";
            res += "  Pumps : " + this.Pumps.Count() + "\n";
            res += "  Valves : " + this.Valves.Count() + "\n";
         
            return res;
        }

    }

}