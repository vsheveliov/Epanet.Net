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
using System.Globalization;
using org.addition.epanet.network.io;

namespace org.addition.epanet.util {

    ///<summary>Epanet utilities methods.</summary>
    public static class Utilities {

        /// <summary>Check if two strings match (case independent), based on the shortest string length.</summary>
        ///  <param name="a">string A.</param>
        /// <param name="b">string B.</param>
        ///  <returns>Boolean is the two strings are similar.</returns>
        public static bool match(this string a, string b) {
            if (a.Length == 0 || b.Length == 0)
                return false;

            if (a.Length > b.Length) {
                string tmp = a.Substring(0, b.Length);
                if (tmp.Equals(b, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else {
                string tmp = b.Substring(0, a.Length);
                if (a.Equals(tmp, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>Check if two strings match (case independent), based on the shortest string length.</summary>
        /// <param name="str">string A.</param>
        /// <param name="substr">string B.</param>
        /// <returns>Boolean is the two strings are similar.</returns>
        public static bool Match(this string str, string substr) {
            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(substr)) return false;

            if (str.Length < substr.Length) return false;

            return str.StartsWith(substr, StringComparison.OrdinalIgnoreCase);
        }


        /// <summary>Parse time string to a double number.</summary>
        ///  <param name="time">Time string.</param>
        /// <param name="units">Units format (PM or AM).</param>
        /// <returns>Time in hours (1.0 is 3600 seconds)</returns>
        public static double GetHour(string time, string units) {
            int n = 0;
            double[] y = {0.0d, 0.0d, 0.0d};
            string[] s = time.Split(':');


            for (int i = 0; i < s.Length && i <= 3; i++) {
                if (!s[i].ToDouble(out y[i])) return -1.0;
                n++;
            }

            if (n == 1) {
                if (string.IsNullOrEmpty(units)) return y[0];

                if (match(units, Keywords.w_SECONDS)) return y[0] / 3600.0;
                if (match(units, Keywords.w_MINUTES)) return y[0] / 60.0;
                if (match(units, Keywords.w_HOURS)) return y[0];
                if (match(units, Keywords.w_DAYS)) return y[0] * 24.0;
            }

            if (n > 1) y[0] = y[0] + y[1] / 60.0 + y[2] / 3600.0;

            if (units.Length == 0)
                return y[0];

            if (units.Equals(Keywords.w_AM, StringComparison.OrdinalIgnoreCase)) {
                if (y[0] >= 13.0) return -1.0;
                if (y[0] >= 12.0) return y[0] - 12.0;
                else return y[0];
            }
            if (units.Equals(Keywords.w_PM, StringComparison.OrdinalIgnoreCase)) {
                if (y[0] >= 13.0) return y[0] - 12.0;
                if (y[0] >= 12.0) return y[0];
                else return y[0] + 12.0;
            }
            return -1.0;
        }



        /// <summary>Convert time to a string.</summary>
        /// <param name="seconds">Time to convert, in seconds.</param>
        /// <returns>Time string in epanet format.</returns>
        public static string GetClockTime(this long seconds) {
            TimeSpan ts = TimeSpan.FromSeconds(seconds);
            return string.Format("{0:00}:{1:00}:{2:00}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
        }

        /// <summary>Computes coeffs. for pump curve.</summary>
        ///  <param name="h0">shutoff head</param>
        /// <param name="h1">design head</param>
        /// <param name="h2">head at max. flow</param>
        /// <param name="q1">design flow</param>
        /// <param name="q2">max. flow</param>
        /// <param name="a">pump curve coeffs. (H = a-bQ^c)</param>
        /// <param name="b">pump curve coeffs. (H = a-bQ^c)</param>
        /// <param name="c">pump curve coeffs. (H = a-bQ^c)</param>
        ///  <returns>Returns true if sucessful, false otherwise.</returns>
        public static bool GetPowerCurve(
            double h0,
            double h1,
            double h2,
            double q1,
            double q2,
            out double a,
            out double b,
            out double c) {
            a = b = c = 0;

            if (
                h0 < Constants.TINY ||
                h0 - h1 < Constants.TINY ||
                h1 - h2 < Constants.TINY ||
                q1 < Constants.TINY ||
                q2 - q1 < Constants.TINY
            ) return false;

            a = h0;
            double h4 = h0 - h1;
            double h5 = h0 - h2;
            c = Math.Log(h5 / h4) / Math.Log(q2 / q1);
            if (c <= 0.0 || c > 20.0) return false;
            b = -h4 / Math.Pow(q1, c);

            if (b >= 0.0)
                return false;
            else
                return true;
        }

        ///<summary>Get value signal, if bigger than 0 returns 1, -1 otherwise.</summary>
        /// <param name="val">Any real number.</param>
        /// <returns>-1 or 1</returns>
        public static double GetSignal(double val) { return val < 0 ? -1d : 1d; }

        public static void Reverse<T>(this LinkedList<T> linkedList) {
            if (linkedList == null || linkedList.Count < 2) return;
            LinkedListNode<T> next;
            var head = linkedList.First;

            while ((next = head.Next) != null) {
                linkedList.Remove(next);
                linkedList.AddFirst(next.Value);
            }
        }

        public static bool IsNumber(this object value) {
            if (value == null) return false;
            var code = Type.GetTypeCode(value.GetType());

            switch (code) {
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal:
                return true;
            default:
                return false;
            }

            // return code >= TypeCode.SByte && code <= TypeCode.Decimal;

        }

        public static bool ToDouble(this string s, out double result) {
            return double.TryParse(
                s,
                NumberStyles.Float | NumberStyles.AllowThousands,
                NumberFormatInfo.InvariantInfo,
                out result);
        }

    }

}