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
using System.Text;
using org.addition.epanet.network.structures;
using org.addition.epanet.util;

namespace org.addition.epanet.network.io.output {

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

    public InpComposer() {
    }

    public override void composer(Network net, string fileName) {
        try {
            buffer = new StreamWriter(File.OpenWrite(fileName), Encoding.Default); // "ISO-8859-1"
            composeHeader(net);
            composeJunctions(net);
            composeReservoirs(net);
            composeTanks(net);
            composePipes(net);
            composePumps(net);
            composeValves(net);
            composeDemands(net);
            composeEmitters(net);
            composeStatus(net);
            composePatterns(net);
            composeCurves(net);
            composeControls(net);
            composeQuality(net);
            composeSource(net);
            composeMixing(net);
            composeReaction(net);
            composeEnergy(net);
            composeTimes(net);
            composeOptions(net);
            composeExtraOptions(net);
            composeReport(net);
            composeLabels(net);
            composeCoordinates(net);
            composeVertices(net);
            composeRules(net);

            buffer.WriteLine(Network.SectType.END.parseStr());
            buffer.Close();
        }
        catch (IOException e){return;}
    }

    public void composeHeader(Network net) {
        if(net.getTitleText().Count == 0)
            return;

        buffer.WriteLine(Network.SectType.TITLE.parseStr());
        
        foreach (string str  in  net.getTitleText()){
            buffer.WriteLine(str);
        }

        buffer.WriteLine();
    }


    private void composeJunctions(Network net) {
        FieldsMap fMap = net.getFieldsMap();
        PropertiesMap pMap = net.getPropertiesMap();

        if(net.getJunctions().Length==0)
            return;


        buffer.WriteLine(Network.SectType.JUNCTIONS.parseStr());
        buffer.WriteLine(JUNCS_SUBTITLE);

        foreach (Node node  in  net.getJunctions()){
            buffer.Write(" {0}\t{1}",node.getId(),fMap.revertUnit(FieldsMap.Type.ELEV, node.getElevation()));

            //if(node.getDemand()!=null && node.getDemand().size()>0 && !node.getDemand()[0].getPattern().getId().equals(""))
            //    buffer.write("\t"+node.getDemand()[0].getPattern().getId());

            if(node.getDemand().Count>0){
                Demand d = node.getDemand()[0];
                buffer.Write("\t{0}",fMap.revertUnit(FieldsMap.Type.DEMAND, d.getBase()));

                if (!string.IsNullOrEmpty(d.getPattern().getId()) && !pMap.getDefPatId().Equals(d.getPattern().getId(), StringComparison.OrdinalIgnoreCase))
                    buffer.Write("\t" + d.getPattern().getId());
            }

            if(!string.IsNullOrEmpty(node.getComment()))
                buffer.Write("\t;"+node.getComment());

            buffer.WriteLine();
        }


        buffer.WriteLine();
    }

    private void composeReservoirs(Network net) {
        FieldsMap fMap = net.getFieldsMap();
        if(net.getTanks().Length==0)
            return;

        List<Tank> reservoirs = new List<Tank>();
        foreach (Tank tank  in  net.getTanks())
            if(tank.getArea() == 0)
                reservoirs.Add(tank);

        if(reservoirs.Count==0)
            return;

        buffer.WriteLine(Network.SectType.RESERVOIRS.parseStr());
        buffer.WriteLine(RESERVOIRS_SUBTITLE);

        foreach (Tank r  in  reservoirs){
            buffer.Write(" {0}\t{1}",r.getId(),fMap.revertUnit(FieldsMap.Type.ELEV, r.getElevation()));


            if (r.getPattern()!=null)
                buffer.Write("\t{0}",r.getPattern().getId());


            if(!string.IsNullOrEmpty(r.getComment()))
                buffer.Write("\t;"+r.getComment());

            buffer.WriteLine();
        }

        buffer.WriteLine();
    }

    private void composeTanks(Network net) {
        FieldsMap fMap = net.getFieldsMap();

        List<Tank> tanks = new List<Tank>();
        foreach (Tank tank  in  net.getTanks())
            if(tank.getArea()!= 0)
                tanks.Add(tank);

        if(tanks.Count ==0)
            return;

        buffer.WriteLine(Network.SectType.TANKS.parseStr());
        buffer.WriteLine(TANK_SUBTITLE);

        foreach (Tank tank  in  tanks){
            double Vmin = tank.getVmin();
            if(Math.Round(Vmin/tank.getArea()) == Math.Round(tank.getHmin()-tank.getElevation()))
                Vmin = 0;

            buffer.Write(" {0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}",
                    tank.getId(),
                    fMap.revertUnit(FieldsMap.Type.ELEV, tank.getElevation()),
                    fMap.revertUnit(FieldsMap.Type.ELEV, tank.getH0() - tank.getElevation()),
                    fMap.revertUnit(FieldsMap.Type.ELEV, tank.getHmin() - tank.getElevation()),
                    fMap.revertUnit(FieldsMap.Type.ELEV, tank.getHmax() - tank.getElevation()),
                    fMap.revertUnit(FieldsMap.Type.ELEV, 2*Math.Sqrt(tank.getArea() / Constants.PI)),
                    fMap.revertUnit(FieldsMap.Type.VOLUME, Vmin));

            if(tank.getVcurve()!=null)
                buffer.Write(" "+tank.getVcurve().getId());

            if(!string.IsNullOrEmpty(tank.getComment()))
                buffer.Write("\t;"+tank.getComment());
            buffer.WriteLine();
        }

        buffer.WriteLine();
    }

    private void composePipes(Network net) {
        FieldsMap fMap = net.getFieldsMap();
        PropertiesMap pMap = net.getPropertiesMap();

        if(net.getLinks().Length ==0)
            return;

        List<Link> pipes = new List<Link>();
        foreach (Link link  in  net.getLinks())
            if(link.getType() == Link.LinkType.PIPE || link.getType() == Link.LinkType.CV)
                pipes.Add(link);


        buffer.WriteLine(Network.SectType.PIPES.parseStr());
        buffer.WriteLine(PIPES_SUBTITLE);

        foreach (Link link  in  pipes){
            double d = link.getDiameter();
            double kc = link.getRoughness();
            if (pMap.getFormflag() == PropertiesMap.FormType.DW)
                kc = fMap.revertUnit(FieldsMap.Type.ELEV, kc*1000.0);

            double km = link.getKm()*Math.Pow(d,4.0)/0.02517;

            buffer.Write(" {0}\t{1}\t{2}\t{3}\t{4}",
                    link.getId(),
                    link.getFirst().getId(),
                    link.getSecond().getId(),
                    fMap.revertUnit(FieldsMap.Type.LENGTH, link.getLenght()),
                    fMap.revertUnit(FieldsMap.Type.DIAM, d));

            //if (pMap.getFormflag() == FormType.DW)
            buffer.Write(" {0}\t{1}", kc, km);

            if (link.getType() == Link.LinkType.CV)
                buffer.Write(" CV");
            else if (link.getStat() == Link.StatType.CLOSED)
                buffer.Write(" CLOSED");
            else if (link.getStat() == Link.StatType.OPEN)
                buffer.Write(" OPEN");

            if(!string.IsNullOrEmpty(link.getComment()))
                buffer.Write("\t;" + link.getComment());

            buffer.WriteLine();
        }

        buffer.WriteLine();
    }

    private void composePumps(Network net) {
        FieldsMap fMap = net.getFieldsMap();
        Pump[] pumps = net.getPumps();
        if(pumps.Length==0)
            return;

        buffer.WriteLine(Network.SectType.PUMPS.parseStr());
        buffer.WriteLine(PUMPS_SUBTITLE);

        foreach (Pump pump  in  pumps)
        {
            buffer.Write(" {0}\t{1}\t{2}", pump.getId(),
                    pump.getFirst().getId(), pump.getSecond().getId());


            // Pump has constant power
            if (pump.getPtype() == Pump.Type.CONST_HP)
                buffer.Write(" POWER " + pump.getKm());
                // Pump has a head curve
            else if (pump.getHcurve()!=null)
                buffer.Write(" HEAD " + pump.getHcurve().getId());
                // Old format used for pump curve
            else
            {
                buffer.Write(" {0}\t{1}\t{2}\t0.0\t{3}",
                        fMap.revertUnit(FieldsMap.Type.HEAD, -pump.getH0()),
                        fMap.revertUnit(FieldsMap.Type.HEAD, -pump.getH0() - pump.getFlowCoefficient()*Math.Pow(pump.getQ0(),pump.getN())),
                        fMap.revertUnit(FieldsMap.Type.FLOW, pump.getQ0()),
                        fMap.revertUnit(FieldsMap.Type.FLOW, pump.getQmax()));

                continue;
            }

            if ( pump.getUpat()!=null)
                buffer.Write(" PATTERN " + pump.getUpat().getId());


            if (pump.getRoughness() != 1.0)
                buffer.Write(" SPEED {0}", pump.getRoughness());

            if(!string.IsNullOrEmpty(pump.getComment()))
                buffer.Write("\t;"+pump.getComment());
            
            buffer.WriteLine();
        }

        buffer.WriteLine();
    }

    private void composeValves(Network net) {
        FieldsMap fMap = net.getFieldsMap();
        var valves = net.getValves();
        if(valves.size()==0)
            return;

        buffer.Write(Network.SectType.VALVES.parseStr());
        buffer.WriteLine();
        buffer.Write(VALVES_SUBTITLE);
        buffer.WriteLine();

        foreach (Valve valve  in  valves)
        {
            double d = valve.getDiameter();
            double kc = valve.getRoughness();
            if (kc == Constants.MISSING)
                kc = 0.0;

            switch (valve.getType()) {
            case Link.LinkType.FCV:
                kc = fMap.revertUnit(FieldsMap.Type.FLOW, kc);
                break;
            case Link.LinkType.PRV:
            case Link.LinkType.PSV:
            case Link.LinkType.PBV:
                kc = fMap.revertUnit(FieldsMap.Type.PRESSURE, kc);
                break;
            }

            double km = valve.getKm()*Math.Pow(d,4)/0.02517;

            buffer.Write(" {0}\t{1}\t{2}\t{3}\t%5s",
                    valve.getId(),
                    valve.getFirst().getId(),
                    valve.getSecond().getId(),
                    fMap.revertUnit(FieldsMap.Type.DIAM, d),
                    valve.getType().ParseStr());

            if (valve.getType() == Link.LinkType.GPV && valve.getCurve() != null)
                buffer.Write(" {0}\t{1}", valve.getCurve().getId(), km);
            else
                buffer.Write(" {0}\t{1}",kc,km);

            if(!string.IsNullOrEmpty(valve.getComment()))
                buffer.Write("\t;"+valve.getComment());

            buffer.WriteLine();
        }
        buffer.WriteLine();
    }

    private void composeDemands(Network net) {
        FieldsMap fMap = net.getFieldsMap();

        if(net.getJunctions().size()==0)
            return;

        buffer.WriteLine(Network.SectType.DEMANDS.parseStr());
        buffer.WriteLine(DEMANDS_SUBTITLE);

        double ucf = fMap.getUnits(FieldsMap.Type.DEMAND);

        foreach (Node node  in  net.getJunctions()){
            if (node.getDemand().Count > 1)
                for(int i = 0;i<node.getDemand().Count;i++){
                    Demand demand = node.getDemand()[i];
                    buffer.Write("{0}\t{1}",node.getId(), ucf * demand.getBase());

                    if (demand.getPattern() != null)
                        buffer.Write("\t"+demand.getPattern().getId());

                    buffer.WriteLine();
                }
        }

        buffer.WriteLine();
    }

    private void composeEmitters(Network net) {
        if(net.getNodes().size()==0)
            return;

        buffer.WriteLine(Network.SectType.EMITTERS.parseStr());
        buffer.WriteLine(EMITTERS_SUBTITLE);
        
        double uflow = net.getFieldsMap().getUnits(FieldsMap.Type.FLOW);
        double upressure = net.getFieldsMap().getUnits(FieldsMap.Type.PRESSURE);
        double Qexp = net.getPropertiesMap().getQexp();

        foreach (Node node  in  net.getJunctions()){
            if(node.getKe()==0.0) continue;
            double ke = uflow/Math.Pow(upressure * node.getKe(), (1.0 / Qexp));
            buffer.WriteLine(" {0}\t{1}",node.getId(),ke);
        }

        buffer.WriteLine();
    }

    private void composeStatus(Network net) {

        if(net.getLinks().size()==0)
            return;

        buffer.WriteLine(Network.SectType.STATUS.parseStr());
        buffer.WriteLine(STATUS_SUBTITLE);

        foreach (Link link  in  net.getLinks())
        {
            if (link.getType() <= Link.LinkType.PUMP)
            {
                if (link.getStat() == Link.StatType.CLOSED)
                    buffer.WriteLine(" {0}\t{1}",link.getId(),Link.StatType.CLOSED.ParseStr());

                    // Write pump speed here for pumps with old-style pump curve input
                else if (link.getType() == Link.LinkType.PUMP){
                    Pump pump = (Pump)link;
                    if (pump.getHcurve() == null &&
                            pump.getPtype() != Pump.Type.CONST_HP &&
                            pump.getRoughness() != 1.0)
                        buffer.WriteLine(" {0}\t{1}", link.getId(), link.getRoughness());
                }
            }
            // Write fixed-status PRVs & PSVs (setting = MISSING)
            else if (link.getRoughness() == Constants.MISSING)
            {
                if (link.getStat() == Link.StatType.OPEN)
                    buffer.WriteLine(" {0}\t{1}",link.getId(),Link.StatType.OPEN.ParseStr());

                if (link.getStat() == Link.StatType.CLOSED)
                    buffer.WriteLine(" {0}\t{1}", link.getId(), Link.StatType.CLOSED.ParseStr());

            }

        }

        buffer.WriteLine();
    }

    private void composePatterns(Network net) {

        var pats = net.getPatterns();

        if(pats.size()<=1)
            return;

        buffer.WriteLine(Network.SectType.PATTERNS.parseStr());
        buffer.WriteLine(PATTERNS_SUBTITLE);
        
        for(int i = 1;i<pats.size();i++)
        {
            Pattern pat = pats[i];
            List<double> F = pat.getFactorsList();
            for (int j=0; j<pats[i].getLength(); j++)
            {
                if (j % 6 == 0)
                    buffer.Write(" {0}",pat.getId());

                buffer.Write(" {0}",F[j]);

                if (j % 6 == 5)
                    buffer.WriteLine();
            }
            buffer.WriteLine();
        }

        buffer.WriteLine();
    }

    private void composeCurves(Network net) {

        var curves = net.getCurves();

        if (curves.size() == 0)
            return;

        buffer.WriteLine(Network.SectType.CURVES.parseStr());
        buffer.WriteLine(CURVE_SUBTITLE);

        foreach (Curve c  in  curves) {
            for (int i = 0; i < c.getNpts(); i++) {
                buffer.WriteLine(" {0}\t{1}\t{2}", c.getId(), c.getX()[i], c.getY()[i]);
            }
        }

        buffer.WriteLine();
    }

    private void composeControls(Network net) {
        var controls = net.getControls();
        FieldsMap fmap = net.getFieldsMap();

        if(controls.Length == 0)
            return;

        buffer.WriteLine(Network.SectType.CONTROLS.parseStr());

        foreach (Control control  in  controls)
        {
            // Check that controlled link exists
            if (control.getLink()==null) continue;

            // Get text of control's link status/setting
            if (control.getSetting() == Constants.MISSING)
                buffer.Write(" LINK {0} {1} ", control.getLink().getId(), control.getStatus().ParseStr());
            else {
                double kc = control.getSetting();
                switch (control.getLink().getType()) {
                case Link.LinkType.PRV:
                case Link.LinkType.PSV:
                case Link.LinkType.PBV:
                    kc = fmap.revertUnit(FieldsMap.Type.PRESSURE, kc);
                    break;
                case Link.LinkType.FCV:
                    kc = fmap.revertUnit(FieldsMap.Type.FLOW, kc);
                    break;
                }
                buffer.Write(" LINK {0} {1} ", control.getLink().getId(), kc);
            }


            switch (control.getType()) {
            // Print level control
            case Control.ControlType.LOWLEVEL:
            case Control.ControlType.HILEVEL:
                double kc = control.getGrade() - control.getNode().getElevation();
                if (control.getNode() is Tank)
                    kc = fmap.revertUnit(FieldsMap.Type.HEAD, kc);
                else
                    kc = fmap.revertUnit(FieldsMap.Type.PRESSURE, kc);

                buffer.Write(
                    " IF NODE {0} {1} {2}",
                    control.getNode().getId(),
                    control.getType().ParseStr(),
                    kc);

                break;

            // Print timer control
            case Control.ControlType.TIMER:
                buffer.Write(
                    " AT {0} {1} HOURS",
                    Control.ControlType.TIMER.ParseStr(),
                    control.getTime() / 3600.0f);

                break;

            // Print time-of-day control
            case Control.ControlType.TIMEOFDAY:
                buffer.Write(
                    " AT {0} {1}",
                    Control.ControlType.TIMEOFDAY.ParseStr(),
                    Utilities.getClockTime(control.getTime()));

                break;
            }
            buffer.WriteLine();
        }
        buffer.WriteLine();
    }

    private void composeQuality(Network net) {
        var nodes = net.getNodes();
        FieldsMap fmap = net.getFieldsMap();
        if(nodes.size() == 0)
            return;

        buffer.WriteLine(Network.SectType.QUALITY.parseStr());
        buffer.WriteLine(QUALITY_SUBTITLE);
        
        foreach (Node node  in  nodes)
        {
            if (node.getC0().Length == 1 ){
                if(node.getC0()[0] == 0.0 ) continue;
                buffer.Write(" {0}\t{1}", node.getId(), fmap.revertUnit(FieldsMap.Type.QUALITY, node.getC0()[0]));
            }

            buffer.WriteLine();
        }
        buffer.WriteLine();
    }

    private void composeSource(Network net) {
        var nodes = net.getNodes();

        if(nodes.size() == 0)
            return;

        buffer.WriteLine(Network.SectType.SOURCES.parseStr());
        buffer.WriteLine(SOURCE_SUBTITLE);
        

        foreach (Node node  in  nodes){
            Source source = node.getSource();
            if (source == null)
                continue;
            
            buffer.Write(" {0}\t{1}\t{2}",
                    node.getId(),
                    source.getType().ParseStr(),
                    source.getC0());

            if (source.getPattern()!=null)
                buffer.Write(" " + source.getPattern().getId());

            buffer.WriteLine();
        }
        buffer.WriteLine();
    }


    private void composeMixing(Network net) {
        if(net.getTanks().size()==0)
            return;

        buffer.WriteLine(Network.SectType.MIXING.parseStr());
        buffer.WriteLine(MIXING_SUBTITLE);
        
        foreach (Tank tank  in  net.getTanks())
        {
            if (tank.getArea() == 0.0) continue;
            buffer.WriteLine(" {0}\t{1}\t{2}",
                    tank.getId(),tank.getMixModel().ParseStr(),
                    tank.getV1max() / tank.getVmax());
        }
        buffer.WriteLine();
    }

    private void composeReaction(Network net) {
        PropertiesMap pMap = net.getPropertiesMap();

        buffer.WriteLine(Network.SectType.REACTIONS.parseStr());
        buffer.WriteLine(REACTIONS_SUBTITLE);

        buffer.WriteLine("ORDER BULK {0}", pMap.getBulkOrder());
        buffer.WriteLine("ORDER WALL {0}", pMap.getWallOrder());
        buffer.WriteLine("ORDER TANK {0}", pMap.getTankOrder());
        buffer.WriteLine("GLOBAL BULK {0}", pMap.getKbulk() * Constants.SECperDAY);
        buffer.WriteLine("GLOBAL WALL {0}", pMap.getKwall() * Constants.SECperDAY);

        //if (pMap.getClimit() > 0.0)
        buffer.WriteLine("LIMITING POTENTIAL {0}", pMap.getClimit());

        //if (pMap.getRfactor() != Constants.MISSING && pMap.getRfactor() != 0.0)
        buffer.WriteLine("ROUGHNESS CORRELATION {0}", pMap.getRfactor());


        foreach (Link link  in  net.getLinks()) {
            if (link.getType() > Link.LinkType.PIPE)
                continue;

            if (link.getKb() != pMap.getKbulk())
                buffer.WriteLine("BULK {0} {1}", link.getId(), link.getKb() * Constants.SECperDAY);
            if (link.getKw() != pMap.getKwall())
                buffer.WriteLine("WALL {0} {1}", link.getId(), link.getKw() * Constants.SECperDAY);
        }

        foreach (Tank tank  in  net.getTanks()) {
            if (tank.getArea() == 0.0) continue;
            if (tank.getKb() != pMap.getKbulk())
                buffer.WriteLine("TANK {0} {1}", tank.getId(), tank.getKb() * Constants.SECperDAY);
        }

        buffer.WriteLine();
    }

    private void composeEnergy(Network net) {
        PropertiesMap pMap = net.getPropertiesMap();

        buffer.WriteLine(Network.SectType.ENERGY.parseStr());

        if (pMap.getEcost() != 0.0)
            buffer.WriteLine("GLOBAL PRICE {0}", pMap.getEcost());

        if (!pMap.getEpatId().Equals(""))
            buffer.WriteLine("GLOBAL PATTERN {0}",  pMap.getEpatId());

        buffer.WriteLine("GLOBAL EFFIC {0}", pMap.getEpump());
        buffer.WriteLine("DEMAND CHARGE {0}", pMap.getDcost());

        foreach (Pump p  in  net.getPumps())
        {
            if (p.getEcost() > 0.0)
                buffer.WriteLine("PUMP {0} PRICE {1}",p.getId(),p.getEcost());

            if (p.getEpat() != null)
                buffer.WriteLine("PUMP {0} PATTERN {1}", p.getId(), p.getEpat().getId());

            if (p.getEcurve() !=null)
                buffer.WriteLine("PUMP {0} EFFIC {1}", p.getId(),p.getEcurve().getId());
        }

        buffer.WriteLine();

    }

    private void composeTimes(Network net) {
        PropertiesMap pMap = net.getPropertiesMap();
        buffer.WriteLine(Network.SectType.TIMES.parseStr());
        buffer.WriteLine("DURATION {0}", Utilities.getClockTime(pMap.getDuration()));
        buffer.WriteLine("HYDRAULIC TIMESTEP {0}", Utilities.getClockTime(pMap.getHstep()));
        buffer.WriteLine("QUALITY TIMESTEP {0}", Utilities.getClockTime(pMap.getQstep()));
        buffer.WriteLine("REPORT TIMESTEP {0}", Utilities.getClockTime(pMap.getRstep()));
        buffer.WriteLine("REPORT START {0}", Utilities.getClockTime(pMap.getRstart()));
        buffer.WriteLine("PATTERN TIMESTEP {0}", Utilities.getClockTime(pMap.getPstep()));
        buffer.WriteLine("PATTERN START {0}", Utilities.getClockTime(pMap.getPstart()));
        buffer.WriteLine("RULE TIMESTEP {0}", Utilities.getClockTime(pMap.getRulestep()));
        buffer.WriteLine("START CLOCKTIME {0}", Utilities.getClockTime(pMap.getTstart()));
        buffer.WriteLine("STATISTIC {0}", pMap.getTstatflag().ParseStr());
        buffer.WriteLine();
    }

    private void composeOptions(Network net) {
        PropertiesMap pMap = net.getPropertiesMap();
        FieldsMap fMap = net.getFieldsMap();
        
        buffer.WriteLine(Network.SectType.OPTIONS.parseStr());
        buffer.WriteLine("UNITS               " + pMap.getFlowflag());
        buffer.WriteLine("PRESSURE            " + pMap.getPressflag());
        buffer.WriteLine("HEADLOSS            " + pMap.getFormflag());

        if (!string.IsNullOrEmpty(pMap.getDefPatId()))
        buffer.WriteLine("PATTERN             " + pMap.getDefPatId());
        
        if (pMap.getHydflag() == PropertiesMap.Hydtype.USE)
        buffer.WriteLine("HYDRAULICS USE      " + pMap.getHydFname());
        
        if (pMap.getHydflag() == PropertiesMap.Hydtype.SAVE)
        buffer.WriteLine("HYDRAULICS SAVE     " + pMap.getHydFname());
        
        if (pMap.getExtraIter() == -1)
        buffer.WriteLine("UNBALANCED          STOP");

        if (pMap.getExtraIter() >= 0)
        buffer.WriteLine("UNBALANCED          CONTINUE " + pMap.getExtraIter());

        switch (pMap.getQualflag()) {
        case PropertiesMap.QualType.CHEM:
            this.buffer.WriteLine("QUALITY             {0} {1}", pMap.getChemName(), pMap.getChemUnits());
            break;
        case PropertiesMap.QualType.TRACE:
            this.buffer.WriteLine("QUALITY             TRACE " + pMap.getTraceNode());
            break;
        case PropertiesMap.QualType.AGE:
            this.buffer.WriteLine("QUALITY             AGE");
            break;
        case PropertiesMap.QualType.NONE:
            this.buffer.WriteLine("QUALITY             NONE");
            break;
        }

        buffer.WriteLine("DEMAND MULTIPLIER {0}", pMap.getDmult());
        buffer.WriteLine("EMITTER EXPONENT  {0}", 1.0/pMap.getQexp());
        buffer.WriteLine("VISCOSITY         {0}", pMap.getViscos()/Constants.VISCOS);
        buffer.WriteLine("DIFFUSIVITY       {0}", pMap.getDiffus()/Constants.DIFFUS);
        buffer.WriteLine("SPECIFIC GRAVITY  {0}", pMap.getSpGrav());
        buffer.WriteLine("TRIALS            {0}", pMap.getMaxIter());
        buffer.WriteLine("ACCURACY          {0}", pMap.getHacc());
        buffer.WriteLine("TOLERANCE         {0}", fMap.revertUnit(FieldsMap.Type.QUALITY, pMap.getCtol()));
        buffer.WriteLine("CHECKFREQ         {0}", pMap.getCheckFreq());
        buffer.WriteLine("MAXCHECK          {0}", pMap.getMaxCheck());
        buffer.WriteLine("DAMPLIMIT         {0}", pMap.getDampLimit());
        buffer.WriteLine();
    }

    private void composeExtraOptions(Network net) {
        PropertiesMap pMap = net.getPropertiesMap();
        var otherObjsNames = pMap.getObjectsNames(true);

        if(otherObjsNames.Count==0)
            return;

        foreach (string objName  in  otherObjsNames){
            object objVal = pMap[objName];
            buffer.WriteLine(objName + " " + objVal);
        }
        buffer.WriteLine();

    }

    private void composeReport(Network net) {

        buffer.WriteLine(Network.SectType.REPORT.parseStr());
        
        PropertiesMap pMap = net.getPropertiesMap();
        FieldsMap fMap = net.getFieldsMap();
        buffer.WriteLine("PAGESIZE       {0}",pMap.getPageSize());
        buffer.WriteLine("STATUS         " + pMap.getStatflag());
        buffer.WriteLine("SUMMARY        " + (pMap.getSummaryflag() ? Keywords.w_YES:Keywords.w_NO));
        buffer.WriteLine("ENERGY         " + (pMap.getEnergyflag() ? Keywords.w_YES:Keywords.w_NO));

        switch (pMap.getNodeflag()){
            case PropertiesMap.ReportFlag.FALSE:
                buffer.WriteLine("NODES NONE");
                break;
            case PropertiesMap.ReportFlag.TRUE:
                buffer.WriteLine("NODES ALL");
                break;
            case PropertiesMap.ReportFlag.SOME:
            {
                int j = 0;
                foreach (Node node  in  net.getNodes()){
                    if(node.isRptFlag()){
                        // if (j % 5 == 0) buffer.WriteLine("NODES "); // BUG: Baseform bug

                        if (j % 5 == 0) {
                            this.buffer.WriteLine();
                            buffer.Write("NODES ");
                        }

                        buffer.Write("{0} ", node.getId());
                        j++;
                    }
                }
                break;
            }
        }

        switch (pMap.getLinkflag()){
            case PropertiesMap.ReportFlag.FALSE:
                buffer.WriteLine("LINKS NONE");
                break;
            case PropertiesMap.ReportFlag.TRUE:
                buffer.WriteLine("LINKS ALL");
                break;
            case PropertiesMap.ReportFlag.SOME:
            {
                int j = 0;
                foreach (Link link  in  net.getLinks()){
                    if(link.isRptFlag()){
                        // if (j % 5 == 0) buffer.write("LINKS \n"); // BUG: Baseform bug
                        if (j % 5 == 0) {
                            this.buffer.WriteLine();
                            buffer.Write("LINKS ");
                        }

                        buffer.Write("{0} ", link.getId());
                        j++;
                    }
                }
                break;
            }
        }

        for(FieldsMap.Type i = 0;i< FieldsMap.Type.FRICTION;i++) {
            Field f = fMap.getField(i);

            if (!f.isEnabled()) {
                this.buffer.WriteLine("{0,-19} NO", f.getName());
                continue;
            }

            this.buffer.WriteLine("{0,-19} PRECISION {1}", f.getName(), f.getPrecision());

            if (f.getRptLim(Field.RangeType.LOW) < Constants.BIG)
                this.buffer.WriteLine("{0,-19} BELOW {1:0.######}", f.getName(), f.getRptLim(Field.RangeType.LOW));

            if (f.getRptLim(Field.RangeType.HI) > -Constants.BIG)
                this.buffer.WriteLine("{0,-19} ABOVE {1:0.######}", f.getName(), f.getRptLim(Field.RangeType.HI));
        }

        buffer.WriteLine();
    }

    private void composeCoordinates(Network net) {
        buffer.WriteLine(Network.SectType.COORDINATES.parseStr());
        buffer.WriteLine(COORDINATES_SUBTITLE);

        foreach (Node node  in  net.getNodes()){
            if(node.getPosition()!=null) {
                buffer.WriteLine(
                    " {0,-16}\t{1,-12}\t{2,-12}",
                    node.getId(),
                    node.getPosition().getX(),
                    node.getPosition().getY());
            }
        }
        buffer.WriteLine();
    }


    private void composeLabels(Network net) {
        buffer.WriteLine(Network.SectType.LABELS.parseStr());
        buffer.WriteLine(";X-Coord\tY-Coord\tLabel & Anchor Node");

        foreach (Label label  in  net.getLabels()) {
            buffer.WriteLine(
                " {0,-16}\t{1,-16}\t\"{2}\" {3,-16}",
                label.getPosition().getX(),
                label.getPosition().getY(),
                label.getText(),
                // label.AnchorNodeId // TODO: add AnchorNodeId property to label
                ""
                );

        }
        buffer.WriteLine();
    }

    private void composeVertices(Network net) {
        buffer.WriteLine(Network.SectType.VERTICES.parseStr());
        buffer.WriteLine(";Link\tX-Coord\tY-Coord");

        foreach (Link link  in  net.getLinks()){
            foreach (Point p  in  link.getVertices()){
                buffer.WriteLine(" {0,-16}\t{1,-16}\t{2,-16}",link.getId(),p.getX(),p.getY());
            }
        }

        buffer.WriteLine();
    }

    private void composeRules(Network net) {
        buffer.WriteLine(Network.SectType.RULES.parseStr());
        buffer.WriteLine();

        foreach (Rule r  in  net.getRules()){
            buffer.WriteLine("RULE " + r.getLabel());
            foreach (string s  in  r.getCode().Split('\n'))
                buffer.WriteLine(s);
            buffer.WriteLine();
        }
        buffer.WriteLine();
    }
}
}