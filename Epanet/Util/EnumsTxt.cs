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

using org.addition.epanet.network;
using org.addition.epanet.network.io;
using org.addition.epanet.network.structures;

namespace org.addition.epanet.util {
    public static class EnumsTxt {

        public static string ParseStr(this Link.LinkType value) {
            switch (value) {
                case Link.LinkType.CV:    return Keywords.w_CV;
                case Link.LinkType.PIPE:  return Keywords.w_PIPE;
                case Link.LinkType.PUMP:  return Keywords.w_PUMP;
                case Link.LinkType.PRV:   return Keywords.w_PRV;
                case Link.LinkType.PSV:   return Keywords.w_PSV;
                case Link.LinkType.PBV:   return Keywords.w_PBV;
                case Link.LinkType.FCV:   return Keywords.w_FCV;
                case Link.LinkType.TCV:   return Keywords.w_TCV;
                case Link.LinkType.GPV:   return Keywords.w_GPV;
                default:                  return null;
            }
        }

        public static bool TryParse(string text, out Rule.Varwords result)
        {
            
            if (text.match(Keywords.wr_DEMAND)) result = Rule.Varwords.r_DEMAND;
            else if (text.match(Keywords.wr_HEAD)) result = Rule.Varwords.r_HEAD;
            else if (text.match(Keywords.wr_GRADE)) result = Rule.Varwords.r_GRADE;
            else if (text.match(Keywords.wr_LEVEL)) result = Rule.Varwords.r_LEVEL;
            else if (text.match(Keywords.wr_PRESSURE)) result = Rule.Varwords.r_PRESSURE;
            else if (text.match(Keywords.wr_FLOW)) result = Rule.Varwords.r_FLOW;
            else if (text.match(Keywords.wr_STATUS)) result = Rule.Varwords.r_STATUS;
            else if (text.match(Keywords.wr_SETTING)) result = Rule.Varwords.r_SETTING;
            else if (text.match(Keywords.wr_POWER)) result = Rule.Varwords.r_POWER;
            else if (text.match(Keywords.wr_TIME)) result = Rule.Varwords.r_CLOCKTIME;
            else if (text.match(Keywords.wr_CLOCKTIME)) result = Rule.Varwords.r_CLOCKTIME;
            else if (text.match(Keywords.wr_FILLTIME)) result = Rule.Varwords.r_FILLTIME;
            else if (text.match(Keywords.wr_DRAINTIME)) result = Rule.Varwords.r_DRAINTIME;
            else {
                result = (Rule.Varwords)(-1);
                return false;
            }

            return true;
        }

        public static bool TryParse(string text, out Rule.Values result) {
            if (text.match(Keywords.wr_ACTIVE)) result = Rule.Values.IS_ACTIVE;
            else if (text.match(Keywords.wr_CLOSED)) result = Rule.Values.IS_CLOSED;
            // else if (text.match("XXXX")) result = Rule.Values.IS_NUMBER;
            else if (text.match(Keywords.wr_OPEN)) result = Rule.Values.IS_OPEN;
            else {
                result = (Rule.Values)(-1);
                return false;
            }

            return true;
        }

        public static bool TryParse(string text, out Rule.Rulewords result) {
            if (text.Match(Keywords.wr_RULE)) result = Rule.Rulewords.r_RULE;
            else if (text.Match(Keywords.wr_IF)) result = Rule.Rulewords.r_IF;
            else if (text.Match(Keywords.wr_AND)) result = Rule.Rulewords.r_AND;
            else if (text.Match(Keywords.wr_OR)) result = Rule.Rulewords.r_ERROR;
            else if (text.Match(Keywords.wr_THEN)) result = Rule.Rulewords.r_THEN;
            else if (text.Match(Keywords.wr_ELSE)) result = Rule.Rulewords.r_ELSE;
            else if (text.Match(Keywords.wr_PRIORITY)) result = Rule.Rulewords.r_PRIORITY;
            else if (text.Match(string.Empty)) result = Rule.Rulewords.r_ERROR;
            else {
                result = (Rule.Rulewords)(-1);
                return false;
            }

            return true;
        }

        public static bool TryParse(string text, out Rule.Operators result) {
            text = text.Trim().ToUpperInvariant();
            switch (text) {
                case Keywords.wr_ABOVE:
                    result = Rule.Operators.ABOVE;
                    break;
                case Keywords.wr_BELOW:
                    result = Rule.Operators.BELOW;
                    break;
                case "=":
                    result = Rule.Operators.EQ;
                    break;
                case ">=":
                    result = Rule.Operators.GE;
                    break;
                case ">":
                    result = Rule.Operators.GT;
                    break;
                case Keywords.wr_IS:
                    result = Rule.Operators.IS;
                    break;
                case "<=":
                    result = Rule.Operators.LE;
                    break;
                case "<":
                    result = Rule.Operators.LT;
                    break;
                case "<>":
                    result = Rule.Operators.NE;
                    break;
                case Keywords.wr_NOT:
                    result = Rule.Operators.NOT;
                    break;
                default:
                    result = (Rule.Operators)(-1);
                    return false;
            }

            return true;
        }


        public static bool TryParse(string text, out Rule.Objects result) {
            if (text.Match(Keywords.wr_JUNC)) result = Rule.Objects.r_JUNC;
            else if (text.Match(Keywords.wr_RESERV)) result = Rule.Objects.r_RESERV;
            else if (text.Match(Keywords.wr_TANK)) result = Rule.Objects.r_TANK;
            else if (text.Match(Keywords.wr_PIPE)) result = Rule.Objects.r_PIPE;
            else if (text.Match(Keywords.wr_PUMP)) result = Rule.Objects.r_PUMP;
            else if (text.Match(Keywords.wr_VALVE)) result = Rule.Objects.r_VALVE;
            else if (text.Match(Keywords.wr_NODE)) result = Rule.Objects.r_NODE;
            else if (text.Match(Keywords.wr_LINK)) result = Rule.Objects.r_LINK;
            else if (text.Match(Keywords.wr_SYSTEM)) result = Rule.Objects.r_SYSTEM;
            else {
                result = (Rule.Objects)(-1);
                return false;
            }

            return true;
        }

        public static bool TryParse(string text, out Tank.MixType result) {
            if (text.Match(Keywords.w_MIXED)) result = Tank.MixType.MIX1;
            else if (text.Match(Keywords.w_2COMP)) result = Tank.MixType.MIX2;
            else if (text.Match(Keywords.w_FIFO)) result = Tank.MixType.FIFO;
            else if (text.Match(Keywords.w_LIFO)) result=Tank.MixType.LIFO;
            else {
                result =(Tank.MixType)(-1);
                return false;
            }

            return true;
        }


        public static string ParseStr(this Tank.MixType value) {
            switch (value) {
                case Tank.MixType.FIFO: return Keywords.w_FIFO;
                case Tank.MixType.LIFO: return Keywords.w_LIFO;
                case Tank.MixType.MIX1: return Keywords.w_MIXED;
                case Tank.MixType.MIX2: return Keywords.w_2COMP;
                default:                return null;
            }
        }


        public static bool TryParse(string text, out FieldsMap.Type result) {
            if (text.Match(Keywords.t_ELEV)) result = FieldsMap.Type.ELEV;
            else if (text.Match(Keywords.t_DEMAND)) result = FieldsMap.Type.DEMAND;
            else if (text.Match(Keywords.t_HEAD)) result = FieldsMap.Type.HEAD;
            else if (text.Match(Keywords.t_PRESSURE)) result = FieldsMap.Type.PRESSURE;
            else if (text.Match(Keywords.t_QUALITY)) result = FieldsMap.Type.QUALITY;
            else if (text.Match(Keywords.t_LENGTH)) result = FieldsMap.Type.LENGTH;
            else if (text.Match(Keywords.t_DIAM)) result = FieldsMap.Type.DIAM;
            else if (text.Match(Keywords.t_FLOW)) result = FieldsMap.Type.FLOW;
            else if (text.Match(Keywords.t_VELOCITY)) result = FieldsMap.Type.VELOCITY;
            else if (text.Match(Keywords.t_HEADLOSS)) result = FieldsMap.Type.HEADLOSS;
            else if (text.Match(Keywords.t_LINKQUAL)) result = FieldsMap.Type.LINKQUAL;
            else if (text.Match(Keywords.t_STATUS)) result = FieldsMap.Type.STATUS;
            else if (text.Match(Keywords.t_SETTING)) result = FieldsMap.Type.SETTING;
            else if (text.Match(Keywords.t_REACTRATE)) result = FieldsMap.Type.REACTRATE;
            else if (text.Match(Keywords.t_FRICTION)) result = FieldsMap.Type.FRICTION;
            else {
                result = (FieldsMap.Type)(-1);
                return false;
            }

            return true;
        }

        public static bool TryParse(string text, out Source.Type result) {
            if (text.Match(Keywords.w_CONCEN)) result = Source.Type.CONCEN;
            else if (text.Match(Keywords.w_FLOWPACED)) result = Source.Type.FLOWPACED;
            else if (text.Match(Keywords.w_MASS)) result = Source.Type.MASS;
            else if (text.Match(Keywords.w_SETPOINT)) result = Source.Type.SETPOINT;
            else {
                result = (Source.Type)(-1);
                return false;
            }

            return true;
        }

        public static string ParseStr(this Source.Type value) {
            switch (value) {
                case Source.Type.CONCEN:    return Keywords.w_CONCEN;
                case Source.Type.FLOWPACED: return Keywords.w_FLOWPACED;
                case Source.Type.MASS:      return Keywords.w_MASS;
                case Source.Type.SETPOINT:  return Keywords.w_SETPOINT;
                default:                    return null;
            }
        }

        public static string ReportStr(this Link.StatType value) {
            switch (value) {
                case Link.StatType.XHEAD:       return Keywords.t_XHEAD;
                case Link.StatType.TEMPCLOSED:  return Keywords.t_TEMPCLOSED;
                case Link.StatType.CLOSED:      return Keywords.t_CLOSED;
                case Link.StatType.OPEN:        return Keywords.t_OPEN;
                case Link.StatType.ACTIVE:      return Keywords.t_ACTIVE;
                case Link.StatType.XFLOW:       return Keywords.t_XFLOW;
                case Link.StatType.XFCV:        return Keywords.t_XFCV;
                case Link.StatType.XPRESSURE:   return Keywords.t_XPRESSURE;
                case Link.StatType.FILLING:     return Keywords.t_FILLING;
                case Link.StatType.EMPTYING:    return Keywords.t_EMPTYING;
                default:                        return null;
            }
        }

        public static string ParseStr(this Link.StatType value) {
            switch (value) {
                case Link.StatType.ACTIVE:     return Keywords.w_ACTIVE;
                case Link.StatType.CLOSED:     return Keywords.w_CLOSED;
                case Link.StatType.EMPTYING:   return string.Empty;
                case Link.StatType.FILLING:    return string.Empty;
                case Link.StatType.OPEN:       return Keywords.w_OPEN;
                case Link.StatType.TEMPCLOSED: return string.Empty;
                case Link.StatType.XFCV:       return string.Empty;
                case Link.StatType.XFLOW:      return string.Empty;
                case Link.StatType.XHEAD:      return string.Empty;
                case Link.StatType.XPRESSURE:  return string.Empty;
                default:                       return null;
            }
        }

        public static string ParseStr(this Control.ControlType value) {
            switch (value) {
                case Control.ControlType.HILEVEL:   return Keywords.w_ABOVE;
                case Control.ControlType.LOWLEVEL:  return Keywords.w_BELOW;
                case Control.ControlType.TIMEOFDAY: return Keywords.w_CLOCKTIME;
                case Control.ControlType.TIMER:     return Keywords.w_TIME;
                default:                            return null;
            }
        }

        public static string ParseStr(this FieldsMap.Type value) {
            switch (value) {
                case FieldsMap.Type.ELEV:      return Keywords.t_ELEV;
                case FieldsMap.Type.DEMAND:    return Keywords.t_DEMAND;
                case FieldsMap.Type.HEAD:      return Keywords.t_HEAD;
                case FieldsMap.Type.PRESSURE:  return Keywords.t_PRESSURE;
                case FieldsMap.Type.QUALITY:   return Keywords.t_QUALITY;
                case FieldsMap.Type.LENGTH:    return Keywords.t_LENGTH;
                case FieldsMap.Type.DIAM:      return Keywords.t_DIAM;
                case FieldsMap.Type.FLOW:      return Keywords.t_FLOW;
                case FieldsMap.Type.VELOCITY:  return Keywords.t_VELOCITY;
                case FieldsMap.Type.HEADLOSS:  return Keywords.t_HEADLOSS;
                case FieldsMap.Type.LINKQUAL:  return Keywords.t_LINKQUAL;
                case FieldsMap.Type.STATUS:    return Keywords.t_STATUS;
                case FieldsMap.Type.SETTING:   return Keywords.t_SETTING;
                case FieldsMap.Type.REACTRATE: return Keywords.t_REACTRATE;
                case FieldsMap.Type.FRICTION:  return Keywords.t_FRICTION;
                default:                       return null;
            }
        }

        public static bool TryParse(string text, out PropertiesMap.FlowUnitsType result) {
            if (text.Match(Keywords.w_CFS)) result = PropertiesMap.FlowUnitsType.CFS;
            else if (text.Match(Keywords.w_GPM)) result = PropertiesMap.FlowUnitsType.GPM;
            else if (text.Match(Keywords.w_MGD)) result = PropertiesMap.FlowUnitsType.MGD;
            else if (text.Match(Keywords.w_IMGD)) result = PropertiesMap.FlowUnitsType.IMGD;
            else if (text.Match(Keywords.w_AFD)) result = PropertiesMap.FlowUnitsType.AFD;
            else if (text.Match(Keywords.w_LPS)) result = PropertiesMap.FlowUnitsType.LPS;
            else if (text.Match(Keywords.w_LPM)) result = PropertiesMap.FlowUnitsType.LPM;
            else if (text.Match(Keywords.w_MLD)) result = PropertiesMap.FlowUnitsType.MLD;
            else if (text.Match(Keywords.w_CMH)) result = PropertiesMap.FlowUnitsType.CMH;
            else if (text.Match(Keywords.w_CMD)) result = PropertiesMap.FlowUnitsType.CMD;
            else {
                result = (PropertiesMap.FlowUnitsType)(-1);
                return false;
            }

            return true;
        }

        public static string ParseStr(this PropertiesMap.FlowUnitsType value) {
            switch (value) {
            case PropertiesMap.FlowUnitsType.AFD:   return Keywords.w_AFD;   
            case PropertiesMap.FlowUnitsType.CFS:   return Keywords.w_CFS;   
            case PropertiesMap.FlowUnitsType.CMD:   return Keywords.w_CMD;   
            case PropertiesMap.FlowUnitsType.CMH:   return Keywords.w_CMH;   
            case PropertiesMap.FlowUnitsType.GPM:   return Keywords.w_GPM;   
            case PropertiesMap.FlowUnitsType.IMGD:  return Keywords.w_IMGD; 
            case PropertiesMap.FlowUnitsType.LPM:   return Keywords.w_LPM;   
            case PropertiesMap.FlowUnitsType.LPS:   return Keywords.w_LPS;   
            case PropertiesMap.FlowUnitsType.MGD:   return Keywords.w_MGD;   
            case PropertiesMap.FlowUnitsType.MLD:   return Keywords.w_MLD;
            default:                                return null;
            }
        }

        /// <summary>Parse string id.</summary>
        public static string ParseStr(this PropertiesMap.FormType value) {
            switch (value) {
                case PropertiesMap.FormType.CM: return Keywords.w_CM;
                case PropertiesMap.FormType.DW: return Keywords.w_DW;
                case PropertiesMap.FormType.HW: return Keywords.w_HW;
                default:                        return null;
            }
        }

        public static bool TryParse(string text, out PropertiesMap.QualType result) {
            if (text.Match(Keywords.w_NONE)) result = PropertiesMap.QualType.NONE;
            else if (text.Match(Keywords.w_CHEM)) result = PropertiesMap.QualType.CHEM;
            else if (text.Match(Keywords.w_AGE)) result = PropertiesMap.QualType.AGE;
            else if (text.Match(Keywords.w_TRACE)) result = PropertiesMap.QualType.TRACE;
            else {
                result =(PropertiesMap.QualType)(-1);
                return false;
            }

            return true;
        }

        public static bool TryParse(string text, out PropertiesMap.StatFlag result) {
            if (text.Match(Keywords.w_NO)) result= PropertiesMap.StatFlag.NO;
            else if (text.Match(Keywords.w_FULL)) result = PropertiesMap.StatFlag.FULL;
            else if (text.Match(Keywords.w_YES)) result = PropertiesMap.StatFlag.YES;
            else {
                result=(PropertiesMap.StatFlag)(-1);
                return false;
            }

            return true;
        }

        public static string ParseStr(this PropertiesMap.TstatType value) {
            switch (value) {
            case PropertiesMap.TstatType.AVG:       return Keywords.w_AVG;
            case PropertiesMap.TstatType.MAX:       return Keywords.w_MAX;
            case PropertiesMap.TstatType.MIN:       return Keywords.w_MIN;
            case PropertiesMap.TstatType.RANGE:     return Keywords.w_RANGE;
            case PropertiesMap.TstatType.SERIES:    return Keywords.w_NONE;
            default:                                return null;
            }
            
        }

        public static bool TryParse(string text, out PropertiesMap.TstatType result) {
            if (text.Match(Keywords.w_NONE)) result = PropertiesMap.TstatType.SERIES;
            else if (text.Match(Keywords.w_NO)) result = PropertiesMap.TstatType.SERIES;
            else if (text.Match(Keywords.w_AVG)) result = PropertiesMap.TstatType.AVG;
            else if (text.Match(Keywords.w_MIN)) result = PropertiesMap.TstatType.MIN;
            else if (text.Match(Keywords.w_MAX)) result = PropertiesMap.TstatType.MAX;
            else if (text.Match(Keywords.w_RANGE)) result = PropertiesMap.TstatType.RANGE;
            else {
                result = (PropertiesMap.TstatType)(-1);
                return false;
            }

            return true;
        }

        public static bool TryParse(string text, out Field.RangeType result) {
            if (text.Match(Keywords.w_BELOW)) result = Field.RangeType.LOW;
            else if (text.Match(Keywords.w_ABOVE)) result = Field.RangeType.HI;
            else if (text.Match(Keywords.w_PRECISION)) result = Field.RangeType.PREC;
            else {
                result = (Field.RangeType)(-1);
                return false;
            }

            return true;

        }

        public static bool TryParse(string text, out Link.LinkType result) {
            if (text.Match(Keywords.w_PRV)) result = Link.LinkType.PRV;
            else if (text.Match(Keywords.w_PSV)) result = Link.LinkType.PSV;
            else if (text.Match(Keywords.w_PBV)) result = Link.LinkType.PBV;
            else if (text.Match(Keywords.w_FCV)) result = Link.LinkType.FCV;
            else if (text.Match(Keywords.w_TCV)) result = Link.LinkType.TCV;
            else if (text.Match(Keywords.w_GPV)) result = Link.LinkType.GPV;
            else {
                result = (Link.LinkType)(-1);
                return false;
            }

            return true;
        }


        public static string parseStr(this Network.SectType value)
        {
            if (value < Network.SectType.TITLE || value > Network.SectType.END) {
                // throw new System.ArgumentOutOfRangeException("value");
                return null;
            }

            return "[" + value + "]";
        }
        
        public static string reportStr(this Network.SectType value) {
            switch (value) {        
                case Network.SectType.BACKDROP:    return Keywords.t_BACKDROP;
                case Network.SectType.CONTROLS:    return Keywords.t_CONTROL;
                case Network.SectType.COORDINATES: return Keywords.t_COORD;
                case Network.SectType.CURVES:      return Keywords.t_CURVE;
                case Network.SectType.DEMANDS:     return Keywords.t_DEMAND;
                case Network.SectType.EMITTERS:    return Keywords.t_EMITTER;
                case Network.SectType.END:         return Keywords.t_END;
                case Network.SectType.ENERGY:      return Keywords.t_ENERGY;
                case Network.SectType.JUNCTIONS:   return Keywords.t_JUNCTION;
                case Network.SectType.LABELS:      return Keywords.t_LABEL;
                case Network.SectType.MIXING:      return Keywords.t_MIXING;
                case Network.SectType.OPTIONS:     return Keywords.t_OPTION;
                case Network.SectType.PATTERNS:    return Keywords.t_PATTERN;
                case Network.SectType.PIPES:       return Keywords.t_PIPE;
                case Network.SectType.PUMPS:       return Keywords.t_PUMP;
                case Network.SectType.QUALITY:     return Keywords.t_QUALITY;
                case Network.SectType.REACTIONS:   return Keywords.t_REACTION;
                case Network.SectType.REPORT:      return Keywords.t_REPORT;
                case Network.SectType.RESERVOIRS:  return Keywords.t_RESERVOIR;
                case Network.SectType.ROUGHNESS:   return Keywords.t_ROUGHNESS;
                case Network.SectType.RULES:       return Keywords.t_RULE;
                case Network.SectType.SOURCES:     return Keywords.t_SOURCE;
                case Network.SectType.STATUS:      return Keywords.t_STATUS;
                case Network.SectType.TAGS:        return Keywords.t_TAG;
                case Network.SectType.TANKS:       return Keywords.t_TANK;
                case Network.SectType.TIMES:       return Keywords.t_TIME;
                case Network.SectType.TITLE:       return Keywords.t_TITLE;
                case Network.SectType.VALVES:      return Keywords.t_VALVE;
                case Network.SectType.VERTICES:    return Keywords.t_VERTICE;
                default:                           return null;
            }
        }

        public static bool TryParse(string text, out Network.SectType result) {

            if (text.Match(Keywords.s_BACKDROP)) { result = Network.SectType.BACKDROP; }
            else if (text.Match(Keywords.s_CONTROLS)) { result = Network.SectType.CONTROLS; }
            else if (text.Match(Keywords.s_COORDS)) { result = Network.SectType.COORDINATES; }
            else if (text.Match(Keywords.s_CURVES)) { result = Network.SectType.CURVES; }
            else if (text.Match(Keywords.s_DEMANDS)) { result = Network.SectType.DEMANDS; }
            else if (text.Match(Keywords.s_EMITTERS)) { result = Network.SectType.EMITTERS; }
            else if (text.Match(Keywords.s_END)) { result = Network.SectType.END; }
            else if (text.Match(Keywords.s_ENERGY)) { result = Network.SectType.ENERGY; }
            else if (text.Match(Keywords.s_JUNCTIONS)) { result = Network.SectType.JUNCTIONS; }
            else if (text.Match(Keywords.s_LABELS)) { result = Network.SectType.LABELS; }
            else if (text.Match(Keywords.s_MIXING)) { result = Network.SectType.MIXING; }
            else if (text.Match(Keywords.s_OPTIONS)) { result = Network.SectType.OPTIONS; }
            else if (text.Match(Keywords.s_PATTERNS)) { result = Network.SectType.PATTERNS; }
            else if (text.Match(Keywords.s_PIPES)) { result = Network.SectType.PIPES; }
            else if (text.Match(Keywords.s_PUMPS)) { result = Network.SectType.PUMPS; }
            else if (text.Match(Keywords.s_QUALITY)) { result = Network.SectType.QUALITY; }
            else if (text.Match(Keywords.s_REACTIONS)) { result = Network.SectType.REACTIONS; }
            else if (text.Match(Keywords.s_REPORT)) { result = Network.SectType.REPORT; }
            else if (text.Match(Keywords.s_RESERVOIRS)) { result = Network.SectType.RESERVOIRS; }
            else if (text.Match(Keywords.s_ROUGHNESS)) { result = Network.SectType.ROUGHNESS; }
            else if (text.Match(Keywords.s_RULES)) { result = Network.SectType.RULES; }
            else if (text.Match(Keywords.s_SOURCES)) { result = Network.SectType.SOURCES; }
            else if (text.Match(Keywords.s_STATUS)) { result = Network.SectType.STATUS; }
            else if (text.Match(Keywords.s_TAGS)) { result = Network.SectType.TAGS; }
            else if (text.Match(Keywords.s_TANKS)) { result = Network.SectType.TANKS; }
            else if (text.Match(Keywords.s_TIMES)) { result = Network.SectType.TIMES; }
            else if (text.Match(Keywords.s_TITLE)) { result = Network.SectType.TITLE; }
            else if (text.Match(Keywords.s_VALVES)) { result = Network.SectType.VALVES; }
            else if (text.Match(Keywords.s_VERTICES)) { result = Network.SectType.VERTICES; }
            else {
                result = (Network.SectType)(-1);
                return false;
            }

            return true;

        }

        public static string ParseStr(this PropertiesMap.PressUnitsType value) {
            switch (value) {
                case PropertiesMap.PressUnitsType.KPA :   return Keywords.w_KPA;
                case PropertiesMap.PressUnitsType.METERS: return Keywords.w_METERS;
                case PropertiesMap.PressUnitsType.PSI:    return Keywords.w_PSI;
                default:                                  return null;
            }
        }
    }



}
