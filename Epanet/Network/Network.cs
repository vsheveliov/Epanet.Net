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
using org.addition.epanet.network.structures;

namespace org.addition.epanet.network {


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
    ///<summary>Fields map with report variables properties and conversion units.</summary>
    [NonSerialized]
    private FieldsMap fields;
    ///<summary>Transient colleciton of junctions.</summary>
    [NonSerialized]
    private List<Node> junctions;
    private readonly List<Label> labels;
    private readonly LinkCollection links;
    private readonly NodeCollection nodes;
    private readonly PatternCollection patterns;

    ///<summary>Properties Map with simulation configuration properties.</summary>
    private readonly PropertiesMap properties;

    ///<summary>Transient colleciton of pumps.</summary>
    [NonSerialized]
    private List<Pump> pumps;

    private RuleCollection rules;
    ///<summary>Transient collection of tanks(and reservoirs)</summary>
    [NonSerialized]
    private List<Tank> tanks;
    private readonly List<string> titleText;
    ///<summary>Transient colleciton of valves.</summary>
    [NonSerialized]
    private List<Valve> valves;


    public Network() {
        titleText = new List<string>();
        patterns = new PatternCollection();
        nodes = new NodeCollection();
        links = new LinkCollection();

        curves = new CurveCollection();
        controls = new List<Control>();
        labels = new List<Label>();
        rules = new RuleCollection();

        tanks = null;
        pumps = null;
        valves = null;

        fields = new FieldsMap();

        properties = new PropertiesMap();

        addPattern("", new Pattern());
    }

    public void addControl(Control ctr) {
        controls.Add(ctr);
    }


    public void addCurve(string id, Curve cur) {
        cur.setId(id);
        curves.Add(cur);
    }

    public void addJunction(string id, Node junc) {
        junc.setId(id);
        nodes.Add(junc);
    }

    public void addPattern(string id, Pattern pat) {
        pat.setId(id);
        patterns.Add(pat);
    }

    public void addPipe(string id, Link linkRef) {
        linkRef.setId(id);
        links.Add(linkRef);
    }

    public void addPump(string id, Pump pump) {
        pump.setId(id);
        if (pumps == null)
            pumps = new List<Pump>();

        links.Add(pump);
        pumps.Add(pump);
        
    }

    public void addRule(Rule r) {
        rules.Add(r);
    }


    public void addTank(string id, Tank tank) {
        tank.setId(id);
        if (tanks == null)
            tanks = new List<Tank>();
      
        nodes.Add(tank);
        tanks.Add(tank);
    }

    public void addValve(string id, Valve valve) {
        valve.setId(id);
        if (valves == null)
            valves = new List<Valve>();

        links.Add(valve);
        valves.Add(valve);

    }

    public Control[] getControls() {
        return controls.ToArray();
    }

    public Curve getCurve(string name) {
        Curve curve;
        return curves.TryGetValue(name, out curve) ? curve : null;
    }

    public Curve[] getCurves() {
        return new List<Curve>(curves).ToArray();
    }

    public FieldsMap getFieldsMap() {
        return fields;
    }

    public Node[] getJunctions() {
        if (junctions == null) {
            List<Node> tempJunc = new List<Node>();
            
            foreach (Node n  in  nodes) {
                if (!(n is Tank))
                    tempJunc.Add(n);
            }

            if (tempJunc.Count == 0)
                return new Node[] {};
            
            junctions = tempJunc;
        }

        return junctions.ToArray();
    }

    public List<Label> getLabels() {
        return labels;
    }

    public Link getLink(string name) {
        Link obj;
        return this.links.TryGetValue(name, out obj) ? obj : null;
    }

    public Link[] getLinks() {
        return new List<Link>(links).ToArray();
    }

    public Node getNode(string name) {
        Node obj;
        return this.nodes.TryGetValue(name, out obj) ? obj : null;
    }

    public Node[] getNodes() {
        return new List<Node>(nodes).ToArray();
    }

    public Pattern getPattern(string name) {
        Pattern obj;
        return this.patterns.TryGetValue(name, out obj) ? obj : null;
    }

    public Pattern[] getPatterns() {
        return new List<Pattern>(patterns).ToArray();
    }

    public PropertiesMap getPropertiesMap() {
        return properties;
    }

    public Pump[] getPumps() {
        if (pumps == null) {
            List<Pump> tempPump = new List<Pump>();
            foreach (Link l  in  links) {
                if (l is Pump)
                    tempPump.Add((Pump) l);
            }

            if (tempPump.Count == 0)
                return new Pump[] {};
            
            pumps = tempPump;
        }

        return pumps.ToArray();
    }

    public Rule getRule(string ruleName) {
        Rule obj;
        return rules.TryGetValue(ruleName, out obj) ? obj : null;

    }

    public Rule[] getRules() {
        return new List<Rule>(rules).ToArray();
    }

    public Tank[] getTanks() {
        if (tanks == null) {
            List<Tank> tempTank = new List<Tank>();
            foreach (Node n  in  nodes) {
                if (n is Tank)
                    tempTank.Add((Tank) n);
            }

            if (tempTank.Count == 0)
                return new Tank[] {};
            
            tanks = tempTank;
        }

        return tanks.ToArray();
    }

    public List<string> getTitleText() { return titleText; }

    public Valve[] getValves() {
        if (valves == null) {
            List<Valve> tempValve = new List<Valve>();
            foreach (Link l  in  links) {
                if (l is Valve)
                    tempValve.Add((Valve) l);
            }

            if (tempValve.Count == 0)
                return new Valve[]{};
            
            valves = tempValve;
        }

        return valves.ToArray();
    }

#region serialization

    private object readResolve() {
        updatedUnitsProperty();
        return this;
    }

    public void updatedUnitsProperty() {
        fields = new FieldsMap();
        fields.prepare(getPropertiesMap().getUnitsflag(),
                getPropertiesMap().getFlowflag(),
                getPropertiesMap().getPressflag(),
                getPropertiesMap().getQualflag(),
                getPropertiesMap().getChemUnits(),
                getPropertiesMap().getSpGrav(),
                getPropertiesMap().getHstep());
    }

#endregion

    public override string ToString() {
        string res = " Network\n";
        res += "  Nodes : " + nodes.Count + "\n";
        res += "  Links : " + links.Count + "\n";
        res += "  Pattern : " + patterns.Count + "\n";
        res += "  Curves : " + curves.Count + "\n";
        res += "  Controls : " + controls.Count + "\n";
        res += "  Labels : " + labels.Count + "\n";
        res += "  Rules : " + rules.Count + "\n";
        if (tanks != null) res += "  Tanks : " + tanks.Count + "\n";
        if (pumps != null) res += "  Pumps : " + pumps.Count + "\n";
        if (valves != null) res += "  Valves : " + valves.Count + "\n";
        return res;
    }

#if COMMENTED
    public Node getNodeByIndex(int idx) { return nodes[idx]; }
    public Link getLinkByIndex(int idx) { return links[idx]; }
#endif
}
}