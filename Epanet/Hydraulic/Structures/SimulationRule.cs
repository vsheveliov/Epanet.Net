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
using System.Linq;

using Epanet.Enums;
using Epanet.Log;
using Epanet.Network;
using Epanet.Network.Structures;
using Epanet.Util;

using EpanetNetwork = Epanet.Network.Network;

namespace Epanet.Hydraulic.Structures {

    public class SimulationRule {

        /// <summary>Temporary action item</summary>
        private class ActItem {
            public ActItem(SimulationRule rule, Action action) {
                this.Rule = rule;
                this.Action = action;
            }

            public SimulationRule Rule;
            public Action Action;
        }

        /// <summary>Rule premise.</summary>
        private class Premise {
            public Premise(string[] tok, Rulewords lOp, IEnumerable<SimulationNode> nodes, IEnumerable<SimulationLink> links) {
                Objects loType;
                Varwords lVar;
                object lObj;
                Operators lROp;

                if (tok.Length != 5 && tok.Length != 6)
                    throw new ENException(ErrorCode.Err201);

                EnumsTxt.TryParse(tok[1], out loType);

                if (loType == Objects.SYSTEM) {
                    EnumsTxt.TryParse(tok[2], out lVar);

                    switch (lVar) {
                    case Varwords.DEMAND:
                    case Varwords.TIME:
                    case Varwords.CLOCKTIME:
                        lObj = Objects.SYSTEM;
                        break;
                    default:
                        throw new ENException(ErrorCode.Err201);
                    }
                }
                else {
                    if (!EnumsTxt.TryParse(tok[3], out lVar))
                        throw new ENException(ErrorCode.Err201);

                    switch (loType) {
                    case Objects.NODE:
                    case Objects.JUNC:
                    case Objects.RESERV:
                    case Objects.TANK:
                        loType = Objects.NODE;
                        break;
                    case Objects.LINK:
                    case Objects.PIPE:
                    case Objects.PUMP:
                    case Objects.VALVE:
                        loType = Objects.LINK;
                        break;
                    default:
                        throw new ENException(ErrorCode.Err201);
                    }

                    if (loType == Objects.NODE) {
                        //Node nodeRef = net.getNode(Tok[2]);
                        SimulationNode nodeRef = nodes.FirstOrDefault(simNode => simNode.Node.Id.Equals(tok[2], StringComparison.OrdinalIgnoreCase));

                        if (nodeRef == null)
                            throw new ENException(ErrorCode.Err203);

                        switch (lVar) {
                        case Varwords.DEMAND:
                        case Varwords.HEAD:
                        case Varwords.GRADE:
                        case Varwords.LEVEL:
                        case Varwords.PRESSURE:
                            break;
                        case Varwords.FILLTIME:
                        case Varwords.DRAINTIME:
                            if (nodeRef is SimulationTank)
                                throw new ENException(ErrorCode.Err201);
                            break;

                        default:
                            throw new ENException(ErrorCode.Err201);
                        }
                        lObj = nodeRef;
                    }
                    else {
                        //Link linkRef = net.getLink(Tok[2]);
                        SimulationLink linkRef = links.FirstOrDefault(simLink => simLink.Link.Id.Equals(tok[2], StringComparison.OrdinalIgnoreCase));

                        if (linkRef == null)
                            throw new ENException(ErrorCode.Err204);

                        switch (lVar) {
                        case Varwords.FLOW:
                        case Varwords.STATUS:
                        case Varwords.SETTING:
                            break;
                        default:
                            throw new ENException(ErrorCode.Err201);
                        }
                        lObj = linkRef;
                    }
                }

                Operators op;

                if (!EnumsTxt.TryParse(loType == Objects.SYSTEM ? tok[3] : tok[4], out op))
                    throw new ENException(ErrorCode.Err201);

                switch (op) {
                case Operators.IS:
                    lROp = Operators.EQ;
                    break;
                case Operators.NOT:
                    lROp = Operators.NE;
                    break;
                case Operators.BELOW:
                    lROp = Operators.LT;
                    break;
                case Operators.ABOVE:
                    lROp = Operators.GT;
                    break;
                default:
                    lROp = op;
                    break;
                }

                // BUG: Baseform bug lStat == Rule.Values.IS_NUMBER
                Values lStat = Values.IS_NUMBER;
                double lVal = Constants.MISSING;

                if (lVar == Varwords.TIME || lVar == Varwords.CLOCKTIME) {
                    lVal = tok.Length == 6
                        ? Utilities.GetHour(tok[4], tok[5]) 
                        : Utilities.GetHour(tok[4]);

                    lVal *= 3600;

                    if (lVal < 0.0)
                        throw new ENException(ErrorCode.Err202);
                }
                else {
                    Values k;

                    if (!EnumsTxt.TryParse(tok[tok.Length - 1], out k) || lStat <= Values.IS_NUMBER) {
                        if (lStat == (Values)(-1) || lStat <= Values.IS_NUMBER) {
                            if (!tok[tok.Length - 1].ToDouble(out lVal))
                                throw new ENException(ErrorCode.Err202);

                            if (lVar == Varwords.FILLTIME || lVar == Varwords.DRAINTIME)
                                lVal *= 3600.0;
                        }
                    }
                    else {
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
            public readonly Rulewords logop;
            private readonly object @object;
            /// <summary>Pressure, flow, etc</summary>
            private readonly Varwords variable;
            /// <summary>Relational operator</summary>
            private readonly Operators relop;
            /// <summary>Variable's status</summary>
            private readonly Values status;
            /// <summary>Variable's value</summary>
            private readonly double value;

            /// <summary>Checks if a particular premise is true.</summary>
            public bool CheckPremise(
                EpanetNetwork net,
                long time1,
                long htime,
                double dsystem) 
            {
                if (this.variable == Varwords.TIME || this.variable == Varwords.CLOCKTIME)
                    return this.CheckTime(net, time1, htime);
                else if (this.status > Values.IS_NUMBER)
                    return this.CheckStatus();
                else
                    return this.CheckValue(net.FieldsMap, dsystem);
            }

            /// <summary>Checks if condition on system time holds.</summary>
            private bool CheckTime(EpanetNetwork net, long time1, long htime) {
                long t1, t2;

                if (this.variable == Varwords.TIME) {
                    t1 = time1;
                    t2 = htime;
                }
                else if (this.variable == Varwords.CLOCKTIME) {
                    t1 = (time1 + net.TStart) % Constants.SECperDAY;
                    t2 = (htime + net.TStart) % Constants.SECperDAY;
                }
                else
                    return false;

                var x = (long)this.value;
                switch (this.relop) {
                case Operators.LT:
                    if (t2 >= x) return false;
                    break;
                case Operators.LE:
                    if (t2 > x) return false;
                    break;
                case Operators.GT:
                    if (t2 <= x) return false;
                    break;
                case Operators.GE:
                    if (t2 < x) return false;
                    break;

                case Operators.EQ:
                case Operators.NE:
                    var flag = false;
                    if (t2 < t1) {
                        if (x >= t1 || x <= t2) flag = true;
                    }
                    else {
                        if (x >= t1 && x <= t2) flag = true;
                    }
                    if (this.relop == Operators.EQ && !flag) return true;
                    if (this.relop == Operators.NE && flag) return true;
                    break;
                }

                return true;
            }

            /// <summary>Checks if condition on link status holds.</summary>
            private bool CheckStatus() {
                switch (this.status) {
                case Values.IS_OPEN:
                case Values.IS_CLOSED:
                case Values.IS_ACTIVE:
                    Values j;
                    var simlink = this.@object as SimulationLink;
                    StatType i = simlink == null ? (StatType)(-1) : simlink.SimStatus;

                    if(i >= StatType.XHEAD && i <= StatType.CLOSED)
                        j = Values.IS_CLOSED;
                    else if (i == StatType.ACTIVE)
                        j = Values.IS_ACTIVE;
                    else
                        j = Values.IS_OPEN;

                    if (j == this.status && this.relop == Operators.EQ)
                        return true;
                    if (j != this.status && this.relop == Operators.NE)
                        return true;
                    break;
                }
                return false;
            }

            /// <summary>Checks if numerical condition on a variable is true.</summary>
            private bool CheckValue(FieldsMap fMap, double dsystem) {
                const double tol = 0.001D;
                double x;

                SimulationLink link = this.@object as SimulationLink;
                SimulationNode node = this.@object as SimulationNode;


                switch (this.variable) {
                case Varwords.DEMAND:
                    if ((Objects)this.@object == Objects.SYSTEM)
                        x = dsystem * fMap.GetUnits(FieldType.DEMAND);
                    else
                        x = node.SimDemand * fMap.GetUnits(FieldType.DEMAND);
                    break;

                case Varwords.HEAD:
                case Varwords.GRADE:
                    x = node.SimHead * fMap.GetUnits(FieldType.HEAD);
                    break;

                case Varwords.PRESSURE:
                    x = (node.SimHead - node.Elevation) * fMap.GetUnits(FieldType.PRESSURE);
                    break;

                case Varwords.LEVEL:
                    x = (node.SimHead - node.Elevation) * fMap.GetUnits(FieldType.HEAD);
                    break;

                case Varwords.FLOW:
                    x = Math.Abs(link.SimFlow) * fMap.GetUnits(FieldType.FLOW);
                    break;

                case Varwords.SETTING:

                    if (link.SimSetting.IsMissing())
                        return false;

                    x = link.SimSetting;
                    switch (link.Type) {
                    case LinkType.PRV:
                    case LinkType.PSV:
                    case LinkType.PBV:
                        x = x * fMap.GetUnits(FieldType.PRESSURE);
                        break;
                    case LinkType.FCV:
                        x = x * fMap.GetUnits(FieldType.FLOW);
                        break;
                    }
                    break;
                case Varwords.FILLTIME: {
                    if (!(this.@object is SimulationTank))
                        return false;

                    SimulationTank tank = (SimulationTank)this.@object;

                    if (tank.IsReservoir)
                        return false;

                    if (tank.SimDemand <= Constants.TINY)
                        return false;

                    x = (tank.Vmax - tank.SimVolume) / tank.SimDemand;

                    break;
                }
                case Varwords.DRAINTIME: {
                    if (!(this.@object is SimulationTank))
                        return false;

                    SimulationTank tank = (SimulationTank)this.@object;

                    if (tank.IsReservoir)
                        return false;

                    if (tank.SimDemand >= -Constants.TINY)
                        return false;

                    x = (tank.Vmin - tank.SimVolume) / tank.SimDemand;
                    break;
                }
                default:
                    return false;
                }

                switch (this.relop) {
                case Operators.EQ:
                    if (Math.Abs(x - this.value) > tol)
                        return false;
                    break;
                case Operators.NE:
                    if (Math.Abs(x - this.value) < tol)
                        return false;
                    break;
                case Operators.LT:
                    if (x > this.value + tol)
                        return false;
                    break;
                case Operators.LE:
                    if (x > this.value - tol)
                        return false;
                    break;
                case Operators.GT:
                    if (x < this.value - tol)
                        return false;
                    break;
                case Operators.GE:
                    if (x < this.value + tol)
                        return false;
                    break;
                }
                return true;
            }

        }

        private class Action {
            private readonly string label;

            public Action(string[] tok, IEnumerable<SimulationLink> links, string label) {
                this.label = label;

                int ntokens = tok.Length;

                Values k;

                if (ntokens != 6)
                    throw new ENException(ErrorCode.Err201);

                //Link linkRef = net.getLink(tok[2]);
                SimulationLink linkRef = links.FirstOrDefault(simLink => simLink.Link.Id.Equals(tok[2], StringComparison.OrdinalIgnoreCase));

                if (linkRef == null)
                    throw new ENException(ErrorCode.Err204);

                if (linkRef.Type == LinkType.CV)
                    throw new ENException(ErrorCode.Err207);

                var s = (Values)(-1);
                double x = Constants.MISSING;

                if (EnumsTxt.TryParse(tok[5], out k) && k > Values.IS_NUMBER) {
                    s = k;
                }
                else {
                    if (!tok[5].ToDouble(out x) || x < 0.0)
                        throw new ENException(ErrorCode.Err202);
                }

                if (!x.IsMissing() && linkRef.Type == LinkType.GPV)
                    throw new ENException(ErrorCode.Err202);

                if (!x.IsMissing() && linkRef.Type == LinkType.PIPE) {
                    s = x == 0.0 ? Values.IS_CLOSED : Values.IS_OPEN;
                    x = Constants.MISSING;
                }

                this.link = linkRef;
                this.status = s;
                this.setting = x;
            }

            public readonly SimulationLink link;
            private readonly Values status;
            private readonly double setting;

            /// <summary>Execute action, returns true if the link was alterated.</summary>
            public bool Execute(EpanetNetwork net, TraceSource log, double tol, long htime) {
                bool flag = false;

                StatType s = this.link.SimStatus;
                double v = this.link.SimSetting;
                double x = this.setting;

                if (this.status == Values.IS_OPEN && s <= StatType.CLOSED) {
                    // Switch link from closed to open
                    this.link.SetLinkStatus(true);
                    flag = true;
                }
                else if (this.status == Values.IS_CLOSED && s > StatType.CLOSED) {
                    // Switch link from not closed to closed
                    this.link.SetLinkStatus(false);
                    flag = true;
                }
                else if (!x.IsMissing()) {
                    // Change link's setting
                    switch (this.link.Type) {
                    case LinkType.PRV:
                    case LinkType.PSV:
                    case LinkType.PBV:
                        x = x / net.FieldsMap.GetUnits(FieldType.PRESSURE);
                        break;
                    case LinkType.FCV:
                        x = x / net.FieldsMap.GetUnits(FieldType.FLOW);
                        break;
                    }
                    if (Math.Abs(x - v) > tol) {
                        this.link.SetLinkSetting(x);
                        flag = true;
                    }
                }

                if (flag) {
                    if (net.Stat_Flag > 0) // Report rule action
                        this.LogRuleExecution(log, htime);
                    return true;
                }

                return false;
            }

            private void LogRuleExecution(TraceSource log, long htime) {
                log.Warning(
                    Properties.Text.FMT63,
                    htime.GetClockTime(),
                    this.link.Type.ParseStr(),
                    this.link.Link.Id,
                    this.label);
            }
        }


        private readonly string label;
        private readonly double priority;
        private readonly List<Premise> pchain = new List<Premise>();
        private readonly List<Action> tchain = new List<Action>();
        private readonly List<Action> fchain = new List<Action>();


        // Simulation Methods


        /// <summary>Evaluate rule premises.</summary>
        private bool EvalPremises(
            EpanetNetwork net,
            long time1,
            long htime,
            double dsystem) {
            bool result = true;

            foreach (var p  in  this.pchain) {
                if (p.logop == Rulewords.OR) {
                    if (!result)
                        result = p.CheckPremise(net, time1, htime, dsystem);
                }
                else {
                    if (!result)
                        return false;
                    result = p.CheckPremise(net, time1, htime, dsystem);
                }

            }
            return result;
        }

        /// <summary>Adds rule's actions to action list.</summary>
        private static void UpdateActionList(SimulationRule rule, List<ActItem> actionList, bool branch) {
            if (branch) {
                // go through the true action branch
                foreach (Action a  in  rule.tchain) {
                    if (!CheckAction(rule, a, actionList)) // add a new action from the "true" chain
                        actionList.Add(new ActItem(rule, a));
                }
            }
            else {
                foreach (Action a  in  rule.fchain) {
                    if (!CheckAction(rule, a, actionList)) // add a new action from the "false" chain
                        actionList.Add(new ActItem(rule, a));
                }
            }
        }

        /// <summary>Checks if an action with the same link is already on the Action List.</summary>
        private static bool CheckAction(SimulationRule rule, Action action, List<ActItem> actionList) {

            foreach (ActItem item  in  actionList) {
                if (item.Action.link == action.link) {
                    // Action with same link
                    if (rule.priority > item.Rule.priority) {
                        // Replace Actitem action with higher priority rule
                        item.Rule = rule;
                        item.Action = action;
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>Implements actions on action list, returns the number of actions executed.</summary>
        private static int TakeActions(
            EpanetNetwork net,
            TraceSource log,
            List<ActItem> actionList,
            long htime) {
            double tol = 1e-3;
            int n = 0;

            foreach (ActItem item  in  actionList) {
                if (item.Action.Execute(net, log, tol, htime))
                    n++;
            }

            return n;
        }


        /// <summary>Checks which rules should fire at current time.</summary>
        private static int Check(
            EpanetNetwork net,
            IEnumerable<SimulationRule> rules,
            TraceSource log,
            long htime,
            long dt,
            double dsystem) {
            // Start of rule evaluation time interval
            long time1 = htime - dt + 1;

            List<ActItem> actionList = new List<ActItem>();

            foreach (SimulationRule rule  in  rules)
                UpdateActionList(rule, actionList, rule.EvalPremises(net, time1, htime, dsystem));

            return TakeActions(net, log, actionList, htime);
        }

        /// <summary>
        /// Updates next time step by checking if any rules will fire before then; 
        /// also updates tank levels.
        /// </summary>
        public static void MinimumTimeStep(
            EpanetNetwork net,
            TraceSource log,
            SimulationRule[] rules,
            List<SimulationTank> tanks,
            long htime,
            long tstep,
            double dsystem,
            out long tstepOut,
            out long htimeOut) {

            long dt; // Normal time increment for rule evaluation
            long dt1; // Actual time increment for rule evaluation

            // Find interval of time for rule evaluation
            long tnow = htime;        // Start of time interval for rule evaluation
            long tmax = tnow + tstep; // End of time interval for rule evaluation

            //If no rules, then time increment equals current time step
            if (rules.Length == 0) {
                dt = tstep;
                dt1 = dt;
            }
            else {
                // Otherwise, time increment equals rule evaluation time step and
                // first actual increment equals time until next even multiple of
                // Rulestep occurs.
                dt = net.RuleStep;
                dt1 = net.RuleStep - tnow % net.RuleStep;
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

            do {
                htime += dt1; // Update simulation clock
                SimulationTank.StepWaterLevels(tanks, net.FieldsMap, dt1); // Find new tank levels
                if (Check(net, rules, log, htime, dt1, dsystem) != 0) break; // Stop if rules fire
                dt = Math.Min(dt, tmax - htime); // Update time increment
                dt1 = dt; // Update actual increment
            }
            while (dt > 0);

            //Compute an updated simulation time step (*tstep)
            // and return simulation time to its original value
            tstepOut = htime - tnow;
            htimeOut = tnow;

        }

        public SimulationRule(Rule rule, IList<SimulationLink> links, IList<SimulationNode> nodes) {
            this.label = rule.Label;

            double tempPriority = 0.0;

            Rulewords ruleState = Rulewords.RULE;

            foreach (string line in rule.Code) {
                string[] tok = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                Rulewords key;

                if (!EnumsTxt.TryParse(tok[0], out key))
                    throw new ENException(ErrorCode.Err201);

                switch (key) {
                case Rulewords.IF:
                    if (ruleState != Rulewords.RULE)
                        throw new ENException(ErrorCode.Err221);
                    ruleState = Rulewords.IF;
                    this.ParsePremise(tok, Rulewords.AND, nodes, links);
                    break;

                case Rulewords.AND:
                    switch (ruleState) {
                    case Rulewords.IF:
                        this.ParsePremise(tok, Rulewords.AND, nodes, links);
                        break;
                    case Rulewords.THEN:
                    case Rulewords.ELSE:
                        this.ParseAction(ruleState, tok, links);
                        break;
                    default:
                        throw new ENException(ErrorCode.Err221);
                    }
                    break;

                case Rulewords.OR:
                    if (ruleState == Rulewords.IF)
                        this.ParsePremise(tok, Rulewords.OR, nodes, links);
                    else
                        throw new ENException(ErrorCode.Err221);
                    break;

                case Rulewords.THEN:
                    if (ruleState != Rulewords.IF)
                        throw new ENException(ErrorCode.Err221);
                    ruleState = Rulewords.THEN;
                    this.ParseAction(ruleState, tok, links);
                    break;

                case Rulewords.ELSE:
                    if (ruleState != Rulewords.THEN)
                        throw new ENException(ErrorCode.Err221);
                    ruleState = Rulewords.ELSE;
                    this.ParseAction(ruleState, tok, links);
                    break;

                case Rulewords.PRIORITY: {
                    if (ruleState != Rulewords.THEN && ruleState != Rulewords.ELSE)
                        throw new ENException(ErrorCode.Err221);

                    ruleState = Rulewords.PRIORITY;

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

        private void ParsePremise(
            string[] tok,
            Rulewords logop,
            IEnumerable<SimulationNode> nodes,
            IEnumerable<SimulationLink> links) {
            
            this.pchain.Add(new Premise(tok, logop, nodes, links));

        }

        private void ParseAction(Rulewords state, string[] tok, IEnumerable<SimulationLink> links) {
            Action a = new Action(tok, links, this.label);

            if (state == Rulewords.THEN)
                this.tchain.Insert(0, a);
            else
                this.fchain.Insert(0, a);
        }

    }

}