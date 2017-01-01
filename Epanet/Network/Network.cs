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
using System.Linq;

using Epanet.Enums;
using Epanet.Network.Structures;

namespace Epanet.Network {


    ///<summary>Hydraulic network structure.</summary>
    public class Network {
        private readonly List<Control> controls = new List<Control>();
        private readonly StringKeyedCollection<Curve> curves = new StringKeyedCollection<Curve>();
        
        [NonSerialized]
        private readonly FieldsMap fields = new FieldsMap();
        private readonly PropertiesMap properties = new PropertiesMap();
        
        private readonly List<Label> labels = new List<Label>();
        private readonly StringKeyedCollection<Link> links = new StringKeyedCollection<Link>();
        private readonly StringKeyedCollection<Node> nodes = new StringKeyedCollection<Node>();
        
        private readonly StringKeyedCollection<Pattern> patterns = new StringKeyedCollection<Pattern>() {
            new Pattern(string.Empty)
        };

        private readonly StringKeyedCollection<Rule> rules = new StringKeyedCollection<Rule>();
        private readonly List<string> titleText = new List<string>();

        public Network() {}
        
        public IList<Control> Controls { get { return this.controls; } }

        public Curve GetCurve(string name) { return this.curves.GetValueOrDefault(name); }

        public IList<Curve> Curves { get { return this.curves; } }

        ///<summary>Fields map with report variables properties and conversion units.</summary>
        public FieldsMap FieldsMap { get { return this.fields; } }

        ///<summary>Transient colleciton of junctions.</summary>
        public IEnumerable<Node> Junctions {
            get { return this.nodes.Where(x => !(x is Tank)); }
        }

        public IList<Label> Labels { get { return this.labels; } }

        public Link GetLink(string name) { return this.links.GetValueOrDefault(name); }

        public IList<Link> Links { get { return this.links; } }

        public Node GetNode(string name) { return this.nodes.GetValueOrDefault(name); }

        public IList<Node> Nodes { get { return this.nodes; } }

        public Pattern GetPattern(string name) { return this.patterns.GetValueOrDefault(name); }

        public IList<Pattern> Patterns { get { return this.patterns; } }

        ///<summary>Properties Map with simulation configuration properties.</summary>
        public PropertiesMap PropertiesMap { get { return this.properties; } }

        ///<summary>Transient colleciton of pumps.</summary>
        public IEnumerable<Pump> Pumps {
            get {
                return this.links.Where(x => x.Type == LinkType.PUMP).Select(x => (Pump)x);
            } 
        }

        public Rule GetRule(string ruleName) { return this.rules.GetValueOrDefault(ruleName); }

        public IList<Rule> Rules { get { return this.rules; } }

        ///<summary>Transient collection of tanks(and reservoirs)</summary>
        public IEnumerable<Tank> Tanks { get { return this.nodes.OfType<Tank>(); } }

        public List<string> TitleText { get { return this.titleText; } }

        ///<summary>Transient colleciton of valves.</summary>
        public IEnumerable<Valve> Valves { get { return this.links.OfType<Valve>(); } }

        public override string ToString() {
            
            return new System.Text.StringBuilder(0x200)
                .AppendLine(" Network")
                .Append("  Nodes    : ").Append(this.nodes.Count).AppendLine()
                .Append("  Links    : ").Append(this.links.Count).AppendLine()
                .Append("  Pattern  : ").Append(this.patterns.Count).AppendLine()
                .Append("  Curves   : ").Append(this.curves.Count).AppendLine()
                .Append("  Controls : ").Append(this.controls.Count).AppendLine()
                .Append("  Labels   : ").Append(this.labels.Count).AppendLine()
                .Append("  Rules    : ").Append(this.rules.Count).AppendLine()
                .Append("  Tanks    : ").Append(this.Tanks.Count()).AppendLine()
                .Append("  Pumps    : ").Append(this.Pumps.Count()).AppendLine()
                .Append("  Valves   : ").Append(this.Valves.Count()).AppendLine()
                .ToString();
            

            /*
            return " Network" + Environment.NewLine +
                   "  Nodes    : " + this.nodes.Count + Environment.NewLine +
                   "  Links    : " + this.links.Count + Environment.NewLine +
                   "  Pattern  : " + this.patterns.Count + Environment.NewLine +
                   "  Curves   : " + this.curves.Count + Environment.NewLine +
                   "  Controls : " + this.controls.Count + Environment.NewLine +
                   "  Labels   : " + this.labels.Count + Environment.NewLine +
                   "  Rules    : " + this.rules.Count + Environment.NewLine +
                   "  Tanks    : " + this.Tanks.Count() + Environment.NewLine +
                   "  Pumps    : " + this.Pumps.Count() + Environment.NewLine +
                   "  Valves   : " + this.Valves.Count() + Environment.NewLine;
            */

            /*
            return
                string.Format(
                    " Network{0}" +
                    "  Nodes    : {1}{0}" +
                    "  Links    : {2}{0}" +
                    "  Pattern  : {3}{0}" +
                    "  Curves   : {4}{0}" +
                    "  Controls : {5}{0}" +
                    "  Labels   : {6}{0}" +
                    "  Rules    : {7}{0}" +
                    "  Tanks    : {8}{0}" +
                    "  Pumps    : {9}{0}" +
                    "  Valves   : {10}{0}",

                    Environment.NewLine,
                    this.nodes.Count,
                    this.links.Count,
                    this.patterns.Count,
                    this.curves.Count,
                    this.controls.Count,
                    this.labels.Count,
                    this.rules.Count,
                    this.Tanks.Count(),
                    this.Pumps.Count(),
                    this.Valves.Count());
            */

        }

    }

}