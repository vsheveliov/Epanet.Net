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
using System.Linq;
using System.Text;
using Epanet.Network.Structures;
using Epanet.Util;

namespace Epanet.Network.IO.Output {

///<summary>INP file composer.</summary>
public class InpComposer : OutputComposer{

    private const string JUNCS_SUBTITLE      = ";ID\tElev\tDemand\tPattern";
    private const string RESERVOIRS_SUBTITLE = ";ID\tHead\tPattern";
    private const string TANK_SUBTITLE       = ";ID\tElevation\tInitLevel\tMinLevel\tMaxLevel\tDiameter\tMinVol\tVolCurve";
    private const string PUMPS_SUBTITLE      = ";ID\tNode1\tNode2\tParameters";
    private const string VALVES_SUBTITLE     = ";ID\tNode1\tNode2\tDiameter\tType\tSetting\tMinorLoss";
    private const string DEMANDS_SUBTITLE    = ";Junction\tDemand\tPattern\tCategory";
    private const string STATUS_SUBTITLE     = ";ID\tStatus/Setting";
    private const string PIPES_SUBTITLE      = ";ID\tNode1\tNode2\tLength\tDiameter\tRoughness\tMinorLoss\tStatus";
    private const string PATTERNS_SUBTITLE   = ";ID\tMultipliers";
    private const string EMITTERS_SUBTITLE   = ";Junction\tCoefficient";
    private const string CURVE_SUBTITLE      = ";ID\tX-Value\tY-Value";
    private const string QUALITY_SUBTITLE    = ";Node\tInitQual";
    private const string SOURCE_SUBTITLE     = ";Node\tType\tQuality\tPattern";
    private const string MIXING_SUBTITLE     = ";Tank\tModel";
    private const string REACTIONS_SUBTITLE  = ";Type\tPipe/Tank";
    private const string COORDINATES_SUBTITLE = ";Node\tX-Coord\tY-Coord";

    TextWriter buffer;

    public override void Composer(Network net, string fileName) {
        try {
            this.buffer = new StreamWriter(File.OpenWrite(fileName), Encoding.Default); // "ISO-8859-1"
            this.ComposeHeader(net);
            this.ComposeJunctions(net);
            this.ComposeReservoirs(net);
            this.ComposeTanks(net);
            this.ComposePipes(net);
            this.ComposePumps(net);
            this.ComposeValves(net);
            this.ComposeDemands(net);
            this.ComposeEmitters(net);
            this.ComposeStatus(net);
            this.ComposePatterns(net);
            this.ComposeCurves(net);
            this.ComposeControls(net);
            this.ComposeQuality(net);
            this.ComposeSource(net);
            this.ComposeMixing(net);
            this.ComposeReaction(net);
            this.ComposeEnergy(net);
            this.ComposeTimes(net);
            this.ComposeOptions(net);
            this.ComposeExtraOptions(net);
            this.ComposeReport(net);
            this.ComposeLabels(net);
            this.ComposeCoordinates(net);
            this.ComposeVertices(net);
            this.ComposeRules(net);

            this.buffer.WriteLine(Network.SectType.END.parseStr());
            this.buffer.Close();
        }
        catch (IOException){}
    }

    private void ComposeHeader(Network net) {
        if(net.TitleText.Count == 0)
            return;

        this.buffer.WriteLine(Network.SectType.TITLE.parseStr());
        
        foreach (string str  in  net.TitleText){
            this.buffer.WriteLine(str);
        }

        this.buffer.WriteLine();
    }


    private void ComposeJunctions(Network net) {
        FieldsMap fMap = net.FieldsMap;
        PropertiesMap pMap = net.PropertiesMap;
        
        if(!net.Junctions.Any())
            return;

        this.buffer.WriteLine(Network.SectType.JUNCTIONS.parseStr());
        this.buffer.WriteLine(JUNCS_SUBTITLE);

        foreach(Node node in net.Junctions) {
            this.buffer.Write(" {0}\t{1}",node.Id,fMap.RevertUnit(FieldsMap.FieldType.ELEV, node.Elevation));

            //if(node.getDemand()!=null && node.getDemand().size()>0 && !node.getDemand()[0].getPattern().getId().equals(""))
            //    buffer.write("\t"+node.getDemand()[0].getPattern().getId());

            if(node.Demand.Count>0){
                Demand d = node.Demand[0];
                this.buffer.Write("\t{0}",fMap.RevertUnit(FieldsMap.FieldType.DEMAND, d.Base));

                if (!string.IsNullOrEmpty(d.Pattern.Id) && !pMap.DefPatId.Equals(d.Pattern.Id, StringComparison.OrdinalIgnoreCase))
                    this.buffer.Write("\t" + d.Pattern.Id);
            }

            if(!string.IsNullOrEmpty(node.Comment))
                this.buffer.Write("\t;"+node.Comment);

            this.buffer.WriteLine();
        }


        this.buffer.WriteLine();
    }

    private void ComposeReservoirs(Network net) {
        FieldsMap fMap = net.FieldsMap;
        
        if(!net.Tanks.Any())
            return;

        List<Tank> reservoirs = new List<Tank>();
        foreach(Tank tank in net.Tanks)
            if (tank.IsReservoir)
                reservoirs.Add(tank);

        if(reservoirs.Count==0)
            return;

        this.buffer.WriteLine(Network.SectType.RESERVOIRS.parseStr());
        this.buffer.WriteLine(RESERVOIRS_SUBTITLE);

        foreach (Tank r  in  reservoirs){
            this.buffer.Write(" {0}\t{1}",r.Id,fMap.RevertUnit(FieldsMap.FieldType.ELEV, r.Elevation));


            if (r.Pattern!=null)
                this.buffer.Write("\t{0}",r.Pattern.Id);


            if(!string.IsNullOrEmpty(r.Comment))
                this.buffer.Write("\t;"+r.Comment);

            this.buffer.WriteLine();
        }

        this.buffer.WriteLine();
    }

    private void ComposeTanks(Network net) {
        FieldsMap fMap = net.FieldsMap;

        List<Tank> tanks = new List<Tank>();
        foreach (Tank tank  in  net.Tanks)
            if(!tank.IsReservoir)
                tanks.Add(tank);

        var tanks2 = net.Nodes.OfType<Tank>().Where(x => x.IsReservoir).ToList();

        if(tanks.Count ==0)
            return;

        this.buffer.WriteLine(Network.SectType.TANKS.parseStr());
        this.buffer.WriteLine(TANK_SUBTITLE);

        foreach (Tank tank  in  tanks){
            double vmin = tank.Vmin;
            if(Math.Round(vmin/tank.Area) == Math.Round(tank.Hmin-tank.Elevation))
                vmin = 0;

            this.buffer.Write(" {0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
                    tank.Id,
                    fMap.RevertUnit(FieldsMap.FieldType.ELEV, tank.Elevation),
                    fMap.RevertUnit(FieldsMap.FieldType.ELEV, tank.H0 - tank.Elevation),
                    fMap.RevertUnit(FieldsMap.FieldType.ELEV, tank.Hmin - tank.Elevation),
                    fMap.RevertUnit(FieldsMap.FieldType.ELEV, tank.Hmax - tank.Elevation),
                    fMap.RevertUnit(FieldsMap.FieldType.ELEV, 2*Math.Sqrt(tank.Area / Math.PI)),
                    fMap.RevertUnit(FieldsMap.FieldType.VOLUME, vmin));

            if(tank.Vcurve!=null)
                this.buffer.Write(" "+tank.Vcurve.Id);

            if(!string.IsNullOrEmpty(tank.Comment))
                this.buffer.Write("\t;"+tank.Comment);
            this.buffer.WriteLine();
        }

        this.buffer.WriteLine();
    }

    private void ComposePipes(Network net) {
        FieldsMap fMap = net.FieldsMap;
        PropertiesMap pMap = net.PropertiesMap;

        if(net.Links.Count ==0)
            return;

        List<Link> pipes = new List<Link>();
        foreach (Link link  in  net.Links)
            if(link.Type == Link.LinkType.PIPE || link.Type == Link.LinkType.CV)
                pipes.Add(link);


        this.buffer.WriteLine(Network.SectType.PIPES.parseStr());
        this.buffer.WriteLine(PIPES_SUBTITLE);

        foreach (Link link  in  pipes){
            double d = link.Diameter;
            double kc = link.Roughness;
            if (pMap.FormFlag == PropertiesMap.FormType.DW)
                kc = fMap.RevertUnit(FieldsMap.FieldType.ELEV, kc*1000.0);

            double km = link.Km*Math.Pow(d,4.0)/0.02517;

            this.buffer.Write(" {0}\t{1}\t{2}\t{3}\t{4}",
                    link.Id,
                    link.FirstNode.Id,
                    link.SecondNode.Id,
                    fMap.RevertUnit(FieldsMap.FieldType.LENGTH, link.Lenght),
                    fMap.RevertUnit(FieldsMap.FieldType.DIAM, d));

            //if (pMap.getFormflag() == FormType.DW)
            this.buffer.Write(" {0}\t{1}", kc, km);

            if (link.Type == Link.LinkType.CV)
                this.buffer.Write(" CV");
            else if (link.Status == Link.StatType.CLOSED)
                this.buffer.Write(" CLOSED");
            else if (link.Status == Link.StatType.OPEN)
                this.buffer.Write(" OPEN");

            if(!string.IsNullOrEmpty(link.Comment))
                this.buffer.Write("\t;" + link.Comment);

            this.buffer.WriteLine();
        }

        this.buffer.WriteLine();
    }

    private void ComposePumps(Network net) {
        FieldsMap fMap = net.FieldsMap;

        if(!net.Pumps.Any())
            return;

        this.buffer.WriteLine(Network.SectType.PUMPS.parseStr());
        this.buffer.WriteLine(PUMPS_SUBTITLE);

        foreach(Pump pump in net.Pumps)
        {
            this.buffer.Write(" {0}\t{1}\t{2}", pump.Id,
                    pump.FirstNode.Id, pump.SecondNode.Id);


            // Pump has constant power
            if (pump.Ptype == Pump.PumpType.CONST_HP)
                this.buffer.Write(" POWER " + pump.Km);
                // Pump has a head curve
            else if (pump.Hcurve!=null)
                this.buffer.Write(" HEAD " + pump.Hcurve.Id);
                // Old format used for pump curve
            else
            {
                this.buffer.Write(" {0}\t{1}\t{2}\t0.0\t{3}",
                        fMap.RevertUnit(FieldsMap.FieldType.HEAD, -pump.H0),
                        fMap.RevertUnit(FieldsMap.FieldType.HEAD, -pump.H0 - pump.FlowCoefficient*Math.Pow(pump.Q0,pump.N)),
                        fMap.RevertUnit(FieldsMap.FieldType.FLOW, pump.Q0),
                        fMap.RevertUnit(FieldsMap.FieldType.FLOW, pump.Qmax));

                continue;
            }

            if ( pump.Upat!=null)
                this.buffer.Write(" PATTERN " + pump.Upat.Id);


            if (pump.Roughness != 1.0)
                this.buffer.Write(" SPEED {0}", pump.Roughness);

            if(!string.IsNullOrEmpty(pump.Comment))
                this.buffer.Write("\t;"+pump.Comment);
            
            this.buffer.WriteLine();
        }

        this.buffer.WriteLine();
    }

    private void ComposeValves(Network net) {
        FieldsMap fMap = net.FieldsMap;
        
        if (!net.Valves.Any())
            return;

        this.buffer.Write(Network.SectType.VALVES.parseStr());
        this.buffer.WriteLine();
        this.buffer.Write(VALVES_SUBTITLE);
        this.buffer.WriteLine();

        foreach (Valve valve in net.Valves) {
            double d = valve.Diameter;
            double kc = valve.Roughness;
            if (kc == Constants.MISSING)
                kc = 0.0;

            switch (valve.Type) {
            case Link.LinkType.FCV:
                kc = fMap.RevertUnit(FieldsMap.FieldType.FLOW, kc);
                break;
            case Link.LinkType.PRV:
            case Link.LinkType.PSV:
            case Link.LinkType.PBV:
                kc = fMap.RevertUnit(FieldsMap.FieldType.PRESSURE, kc);
                break;
            }

            double km = valve.Km * Math.Pow(d, 4) / 0.02517;

            this.buffer.Write(
                " {0}\t{1}\t{2}\t{3}\t{4}",
                valve.Id,
                valve.FirstNode.Id,
                valve.SecondNode.Id,
                fMap.RevertUnit(FieldsMap.FieldType.DIAM, d),
                valve.Type.ParseStr());

            if (valve.Type == Link.LinkType.GPV && valve.Curve != null)
                this.buffer.Write(" {0}\t{1}", valve.Curve.Id, km);
            else
                this.buffer.Write(" {0}\t{1}", kc, km);

            if (!string.IsNullOrEmpty(valve.Comment))
                this.buffer.Write("\t;" + valve.Comment);

            this.buffer.WriteLine();
        }
        this.buffer.WriteLine();
    }

    private void ComposeDemands(Network net) {
        FieldsMap fMap = net.FieldsMap;
        
        if(!net.Junctions.Any())
            return;

        this.buffer.WriteLine(Network.SectType.DEMANDS.parseStr());
        this.buffer.WriteLine(DEMANDS_SUBTITLE);

        double ucf = fMap.GetUnits(FieldsMap.FieldType.DEMAND);

        foreach(Node node in net.Junctions) {
            if (node.Demand.Count > 1)
                for(int i = 0;i<node.Demand.Count;i++){
                    Demand demand = node.Demand[i];
                    this.buffer.Write("{0}\t{1}",node.Id, ucf * demand.Base);

                    if (demand.Pattern != null)
                        this.buffer.Write("\t"+demand.Pattern.Id);

                    this.buffer.WriteLine();
                }
        }

        this.buffer.WriteLine();
    }

    private void ComposeEmitters(Network net) {
       
        if(net.Nodes.Count == 0)
            return;

        this.buffer.WriteLine(Network.SectType.EMITTERS.parseStr());
        this.buffer.WriteLine(EMITTERS_SUBTITLE);
        
        double uflow = net.FieldsMap.GetUnits(FieldsMap.FieldType.FLOW);
        double upressure = net.FieldsMap.GetUnits(FieldsMap.FieldType.PRESSURE);
        double qexp = net.PropertiesMap.QExp;

        foreach (Node node  in  net.Junctions){
            if(node.Ke==0.0) continue;
            double ke = uflow/Math.Pow(upressure * node.Ke, (1.0 / qexp));
            this.buffer.WriteLine(" {0}\t{1}",node.Id,ke);
        }

        this.buffer.WriteLine();
    }

    private void ComposeStatus(Network net) {

        if(net.Links.Count==0)
            return;

        this.buffer.WriteLine(Network.SectType.STATUS.parseStr());
        this.buffer.WriteLine(STATUS_SUBTITLE);

        foreach (Link link  in  net.Links)
        {
            if (link.Type <= Link.LinkType.PUMP)
            {
                if (link.Status == Link.StatType.CLOSED)
                    this.buffer.WriteLine(" {0}\t{1}",link.Id,Link.StatType.CLOSED.ParseStr());

                    // Write pump speed here for pumps with old-style pump curve input
                else if (link.Type == Link.LinkType.PUMP){
                    Pump pump = (Pump)link;
                    if (pump.Hcurve == null &&
                            pump.Ptype != Pump.PumpType.CONST_HP &&
                            pump.Roughness != 1.0)
                        this.buffer.WriteLine(" {0}\t{1}", link.Id, link.Roughness);
                }
            }
            // Write fixed-status PRVs & PSVs (setting = MISSING)
            else if (link.Roughness == Constants.MISSING)
            {
                if (link.Status == Link.StatType.OPEN)
                    this.buffer.WriteLine(" {0}\t{1}",link.Id,Link.StatType.OPEN.ParseStr());

                if (link.Status == Link.StatType.CLOSED)
                    this.buffer.WriteLine(" {0}\t{1}", link.Id, Link.StatType.CLOSED.ParseStr());

            }

        }

        this.buffer.WriteLine();
    }

    private void ComposePatterns(Network net) {

        var pats = net.Patterns;

        if(pats.Count<=1)
            return;

        this.buffer.WriteLine(Network.SectType.PATTERNS.parseStr());
        this.buffer.WriteLine(PATTERNS_SUBTITLE);

        for (int i = 1; i < pats.Count; i++) {
            Pattern pat = pats[i];
            List<double> f = pat.FactorsList;
            for (int j = 0; j < pats[i].Length; j++) {
                if (j % 6 == 0)
                    this.buffer.Write(" {0}", pat.Id);

                this.buffer.Write(" {0}", f[j]);

                if (j % 6 == 5)
                    this.buffer.WriteLine();
            }
            this.buffer.WriteLine();
        }

        this.buffer.WriteLine();
    }

    private void ComposeCurves(Network net) {

        var curves = net.Curves;

        if (curves.Count == 0)
            return;

        this.buffer.WriteLine(Network.SectType.CURVES.parseStr());
        this.buffer.WriteLine(CURVE_SUBTITLE);

        foreach (Curve c  in  curves) {
            for (int i = 0; i < c.Npts; i++) {
                this.buffer.WriteLine(" {0}\t{1}\t{2}", c.Id, c.X[i], c.Y[i]);
            }
        }

        this.buffer.WriteLine();
    }

    private void ComposeControls(Network net) {
        var controls = net.Controls;
        FieldsMap fmap = net.FieldsMap;

        if(controls.Count == 0)
            return;

        this.buffer.WriteLine(Network.SectType.CONTROLS.parseStr());

        foreach (Control control  in  controls)
        {
            // Check that controlled link exists
            if (control.Link==null) continue;

            // Get text of control's link status/setting
            if (control.Setting == Constants.MISSING)
                this.buffer.Write(" LINK {0} {1} ", control.Link.Id, control.Status.ParseStr());
            else {
                double kc = control.Setting;
                switch (control.Link.Type) {
                case Link.LinkType.PRV:
                case Link.LinkType.PSV:
                case Link.LinkType.PBV:
                    kc = fmap.RevertUnit(FieldsMap.FieldType.PRESSURE, kc);
                    break;
                case Link.LinkType.FCV:
                    kc = fmap.RevertUnit(FieldsMap.FieldType.FLOW, kc);
                    break;
                }
                this.buffer.Write(" LINK {0} {1} ", control.Link.Id, kc);
            }


            switch (control.Type) {
            // Print level control
            case Control.ControlType.LOWLEVEL:
            case Control.ControlType.HILEVEL:
                double kc = control.Grade - control.Node.Elevation;
                if (control.Node is Tank)
                    kc = fmap.RevertUnit(FieldsMap.FieldType.HEAD, kc);
                else
                    kc = fmap.RevertUnit(FieldsMap.FieldType.PRESSURE, kc);

                this.buffer.Write(
                    " IF NODE {0} {1} {2}",
                    control.Node.Id,
                    control.Type.ParseStr(),
                    kc);

                break;

            // Print timer control
            case Control.ControlType.TIMER:
                this.buffer.Write(
                    " AT {0} {1} HOURS",
                    Control.ControlType.TIMER.ParseStr(),
                    control.Time / 3600.0f);

                break;

            // Print time-of-day control
            case Control.ControlType.TIMEOFDAY:
                this.buffer.Write(
                    " AT {0} {1}",
                    Control.ControlType.TIMEOFDAY.ParseStr(),
                    control.Time.GetClockTime());

                break;
            }
            this.buffer.WriteLine();
        }
        this.buffer.WriteLine();
    }

    private void ComposeQuality(Network net) {
        FieldsMap fmap = net.FieldsMap;

        if(net.Nodes.Count == 0)
            return;

        this.buffer.WriteLine(Network.SectType.QUALITY.parseStr());
        this.buffer.WriteLine(QUALITY_SUBTITLE);
        
        foreach (Node node  in  net.Nodes)
        {
            if (node.C0.Length == 1 ){
                if(node.C0[0] == 0.0 ) continue;
                this.buffer.Write(" {0}\t{1}", node.Id, fmap.RevertUnit(FieldsMap.FieldType.QUALITY, node.C0[0]));
            }

            this.buffer.WriteLine();
        }
        this.buffer.WriteLine();
    }

    private void ComposeSource(Network net) {
        
        if(net.Nodes.Count == 0)
            return;

        this.buffer.WriteLine(Network.SectType.SOURCES.parseStr());
        this.buffer.WriteLine(SOURCE_SUBTITLE);


        foreach(Node node in net.Nodes) {
            Source source = node.Source;
            if (source == null)
                continue;
            
            this.buffer.Write(" {0}\t{1}\t{2}",
                    node.Id,
                    source.Type.ParseStr(),
                    source.C0);

            if (source.Pattern!=null)
                this.buffer.Write(" " + source.Pattern.Id);

            this.buffer.WriteLine();
        }
        this.buffer.WriteLine();
    }


    private void ComposeMixing(Network net) {

        if(!net.Tanks.Any())
            return;

        this.buffer.WriteLine(Network.SectType.MIXING.parseStr());
        this.buffer.WriteLine(MIXING_SUBTITLE);

        foreach(Tank tank in net.Tanks) {
            if (tank.IsReservoir) continue;
            this.buffer.WriteLine(
                " {0}\t{1}\t{2}",
                tank.Id,
                tank.MixModel.ParseStr(),
                tank.V1Max / tank.Vmax);
        }

        this.buffer.WriteLine();
    }

    private void ComposeReaction(Network net) {
        PropertiesMap pMap = net.PropertiesMap;

        this.buffer.WriteLine(Network.SectType.REACTIONS.parseStr());
        this.buffer.WriteLine(REACTIONS_SUBTITLE);

        this.buffer.WriteLine("ORDER BULK {0}", pMap.BulkOrder);
        this.buffer.WriteLine("ORDER WALL {0}", pMap.WallOrder);
        this.buffer.WriteLine("ORDER TANK {0}", pMap.TankOrder);
        this.buffer.WriteLine("GLOBAL BULK {0}", pMap.KBulk * Constants.SECperDAY);
        this.buffer.WriteLine("GLOBAL WALL {0}", pMap.KWall * Constants.SECperDAY);

        //if (pMap.getClimit() > 0.0)
        this.buffer.WriteLine("LIMITING POTENTIAL {0}", pMap.CLimit);

        //if (pMap.getRfactor() != Constants.MISSING && pMap.getRfactor() != 0.0)
        this.buffer.WriteLine("ROUGHNESS CORRELATION {0}", pMap.RFactor);


        foreach (Link link  in  net.Links) {
            if (link.Type > Link.LinkType.PIPE)
                continue;

            if (link.Kb != pMap.KBulk)
                this.buffer.WriteLine("BULK {0} {1}", link.Id, link.Kb * Constants.SECperDAY);
            if (link.Kw != pMap.KWall)
                this.buffer.WriteLine("WALL {0} {1}", link.Id, link.Kw * Constants.SECperDAY);
        }

        foreach (Tank tank  in  net.Tanks) {
            if (tank.IsReservoir) continue;
            if (tank.Kb != pMap.KBulk)
                this.buffer.WriteLine("TANK {0} {1}", tank.Id, tank.Kb * Constants.SECperDAY);
        }

        this.buffer.WriteLine();
    }

    private void ComposeEnergy(Network net) {
        PropertiesMap pMap = net.PropertiesMap;

        this.buffer.WriteLine(Network.SectType.ENERGY.parseStr());

        if (pMap.ECost != 0.0)
            this.buffer.WriteLine("GLOBAL PRICE {0}", pMap.ECost);

        if (!pMap.EPatId.Equals(""))
            this.buffer.WriteLine("GLOBAL PATTERN {0}",  pMap.EPatId);

        this.buffer.WriteLine("GLOBAL EFFIC {0}", pMap.EPump);
        this.buffer.WriteLine("DEMAND CHARGE {0}", pMap.DCost);

        foreach (Pump p  in  net.Pumps)
        {
            if (p.Ecost > 0.0)
                this.buffer.WriteLine("PUMP {0} PRICE {1}",p.Id,p.Ecost);

            if (p.Epat != null)
                this.buffer.WriteLine("PUMP {0} PATTERN {1}", p.Id, p.Epat.Id);

            if (p.Ecurve !=null)
                this.buffer.WriteLine("PUMP {0} EFFIC {1}", p.Id,p.Ecurve.Id);
        }

        this.buffer.WriteLine();

    }

    private void ComposeTimes(Network net) {
        PropertiesMap pMap = net.PropertiesMap;
        this.buffer.WriteLine(Network.SectType.TIMES.parseStr());
        this.buffer.WriteLine("DURATION {0}", pMap.Duration.GetClockTime());
        this.buffer.WriteLine("HYDRAULIC TIMESTEP {0}", pMap.HStep.GetClockTime());
        this.buffer.WriteLine("QUALITY TIMESTEP {0}", pMap.QStep.GetClockTime());
        this.buffer.WriteLine("REPORT TIMESTEP {0}", pMap.RStep.GetClockTime());
        this.buffer.WriteLine("REPORT START {0}", pMap.RStart.GetClockTime());
        this.buffer.WriteLine("PATTERN TIMESTEP {0}", pMap.PStep.GetClockTime());
        this.buffer.WriteLine("PATTERN START {0}", pMap.PStart.GetClockTime());
        this.buffer.WriteLine("RULE TIMESTEP {0}", pMap.RuleStep.GetClockTime());
        this.buffer.WriteLine("START CLOCKTIME {0}", pMap.TStart.GetClockTime());
        this.buffer.WriteLine("STATISTIC {0}", pMap.TStatFlag.ParseStr());
        this.buffer.WriteLine();
    }

    private void ComposeOptions(Network net) {
        PropertiesMap pMap = net.PropertiesMap;
        FieldsMap fMap = net.FieldsMap;
        
        this.buffer.WriteLine(Network.SectType.OPTIONS.parseStr());
        this.buffer.WriteLine("UNITS               " + pMap.FlowFlag);
        this.buffer.WriteLine("PRESSURE            " + pMap.PressFlag);
        this.buffer.WriteLine("HEADLOSS            " + pMap.FormFlag);

        if (!string.IsNullOrEmpty(pMap.DefPatId))
        this.buffer.WriteLine("PATTERN             " + pMap.DefPatId);
        
        if (pMap.HydFlag == PropertiesMap.HydType.USE)
        this.buffer.WriteLine("HYDRAULICS USE      " + pMap.HydFname);
        
        if (pMap.HydFlag == PropertiesMap.HydType.SAVE)
        this.buffer.WriteLine("HYDRAULICS SAVE     " + pMap.HydFname);
        
        if (pMap.ExtraIter == -1)
        this.buffer.WriteLine("UNBALANCED          STOP");

        if (pMap.ExtraIter >= 0)
        this.buffer.WriteLine("UNBALANCED          CONTINUE " + pMap.ExtraIter);

        switch (pMap.QualFlag) {
        case PropertiesMap.QualType.CHEM:
            this.buffer.WriteLine("QUALITY             {0} {1}", pMap.ChemName, pMap.ChemUnits);
            break;
        case PropertiesMap.QualType.TRACE:
            this.buffer.WriteLine("QUALITY             TRACE " + pMap.TraceNode);
            break;
        case PropertiesMap.QualType.AGE:
            this.buffer.WriteLine("QUALITY             AGE");
            break;
        case PropertiesMap.QualType.NONE:
            this.buffer.WriteLine("QUALITY             NONE");
            break;
        }

        this.buffer.WriteLine("DEMAND MULTIPLIER {0}", pMap.DMult);
        this.buffer.WriteLine("EMITTER EXPONENT  {0}", 1.0/pMap.QExp);
        this.buffer.WriteLine("VISCOSITY         {0}", pMap.Viscos/Constants.VISCOS);
        this.buffer.WriteLine("DIFFUSIVITY       {0}", pMap.Diffus/Constants.DIFFUS);
        this.buffer.WriteLine("SPECIFIC GRAVITY  {0}", pMap.SpGrav);
        this.buffer.WriteLine("TRIALS            {0}", pMap.MaxIter);
        this.buffer.WriteLine("ACCURACY          {0}", pMap.HAcc);
        this.buffer.WriteLine("TOLERANCE         {0}", fMap.RevertUnit(FieldsMap.FieldType.QUALITY, pMap.Ctol));
        this.buffer.WriteLine("CHECKFREQ         {0}", pMap.CheckFreq);
        this.buffer.WriteLine("MAXCHECK          {0}", pMap.MaxCheck);
        this.buffer.WriteLine("DAMPLIMIT         {0}", pMap.DampLimit);
        this.buffer.WriteLine();
    }

    private void ComposeExtraOptions(Network net) {
        PropertiesMap pMap = net.PropertiesMap;
        var otherObjsNames = pMap.GetObjectsNames(true);

        if(otherObjsNames.Count==0)
            return;

        foreach (string objName  in  otherObjsNames){
            object objVal = pMap[objName];
            this.buffer.WriteLine(objName + " " + objVal);
        }
        this.buffer.WriteLine();

    }

    private void ComposeReport(Network net) {

        this.buffer.WriteLine(Network.SectType.REPORT.parseStr());
        
        PropertiesMap pMap = net.PropertiesMap;
        FieldsMap fMap = net.FieldsMap;
        this.buffer.WriteLine("PAGESIZE       {0}",pMap.PageSize);
        this.buffer.WriteLine("STATUS         " + pMap.Stat_Flag);
        this.buffer.WriteLine("SUMMARY        " + (pMap.SummaryFlag ? Keywords.w_YES:Keywords.w_NO));
        this.buffer.WriteLine("ENERGY         " + (pMap.EnergyFlag ? Keywords.w_YES:Keywords.w_NO));

        switch (pMap.NodeFlag){
            case PropertiesMap.ReportFlag.FALSE:
                this.buffer.WriteLine("NODES NONE");
                break;
            case PropertiesMap.ReportFlag.TRUE:
                this.buffer.WriteLine("NODES ALL");
                break;
            case PropertiesMap.ReportFlag.SOME:
            {
                int j = 0;
                foreach (Node node  in  net.Nodes){
                    if(node.RptFlag){
                        // if (j % 5 == 0) buffer.WriteLine("NODES "); // BUG: Baseform bug

                        if (j % 5 == 0) {
                            this.buffer.WriteLine();
                            this.buffer.Write("NODES ");
                        }

                        this.buffer.Write("{0} ", node.Id);
                        j++;
                    }
                }
                break;
            }
        }

        switch (pMap.LinkFlag){
            case PropertiesMap.ReportFlag.FALSE:
                this.buffer.WriteLine("LINKS NONE");
                break;
            case PropertiesMap.ReportFlag.TRUE:
                this.buffer.WriteLine("LINKS ALL");
                break;
            case PropertiesMap.ReportFlag.SOME:
            {
                int j = 0;
                foreach (Link link  in  net.Links){
                    if(link.RptFlag){
                        // if (j % 5 == 0) buffer.write("LINKS \n"); // BUG: Baseform bug
                        if (j % 5 == 0) {
                            this.buffer.WriteLine();
                            this.buffer.Write("LINKS ");
                        }

                        this.buffer.Write("{0} ", link.Id);
                        j++;
                    }
                }
                break;
            }
        }

        for(FieldsMap.FieldType i = 0;i< FieldsMap.FieldType.FRICTION;i++) {
            Field f = fMap.GetField(i);

            if (!f.Enabled) {
                this.buffer.WriteLine("{0,-19} NO", f.Name);
                continue;
            }

            this.buffer.WriteLine("{0,-19} PRECISION {1}", f.Name, f.Precision);

            if (f.GetRptLim(Field.RangeType.LOW) < Constants.BIG)
                this.buffer.WriteLine("{0,-19} BELOW {1:0.######}", f.Name, f.GetRptLim(Field.RangeType.LOW));

            if (f.GetRptLim(Field.RangeType.HI) > -Constants.BIG)
                this.buffer.WriteLine("{0,-19} ABOVE {1:0.######}", f.Name, f.GetRptLim(Field.RangeType.HI));
        }

        this.buffer.WriteLine();
    }

    private void ComposeCoordinates(Network net) {
        this.buffer.WriteLine(Network.SectType.COORDINATES.parseStr());
        this.buffer.WriteLine(COORDINATES_SUBTITLE);

        foreach (Node node  in  net.Nodes){
            if(!node.Position.IsInvalid) {
                this.buffer.WriteLine(
                    " {0,-16}\t{1,-12}\t{2,-12}",
                    node.Id,
                    node.Position.X,
                    node.Position.Y);
            }
        }
        this.buffer.WriteLine();
    }


    private void ComposeLabels(Network net) {
        this.buffer.WriteLine(Network.SectType.LABELS.parseStr());
        this.buffer.WriteLine(";X-Coord\tY-Coord\tLabel & Anchor Node");

        foreach (Label label  in  net.Labels) {
            this.buffer.WriteLine(
                " {0,-16}\t{1,-16}\t\"{2}\" {3,-16}",
                label.Position.X,
                label.Position.Y,
                label.Text,
                // label.AnchorNodeId // TODO: add AnchorNodeId property to label
                ""
                );

        }
        this.buffer.WriteLine();
    }

    private void ComposeVertices(Network net) {
        this.buffer.WriteLine(Network.SectType.VERTICES.parseStr());
        this.buffer.WriteLine(";Link\tX-Coord\tY-Coord");

        foreach (Link link  in  net.Links){
            foreach (EnPoint p  in  link.Vertices){
                this.buffer.WriteLine(" {0,-16}\t{1,-16}\t{2,-16}",link.Id,p.X,p.Y);
            }
        }

        this.buffer.WriteLine();
    }

    private void ComposeRules(Network net) {
        this.buffer.WriteLine(Network.SectType.RULES.parseStr());
        this.buffer.WriteLine();

        foreach (Rule r  in  net.Rules){
            this.buffer.WriteLine("RULE " + r.Label);
            foreach (string s  in  r.Code)
                this.buffer.WriteLine(s);
            this.buffer.WriteLine();
        }
        this.buffer.WriteLine();
    }
}
}