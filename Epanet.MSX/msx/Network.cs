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

using System.Collections.Generic;
using Epanet.MSX.Structures;

namespace Epanet.MSX {

    /// <summary>MSX PROJECT VARIABLES</summary>
    public class Network {
        ///<summary>Project title</summary>
        public string Title;

        ///<summary>Numbers of each type of object [MAX_OBJECTS]</summary>
        public readonly int[] Nobjects;
        ///<summary>Unit system flag</summary>
        public EnumTypes.UnitSystemType Unitsflag;
        ///<summary>Flow units flag</summary>
        public EnumTypes.FlowUnitsType Flowflag;
        ///<summary>Report results flag</summary>
        public bool Rptflag;
        ///<summary>Degree of coupling for solving DAE's</summary>
        public EnumTypes.CouplingType Coupling;
        ///<summary>Surface area units</summary>
        public EnumTypes.AreaUnitsType AreaUnits;
        ///<summary>Reaction rate time units</summary>
        public EnumTypes.RateUnitsType RateUnits;
        ///<summary>Choice of ODE solver</summary>
        public EnumTypes.SolverType Solver;

        ///<summary>Lines per page in report</summary>
        public int PageSize;
        ///<summary>Number of reporting periods</summary>
        public int Nperiods;
        ///<summary>Error code</summary>
        public int ErrCode;

        ///<summary>Quality time step (sec)</summary>
        public long Qstep;
        ///<summary>Time pattern time step (sec)</summary>
        public long Pstep;
        ///<summary>Starting pattern time (sec)</summary>
        public long Pstart;
        ///<summary>Reporting time step (sec)</summary>
        public long Rstep;
        ///<summary>Time when reporting starts</summary>
        public long Rstart;
        ///<summary>Next reporting time (sec)</summary>
        public long Rtime;
        ///<summary>Current hydraulic time (sec)</summary>
        public long Htime;
        ///<summary>Current quality time (sec)</summary>
        public long Qtime;

        ///<summary>Reporting statistic flag</summary>
        public EnumTypes.TstatType Statflag;
        ///<summary>Duration of simulation (sec)</summary>
        public long Dur;

        ///<summary>Node demands</summary>
        public float[] D;
        ///<summary>Node heads</summary>
        public float[] H;
        ///<summary>Link flows</summary>
        public float[] Q;

        ///<summary>Unit conversion factors [MAX_UNIT_TYPES]</summary>
        public readonly double[] Ucf;
        ///<summary>Species initial quality vector</summary>
        public double[] C0;
        ///<summary>Species concentration vector</summary>
        public double[] C1;

        ///<summary>Default relative error tolerance</summary>
        public double DefRtol;
        ///<summary>Default absolute error tolerance</summary>
        public double DefAtol;

        ///<summary>First WQ segment in each pipe/tank</summary>
        public LinkedList<Pipe>[] Segments;
        ///<summary>WQ species data</summary>
        public Species[] Species;
        ///<summary>Expression parameters</summary>
        public Param[] Param;
        ///<summary>Expression constants</summary>
        public Const[] Const;
        ///<summary>Intermediate terms</summary>
        public Term[] Term;
        ///<summary>Node data</summary>
        public Node[] Node;
        ///<summary>Link data</summary>
        public Link[] Link;
        ///<summary>Tank data</summary>
        public Tank[] Tank;
        ///<summary>Pattern data</summary>
        public Pattern[] Pattern;

        public string RptFilename;



        public Network() {
            this.Nobjects = new int[(int)EnumTypes.ObjectTypes.MAX_OBJECTS];
            this.Ucf = new double[(int)EnumTypes.UnitsType.MAX_UNIT_TYPES];

        }

        /// <summary>Assigns default values to project variables.</summary>
        public void SetDefaults() {
            this.Title = "";
            this.Rptflag = false;
            
            for (int i = 0; i < (int)EnumTypes.ObjectTypes.MAX_OBJECTS; i++)
                this.Nobjects[i] = 0;

            this.Unitsflag = EnumTypes.UnitSystemType.US;
            this.Flowflag = EnumTypes.FlowUnitsType.GPM;
            this.Statflag = EnumTypes.TstatType.SERIES;
            this.DefRtol = 0.001;
            this.DefAtol = 0.01;
            this.Solver = EnumTypes.SolverType.EUL;
            this.Coupling = EnumTypes.CouplingType.NO_COUPLING;
            this.AreaUnits = EnumTypes.AreaUnitsType.FT2;
            this.RateUnits = EnumTypes.RateUnitsType.DAYS;
            this.Qstep = 300;
            this.Rstep = 3600;
            this.Rstart = 0;
            this.Dur = 0;
            this.Node = null;
            this.Link = null;
            this.Tank = null;
            this.D = null;
            this.Q = null;
            this.H = null;
            this.Species = null;
            this.Term = null;
            this.Const = null;
            this.Pattern = null;
        }
    }

}