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

using Epanet.Util;

namespace Epanet.Enums {

    public static class EnumsTxt {

        public static bool TryParse(string text, out Varwords result)
        {
            if (text.Match(Keywords.wr_DEMAND)) result = Varwords.DEMAND;
            else if (text.Match(Keywords.wr_HEAD)) result = Varwords.HEAD;
            else if (text.Match(Keywords.wr_GRADE)) result = Varwords.GRADE;
            else if (text.Match(Keywords.wr_LEVEL)) result = Varwords.LEVEL;
            else if (text.Match(Keywords.wr_PRESSURE)) result = Varwords.PRESSURE;
            else if (text.Match(Keywords.wr_FLOW)) result = Varwords.FLOW;
            else if (text.Match(Keywords.wr_STATUS)) result = Varwords.STATUS;
            else if (text.Match(Keywords.wr_SETTING)) result = Varwords.SETTING;
            else if (text.Match(Keywords.wr_POWER)) result = Varwords.POWER;
            else if (text.Match(Keywords.wr_TIME)) result = Varwords.CLOCKTIME;
            else if (text.Match(Keywords.wr_CLOCKTIME)) result = Varwords.CLOCKTIME;
            else if (text.Match(Keywords.wr_FILLTIME)) result = Varwords.FILLTIME;
            else if (text.Match(Keywords.wr_DRAINTIME)) result = Varwords.DRAINTIME;
            else {
                result = (Varwords)(-1);
                return false;
            }

            return true;
        }


        public static bool TryParse(string text, out Values result)
        {
            if (text.Match(Keywords.wr_ACTIVE)) result = Values.IS_ACTIVE;
            else if (text.Match(Keywords.wr_CLOSED)) result = Values.IS_CLOSED;
            // else if (text.Match("XXXX"))             result = Rule.Values.IS_NUMBER;
            else if (text.Match(Keywords.wr_OPEN)) result = Values.IS_OPEN;
            else {
                result = (Values)(-1);
                return false;
            }

            return true;
        }


        public static bool TryParse(this string text, out Rulewords result) {
            int index = text.FindMatch(
                text,
                Keywords.wr_AND,
                Keywords.wr_ELSE,
                string.Empty,
                Keywords.wr_IF,
                Keywords.wr_OR,
                Keywords.wr_PRIORITY,
                Keywords.wr_RULE,
                Keywords.wr_THEN);

            return (int)(result = (Rulewords)index) != -1;
        }

        public static bool TryParse(this string text, out Operators result) {
            text = text.Trim();
            
            if (text.Equals(Keywords.wr_ABOVE, StringComparison.OrdinalIgnoreCase)) 
                result = Operators.ABOVE;
            else if (text.Equals(Keywords.wr_BELOW, StringComparison.OrdinalIgnoreCase)) 
                result = Operators.BELOW;
            else if (text == "=") 
                result = Operators.EQ;
            else if (text == ">=") 
                result = Operators.GE;
            else if (text == ">") 
                result = Operators.GT;
            else if (text.Equals(Keywords.wr_IS, StringComparison.OrdinalIgnoreCase)) 
                result = Operators.IS;
            else if (text == "<=") 
                result = Operators.LE;
            else if (text == "<") 
                result = Operators.LT;
            else if (text == "<>") 
                result = Operators.NE;
            else if (text.Equals(Keywords.wr_NOT, StringComparison.OrdinalIgnoreCase))
                result = Operators.NOT;
            else {
                result = (Operators)(-1);
                return false;
            }

            return true;
        }

        public static bool TryParse(this string text, out Objects result) {
            if (text.Match(Keywords.wr_JUNC))        result = Objects.JUNC;
            else if (text.Match(Keywords.w_RESERV)) result = Objects.RESERV;
            else if (text.Match(Keywords.w_TANK))   result = Objects.TANK;
            else if (text.Match(Keywords.w_PIPE))   result = Objects.PIPE;
            else if (text.Match(Keywords.w_PUMP))   result = Objects.PUMP;
            else if (text.Match(Keywords.w_VALVE))  result = Objects.VALVE;
            else if (text.Match(Keywords.w_NODE))   result = Objects.NODE;
            else if (text.Match(Keywords.w_LINK))   result = Objects.LINK;
            else if (text.Match(Keywords.w_SYSTEM)) result = Objects.SYSTEM;
            else {
                result = (Objects)(-1);
                return false;
            }

            return true;
        }

        public static bool TryParse(this string text, out MixType result) {
            if (text.Match(Keywords.w_MIXED))      result = MixType.MIX1;
            else if (text.Match(Keywords.w_2COMP)) result = MixType.MIX2;
            else if (text.Match(Keywords.w_FIFO))  result = MixType.FIFO;
            else if (text.Match(Keywords.w_LIFO))  result = MixType.LIFO;
            else {
                result = (MixType)(-1);
                return false;
            }

            return true;
        }

        public static string ParseStr(this MixType value) {
            switch (value) {
            case MixType.FIFO:   return Keywords.w_FIFO;
            case MixType.LIFO:   return Keywords.w_LIFO;
            case MixType.MIX1:   return Keywords.w_MIXED;
            case MixType.MIX2:   return Keywords.w_2COMP;
            default:                  return null;
            }
        }

        public static bool TryParse(this string text, out FieldType result) {
            if (text.Match(Keywords.t_ELEV))           result = FieldType.ELEV;
            else if (text.Match(Keywords.t_DEMAND))    result = FieldType.DEMAND;
            else if (text.Match(Keywords.t_HEAD))      result = FieldType.HEAD;
            else if (text.Match(Keywords.t_PRESSURE))  result = FieldType.PRESSURE;
            else if (text.Match(Keywords.t_QUALITY))   result = FieldType.QUALITY;
            else if (text.Match(Keywords.t_LENGTH))    result = FieldType.LENGTH;
            else if (text.Match(Keywords.t_DIAM))      result = FieldType.DIAM;
            else if (text.Match(Keywords.t_FLOW))      result = FieldType.FLOW;
            else if (text.Match(Keywords.t_VELOCITY))  result = FieldType.VELOCITY;
            else if (text.Match(Keywords.t_HEADLOSS))  result = FieldType.HEADLOSS;
            else if (text.Match(Keywords.t_LINKQUAL))  result = FieldType.LINKQUAL;
            else if (text.Match(Keywords.t_STATUS))    result = FieldType.STATUS;
            else if (text.Match(Keywords.t_SETTING))   result = FieldType.SETTING;
            else if (text.Match(Keywords.t_REACTRATE)) result = FieldType.REACTRATE;
            else if (text.Match(Keywords.t_FRICTION))  result = FieldType.FRICTION;
            else {
                result = (FieldType)(-1);
                return false;
            }

            return true;
        }

        public static bool TryParse(this string text, out SourceType result) {
            if (text.Match(Keywords.w_CONCEN))         result = SourceType.CONCEN;
            else if (text.Match(Keywords.w_FLOWPACED)) result = SourceType.FLOWPACED;
            else if (text.Match(Keywords.w_MASS))      result = SourceType.MASS;
            else if (text.Match(Keywords.w_SETPOINT))  result = SourceType.SETPOINT;
            else {
                result = (SourceType)(-1);
                return false;
            }

            return true;
        }

        public static string ReportStr(this StatType value) {
            switch (value) {
            case StatType.XHEAD:      return Keywords.t_XHEAD;
            case StatType.TEMPCLOSED: return Keywords.t_TEMPCLOSED;
            case StatType.CLOSED:     return Keywords.t_CLOSED;
            case StatType.OPEN:       return Keywords.t_OPEN;
            case StatType.ACTIVE:     return Keywords.t_ACTIVE;
            case StatType.XFLOW:      return Keywords.t_XFLOW;
            case StatType.XFCV:       return Keywords.t_XFCV;
            case StatType.XPRESSURE:  return Keywords.t_XPRESSURE;
            case StatType.FILLING:    return Keywords.t_FILLING;
            case StatType.EMPTYING:   return Keywords.t_EMPTYING;
            default:                       return null;
            }
        }

        public static string ParseStr(this ControlType value) {
            switch (value) {
            case ControlType.HILEVEL:   return Keywords.w_ABOVE;
            case ControlType.LOWLEVEL:  return Keywords.w_BELOW;
            case ControlType.TIMEOFDAY: return Keywords.w_CLOCKTIME;
            case ControlType.TIMER:     return Keywords.w_TIME;
            default:                    return null;
            }
        }

        public static string ParseStr(this FieldType value) {
            switch (value) {
            case FieldType.ELEV:      return Keywords.t_ELEV;
            case FieldType.DEMAND:    return Keywords.t_DEMAND;
            case FieldType.HEAD:      return Keywords.t_HEAD;
            case FieldType.PRESSURE:  return Keywords.t_PRESSURE;
            case FieldType.QUALITY:   return Keywords.t_QUALITY;
            case FieldType.LENGTH:    return Keywords.t_LENGTH;
            case FieldType.DIAM:      return Keywords.t_DIAM;
            case FieldType.FLOW:      return Keywords.t_FLOW;
            case FieldType.VELOCITY:  return Keywords.t_VELOCITY;
            case FieldType.HEADLOSS:  return Keywords.t_HEADLOSS;
            case FieldType.LINKQUAL:  return Keywords.t_LINKQUAL;
            case FieldType.STATUS:    return Keywords.t_STATUS;
            case FieldType.SETTING:   return Keywords.t_SETTING;
            case FieldType.REACTRATE: return Keywords.t_REACTRATE;
            case FieldType.FRICTION:  return Keywords.t_FRICTION;
            default:                  return null;
            }
        }

        public static bool TryParse(this string text, out FlowUnitsType result) {
            if (text.Match(Keywords.w_CFS))       result = FlowUnitsType.CFS;
            else if (text.Match(Keywords.w_GPM))  result = FlowUnitsType.GPM;
            else if (text.Match(Keywords.w_MGD))  result = FlowUnitsType.MGD;
            else if (text.Match(Keywords.w_IMGD)) result = FlowUnitsType.IMGD;
            else if (text.Match(Keywords.w_AFD))  result = FlowUnitsType.AFD;
            else if (text.Match(Keywords.w_LPS))  result = FlowUnitsType.LPS;
            else if (text.Match(Keywords.w_LPM))  result = FlowUnitsType.LPM;
            else if (text.Match(Keywords.w_MLD))  result = FlowUnitsType.MLD;
            else if (text.Match(Keywords.w_CMH))  result = FlowUnitsType.CMH;
            else if (text.Match(Keywords.w_CMD))  result = FlowUnitsType.CMD;
            else {
                result = (FlowUnitsType)(-1);
                return false;
            }

            return true;
        }

        public static string ParseStr(this FlowUnitsType value) {
            switch (value) {
            case FlowUnitsType.AFD:  return Keywords.w_AFD;
            case FlowUnitsType.CFS:  return Keywords.w_CFS;
            case FlowUnitsType.CMD:  return Keywords.w_CMD;
            case FlowUnitsType.CMH:  return Keywords.w_CMH;
            case FlowUnitsType.GPM:  return Keywords.w_GPM;
            case FlowUnitsType.IMGD: return Keywords.w_IMGD;
            case FlowUnitsType.LPM:  return Keywords.w_LPM;
            case FlowUnitsType.LPS:  return Keywords.w_LPS;
            case FlowUnitsType.MGD:  return Keywords.w_MGD;
            case FlowUnitsType.MLD:  return Keywords.w_MLD;
            default:                               return null;
            }
        }

        /// <summary>Parse string id.</summary>
        public static string ParseStr(this FormType value) {
            switch (value) {
            case FormType.CM: return Keywords.w_CM;
            case FormType.DW: return Keywords.w_DW;
            case FormType.HW: return Keywords.w_HW;
            default:                        return null;
            }
        }

        public static bool TryParse(this string text, out QualType result) {
            if (text.Match(Keywords.w_NONE))       result = QualType.NONE;
            else if (text.Match(Keywords.w_CHEM))  result = QualType.CHEM;
            else if (text.Match(Keywords.w_AGE))   result = QualType.AGE;
            else if (text.Match(Keywords.w_TRACE)) result = QualType.TRACE;
            else {
                result = (QualType)(-1);
                return false;
            }

            return true;
        }

        public static bool TryParse(this string text, out StatFlag result) {
            if (text.Match(Keywords.w_NO))        result = StatFlag.NO;
            else if (text.Match(Keywords.w_FULL)) result = StatFlag.FULL;
            else if (text.Match(Keywords.w_YES))  result = StatFlag.YES;
            else {
                result = (StatFlag)(-1);
                return false;
            }

            return true;
        }

        public static string ParseStr(this StatFlag value) {
            switch (value) {
            case StatFlag.NO:      return Keywords.w_NO;
            case StatFlag.FULL:    return Keywords.w_FULL;
            case StatFlag.YES:     return Keywords.w_YES;
            default:                             return null;
            }
        }

        public static string ParseStr(this TimeStatType value) {
            switch (value) {
            case TimeStatType.AVG:    return Keywords.w_AVG;
            case TimeStatType.MAX:    return Keywords.w_MAX;
            case TimeStatType.MIN:    return Keywords.w_MIN;
            case TimeStatType.RANGE:  return Keywords.w_RANGE;
            case TimeStatType.SERIES: return Keywords.w_NONE;
            default:                             return null;
            }
        }
        public static bool TryParse(this string text, out TimeStatType result) {
            if (text.Match(Keywords.w_NONE))       result = TimeStatType.SERIES;
            else if (text.Match(Keywords.w_NO))    result = TimeStatType.SERIES;
            else if (text.Match(Keywords.w_AVG))   result = TimeStatType.AVG;
            else if (text.Match(Keywords.w_MIN))   result = TimeStatType.MIN;
            else if (text.Match(Keywords.w_MAX))   result = TimeStatType.MAX;
            else if (text.Match(Keywords.w_RANGE)) result = TimeStatType.RANGE;
            else {
                result = (TimeStatType)(-1);
                return false;
            }

            return true;
        }

        public static bool TryParse(this string text, out RangeType result) {
            if (text.Match(Keywords.w_BELOW))          result = RangeType.LOW;
            else if (text.Match(Keywords.w_ABOVE))     result = RangeType.HI;
            else if (text.Match(Keywords.w_PRECISION)) result = RangeType.PREC;
            else {
                result = (RangeType)(-1);
                return false;
            }

            return true;
        }

        public static string ParseStr(this SectType value) {
            if (value < SectType.TITLE || value > SectType.END) {
                // throw new System.ArgumentOutOfRangeException("value");
                return null;
            }

            return "[" + value + "]";
        }

        public static string ParseStr(this PressUnitsType value) {
            switch (value) {
            case PressUnitsType.KPA:    return Keywords.w_KPA;
            case PressUnitsType.METERS: return Keywords.w_METERS;
            case PressUnitsType.PSI:    return Keywords.w_PSI;
            default:                                  return null;
            }
        }

        public static bool TryParse(this string text, out PressUnitsType result) {
            if(text.Match(Keywords.w_PSI))         result = PressUnitsType.PSI;
            else if(text.Match(Keywords.w_KPA))    result = PressUnitsType.KPA;
            else if(text.Match(Keywords.w_METERS)) result = PressUnitsType.METERS;
            else {
                result = (PressUnitsType)(-1);
                return false;
            }

            return true;
            
        }

        public static bool TryParse(this string text, out FormType result) {
            if(text.Match(Keywords.w_HW))       result = FormType.HW;
            else if(text.Match(Keywords.w_DW))  result = FormType.DW;
            else if(text.Match(Keywords.w_CM))  result = FormType.CM;
            else {
                result = (FormType)(-1);
                return false;
            }

            return true;
        }

        public static bool TryParse(this string text, out HydType result) {
            if(text.Match(Keywords.w_USE))       result = HydType.USE;
            else if(text.Match(Keywords.w_SAVE)) result = HydType.SAVE;
            else {
                result = (HydType)(-1);
                return false;
            }

            return true;
        }

        public static bool TryParse(this string s, out ValveType result) { throw new NotImplementedException(); }
    }

}
