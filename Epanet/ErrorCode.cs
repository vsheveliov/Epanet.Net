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

namespace Epanet {
    /// <summary>Epanet error codes</summary>
    public enum ErrorCode {
        /// <summary>No error</summary>
        Ok = 0,

        /// <summary> WARNING: System hydraulically unbalanced.</summary>
        Warn1 = 1,

        /// <summary> WARNING: System may be hydraulically unstable.</summary>
        Warn2 = 2,

        /// <summary> WARNING: System disconnected.</summary>
        Warn3 = 3,

        /// <summary> WARNING: Pumps cannot deliver enough flow or head.</summary>
        Warn4 = 4,

        /// <summary> WARNING: Valves cannot deliver enough flow.</summary>
        Warn5 = 5,

        /// <summary> WARNING: System has negative pressures.</summary>
        Warn6 = 6,

        /// <summary>System Error 101: insufficient memory available.</summary>
        Err101 = 101,

        /// <summary>System Error 102: no network data available.</summary>
        Err102 = 102,

        /// <summary>System Error 103: hydraulics not initialized.</summary>
        Err103 = 103,

        /// <summary>System Error 104: no hydraulics for water quality analysis.</summary>
        Err104 = 104,

        /// <summary>System Error 105: water quality not initialized.</summary>
        Err105 = 105,

        /// <summary>System Error 106: no results saved to report on.</summary>
        Err106 = 106,

        /// <summary>System Error 107: hydraulics supplied from external file.</summary>
        Err107 = 107,

        /// <summary>System Error 108: cannot use external file while hydraulics solver is active.</summary>
        Err108 = 108,

        /// <summary>System Error 109: cannot change time parameter when solver is active.</summary>
        Err109 = 109,

        /// <summary>System Error 110: cannot solve network hydraulic equations.</summary>
        Err110 = 110,

        /// <summary>System Error 120: cannot solve water quality transport equations.</summary>
        Err120 = 120,

        /// <summary>Input Error 200: one or more errors in input file.</summary>
        Err200 = 200,

        /// <summary>Input Error 201: syntax error in following line of [%s] section:</summary>
        Err201 = 201,

        /// <summary>Input Error 202: %s %s contains illegal numeric value.</summary>
        Err202 = 202,

        /// <summary>Input Error 203: %s %s refers to undefined node.</summary>
        Err203 = 203,

        /// <summary>Input Error 204: %s %s refers to undefined link.</summary>
        Err204 = 204,

        /// <summary>Input Error 205: %s %s refers to undefined time pattern.</summary>
        Err205 = 205,

        /// <summary>Input Error 206: %s %s refers to undefined curve.</summary>
        Err206 = 206,

        /// <summary>Input Error 207: %s %s attempts to control a CV.</summary>
        Err207 = 207,

        /// <summary>Input Error 208: %s specified for undefined Node %s.</summary>
        Err208 = 208,

        /// <summary>Input Error 209: illegal %s value for Node %s.</summary>
        Err209 = 209,

        /// <summary>Input Error 210: %s specified for undefined Link %s.</summary>
        Err210 = 210,

        /// <summary>Input Error 211: illegal %s value for Link %s.</summary>
        Err211 = 211,

        /// <summary>Input Error 212: trace node %.0s %s is undefined.</summary>
        Err212 = 212,

        /// <summary>Input Error 213: illegal option value in [%s] section:</summary>
        Err213 = 213,

        /// <summary>Input Error 214: following line of [%s] section contains too many characters:</summary>
        Err214 = 214,

        /// <summary>Input Error 215: %s %s is a duplicate ID.</summary>
        Err215 = 215,

        /// <summary>Input Error 216: %s data specified for undefined Pump %s.</summary>
        Err216 = 216,

        /// <summary>Input Error 217: invalid %s data for Pump %s.</summary>
        Err217 = 217,

        /// <summary>Input Error 219: %s %s illegally connected to a tank.</summary>
        Err219 = 219,

        /// <summary>Input Error 220: %s %s illegally connected to another valve.</summary>
        Err220 = 220,

        /// <summary>Mis-placed %s clause in rule %s</summary>
        Err221 = 221,

        /*** Updated on 10/25/00 ***/

        /// <summary>Input Error 222: %s %s has same start and end nodes.</summary>
        Err222 = 222,

        /// <summary>Input Error 223: not enough nodes in network</summary>
        Err223 = 223,

        /// <summary>Input Error 224: no tanks or reservoirs in network.</summary>
        Err224 = 224,

        /// <summary>Input Error 225: invalid lower/upper levels for Tank %s.</summary>
        Err225 = 225,

        /// <summary>Input Error 226: no head curve supplied for Pump %s.</summary>
        Err226 = 226,

        /// <summary>Input Error 227: invalid head curve for Pump %s.</summary>
        Err227 = 227,

        /// <summary>Input Error 230: Curve %s has nonincreasing x-values.</summary>
        Err230 = 230,

        /// <summary>Input Error 233: Node %s is unconnected.</summary>
        Err233 = 233,

        /// <summary>Input Error 240: %s %s refers to undefined source.</summary>
        Err240 = 240,

        /// <summary>Input Error 241: %s %s refers to undefined control.</summary>
        Err241 = 241,

        /// <summary>Input Error 250: function call contains invalid format.</summary>
        Err250 = 250,

        /// <summary>Input Error 251: function call contains invalid parameter code.</summary>
        Err251 = 251,

        /// <summary>File Error 301: identical file names.</summary>
        Err301 = 301,

        /// <summary>File Error 302: cannot open input file.</summary>
        Err302 = 302,

        /// <summary>File Error 303: cannot open report file.</summary>
        Err303 = 303,

        /// <summary>File Error 304: cannot open binary output file.</summary>
        Err304 = 304,

        /// <summary>File Error 305: cannot open hydraulics file.</summary>
        Err305 = 305,

        /// <summary>File Error 306: hydraulics file does not match network data.</summary>
        Err306 = 306,

        /// <summary>File Error 307: cannot read hydraulics file.</summary>
        Err307 = 307,

        /// <summary>File Error 308: cannot save results to file.</summary>
        Err308 = 308,

        /// <summary>File Error 309: cannot save results to report file.</summary>
        Err309 = 309,

        Err1000 = 1000,


    }
}