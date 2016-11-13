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
using org.addition.epanet.log;
using org.addition.epanet.network;
using org.addition.epanet.network.structures;
using org.addition.epanet.util;

namespace org.addition.epanet.hydraulic.structures
{

    public class SimulationRule
    {

        // Temporary action item
        public class ActItem
        {
            public ActItem(SimulationRule rule, Action action)
            {
                this.rule = rule;
                this.action = action;
            }

            public SimulationRule rule;
            public Action action;
        }

        // Rules execution result
        public class Result
        {
            public Result(long step, long htime)
            {
                this.step = step;
                this.htime = htime;
            }

            public long step;
            public long htime;

        }

        // Rule premise
        public class Premise
        {
            public Premise(string[] Tok, Rule.Rulewords lOp, List<SimulationNode> nodes, List<SimulationLink> links)
            {
                Rule.Objects loType;
                Rule.Varwords lVar;
                object lObj;
                Rule.Operators lROp;

                if (Tok.Length != 5 && Tok.Length != 6)
                    throw new ENException(ErrorCode.Err201);

                EnumsTxt.TryParse(Tok[1], out loType);

                if (loType == Rule.Objects.r_SYSTEM)
                {
                    EnumsTxt.TryParse(Tok[2], out lVar);

                    if (lVar != Rule.Varwords.r_DEMAND && lVar != Rule.Varwords.r_TIME &&
                        lVar != Rule.Varwords.r_CLOCKTIME)
                        throw new ENException(ErrorCode.Err201);

                    lObj = Rule.Objects.r_SYSTEM;
                }
                else
                {
                    if(!EnumsTxt.TryParse(Tok[3], out lVar))
                        throw new ENException(ErrorCode.Err201);

                    switch (loType)
                    {
                        case Rule.Objects.r_NODE:
                        case Rule.Objects.r_JUNC:
                        case Rule.Objects.r_RESERV:
                        case Rule.Objects.r_TANK:
                            loType = Rule.Objects.r_NODE;
                            break;
                        case Rule.Objects.r_LINK:
                        case Rule.Objects.r_PIPE:
                        case Rule.Objects.r_PUMP:
                        case Rule.Objects.r_VALVE:
                            loType = Rule.Objects.r_LINK;
                            break;
                        default:
                            throw new ENException(ErrorCode.Err201);
                    }

                    if (loType == Rule.Objects.r_NODE)
                    {
                        //Node nodeRef = net.getNode(Tok[2]);
                        SimulationNode nodeRef = null;
                        foreach (SimulationNode simNode  in  nodes)
                        if (simNode.getNode().getId().Equals(Tok[2], StringComparison.OrdinalIgnoreCase))
                            nodeRef = simNode;

                        if (nodeRef == null)
                            throw new ENException(ErrorCode.Err203);
                        switch (lVar)
                        {
                            case Rule.Varwords.r_DEMAND:
                            case Rule.Varwords.r_HEAD:
                            case Rule.Varwords.r_GRADE:
                            case Rule.Varwords.r_LEVEL:
                            case Rule.Varwords.r_PRESSURE:
                                break;
                            case Rule.Varwords.r_FILLTIME:
                            case Rule.Varwords.r_DRAINTIME:
                                if (nodeRef is SimulationTank)
                                throw new ENException(ErrorCode.Err201);
                                break;

                            default:
                                throw new ENException(ErrorCode.Err201);
                        }
                        lObj = nodeRef;
                    }
                    else
                    {
                        //Link linkRef = net.getLink(Tok[2]);
                        SimulationLink linkRef = null;
                        foreach (SimulationLink simLink  in  links)
                        if (simLink.getLink().getId().Equals(Tok[2], StringComparison.OrdinalIgnoreCase))
                            linkRef = simLink;

                        if (linkRef == null)
                            throw new ENException(ErrorCode.Err204);
                        switch (lVar)
                        {
                            case Rule.Varwords.r_FLOW:
                            case Rule.Varwords.r_STATUS:
                            case Rule.Varwords.r_SETTING:
                                break;
                            default:
                                throw new ENException(ErrorCode.Err201);
                        }
                        lObj = linkRef;
                    }
                }

                Rule.Operators op;
                
                if(!EnumsTxt.TryParse(loType == Rule.Objects.r_SYSTEM ? Tok[3] : Tok[4], out op))
                    throw new ENException(ErrorCode.Err201);

                switch (op)
                {
                    case Rule.Operators.IS:
                        lROp = Rule.Operators.EQ;
                        break;
                    case Rule.Operators.NOT:
                        lROp = Rule.Operators.NE;
                        break;
                    case Rule.Operators.BELOW:
                        lROp = Rule.Operators.LT;
                        break;
                    case Rule.Operators.ABOVE:
                        lROp = Rule.Operators.GT;
                        break;
                    default:
                        lROp = op;
                        break;
                }

                // BUG: Baseform bug lStat == Rule.Values.IS_NUMBER
                Rule.Values lStat = Rule.Values.IS_NUMBER;
                double lVal = Constants.MISSING;

                if (lVar == Rule.Varwords.r_TIME || lVar == Rule.Varwords.r_CLOCKTIME)
                {
                    if (Tok.Length == 6)
                        lVal = Utilities.getHour(Tok[4], Tok[5])*3600.0;
                    else
                        lVal = Utilities.getHour(Tok[4], "")*3600.0;

                    if (lVal < 0.0)
                        throw new ENException(ErrorCode.Err202);
                }
                else {
                    Rule.Values k;

                    if(!EnumsTxt.TryParse(Tok[Tok.Length - 1], out k) || lStat <= Rule.Values.IS_NUMBER)
                    {
                        if (lStat == (Rule.Values)(-1) || lStat <= Rule.Values.IS_NUMBER)
                        {
                            if (double.IsNaN(lVal = Utilities.getDouble(Tok[Tok.Length - 1])))
                                throw new ENException(ErrorCode.Err202);

                            if (lVar == Rule.Varwords.r_FILLTIME || lVar == Rule.Varwords.r_DRAINTIME)
                                lVal = lVal*3600.0;
                        }
                    }
                    else
                    {
                        lStat = k;
                    }

                }

                this.status = lStat;
                this.value = lVal;
                this.logop = lOp;
                this.relop = lROp;
                this.variable = lVar;
                this.@object = lObj;
            }

            /// <summary>Logical operator</summary>
            private readonly Rule.Rulewords logop ; 
            private readonly object @object;
            /// <summary>Pressure, flow, etc</summary>
            private readonly Rule.Varwords variable ; 
            /// <summary>Relational operator</summary>
            private readonly Rule.Operators relop; 
            /// <summary>Variable's status</summary>
            private readonly Rule.Values status;
            /// <summary>Variable's value</summary>
            private readonly double value; 

            public Rule.Rulewords getLogop()
            {
                return this.logop;
            }

            public object getObject()
            {
                return this.@object;
            }

            public Rule.Varwords getVariable()
            {
                return this.variable;
            }

            public Rule.Operators getRelop()
            {
                return this.relop;
            }

            public Rule.Values getStatus()
            {
                return this.status;
            }

            public double getValue()
            {
                return this.value;
            }


            // Checks if a particular premise is true
            public bool checkPremise(FieldsMap fMap, PropertiesMap pMap,
                long Time1, long Htime, double dsystem)
            {
                if (this.variable == Rule.Varwords.r_TIME || this.variable == Rule.Varwords.r_CLOCKTIME)
                    return (this.checkTime(pMap, Time1, Htime));
                else if (this.status > Rule.Values.IS_NUMBER)
                    return (this.checkStatus());
                else
                    return (this.checkValue(fMap, dsystem));
            }

            // Checks if condition on system time holds
            private bool checkTime(PropertiesMap pMap, long Time1, long Htime)
            {
                bool flag;
                long t1, t2, x;

                if (this.variable == Rule.Varwords.r_TIME)
                {
                    t1 = Time1;
                    t2 = Htime;
                }
                else if (this.variable == Rule.Varwords.r_CLOCKTIME)
                {
                    t1 = (Time1 + pMap.getTstart())%Constants.SECperDAY;
                    t2 = (Htime + pMap.getTstart())%Constants.SECperDAY;
                }
                else
                    return false;

                x = (long) (this.value);
                switch (this.relop)
                {
                    case Rule.Operators.LT:
                        if (t2 >= x) return (false);
                        break;
                    case Rule.Operators.LE:
                        if (t2 > x) return (false);
                        break;
                    case Rule.Operators.GT:
                        if (t2 <= x) return (false);
                        break;
                    case Rule.Operators.GE:
                        if (t2 < x) return (false);
                        break;

                    case Rule.Operators.EQ:
                    case Rule.Operators.NE:
                        flag = false;
                        if (t2 < t1)
                        {
                            if (x >= t1 || x <= t2) flag = true;
                        }
                        else
                        {
                            if (x >= t1 && x <= t2) flag = true;
                        }
                        if (this.relop == Rule.Operators.EQ && !flag) return true;
                        if (this.relop == Rule.Operators.NE && flag) return true;
                        break;
                }

                return true;
            }

            // Checks if condition on link status holds
            private bool checkStatus()
            {
                switch (this.status)
                {
                    case Rule.Values.IS_OPEN:
                    case Rule.Values.IS_CLOSED:
                    case Rule.Values.IS_ACTIVE:
                        Rule.Values j;
                        Link.StatType i = (Link.StatType)(-1);
                        if (this.@object is SimulationLink)
                        i = ((SimulationLink)this.@object).getSimStatus();

                        if (i != null && i <= Link.StatType.CLOSED)
                            j = Rule.Values.IS_CLOSED;
                        else if (i == Link.StatType.ACTIVE)
                            j = Rule.Values.IS_ACTIVE;
                        else
                            j = Rule.Values.IS_OPEN;

                        if (j == this.status && this.relop == Rule.Operators.EQ)
                            return true;
                        if (j != this.status && this.relop == Rule.Operators.NE)
                            return true;
                        break;
                }
                return false;
            }

            /// <summary>Checks if numerical condition on a variable is true.</summary>
            private bool checkValue(FieldsMap fMap, double dsystem) {
                double tol = 1e-3;
                double x;

                SimulationLink link = null;
                SimulationNode node = null;

                if (this.@object is SimulationLink)
                    link = ((SimulationLink)this.@object);
                else if (this.@object is SimulationNode)
                    node = ((SimulationNode)this.@object);

                switch (this.variable) {
                case Rule.Varwords.r_DEMAND:
                    if ((Rule.Objects)this.@object == Rule.Objects.r_SYSTEM)
                        x = dsystem * fMap.getUnits(FieldsMap.Type.DEMAND);
                    else
                        x = node.getSimDemand() * fMap.getUnits(FieldsMap.Type.DEMAND);
                    break;

                case Rule.Varwords.r_HEAD:
                case Rule.Varwords.r_GRADE:
                    x = node.getSimHead() * fMap.getUnits(FieldsMap.Type.HEAD);
                    break;

                case Rule.Varwords.r_PRESSURE:
                    x = (node.getSimHead() - node.getElevation()) * fMap.getUnits(FieldsMap.Type.PRESSURE);
                    break;

                case Rule.Varwords.r_LEVEL:
                    x = (node.getSimHead() - node.getElevation()) * fMap.getUnits(FieldsMap.Type.HEAD);
                    break;

                case Rule.Varwords.r_FLOW:
                    x = Math.Abs(link.getSimFlow()) * fMap.getUnits(FieldsMap.Type.FLOW);
                    break;

                case Rule.Varwords.r_SETTING:

                    if (link.getSimSetting() == Constants.MISSING)
                        return false;

                    x = link.getSimSetting();
                    switch (link.getType()) {
                    case Link.LinkType.PRV:
                    case Link.LinkType.PSV:
                    case Link.LinkType.PBV:
                        x = x * fMap.getUnits(FieldsMap.Type.PRESSURE);
                        break;
                    case Link.LinkType.FCV:
                        x = x * fMap.getUnits(FieldsMap.Type.FLOW);
                        break;
                    }
                    break;
                case Rule.Varwords.r_FILLTIME: {
                    if (!(this.@object is SimulationTank))
                        return false;

                    SimulationTank tank = (SimulationTank)this.@object;

                    if (tank.isReservoir())
                        return false;

                    if (tank.getSimDemand() <= Constants.TINY)
                        return false;

                    x = (tank.getVmax() - tank.getSimVolume()) / tank.getSimDemand();

                    break;
                }
                case Rule.Varwords.r_DRAINTIME: {
                    if (!(this.@object is SimulationTank))
                        return false;

                    SimulationTank tank = (SimulationTank)this.@object;

                    if (tank.isReservoir())
                        return false;

                    if (tank.getSimDemand() >= -Constants.TINY)
                        return false;

                    x = (tank.getVmin() - tank.getSimVolume()) / tank.getSimDemand();
                    break;
                }
                default:
                    return false;
                }

                switch (this.relop) {
                case Rule.Operators.EQ:
                    if (Math.Abs(x - this.value) > tol)
                        return false;
                    break;
                case Rule.Operators.NE:
                    if (Math.Abs(x - this.value) < tol)
                        return false;
                    break;
                case Rule.Operators.LT:
                    if (x > this.value + tol)
                        return false;
                    break;
                case Rule.Operators.LE:
                    if (x > this.value - tol)
                        return false;
                    break;
                case Rule.Operators.GT:
                    if (x < this.value - tol)
                        return false;
                    break;
                case Rule.Operators.GE:
                    if (x < this.value + tol)
                        return false;
                    break;
                }
                return true;
            }

        }

        public class Action
        {
            private readonly string _label;

            public Action(string[] tok, List<SimulationLink> links, string label) {
                this._label = label;

                int Ntokens = tok.Length;

                Rule.Values s, k;
                double x;

                if (Ntokens != 6)
                    throw new ENException(ErrorCode.Err201);

                //Link linkRef = net.getLink(tok[2]);
                SimulationLink linkRef = null;
                foreach (SimulationLink simLink  in  links)
                if (simLink.getLink().getId().Equals(tok[2], StringComparison.OrdinalIgnoreCase))
                    linkRef = simLink;

                if (linkRef == null)
                    throw new ENException(ErrorCode.Err204);

                if (linkRef.getType() == Link.LinkType.CV)
                    throw new ENException(ErrorCode.Err207);

                s = (Rule.Values)(-1);
                x = Constants.MISSING;

                if (EnumsTxt.TryParse(tok[5], out k) && k > Rule.Values.IS_NUMBER) {
                    s = k;
                }
                else
                {
                    if (!tok[5].ToDouble(out x) || x < 0.0)
                        throw new ENException(ErrorCode.Err202);
                }

                if (x != Constants.MISSING && linkRef.getType() == Link.LinkType.GPV)
                    throw new ENException(ErrorCode.Err202);

                if (x != Constants.MISSING && linkRef.getType() == Link.LinkType.PIPE) {
                    s = x == 0.0 ? Rule.Values.IS_CLOSED : Rule.Values.IS_OPEN;
                    x = Constants.MISSING;
                }

                this.link = linkRef;
                this.status = s;
                this.setting = x;
            }

            public readonly SimulationLink link ;
            private readonly Rule.Values status ;
            private readonly double setting;

            public SimulationLink getLink()
            {
                return this.link;
            }

            public Rule.Values getStatus()
            {
                return this.status;
            }

            public double getSetting()
            {
                return this.setting;
            }

            // Execute action, returns true if the link was alterated.
            public bool execute(FieldsMap fMap, PropertiesMap pMap, TraceSource log, double tol, long Htime)
            {
                bool flag = false;

                Link.StatType s = this.link.getSimStatus();
                double v = this.link.getSimSetting();
                double x = this.setting;

                if (this.status == Rule.Values.IS_OPEN && s <= Link.StatType.CLOSED)
                {
                    // Switch link from closed to open
                    this.link.setLinkStatus(true);
                    flag = true;
                }
                else if (this.status == Rule.Values.IS_CLOSED && s > Link.StatType.CLOSED)
                {
                    // Switch link from not closed to closed
                    this.link.setLinkStatus(false);
                    flag = true;
                }
                else if (x != Constants.MISSING)
                {
                    // Change link's setting
                    switch (this.link.getType())
                    {
                        case Link.LinkType.PRV:
                        case Link.LinkType.PSV:
                        case Link.LinkType.PBV:
                            x = x/fMap.getUnits(FieldsMap.Type.PRESSURE);
                            break;
                        case Link.LinkType.FCV:
                            x = x/fMap.getUnits(FieldsMap.Type.FLOW);
                            break;
                    }
                    if (Math.Abs(x - v) > tol)
                    {
                        this.link.setLinkSetting(x);
                        flag = true;
                    }
                }

                if (flag)
                {
                    if (pMap.getStatflag() != null) // Report rule action
                        this.logRuleExecution(log, Htime);
                    return true;
                }

                return false;
            }

            public void logRuleExecution(TraceSource log, long Htime)
            {
                log.Warning(Utilities.getText("FMT63"), Htime.getClockTime(), this.link.getType().ParseStr(), this.link.getLink().getId(), this._label);
            }
        }


        private readonly string label ;
        private readonly double priority;
        private readonly List<Premise> Pchain ;
        private readonly List<Action> Tchain ;
        private readonly List<Action> Fchain ;

        public string getLabel()
        {
            return this.label;
        }

        public double getPriority()
        {
            return this.priority;
        }

        public Premise[] getPchain()
        {
            return this.Pchain.ToArray();
        }

        public List<Action> getTchain()
        {
            return this.Tchain;
        }

        public List<Action> getFchain()
        {
            return this.Fchain;
        }


        // Simulation Methods


        // Evaluate rule premises.
        private bool evalPremises(FieldsMap fMap, PropertiesMap pMap,
            long Time1, long Htime, double dsystem)
        {
            bool result = true;

            foreach (var p  in  this.getPchain())
            {
                if (p.getLogop() == Rule.Rulewords.r_OR)
                {
                    if (!result)
                        result = p.checkPremise(fMap, pMap, Time1, Htime, dsystem);
                }
                else
                {
                    if (!result)
                        return false;
                    result = p.checkPremise(fMap, pMap, Time1, Htime, dsystem);
                }

            }
            return result;
        }

        // Adds rule's actions to action list
        private static void updateActionList(SimulationRule rule, List<ActItem> actionList, bool branch)
        {
            if (branch)
            {
                // go through the true action branch
                foreach (Action a  in  rule.getTchain())
                {
                    if (!checkAction(rule, a, actionList)) // add a new action from the "true" chain
                        actionList.Add(new ActItem(rule, a));
                }
            }
            else
            {
                foreach (Action a  in  rule.getFchain())
                {
                    if (!checkAction(rule, a, actionList)) // add a new action from the "false" chain
                        actionList.Add(new ActItem(rule, a));
                }
            }
        }

        // Checks if an action with the same link is already on the Action List
        private static bool checkAction(SimulationRule rule, Action action, List<ActItem> actionList)
        {

            foreach (ActItem item  in  actionList)
            {
                if (item.action.link == action.link)
                {
                    // Action with same link
                    if (rule.priority > item.rule.priority)
                    {
                        // Replace Actitem action with higher priority rule
                        item.rule = rule;
                        item.action = action;
                    }

                    return true;
                }
            }

            return false;
        }

        // Implements actions on action list, returns the number of actions executed.
        private static int takeActions(FieldsMap fMap, PropertiesMap pMap, TraceSource log, List<ActItem> actionList,
            long htime)
        {
            double tol = 1e-3;
            int n = 0;

            foreach (ActItem item  in  actionList)
            {
                if (item.action.execute(fMap, pMap, log, tol, htime))
                    n++;
            }

            return n;
        }


        // Checks which rules should fire at current time.
        private static int check(FieldsMap fMap, PropertiesMap pMap, List<SimulationRule> rules, TraceSource log,
            long Htime, long dt, double dsystem)
        {
            // Start of rule evaluation time interval
            long Time1 = Htime - dt + 1;

            List<ActItem> actionList = new List<ActItem>();

            foreach (SimulationRule rule  in  rules)
            updateActionList(rule, actionList, rule.evalPremises(fMap, pMap, Time1, Htime, dsystem));

            return takeActions(fMap, pMap, log, actionList, Htime);
        }

        // updates next time step by checking if any rules will fire before then; also updates tank levels.
        public static Result minimumTimeStep(FieldsMap fMap, PropertiesMap pMap, TraceSource log,
            List<SimulationRule> rules, List<SimulationTank> tanks,
            long Htime, long tstep, double dsystem)
        {
            long tnow,
                // Start of time interval for rule evaluation
                tmax,
                // End of time interval for rule evaluation
                dt,
                // Normal time increment for rule evaluation
                dt1; // Actual time increment for rule evaluation

            // Find interval of time for rule evaluation
            tnow = Htime;
            tmax = tnow + tstep;

            //If no rules, then time increment equals current time step
            if (rules.Count == 0)
            {
                dt = tstep;
                dt1 = dt;
            }
            else
            {
                // Otherwise, time increment equals rule evaluation time step and
                // first actual increment equals time until next even multiple of
                // Rulestep occurs.
                dt = pMap.getRulestep();
                dt1 = pMap.getRulestep() - (tnow%pMap.getRulestep());
            }

            // Make sure time increment is no larger than current time step
            dt = Math.Min(dt, tstep);
            dt1 = Math.Min(dt1, tstep);

            if (dt1 == 0)
                dt1 = dt;

            // Step through time, updating tank levels, until either
            // a rule fires or we reach the end of evaluation period.
            //
            // Note: we are updating the global simulation time (Htime)
            //       here because it is used by functions in RULES.C(this class)
            //       to evaluate rules when checkrules() is called.
            //       It is restored to its original value after the
            //       rule evaluation process is completed (see below).
            //       Also note that dt1 will equal dt after the first
            //       time increment is taken.

            do
            {
                Htime += dt1; // Update simulation clock
                SimulationTank.stepWaterLevels(tanks, fMap, dt1); // Find new tank levels
                if (check(fMap, pMap, rules, log, Htime, dt1, dsystem) != 0) break; // Stop if rules fire
                dt = Math.Min(dt, tmax - Htime); // Update time increment
                dt1 = dt; // Update actual increment
            } while (dt > 0);

            //Compute an updated simulation time step (*tstep)
            // and return simulation time to its original value
            tstep = Htime - tnow;
            Htime = tnow;

            return new Result(tstep, Htime);
        }

        public SimulationRule(Rule _rule, List<SimulationLink> links, List<SimulationNode> nodes)
        {
            this.label = _rule.getLabel();
            this.Pchain = new List<Premise>();
            this.Tchain = new List<Action>();
            this.Fchain = new List<Action>();

            double tempPriority = 0.0;

            Rule.Rulewords ruleState = Rule.Rulewords.r_RULE;
            foreach (string _line  in  _rule.getCode().Split('\n'))
            {
                string[] tok = _line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                Rule.Rulewords key;

                if(!EnumsTxt.TryParse(tok[0], out key))
                    throw new ENException(ErrorCode.Err201);

                switch (key)
                {
                    case Rule.Rulewords.r_IF:
                        if (ruleState != Rule.Rulewords.r_RULE)
                            throw new ENException(ErrorCode.Err221);
                        ruleState = Rule.Rulewords.r_IF;
                    this.parsePremise(tok, Rule.Rulewords.r_AND, nodes, links);
                        break;

                    case Rule.Rulewords.r_AND:
                        if (ruleState == Rule.Rulewords.r_IF)
                            this.parsePremise(tok, Rule.Rulewords.r_AND, nodes, links);
                        else if (ruleState == Rule.Rulewords.r_THEN || ruleState == Rule.Rulewords.r_ELSE)
                            this.parseAction(ruleState, tok, links);
                        else
                            throw new ENException(ErrorCode.Err221);
                        break;

                    case Rule.Rulewords.r_OR:
                        if (ruleState == Rule.Rulewords.r_IF)
                            this.parsePremise(tok, Rule.Rulewords.r_OR, nodes, links);
                        else
                            throw new ENException(ErrorCode.Err221);
                        break;

                    case Rule.Rulewords.r_THEN:
                        if (ruleState != Rule.Rulewords.r_IF)
                            throw new ENException(ErrorCode.Err221);
                        ruleState = Rule.Rulewords.r_THEN;
                    this.parseAction(ruleState, tok, links);
                        break;

                    case Rule.Rulewords.r_ELSE:
                        if (ruleState != Rule.Rulewords.r_THEN)
                            throw new ENException(ErrorCode.Err221);
                        ruleState = Rule.Rulewords.r_ELSE;
                    this.parseAction(ruleState, tok, links);
                        break;

                    case Rule.Rulewords.r_PRIORITY:
                    {
                        if (ruleState != Rule.Rulewords.r_THEN && ruleState != Rule.Rulewords.r_ELSE)
                            throw new ENException(ErrorCode.Err221);

                        ruleState = Rule.Rulewords.r_PRIORITY;

                        if (!tok[1].ToDouble(out tempPriority))
                            throw new ENException(ErrorCode.Err202);

                        break;
                    }

                    default:
                        throw new ENException(ErrorCode.Err201);
                }
            }

            this.priority = tempPriority;
        }

        protected void parsePremise(string[] Tok, Rule.Rulewords logop, List<SimulationNode> nodes,
            List<SimulationLink> links)
        {
            Premise p = new Premise(Tok, logop, nodes, links);
            this.Pchain.Add(p);

        }

        protected void parseAction(Rule.Rulewords state, string[] tok, List<SimulationLink> links)
        {
            Action a = new Action(tok, links, this.label);

            if (state == Rule.Rulewords.r_THEN)
                this.Tchain.Insert(0, a);
            else
                this.Fchain.Insert(0, a);
        }

    }
}