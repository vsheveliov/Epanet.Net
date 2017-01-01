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

using Epanet.Enums;

namespace Epanet.Util {

    ///<summary>Epanet utilities methods.</summary>
    public static class Utilities {
        /// <summary>Check if two strings match (case independent), based on the shortest string length.</summary>
        /// <param name="a">string A.</param>
        /// <param name="b">string B.</param>
        /// <returns>Boolean is the two strings are similar.</returns>
        public static bool Match2(this string a, string b) {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;

            return a.Length > b.Length 
                ? a.StartsWith(b, StringComparison.OrdinalIgnoreCase) 
                : b.StartsWith(a, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Sees if substr matches any part of str (not case sensitive).
        /// </summary>
        /// <param name="str">string being searched</param>
        /// <param name="substr">substring being searched for</param>
        /// <returns>returns <c>true</c> if substr found in str, <c>false</c> if not</returns>
        public static bool Match(this string str, string substr) {
            
            /* Fail if substring is empty */
            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(substr))
                return false;

            /* Skip leading blanks of str. */
            str = str.TrimStart();

            /* Check if substr matches remainder of str. */
            return str.StartsWith(substr, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Converts time from units to hours.</summary>
        /// <param name="time">string containing a time value</param>
        /// <param name="units">string containing time units (PM or AM)</param>
        /// <returns>numerical value of time in hours (1.0 is 3600 seconds), or -1 if an error occurs </returns>
        public static double GetHour(string time, string units = null) {
            const int COUNT = 3;
            double[] y = new double[COUNT];

            // Separate clock time into hrs, min, sec. 
            string[] s = (time ?? string.Empty).Split(':');

            int n;
            for(n = 0; n < COUNT && n < s.Length; n++) {
                if (!double.TryParse(s[n], out y[n])) return -1;
            }

            // If decimal time with units attached then convert to hours. 
            if (n == 1) {
                if (string.IsNullOrEmpty(units)) return y[0];

                if (Match(units, Keywords.w_SECONDS)) return y[0] / 3600.0;
                if (Match(units, Keywords.w_MINUTES)) return y[0] / 60.0;
                if (Match(units, Keywords.w_HOURS)) return y[0];
                if (Match(units, Keywords.w_DAYS)) return y[0] * 24.0;
            }

            // Convert hh:mm:ss format to decimal hours 
            if (n > 1) y[0] = y[0] + y[1] / 60.0 + y[2] / 3600.0;

            // If am/pm attached then adjust hour accordingly 
            // (12 am is midnight, 12 pm is noon) 
            if (string.IsNullOrEmpty(units)) return y[0];

            if (units.Equals(Keywords.w_AM, StringComparison.OrdinalIgnoreCase)) {
                if (y[0] >= 13.0) return -1.0;
                if (y[0] >= 12.0) return y[0] - 12.0;
                return y[0];
            }

            if (units.Equals(Keywords.w_PM, StringComparison.OrdinalIgnoreCase)) {
                if (y[0] >= 13.0) return -1.0;
                if (y[0] >= 12.0) return y[0];
                return y[0] + 12.0;
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

        ///<summary>Get value signal, if bigger than 0 returns 1, -1 otherwise.</summary>
        /// <param name="val">Any real number.</param>
        /// <returns>-1 or 1</returns>
        public static double GetSignal(double val) {
            return val < 0 ? -1d : 1d;
        }

        public static void Reverse<T>(this LinkedList<T> linkedList) {
            if (linkedList == null || linkedList.Count < 2) return;
            
            LinkedListNode<T> next;

            var head = linkedList.First;

            while ((next = head.Next) != null) {
                linkedList.Remove(next);
                linkedList.AddFirst(next.Value);
            }
        }

    

        public static bool ToDouble(this string s, out double result) {
            return double.TryParse(
                s,
                NumberStyles.Float | NumberStyles.AllowThousands,
                NumberFormatInfo.InvariantInfo,
                out result);
        }

        public static bool IsMissing(this double value) {
            return value == Constants.MISSING;
        }

    }

}