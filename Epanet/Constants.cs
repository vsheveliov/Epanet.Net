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

    ///<summary>Epanet constants.</summary>
    public static class Constants {
        /// public static double PI = 3.141592654;

        ///<summary>Max. # of disconnected nodes listed</summary>
        public const int MAXCOUNT = 10;
        ///<summary>Max. # title lines</summary>
        public const int MAXTITLE = 3;
        /// <summary>Max. # characters read from input line.</summary>
        public const int MAXLINE = 255;
        /// <summary>Max. items per line of input</summary>
        public const int MAXTOKS = 40;       
        ///<summary>Max. input errors reported</summary>
        public const int MAXERRS = 10;
        //public const int     MAXMSG = 79;
        public const int     MAXID  = 31;
        //public const int     MAXFNAME = 259;

        ///<summary>Epanet binary files code version</summary>
        public const int CODEVERSION = 20012;

        ///<summary>Epanet binary files ID</summary>
        public const int MAGICNUMBER = 516114521;

        ///<summary>Epanet binary files version</summary>
        public const int VERSION = 200;

        ///<summary>Equivalent to zero flow</summary>
        public const double QZERO = 1e-6d;

        ///<summary>Big coefficient</summary>
        public const double CBIG = 1e8d;
        ///<summary>Small coefficient</summary>
        public const double CSMALL = 1 - 6d;

        ///<summary>Default max. # hydraulic iterations</summary>
        public const int MAXITER = 200;
        ///<summary>Default hydraulics convergence ratio</summary>
        public const double HACC = 0.001d;
        ///<summary>Default hydraulic head tolerance (ft)</summary>
        public const double HTOL = 0.0005d;
        ///<summary>Default flow rate tolerance (cfs)</summary>
        public const double QTOL = 0.0001d;

        ///<summary>Default water age tolerance (hrs)</summary>
        public const double AGETOL = 0.01d;
        ///<summary>Default concentration tolerance</summary>
        public const double CHEMTOL = 0.01d;
        ///<summary>Default uses no page breaks</summary>
        public const int PAGESIZE = 0;
        ///<summary>Default specific gravity</summary>
        public const double SPGRAV = 1.0d;
        ///<summary>Default pump efficiency</summary>
        public const double EPUMP = 75d;
        ///<summary>Default demand pattern ID</summary>
        public const string DEFPATID = "1";

        ///<summary>Default low flow resistance tolerance</summary>
        public const double RQTOL = 1E-7d;

        ///<summary>Default status check frequency</summary>
        public const int CHECKFREQ = 2;
        ///<summary>Default # iterations for status checks</summary>
        public const int MAXCHECK = 10;
        ///<summary>Default damping threshold</summary>
        public const double DAMPLIMIT = 0;

        ///<summary>Max. # types of network variables</summary>
        public const int MAXVAR = 21;

        public const double BIG = 1E10d;
        public const double TINY = 1E-6d;

        public const double MISSING = -1E10d;

        // ReSharper disable InconsistentNaming
        public const double GPMperCFS = 448.831d;
        public const double AFDperCFS = 1.9837d;
        public const double MGDperCFS = 0.64632d;
        public const double IMGDperCFS = 0.5382d;
        public const double LPSperCFS = 28.317d;
        public const double LPMperCFS = 1699.0d;
        public const double CMHperCFS = 101.94d;
        public const double CMDperCFS = 2446.6d;
        public const double MLDperCFS = 2.4466d;
        public const double M3perFT3 = 0.028317d;
        public const double LperFT3 = 28.317d;
        public const double MperFT = 0.3048d;
        public const double MMperFT = 304.8d;
        public const double INperFT = 12.0d;
        public const double PSIperFT = 0.4333d;
        public const double KPAperPSI = 6.895d;
        public const double KWperHP = 0.7457d;
        public const int SECperDAY = 86400;
        // ReSharper restore InconsistentNaming

        public const double DIFFUS = 1.3E-8d;
        public const double VISCOS = 1.1E-5;
    }

}