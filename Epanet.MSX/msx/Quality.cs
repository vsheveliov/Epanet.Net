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
using Epanet.Hydraulic.IO;
using Epanet.MSX.Structures;
using Epanet.Util;

namespace Epanet.MSX {

    public class Quality {


        private int UP_NODE(int i) {
            return _flowDir[i] == '+' ? _msx.Link[i].N1 : _msx.Link[i].N2;
        }

        private int DOWN_NODE(int i) {
            return _flowDir[i] == '+' ? _msx.Link[i].N2 : _msx.Link[i].N1;
        }

        private double Linkvol(int k) {
            return 0.785398 * _msx.Link[k].Len * Math.Pow(_msx.Link[k].Diam, 2);
        }

        //  External variables
        //--------------------
        private Network _msx; // MSX project data
        private TankMix _tank;
        private Chemical _chemical;
        private Output _out;
        private EnToolkit2 _tk2;

        public void LoadDependencies(EpanetMSX epa) {
            _msx = epa.Network; // MSX project data
            _tank = epa.TankMix;
            _chemical = epa.Chemical;
            _out = epa.Output;
            _tk2 = epa.EnToolkit;
        }

        //  Local variables
        //-----------------
        //<summary>pointer to unused pipe segment</summary>
        //Pipe            FreeSeg;
        ///<summary>new segment added to each pipe</summary>
        private Pipe[] _newSeg;
        ///<summary>flow direction for each pipe</summary>
        private char[] _flowDir;
        ///<summary>inflow flow volume to each node</summary>
        private double[] _volIn;
        ///<summary>mass inflow of each species to each node</summary>
        private double[,] _massIn;
        ///<summary>work matrix</summary>
        private double[,] _x;
        ///<summary>wall species indicator</summary>
        private bool _hasWallSpecies;
        //<summary>out of memory indicator</summary>
        //&bool         OutOfMemory;


        /// <summary>Opens the WQ routing system.</summary>
        public ErrorCodeType MSXqual_open() {
// --- set flags
            //MSX.QualityOpened = false;
            //MSX.Saveflag = false;
            //OutOfMemory = false;
            _hasWallSpecies = false;

// --- initialize array pointers to null

            _msx.C1 = null;
            _msx.Segments = null;
            // MSX.LastSeg = null;
            _x = null;
            _newSeg = null;
            _flowDir = null;
            _volIn = null;
            _massIn = null;

// --- open the chemistry system

            ErrorCodeType errcode = _chemical.MSXchem_open();
            if (errcode > 0) return errcode;

// --- allocate a memory pool for pipe segments

            //QualPool = AllocInit();
            //if ( QualPool == null ) return ERR_MEMORY;

// --- allocate memory used for species concentrations

            _x = new double[_msx.Nobjects[(int)ObjectTypes.NODE] + 1,_msx.Nobjects[(int)ObjectTypes.SPECIES] + 1];
            _msx.C1 = new double[_msx.Nobjects[(int)ObjectTypes.SPECIES] + 1];
            //(double *) calloc(MSX.Nobjects[ObjectTypes.SPECIES.id]+1, sizeof(double));

// --- allocate memory used for pointers to the first, last,
//     and new WQ segments in each link and tank

            int n = _msx.Nobjects[(int)ObjectTypes.LINK]
                    + _msx.Nobjects[(int)ObjectTypes.TANK] + 1;
            _msx.Segments = new LinkedList<Pipe>[n]; //(Pseg *) calloc(n, sizeof(Pseg));
            for (int i = 0; i < n; i++)
                _msx.Segments[i] = new LinkedList<Pipe>();

            //MSX.LastSeg  = (Pseg *) calloc(n, sizeof(Pseg));
            _newSeg = new Pipe[n]; //(Pseg *) calloc(n, sizeof(Pseg));

// --- allocate memory used flow direction in each link

            _flowDir = new char[n]; //(char *) calloc(n, sizeof(char));

// --- allocate memory used to accumulate mass and volume
//     inflows to each node

            n = _msx.Nobjects[(int)ObjectTypes.NODE] + 1;
            _volIn = new double[n]; //(double *) calloc(n, sizeof(double));
            _massIn = new double[n,_msx.Nobjects[(int)ObjectTypes.SPECIES] + 1];

// --- check for successful memory allocation

            //CALL(errcode, MEMCHECK(X));
            //CALL(errcode, MEMCHECK(MSX.C1));
            //CALL(errcode, MEMCHECK(MSX.Segments));
            //CALL(errcode, MEMCHECK(MSX.LastSeg));
            //CALL(errcode, MEMCHECK(NewSeg));
            //CALL(errcode, MEMCHECK(FlowDir));
            //CALL(errcode, MEMCHECK(VolIn));
            //CALL(errcode, MEMCHECK(MassIn));

// --- check if wall species are present

            for (n = 1; n <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; n++) {
                if (_msx.Species[n].Type == SpeciesType.WALL) _hasWallSpecies = true;
            }
            //if ( errcode == 0)
            //    MSX.QualityOpened = true;
            return errcode;
        }


        /// <summary>Re-initializes the WQ routing system.</summary>
        public int MSXqual_init() {

            int errcode = 0;

            // Initialize node concentrations, tank volumes, & source mass flows
            for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.NODE]; i++) {
                for (int j = 1; j <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; j++)
                    _msx.Node[i].C[j] = _msx.Node[i].C0[j];
            }
            for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.TANK]; i++) {
                _msx.Tank[i].Hstep = 0.0;
                _msx.Tank[i].V = _msx.Tank[i].V0;
                int nn = _msx.Tank[i].Node;
                for (int j = 1; j <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; j++)
                    _msx.Tank[i].C[j] = _msx.Node[nn].C0[j];
            }
            for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.PATTERN]; i++) {
                _msx.Pattern[i].Interval = 0;
                _msx.Pattern[i].Current = 0; //MSX.Pattern[i]);//first);
            }

            // Check if a separate WQ report is required
            _msx.Rptflag = false;
            int n = 0;
            for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.NODE]; i++)
                n += _msx.Node[i].Rpt ? 1 : 0;
            for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.LINK]; i++)
                n += _msx.Link[i].Rpt ? 1 : 0;
            if (n > 0) {
                n = 0;
                for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; i++)
                    n += _msx.Species[i].Rpt;
            }
            if (n > 0) _msx.Rptflag = true;

            //if ( MSX.Rptflag )
            //    MSX.Saveflag = true;

            // reset memory pool

            //AllocSetPool(QualPool);
            //FreeSeg = null;
            //AllocReset();

            // re-position hydraulics file

            //fseek(MSX.HydFile.file, MSX.HydOffset, SEEK_SET);

            //MSX.HydFile.close();
            //MSX.HydFile.openAsBinaryReader();
            //DataInputStream din = (DataInputStream)MSX.HydFile.getFileIO();

            //try {
            //    din.skipBytes((int)MSX.HydOffset);
            //}
            //catch (IOException e) {
            //    e.printStackTrace();
            //}

            //  set elapsed times to zero
            _msx.Htime = 0; //Hydraulic solution time
            _msx.Qtime = 0; //Quality routing time
            _msx.Rtime = 0; //MSX.Rstart;                //Reporting time
            _msx.Nperiods = 0; //Number fo reporting periods

            // open binary output file if results are to be saved
            //if ( MSX.Saveflag ) errcode = out.MSXout_open();
            return errcode;
        }

        ///<summary>Updates WQ conditions over a single WQ time step.</summary>
        public ErrorCodeType MSXqual_step(long[] t, long[] tleft) {
            ErrorCodeType errcode = 0;

            // Set the shared memory pool to the water quality pool and the overall time step to nominal WQ time step

            //AllocSetPool(QualPool);
            long tstep = _msx.Qstep;

            // Repeat until the end of the time step
            do {
                // Find the time until the next hydraulic event occurs
                long dt = tstep;
                long hstep = _msx.Htime - _msx.Qtime;

                // Check if next hydraulic event occurs within the current time step
                if (hstep <= dt) {
                    // Reduce current time step to end at next hydraulic event
                    dt = hstep;

                    // route WQ over this time step
                    if (dt > 0)
                        errcode = Utilities.Call(errcode, Transport(dt));

                    _msx.Qtime += dt;

                    // retrieve new hydraulic solution
                    if (_msx.Qtime == _msx.Htime) errcode = Utilities.Call(errcode, GetHydVars());

                    // report results if its time to do so
                    if (_msx.Qtime == _msx.Rtime) // MSX.Saveflag &&
                    {
                        errcode = Utilities.Call(errcode, _out.MSXout_saveResults());
                        _msx.Rtime += _msx.Rstep;
                        _msx.Nperiods++;
                    }
                }

                // Otherwise just route WQ over the current time step

                else {
                    errcode = Utilities.Call(errcode, Transport(dt));
                    _msx.Qtime += dt;
                }

                // Reduce overall time step by the size of the current time step

                tstep -= dt;

            }
            while (errcode == 0 && tstep > 0);

            // Update the current time into the simulation and the amount remaining
            t[0] = _msx.Qtime;
            tleft[0] = _msx.Dur - _msx.Qtime;

            // If there's no time remaining, then save the final records to output file
            if (tleft[0] <= 0)
                errcode = Utilities.Call(errcode, _out.MSXout_saveFinalResults())
                    ;

            return errcode;
        }

        /// <summary>Retrieves WQ for species m at node n.</summary>
        public double MSXqual_getNodeQual(int j, int m) {
// --- return 0 for WALL species

            if (_msx.Species[m].Type == SpeciesType.WALL) return 0.0;

// --- if node is a tank, return its internal concentration

            int k = _msx.Node[j].Tank;
            if (k > 0 && _msx.Tank[k].A > 0.0) {
                return _msx.Tank[k].C[m];
            }

// --- otherwise return node's concentration (which includes
//     any contribution from external sources)

            return _msx.Node[j].C[m];
        }

        /// <summary>Computes average quality in link k.</summary>
        public double MSXqual_getLinkQual(int k, int m) {
            double vsum = 0.0,
                   msum = 0.0;
            //Pipe    seg;

            foreach (Pipe seg  in  _msx.Segments[k]) {
                vsum += seg.V;
                msum += seg.C[m] * seg.V;
                //seg = seg->prev;
            }
            if (vsum > 0.0) return msum / vsum;
            else {
                return (MSXqual_getNodeQual(_msx.Link[k].N1, m) +
                        MSXqual_getNodeQual(_msx.Link[k].N2, m)) / 2.0;
            }
        }

        /// <summary>Checks if two sets of concentrations are the same</summary>
        public bool MSXqual_isSame(double[] c1, double[] c2) {
            for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; i++) {
                if (Math.Abs(c1[i] - c2[i]) >= _msx.Species[i].ATol) return false;
            }
            return true;
        }

        /// <summary>
        /// Retrieves hydraulic solution and time step for next hydraulic event  from a hydraulics file.
        /// </summary>
        private ErrorCodeType GetHydVars() {
            AwareStep step = _tk2.GetStep((int)_msx.Htime);

            int n = _msx.Nobjects[(int)ObjectTypes.NODE];
            for (int i = 0; i < n; i++)
                _msx.D[i + 1] = (float)step.GetNodeDemand(i, null, null);

            for (int i = 0; i < n; i++)
                _msx.H[i + 1] = (float)step.GetNodeHead(i, null, null);

            n = _msx.Nobjects[(int)ObjectTypes.LINK];

            for (int i = 0; i < n; i++)
                _msx.Q[i + 1] = (float)step.GetLinkFlow(i, null, null);

            // update elapsed time until next hydraulic event
            _msx.Htime = step.Time + step.Step;

            // Initialize pipe segments (at time 0) or else re-orient segments to accommodate any flow reversals
            if (_msx.Qtime < _msx.Dur) {
                if (_msx.Qtime == 0)
                    InitSegs();
                else
                    ReorientSegs();
            }

            return 0;
        }

        /// <summary>
        /// Transports constituent mass through pipe network
        /// under a period of constant hydraulic conditions.
        /// </summary>
        private ErrorCodeType Transport(long tstep) {
            ErrorCodeType errcode = 0;

// --- repeat until time step is exhausted

            long qtime = 0;
            while (errcode == 0 &&
                   qtime < tstep) {
                // Qstep is nominal quality time step
                long dt = Math.Min(_msx.Qstep, tstep - qtime);
                qtime += dt; // update amount of input tstep taken
                errcode = _chemical.MSXchem_react(dt); // react species in each pipe & tank
                if (errcode != 0)
                    return errcode;
                AdvectSegs(dt); // advect segments in each pipe
                Accumulate(dt); // accumulate all inflows at nodes
                UpdateNodes(dt); // update nodal quality
                SourceInput(dt); // compute nodal inputs from sources
                Release(dt); // release new outflows from nodes
            }
            return errcode;
        }

        /// <summary>Initializes water quality in pipe segments.</summary>
        private void InitSegs() {


// --- examine each link

            for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.LINK]; i++) {
                // --- establish flow direction

                _flowDir[i] = '+';
                if (_msx.Q[i] < 0.0) _flowDir[i] = '-';

// --- start with no segments

                //MSX.LastSeg[k] = null;
                _msx.Segments[i].Clear();
                _newSeg[i] = null;

// --- use quality of downstream node for BULK species
//     if no initial link quality supplied

                int n = DOWN_NODE(i);
                for (int j = 1; j <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; j++) {
                    if (_msx.Link[i].C0[j] != Constants.MISSING)
                        _msx.C1[j] = _msx.Link[i].C0[j];
                    else if (_msx.Species[j].Type == SpeciesType.BULK)
                        _msx.C1[j] = _msx.Node[n].C0[j];
                    else _msx.C1[j] = 0.0;
                }

                // --- fill link with a single segment of this quality

                var v = Linkvol(i);
                if (v > 0.0)
                    _msx.Segments[i].AddLast(CreateSeg(v, _msx.C1));
                //MSXqual_addSeg(k, createSeg(v, MSX.C1));
            }

// --- initialize segments in tanks

            for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.TANK]; i++) {
                // --- skip reservoirs

                if (_msx.Tank[i].A == 0.0) continue;

// --- tank segment pointers are stored after those for links

                int k = _msx.Tank[i].Node;
                for (int j = 1; j <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; j++)
                    _msx.C1[j] = _msx.Node[k].C0[j];
                k = _msx.Nobjects[(int)ObjectTypes.LINK] + i;
                //MSX.LastSeg[k] = null;
                _msx.Segments[k].Clear();

// --- add 2 segments for 2-compartment model

                if (_msx.Tank[i].MixModel == MixType.MIX2) {
                    var v = Math.Max(0, _msx.Tank[i].V - _msx.Tank[i].VMix);
                    //MSXqual_addSeg(k, createSeg(v, MSX.C1));
                    _msx.Segments[k].AddLast(CreateSeg(v, _msx.C1));
                    v = _msx.Tank[i].V - v;
                    //MSXqual_addSeg(k, createSeg(v, MSX.C1));
                    _msx.Segments[k].AddLast(CreateSeg(v, _msx.C1));
                }

                // --- add one segment for all other models

                else {
                    var v = _msx.Tank[i].V;
                    _msx.Segments[k].AddLast(CreateSeg(v, _msx.C1));
                    //MSXqual_addSeg(k, createSeg(v, MSX.C1));
                }
            }
        }

        /// <summary>Re-orients pipe segments (if flow reverses).</summary>
        private void ReorientSegs() {
            //Pseg   seg, pseg, nseg;

// --- examine each link

            for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.LINK]; i++) {
                // --- find new flow direction

                var newdir = '+';
                if (_msx.Q[i] == 0.0) newdir = _flowDir[i];
                else if (_msx.Q[i] < 0.0) newdir = '-';

// --- if direction changes, then reverse the order of segments
//     (first to last) and save new direction

                if (newdir != _flowDir[i]) {
                    //seg = MSX.Segments[k];
                    //MSX.Segments[k] = MSX.LastSeg[k];
                    //MSX.LastSeg[k] = seg;
                    //pseg = null;
                    //while (seg != null)
                    //{
                    //    nseg = seg->prev;
                    //    seg->prev = pseg;
                    //    seg->next = nseg;
                    //    pseg = seg;
                    //    seg = nseg;
                    //}
                    _msx.Segments[i].Reverse();
                    _flowDir[i] = newdir;
                }
            }
        }

        ///<summary>Advects WQ segments within each pipe.</summary>
        private void AdvectSegs(long dt) {

            // Examine each link

            for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.LINK]; i++) {
                // Zero out WQ in new segment to be added at entrance of link
                for (int m = 1; m <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; m++)
                    _msx.C1[m] = 0.0;

                // Get a free segment to add to entrance of link
                _newSeg[i] = CreateSeg(0.0d, _msx.C1);

                // Skip zero-length links (pumps & valves) & no-flow links
                if (_newSeg[i] == null ||
                    _msx.Link[i].Len == 0.0 || _msx.Q[i] == 0.0) continue;

                // Find conc. of wall species in new segment to be added and adjust conc.
                // of wall species to reflect shifted positions of existing segments
                if (_hasWallSpecies) {
                    GetNewSegWallQual(i, dt, _newSeg[i]);
                    ShiftSegWallQual(i, dt);
                }
            }
        }

        ///<summary>
        /// Computes wall species concentrations for a new WQ segment that
        /// enters a pipe from its upstream node.
        /// </summary>
        private void GetNewSegWallQual(int k, long dt, Pipe newseg) {
            //Pipe  seg;

            if (newseg == null)
                return;

            // Get volume of inflow to link
            double v = Linkvol(k);
            double vin = Math.Abs(_msx.Q[k]) * dt;
            if (vin > v) vin = v;

            // Start at last (most upstream) existing WQ segment
            var vsum = 0.0;
            double vleft = vin;
            for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; i++) {
                if (_msx.Species[i].Type == SpeciesType.WALL)
                    newseg.C[i] = 0.0;
            }

            // repeat while some inflow volume still remains

            for (var segIt = _msx.Segments[k].Last; segIt != null; segIt = segIt.Previous) {
                // Move to next downstream WQ segment
                Pipe seg = segIt.Value;

                // Find volume added by this segment
                double vadded = seg.V;
                if (vadded > vleft) vadded = vleft;

                // Update total volume added and inflow volume remaining

                vsum += vadded;
                vleft -= vadded;

                // Add wall species mass contributed by this segment to new segment
                for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; i++) {
                    if (_msx.Species[i].Type == SpeciesType.WALL)
                        newseg.C[i] += vadded * seg.C[i];
                }
            }

            // Convert mass of wall species in new segment to concentration

            if (vsum > 0.0) {
                for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; i++) {
                    if (_msx.Species[i].Type == SpeciesType.WALL) newseg.C[i] /= vsum;
                }
            }
        }

        ///<summary>
        /// Recomputes wall species concentrations in segments that remain
        /// within a pipe after flow is advected over current time step.
        /// </summary>
        private void ShiftSegWallQual(int k, long dt) {
            // Find volume of water displaced in pipe
            double v = Linkvol(k);
            double vin = Math.Abs(_msx.Q[k]) * dt;
            if (vin > v) vin = v;

            // Set future start position (measured by pipe volume) of original last segment
            double vstart = vin;

            // Examine each segment, from upstream to downstream
            for (var segIt = _msx.Segments[k].Last; segIt != null; segIt = segIt.Previous) {
                // Move to next downstream WQ segment
                Pipe seg1 = segIt.Value;

                // Initialize a "mixture" WQ
                for (int m = 1; m <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; m++)
                    _msx.C1[m] = 0.0;

                // Find the future end position of this segment

                double vend = vstart + seg1.V;
                if (vend > v)
                    vend = v;

                double vcur = vstart;
                double vsum = 0;

                // find volume taken up by the segment after it moves down the pipe
                Pipe seg2 = null;

                for (var segIt2 = _msx.Segments[k].Last; segIt2 != null; segIt2 = segIt.Previous) {
                    // Move to next downstream WQ segment
                    seg2 = segIt2.Value;

                    if (seg2.V == 0.0)
                        continue;

                    vsum += seg2.V;
                    if (vsum >= vstart && vsum <= vend) {
                        for (int m = 1; m <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; m++) {
                            if (_msx.Species[m].Type == SpeciesType.WALL)
                                _msx.C1[m] += (vsum - vcur) * seg2.C[m];
                        }
                        vcur = vsum;
                    }
                    if (vsum >= vend) break;
                }

                // Update the wall species concentrations in the segment
                for (int m = 1; m <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; m++) {
                    if (_msx.Species[m].Type != SpeciesType.WALL)
                        continue;

                    if (seg2 != null)
                        _msx.C1[m] += (vend - vcur) * seg2.C[m];

                    seg1.C[m] = _msx.C1[m] / (vend - vstart);

                    if (seg1.C[m] < 0.0)
                        seg1.C[m] = 0.0;
                }

                // re-start at the current end location

                vstart = vend;
                if (vstart >= v)
                    break;
            }
        }


        ///<summary>accumulates mass inflow at downstream node of each link.</summary>

        private void Accumulate(long dt) {
            // Compute average conc. of segments incident on each node
            //     (for use if there is no transport through the node)
            GetIncidentConcen();

            // Reset cumlulative inflow to each node to zero
            Array.Clear(_volIn, 0, _msx.Nobjects[(int)ObjectTypes.NODE] + 1);

            for (int ij = 0; ij < _msx.Nobjects[(int)ObjectTypes.NODE] + 1; ij++)
                for (int jj = 0; jj < _msx.Nobjects[(int)ObjectTypes.SPECIES] + 1; jj++)
                    _massIn[ij, jj] = 0.0;

            // move mass from first segment of each link into link's downstream node

            for (int k = 1; k <= _msx.Nobjects[(int)ObjectTypes.LINK]; k++) {
                int i = UP_NODE(k); // upstream node
                int j = DOWN_NODE(k); // downstream node
                double v = Math.Abs(_msx.Q[k]) * dt;

                // if link volume < flow volume, then transport upstream node's
                // quality to downstream node and remove all link segments

                double cseg;
                if (Linkvol(k) < v) {
                    Pipe seg = null;
                    _volIn[j] += v;
                    if (_msx.Segments[k].Count > 0)
                        seg = _msx.Segments[k].First.Value; //get(0);
                    for (int m = 1; m <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; m++) {
                        if (_msx.Species[m].Type != SpeciesType.BULK) continue;
                        cseg = _msx.Node[i].C[m];
                        if (seg != null) cseg = seg.C[m];
                        _massIn[j,m] += v * cseg;
                    }
                    // Remove all segments in the pipe
                    _msx.Segments[k].Clear();
                }
                else
                    while (v > 0.0) {
                        // Otherwise remove flow volume from leading segments and accumulate flow mass at
                        // downstream node identify leading segment in pipe
                        Pipe seg = null;

                        if (_msx.Segments[k].Count > 0)
                            seg = _msx.Segments[k].First.Value; //get(0);

                        if (seg == null)
                            break;

                        // Volume transported from this segment is minimum of remaining flow volume & segment volume
                        // (unless leading segment is also last segment)

                        double vseg = Math.Min(seg.V, v);

                        if (_msx.Segments[k].Count == 1) // if (seg == MSX.LastSeg[k]) vseg = v;
                            vseg = v;

                        //update volume & mass entering downstream node
                        for (int m = 1; m <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; m++) {
                            if (_msx.Species[m].Type != SpeciesType.BULK) continue;
                            cseg = seg.C[m];
                            _massIn[j,m] += vseg * cseg;
                        }
                        _volIn[j] += vseg;

                        // Reduce flow volume by amount transported
                        v -= vseg;

                        // If all of segment's volume was transferred, then replace leading segment with the one behind it
                        // (Note that the current seg is recycled for later use.)
                        if (v >= 0.0 && vseg >= seg.V) {
                            if (_msx.Segments[k].Count > 0)
                                _msx.Segments[k].RemoveFirst(); //remove(0);
                            //MSX.Segments[k] = seg->prev;
                            //if (MSX.Segments[k] == null) MSX.LastSeg[k] = null;
                            //MSXqual_removeSeg(seg);
                        }
                        // Otherwise reduce segment's volume
                        else {
                            seg.V = seg.V - vseg;
                        }
                    }
            }
        }

        ///<summary>
        /// Determines average WQ for bulk species in link end segments that are
        /// incident on each node.
        /// </summary>
        private void GetIncidentConcen() {
            // zero-out memory used to store accumulated totals
            Array.Clear(_volIn, 0, _msx.Nobjects[(int)ObjectTypes.NODE] + 1);
            for (int i = 0; i < _msx.Nobjects[(int)ObjectTypes.NODE] + 1; i++)
                for (int j = 0; j < _msx.Nobjects[(int)ObjectTypes.SPECIES] + 1; j++) {
                    _massIn[i,j] = 0.0;
                    _x[i,j] = 0.0;
                }
            // examine each link
            for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.LINK]; i++) {
                int jj = DOWN_NODE(i); // downstream node
                if (_msx.Segments[i].Count > 0) // accumulate concentrations
                {
                    for (int j = 1; j <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; j++) {
                        if (_msx.Species[j].Type == SpeciesType.BULK)
                            _massIn[jj, j] += _msx.Segments[i].First.Value.C[j];
                    }
                    _volIn[jj]++;
                }
                jj = UP_NODE(i); // upstream node
                if (_msx.Segments[i].Count > 0) // accumulate concentrations
                {
                    for (int j = 1; j <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; j++) {
                        if (_msx.Species[j].Type == SpeciesType.BULK)
                            _massIn[jj, j] += _msx.Segments[i].Last.Value.C[j];
                        //get(MSX.Segments[k].size()-1).getC()[m];
                    }
                    _volIn[jj]++;
                }
            }

            // Compute avg. incident concen. at each node
            for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.NODE]; i++) {
                if (_volIn[i] > 0.0) {
                    for (int j = 1; j <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; j++)
                        _x[i, j] = _massIn[i, j] / _volIn[i];
                }
            }
        }

        ///<summary>
        /// Updates the concentration at each node to the mixture
        /// concentration of the accumulated inflow from connecting pipes.
        /// </summary>
        private void UpdateNodes(long dt) {
            // Examine each node
            for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.NODE]; i++) {
                // Node is a junction
                int jj = _msx.Node[i].Tank;
                if (jj <= 0) {
                    // Add any external inflow (i.e., negative demand) to total inflow volume

                    if (_msx.D[i] < 0.0)
                        _volIn[i] -= _msx.D[i] * dt;

                    // If inflow volume is non-zero, then compute the mixture
                    // concentration resulting at the node
                    if (_volIn[i] > 0.0) {
                        for (int j = 1; j <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; j++)
                            _msx.Node[i].C[j] = _massIn[i, j] / _volIn[i];
                    }
                    // Otherwise use the avg. of the concentrations in the links incident on the node
                    else {
                        for (int j = 1; j <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; j++)
                            _msx.Node[i].C[j] = _x[i, j];
                    }

                    // Compute new equilibrium mixture
                    _chemical.MSXchem_equil(ObjectTypes.NODE, _msx.Node[i].C);
                }

                // Node is a tank or reservoir
                else {

                    if (_msx.Tank[jj].A == 0.0) {
                        // Use initial quality for reservoirs
                        for (int j = 1; j <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; j++)
                            _msx.Node[i].C[j] = _msx.Node[i].C0[j];
                    }
                    else {
                        // otherwise update tank WQ based on mixing model
                        if (_volIn[i] > 0.0) {
                            for (int j = 1; j <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; j++) {
                                _msx.C1[j] = _massIn[i,j] / _volIn[i];
                            }
                        }
                        else
                            for (int j = 1; j <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; j++)
                                _msx.C1[j] = 0.0;

                        switch (_msx.Tank[jj].MixModel) {
                        case MixType.MIX1:
                            _tank.MSXtank_mix1(jj, _volIn[i], _msx.C1, dt);
                            break;
                        case MixType.MIX2:
                            _tank.MSXtank_mix2(jj, _volIn[i], _msx.C1, dt);
                            break;
                        case MixType.FIFO:
                            _tank.MSXtank_mix3(jj, _volIn[i], _msx.C1, dt);
                            break;
                        case MixType.LIFO:
                            _tank.MSXtank_mix4(jj, _volIn[i], _msx.C1, dt);
                            break;
                        }
                        for (int j = 1; j <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; j++)
                            _msx.Node[i].C[j] = _msx.Tank[jj].C[j];
                        _msx.Tank[jj].V = _msx.Tank[jj].V + _msx.D[i] * dt;
                    }
                }
            }
        }

        ///<summary>Computes contribution (if any) of mass additions from WQ sources at each node.</summary>
        private void SourceInput(long dt) {
            // Establish a flow cutoff which indicates no outflow from a node
            double qcutoff = 10.0 * Constants.TINY;

            // consider each node
            for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.NODE]; i++) {
                // Skip node if no WQ source
                if (_msx.Node[i].Sources.Count == 0)
                    continue;

                // find total flow volume leaving node

                double volout = _msx.Node[i].Tank == 0
                    ? _volIn[i] // Junctions
                    : _volIn[i] - _msx.D[i] * dt;

                double qout = volout / dt;

                // evaluate source input only if node outflow > cutoff flow

                if (qout <= qcutoff)
                    continue;

                // Add contribution of each source species
                foreach (Source source  in  _msx.Node[i].Sources) {
                    AddSource(i, source, volout, dt);
                }

                // Compute a new chemical equilibrium at the source node
                _chemical.MSXchem_equil(ObjectTypes.NODE, _msx.Node[i].C);
            }
        }


        ///<summary>Updates concentration of particular species leaving a node that receives external source input.</summary>
        private void AddSource(int n, Source source, double volout, long dt) {
            // Only analyze bulk species
            int m = source.Species;
            var massadded = 0.0;
            if (!(source.C0 > 0.0) || _msx.Species[m].Type != SpeciesType.BULK) return;
            
            // Mass added depends on type of source

            double s = GetSourceQual(source);
            switch (source.Type) {
            // Concen. Source : Mass added = source concen. * -(demand)
            case SourceType.CONCEN:
                // Only add source mass if demand is negative
                if (_msx.D[n] < 0.0)
                    massadded = -s * _msx.D[n] * dt;

                // If node is a tank then set concen. to 0.
                // (It will be re-set to true value later on)
                if (_msx.Node[n].Tank > 0)
                    _msx.Node[n].C[m] = 0.0;
                break;

            // Mass Inflow Booster Source
            case SourceType.MASS:
                massadded = s * dt / Constants.LperFT3;
                break;

            // Setpoint Booster Source: Mass added is difference between source & node concen. times outflow volume
            case SourceType.SETPOINT:
                if (s > _msx.Node[n].C[m])
                    massadded = (s - _msx.Node[n].C[m]) * volout;
                break;

            // Flow-Paced Booster Source: Mass added = source concen. times outflow volume
            case SourceType.FLOWPACED:
                massadded = s * volout;
                break;
            }

            // Adjust nodal concentration to reflect source addition
            _msx.Node[n].C[m] += massadded / volout;
        }

        ///<summary>Releases outflow from nodes into incident links.</summary>
        private void Release(long dt) {
            Pipe seg = null;

            // Examine each link
            for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.LINK]; i++) {
                // Ignore links with no flow

                if (_msx.Q[i] == 0.0) {
                    //MSXqual_removeSeg(NewSeg[k]);
                    _newSeg[i] = null;
                    continue;
                }

                // Find flow volume released to link from upstream node
                // (NOTE: Flow volume is allowed to be > link volume.)
                int n = UP_NODE(i);
                double q = Math.Abs(_msx.Q[i]);
                double v = q * dt;

                // Place bulk WQ at upstream node in new segment identified for link

                for (int j = 1; j <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; j++) {
                    if (_msx.Species[j].Type == SpeciesType.BULK)
                        _newSeg[i].C[j] = _msx.Node[n].C[j];
                }

                // If link has no last segment, then we must add a new one
                var useNewSeg = false;

                //seg = MSX.LastSeg[k];
                if (_msx.Segments[i].Count > 0)
                    seg = _msx.Segments[i].Last.Value; //get(MSX.Segments[k].size()-1);

                //else
                if (seg == null)
                    useNewSeg = true;

                // Ostherwise check if quality in last segment differs from that of the new segment
                else if (!MSXqual_isSame(seg.C, _newSeg[i].C))
                    useNewSeg = true;


                // Quality of last seg & new seg are close simply increase volume of last seg
                if (useNewSeg == false) {
                    seg.V = seg.V + v;
                    //MSXqual_removeSeg(NewSeg[k]);
                    _newSeg[i] = null;
                }
                else {
                    // Otherwise add the new seg to the end of the link
                    _newSeg[i].V = v;
                    _msx.Segments[i].AddLast(_newSeg[i]);
                    //MSXqual_addSeg(k, NewSeg[k]);
                }

            } //next link
        }

        ///<summary>Determines source concentration in current time period</summary>
        private double GetSourceQual(Source source) {
            double f = 1.0;

            // Get source concentration (or mass flow) in original units
            double c = source.C0;

            // Convert mass flow rate from min. to sec.
            if (source.Type == SourceType.MASS) c /= 60.0;

            // Apply time pattern if assigned
            int i = source.Pattern;

            if (i == 0)
                return c;

            long k = (_msx.Qtime + _msx.Pstart) / _msx.Pstep % _msx.Pattern[i].Length;

            if (k != _msx.Pattern[i].Interval) {
                if (k < _msx.Pattern[i].Interval) {
                    _msx.Pattern[i].Current = 0; //); = MSX.Pattern[i].first;
                    _msx.Pattern[i].Interval = 0; //interval = 0;
                }
                while (_msx.Pattern[i].Current != 0 && _msx.Pattern[i].Interval < k) {
                    _msx.Pattern[i].Current = _msx.Pattern[i].Current + 1;
                    _msx.Pattern[i].Interval = _msx.Pattern[i].Interval + 1;
                }
            }

            if (_msx.Pattern[i].Current != 0)
                f = _msx.Pattern[i].Multipliers[_msx.Pattern[i].Current];

            return c * f;
        }


        public Pipe CreateSeg(double v, double[] c) {
            var seg = new Pipe {
                C = new double[_msx.Nobjects[(int)ObjectTypes.SPECIES] + 1],
                V = v
            };

            // Assign volume, WQ, & integration time step to the new segment
            for (int i = 1; i <= _msx.Nobjects[(int)ObjectTypes.SPECIES]; i++)
                seg.C[i] = c[i];

            seg.Hstep = 0.0;
            return seg;
        }



    }

}