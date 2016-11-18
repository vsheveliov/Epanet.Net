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

        private readonly List<Control> controls;
        private readonly CurveCollection curves;
        
        [System.NonSerialized]
        private readonly FieldsMap fields;
        
        private readonly List<Label> labels;
        private readonly LinkCollection links;
        private readonly NodeCollection nodes;
        private readonly PatternCollection patterns;
        private readonly PropertiesMap properties;
        private readonly RuleCollection rules;
        private readonly List<string> titleText;

        public Network() {
            this.titleText = new List<string>();
            this.patterns = new PatternCollection();
            this.nodes = new NodeCollection();
            this.links = new LinkCollection();

            this.curves = new CurveCollection();
            this.controls = new List<Control>();
            this.labels = new List<Label>();
            this.rules = new RuleCollection();

            this.fields = new FieldsMap();

            this.properties = new PropertiesMap();

            this.patterns.Add(new Pattern(string.Empty));
        }
        
        public IList<Control> Controls { get { return this.controls; } }

        public Curve GetCurve(string name) {
            Curve curve;
            return this.curves.TryGetValue(name, out curve) ? curve : null;
        }

        public IList<Curve> Curves { get { return this.curves; } }

        ///<summary>Fields map with report variables properties and conversion units.</summary>
        public FieldsMap FieldsMap { get { return this.fields; } }

        ///<summary>Transient colleciton of junctions.</summary>
        public IEnumerable<Node> Junctions {
            get {
                return this.nodes.Where(x => !(x is Tank));

            }
        }

        public IList<Label> Labels { get { return this.labels; } }

        public Link GetLink(string name) {
            Link obj;
            return this.links.TryGetValue(name, out obj) ? obj : null;
        }

        public IList<Link> Links { get { return this.links; } }

        public Node GetNode(string name) {
            Node obj;
            return this.nodes.TryGetValue(name, out obj) ? obj : null;
        }

        public IList<Node> Nodes { get { return this.nodes; } }

        public Pattern GetPattern(string name) {
            Pattern obj;
            return this.patterns.TryGetValue(name, out obj) ? obj : null;
        }

        public IList<Pattern> Patterns { get { return this.patterns; } }

        ///<summary>Properties Map with simulation configuration properties.</summary>
        public PropertiesMap PropertiesMap { get { return this.properties; } }

        ///<summary>Transient colleciton of pumps.</summary>
        public IEnumerable<Pump> Pumps {
            get {
                return this.links.Where(x => x.Type == Link.LinkType.PUMP).Select(x => (Pump)x);
            } 
        }

        public Rule GetRule(string ruleName) {
            Rule obj;
            return this.rules.TryGetValue(ruleName, out obj) ? obj : null;

        }

        public IList<Rule> Rules { get { return this.rules; } }

        ///<summary>Transient collection of tanks(and reservoirs)</summary>
        public IEnumerable<Tank> Tanks { get { return this.nodes.OfType<Tank>(); } }

        public List<string> TitleText { get { return this.titleText; } }

        ///<summary>Transient colleciton of valves.</summary>
        public IEnumerable<Valve> Valves { get { return this.links.OfType<Valve>(); } }

        public override string ToString() {
            string res = " Network\n";
            res += "  Nodes : " + this.nodes.Count + "\n";
            res += "  Links : " + this.links.Count + "\n";
            res += "  Pattern : " + this.patterns.Count + "\n";
            res += "  Curves : " + this.curves.Count + "\n";
            res += "  Controls : " + this.controls.Count + "\n";
            res += "  Labels : " + this.labels.Count + "\n";
            res += "  Rules : " + this.rules.Count + "\n";
            res += "  Tanks : " + this.Tanks.Count() + "\n";
            res += "  Pumps : " + this.Pumps.Count() + "\n";
            res += "  Valves : " + this.Valves.Count() + "\n";
         
            return res;
        }

    }

}