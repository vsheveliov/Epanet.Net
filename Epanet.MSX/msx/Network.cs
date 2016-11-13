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

using System.Collections.Generic;
using org.addition.epanet.msx.Structures;

namespace org.addition.epanet.msx {

// MSX PROJECT VARIABLES
public class Network {
    public string          Title;              // Project title

    public int []          Nobjects;           // Numbers of each type of object [MAX_OBJECTS]
    public EnumTypes.UnitSystemType  Unitsflag;          // Unit system flag
    public EnumTypes.FlowUnitsType   Flowflag;           // Flow units flag
    public bool         Rptflag;            // Report results flag
    public EnumTypes.CouplingType    Coupling;           // Degree of coupling for solving DAE's
    public EnumTypes.AreaUnitsType   AreaUnits;          // Surface area units
    public EnumTypes.RateUnitsType   RateUnits;          // Reaction rate time units
    public EnumTypes.SolverType      Solver;             // Choice of ODE solver

    public int     PageSize;                   // Lines per page in report
    public int     Nperiods;                   // Number of reporting periods
    public int     ErrCode;                    // Error code

    public long    Qstep;                      // Quality time step (sec)
    public long    Pstep;                      // Time pattern time step (sec)
    public long    Pstart;                     // Starting pattern time (sec)
    public long    Rstep;                      // Reporting time step (sec)
    public long    Rstart;                     // Time when reporting starts
    public long    Rtime;                      // Next reporting time (sec)
    public long    Htime;                      // Current hydraulic time (sec)
    public long    Qtime;                      // Current quality time (sec)

    public EnumTypes.TstatType   Statflag;               // Reporting statistic flag
    public long        Dur;                    // Duration of simulation (sec)

    public float []D;                          // Node demands
    public float []H;                          // Node heads
    public float []Q;                          // Link flows

    public double []   Ucf;                    // Unit conversion factors [MAX_UNIT_TYPES]
    public double []   C0;						// Species initial quality vector
    public double []   C1;                     // Species concentration vector

    public double      DefRtol;                // Default relative error tolerance
    public double      DefAtol;                // Default absolute error tolerance

    public LinkedList<Pipe>[] Segments;              // First WQ segment in each pipe/tank

    public Species []Species;                  // WQ species data
    public Param   []Param;                    // Expression parameters
    public Const   []Const;                    // Expression constants
    public Term    []Term;                     // Intermediate terms
    public Node    []Node;                     // Node data
    public Link    []Link;                     // Link data
    public Tank    []Tank;                     // Tank data
    public Pattern []Pattern;                  // Pattern data

    public string rptFilename;

    public Network(){
        Nobjects = new int[(int) EnumTypes.ObjectTypes.MAX_OBJECTS];
        Ucf = new double[(int) EnumTypes.UnitsType.MAX_UNIT_TYPES];

    }

    public Species[] getSpecies() {
        return Species;
    }

    public Node [] getNodes(){
        return Node;
    }

    public Link [] getLinks(){
        return Link;
    }

    public int getNperiods() {
        return Nperiods;
    }

    public long getQstep() {
        return Qstep;
    }

    public long getQtime() {
        return Qtime;
    }

    public long getDuration() {
        return Dur;
    }

    public void setQstep(long qstep) {
        Qstep = qstep;
    }

    public void setDur(long dur) {
        Dur = dur;
    }

    public void setRstep(long rstep) {
        this.Rstep = rstep;
    }

    public long getRstart() {
        return Rstart;
    }

    public void setRstart(long rstart) {
        Rstart = rstart;
    }
}

}