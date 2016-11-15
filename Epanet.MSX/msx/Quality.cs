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
            return this.flowDir[i] == '+' ? this.msx.Link[i].N1 : this.msx.Link[i].N2;
        }

        private int DOWN_NODE(int i) {
            return this.flowDir[i] == '+' ? this.msx.Link[i].N2 : this.msx.Link[i].N1;
        }

        private double Linkvol(int k) {
            return 0.785398 * this.msx.Link[k].Len * Math.Pow(this.msx.Link[k].Diam, 2);
        }

        //  External variables
        //--------------------
        private Network msx; // MSX project data
        private TankMix tank;
        private Chemical chemical;
        private Output @out;
        private ENToolkit2 tk2;

        public void LoadDependencies(EpanetMSX epa) {
            this.msx = epa.Network; // MSX project data
            this.tank = epa.TankMix;
            this.chemical = epa.Chemical;
            this.@out = epa.Output;
            this.tk2 = epa.EnToolkit;
        }

        //  Local variables
        //-----------------
        //<summary>pointer to unused pipe segment</summary>
        //Pipe            FreeSeg;
        ///<summary>new segment added to each pipe</summary>
        private Pipe[] newSeg;
        ///<summary>flow direction for each pipe</summary>
        private char[] flowDir;
        ///<summary>inflow flow volume to each node</summary>
        private double[] volIn;
        ///<summary>mass inflow of each species to each node</summary>
        private double[,] massIn;
        ///<summary>work matrix</summary>
        private double[,] x;
        ///<summary>wall species indicator</summary>
        private bool hasWallSpecies;
        //<summary>out of memory indicator</summary>
        //&bool         OutOfMemory;


        /// <summary>Opens the WQ routing system.</summary>
        public EnumTypes.ErrorCodeType MSXqual_open() {
            EnumTypes.ErrorCodeType errcode;

// --- set flags
            //MSX.QualityOpened = false;
            //MSX.Saveflag = false;
            //OutOfMemory = false;
            this.hasWallSpecies = false;

// --- initialize array pointers to null

            this.msx.C1 = null;
            this.msx.Segments = null;
            // MSX.LastSeg = null;
            this.x = null;
            this.newSeg = null;
            this.flowDir = null;
            this.volIn = null;
            this.massIn = null;

// --- open the chemistry system

            errcode = this.chemical.MSXchem_open();
            if (errcode > 0) return errcode;

// --- allocate a memory pool for pipe segments

            //QualPool = AllocInit();
            //if ( QualPool == null ) return ERR_MEMORY;

// --- allocate memory used for species concentrations

            this.x = new double[this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE] + 1,this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + 1];
            this.msx.C1 = new double[this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + 1];
            //(double *) calloc(MSX.Nobjects[ObjectTypes.SPECIES.id]+1, sizeof(double));

// --- allocate memory used for pointers to the first, last,
//     and new WQ segments in each link and tank

            int n = this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK]
                    + this.msx.Nobjects[(int)EnumTypes.ObjectTypes.TANK] + 1;
            this.msx.Segments = new LinkedList<Pipe>[n]; //(Pseg *) calloc(n, sizeof(Pseg));
            for (int i = 0; i < n; i++)
                this.msx.Segments[i] = new LinkedList<Pipe>();

            //MSX.LastSeg  = (Pseg *) calloc(n, sizeof(Pseg));
            this.newSeg = new Pipe[n]; //(Pseg *) calloc(n, sizeof(Pseg));

// --- allocate memory used flow direction in each link

            this.flowDir = new char[n]; //(char *) calloc(n, sizeof(char));

// --- allocate memory used to accumulate mass and volume
//     inflows to each node

            n = this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE] + 1;
            this.volIn = new double[n]; //(double *) calloc(n, sizeof(double));
            this.massIn = new double[n,this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + 1];

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

            for (n = 1; n <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; n++) {
                if (this.msx.Species[n].Type == EnumTypes.SpeciesType.WALL) this.hasWallSpecies = true;
            }
            //if ( errcode == 0)
            //    MSX.QualityOpened = true;
            return errcode;
        }


        /// <summary>Re-initializes the WQ routing system.</summary>
        public int MSXqual_init() {

            int errcode = 0;

            // Initialize node concentrations, tank volumes, & source mass flows
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; i++) {
                for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++)
                    this.msx.Node[i].C[j] = this.msx.Node[i].C0[j];
            }
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.TANK]; i++) {
                this.msx.Tank[i].Hstep = 0.0;
                this.msx.Tank[i].V = this.msx.Tank[i].V0;
                int nn = this.msx.Tank[i].Node;
                for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++)
                    this.msx.Tank[i].C[j] = this.msx.Node[nn].C0[j];
            }
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.PATTERN]; i++) {
                this.msx.Pattern[i].Interval = 0;
                this.msx.Pattern[i].Current = 0; //MSX.Pattern[i]);//first);
            }

            // Check if a separate WQ report is required
            this.msx.Rptflag = false;
            int n = 0;
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; i++)
                n += this.msx.Node[i].Rpt ? 1 : 0;
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; i++)
                n += this.msx.Link[i].Rpt ? 1 : 0;
            if (n > 0) {
                n = 0;
                for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; i++)
                    n += this.msx.Species[i].Rpt;
            }
            if (n > 0) this.msx.Rptflag = true;

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
            this.msx.Htime = 0; //Hydraulic solution time
            this.msx.Qtime = 0; //Quality routing time
            this.msx.Rtime = 0; //MSX.Rstart;                //Reporting time
            this.msx.Nperiods = 0; //Number fo reporting periods

            // open binary output file if results are to be saved
            //if ( MSX.Saveflag ) errcode = out.MSXout_open();
            return errcode;
        }

        ///<summary>Updates WQ conditions over a single WQ time step.</summary>
        public EnumTypes.ErrorCodeType MSXqual_step(long[] t, long[] tleft) {
            long dt, hstep, tstep;
            EnumTypes.ErrorCodeType errcode = 0;

            // Set the shared memory pool to the water quality pool and the overall time step to nominal WQ time step

            //AllocSetPool(QualPool);
            tstep = this.msx.Qstep;

            // Repeat until the end of the time step
            do {
                // Find the time until the next hydraulic event occurs
                dt = tstep;
                hstep = this.msx.Htime - this.msx.Qtime;

                // Check if next hydraulic event occurs within the current time step
                if (hstep <= dt) {
                    // Reduce current time step to end at next hydraulic event
                    dt = hstep;

                    // route WQ over this time step
                    if (dt > 0)
                        errcode = Utilities.Call(errcode, this.Transport(dt));

                    this.msx.Qtime += dt;

                    // retrieve new hydraulic solution
                    if (this.msx.Qtime == this.msx.Htime) errcode = Utilities.Call(errcode, this.GetHydVars());

                    // report results if its time to do so
                    if (this.msx.Qtime == this.msx.Rtime) // MSX.Saveflag &&
                    {
                        errcode = Utilities.Call(errcode, this.@out.MSXout_saveResults());
                        this.msx.Rtime += this.msx.Rstep;
                        this.msx.Nperiods++;
                    }
                }

                // Otherwise just route WQ over the current time step

                else {
                    errcode = Utilities.Call(errcode, this.Transport(dt));
                    this.msx.Qtime += dt;
                }

                // Reduce overall time step by the size of the current time step

                tstep -= dt;

            }
            while (errcode == 0 && tstep > 0);

            // Update the current time into the simulation and the amount remaining
            t[0] = this.msx.Qtime;
            tleft[0] = this.msx.Dur - this.msx.Qtime;

            // If there's no time remaining, then save the final records to output file
            if (tleft[0] <= 0)
                errcode = Utilities.Call(errcode, this.@out.MSXout_saveFinalResults())
                    ;

            return errcode;
        }

        /// <summary>Retrieves WQ for species m at node n.</summary>
        public double MSXqual_getNodeQual(int j, int m) {
// --- return 0 for WALL species

            if (this.msx.Species[m].Type == EnumTypes.SpeciesType.WALL) return 0.0;

// --- if node is a tank, return its internal concentration

            int k = this.msx.Node[j].Tank;
            if (k > 0 && this.msx.Tank[k].A > 0.0) {
                return this.msx.Tank[k].C[m];
            }

// --- otherwise return node's concentration (which includes
//     any contribution from external sources)

            return this.msx.Node[j].C[m];
        }

        /// <summary>Computes average quality in link k.</summary>
        public double MSXqual_getLinkQual(int k, int m) {
            double vsum = 0.0,
                   msum = 0.0;
            //Pipe    seg;

            foreach (Pipe seg  in  this.msx.Segments[k]) {
                vsum += seg.V;
                msum += seg.C[m] * seg.V;
                //seg = seg->prev;
            }
            if (vsum > 0.0) return msum / vsum;
            else {
                return (this.MSXqual_getNodeQual(this.msx.Link[k].N1, m) +
                        this.MSXqual_getNodeQual(this.msx.Link[k].N2, m)) / 2.0;
            }
        }

        /// <summary>Checks if two sets of concentrations are the same</summary>
        public bool MSXqual_isSame(double[] c1, double[] c2) {
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; i++) {
                if (Math.Abs(c1[i] - c2[i]) >= this.msx.Species[i].ATol) return false;
            }
            return true;
        }

        /// <summary>
        /// Retrieves hydraulic solution and time step for next hydraulic event  from a hydraulics file.
        /// </summary>
        private EnumTypes.ErrorCodeType GetHydVars() {
            AwareStep step = this.tk2.GetStep((int)this.msx.Htime);

            int n = this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE];
            for (int i = 0; i < n; i++)
                this.msx.D[i + 1] = (float)step.GetNodeDemand(i, null, null);

            for (int i = 0; i < n; i++)
                this.msx.H[i + 1] = (float)step.GetNodeHead(i, null, null);

            n = this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK];

            for (int i = 0; i < n; i++)
                this.msx.Q[i + 1] = (float)step.GetLinkFlow(i, null, null);

            // update elapsed time until next hydraulic event
            this.msx.Htime = step.Time + step.Step;

            // Initialize pipe segments (at time 0) or else re-orient segments to accommodate any flow reversals
            if (this.msx.Qtime < this.msx.Dur) {
                if (this.msx.Qtime == 0)
                    this.InitSegs();
                else
                    this.ReorientSegs();
            }

            return 0;
        }

        /// <summary>
        /// Transports constituent mass through pipe network
        /// under a period of constant hydraulic conditions.
        /// </summary>
        private EnumTypes.ErrorCodeType Transport(long tstep) {
            long qtime, dt;
            EnumTypes.ErrorCodeType errcode = 0;

// --- repeat until time step is exhausted

            qtime = 0;
            while (errcode == 0 &&
                   qtime < tstep) {
                // Qstep is nominal quality time step
                dt = Math.Min(this.msx.Qstep, tstep - qtime); // get actual time step
                qtime += dt; // update amount of input tstep taken
                errcode = this.chemical.MSXchem_react(dt); // react species in each pipe & tank
                if (errcode != 0)
                    return errcode;
                this.AdvectSegs(dt); // advect segments in each pipe
                this.Accumulate(dt); // accumulate all inflows at nodes
                this.UpdateNodes(dt); // update nodal quality
                this.SourceInput(dt); // compute nodal inputs from sources
                this.Release(dt); // release new outflows from nodes
            }
            return errcode;
        }

        /// <summary>Initializes water quality in pipe segments.</summary>
        private void InitSegs() {


// --- examine each link

            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; i++) {
                // --- establish flow direction

                this.flowDir[i] = '+';
                if (this.msx.Q[i] < 0.0) this.flowDir[i] = '-';

// --- start with no segments

                //MSX.LastSeg[k] = null;
                this.msx.Segments[i].Clear();
                this.newSeg[i] = null;

// --- use quality of downstream node for BULK species
//     if no initial link quality supplied

                int n = this.DOWN_NODE(i);
                for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) {
                    if (this.msx.Link[i].C0[j] != Constants.MISSING)
                        this.msx.C1[j] = this.msx.Link[i].C0[j];
                    else if (this.msx.Species[j].Type == EnumTypes.SpeciesType.BULK)
                        this.msx.C1[j] = this.msx.Node[n].C0[j];
                    else this.msx.C1[j] = 0.0;
                }

                // --- fill link with a single segment of this quality

                var v = this.Linkvol(i);
                if (v > 0.0)
                    this.msx.Segments[i].AddLast(this.CreateSeg(v, this.msx.C1));
                //MSXqual_addSeg(k, createSeg(v, MSX.C1));
            }

// --- initialize segments in tanks

            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.TANK]; i++) {
                // --- skip reservoirs

                if (this.msx.Tank[i].A == 0.0) continue;

// --- tank segment pointers are stored after those for links

                int k = this.msx.Tank[i].Node;
                for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++)
                    this.msx.C1[j] = this.msx.Node[k].C0[j];
                k = this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK] + i;
                //MSX.LastSeg[k] = null;
                this.msx.Segments[k].Clear();

// --- add 2 segments for 2-compartment model

                if (this.msx.Tank[i].MixModel == EnumTypes.MixType.MIX2) {
                    var v = Math.Max(0, this.msx.Tank[i].V - this.msx.Tank[i].VMix);
                    //MSXqual_addSeg(k, createSeg(v, MSX.C1));
                    this.msx.Segments[k].AddLast(this.CreateSeg(v, this.msx.C1));
                    v = this.msx.Tank[i].V - v;
                    //MSXqual_addSeg(k, createSeg(v, MSX.C1));
                    this.msx.Segments[k].AddLast(this.CreateSeg(v, this.msx.C1));
                }

                // --- add one segment for all other models

                else {
                    var v = this.msx.Tank[i].V;
                    this.msx.Segments[k].AddLast(this.CreateSeg(v, this.msx.C1));
                    //MSXqual_addSeg(k, createSeg(v, MSX.C1));
                }
            }
        }

        /// <summary>Re-orients pipe segments (if flow reverses).</summary>
        private void ReorientSegs() {
            //Pseg   seg, pseg, nseg;

// --- examine each link

            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; i++) {
                // --- find new flow direction

                var newdir = '+';
                if (this.msx.Q[i] == 0.0) newdir = this.flowDir[i];
                else if (this.msx.Q[i] < 0.0) newdir = '-';

// --- if direction changes, then reverse the order of segments
//     (first to last) and save new direction

                if (newdir != this.flowDir[i]) {
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
                    this.msx.Segments[i].Reverse();
                    this.flowDir[i] = newdir;
                }
            }
        }

        ///<summary>Advects WQ segments within each pipe.</summary>
        private void AdvectSegs(long dt) {

            // Examine each link

            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; i++) {
                // Zero out WQ in new segment to be added at entrance of link
                for (int m = 1; m <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                    this.msx.C1[m] = 0.0;

                // Get a free segment to add to entrance of link
                this.newSeg[i] = this.CreateSeg(0.0d, this.msx.C1);

                // Skip zero-length links (pumps & valves) & no-flow links
                if (this.newSeg[i] == null ||
                    this.msx.Link[i].Len == 0.0 || this.msx.Q[i] == 0.0) continue;

                // Find conc. of wall species in new segment to be added and adjust conc.
                // of wall species to reflect shifted positions of existing segments
                if (this.hasWallSpecies) {
                    this.GetNewSegWallQual(i, dt, this.newSeg[i]);
                    this.ShiftSegWallQual(i, dt);
                }
            }
        }

        ///<summary>
        /// Computes wall species concentrations for a new WQ segment that
        /// enters a pipe from its upstream node.
        /// </summary>
        private void GetNewSegWallQual(int k, long dt, Pipe newseg) {
            //Pipe  seg;
            double v, vin, vsum, vadded, vleft;


            if (newseg == null)
                return;

            // Get volume of inflow to link
            v = this.Linkvol(k);
            vin = Math.Abs(this.msx.Q[k]) * dt;
            if (vin > v) vin = v;

            // Start at last (most upstream) existing WQ segment
            vsum = 0.0;
            vleft = vin;
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; i++) {
                if (this.msx.Species[i].Type == EnumTypes.SpeciesType.WALL)
                    newseg.C[i] = 0.0;
            }

            // repeat while some inflow volume still remains

            for (var segIt = this.msx.Segments[k].Last; segIt != null; segIt = segIt.Previous) {
                // Move to next downstream WQ segment
                Pipe seg = segIt.Value;

                // Find volume added by this segment
                vadded = seg.V;
                if (vadded > vleft) vadded = vleft;

                // Update total volume added and inflow volume remaining

                vsum += vadded;
                vleft -= vadded;

                // Add wall species mass contributed by this segment to new segment
                for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; i++) {
                    if (this.msx.Species[i].Type == EnumTypes.SpeciesType.WALL)
                        newseg.C[i] += vadded * seg.C[i];
                }
            }

            // Convert mass of wall species in new segment to concentration

            if (vsum > 0.0) {
                for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; i++) {
                    if (this.msx.Species[i].Type == EnumTypes.SpeciesType.WALL) newseg.C[i] /= vsum;
                }
            }
        }

        ///<summary>
        /// Recomputes wall species concentrations in segments that remain
        /// within a pipe after flow is advected over current time step.
        /// </summary>
        private void ShiftSegWallQual(int k, long dt) {
            double v, vin, vstart, vend, vcur, vsum;

            // Find volume of water displaced in pipe
            v = this.Linkvol(k);
            vin = Math.Abs(this.msx.Q[k]) * dt;
            if (vin > v) vin = v;

            // Set future start position (measured by pipe volume) of original last segment
            vstart = vin;

            // Examine each segment, from upstream to downstream
            for (var segIt = this.msx.Segments[k].Last; segIt != null; segIt = segIt.Previous) {
                // Move to next downstream WQ segment
                Pipe seg1 = segIt.Value;

                // Initialize a "mixture" WQ
                for (int m = 1; m <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                    this.msx.C1[m] = 0.0;

                // Find the future end position of this segment

                vend = vstart + seg1.V;
                if (vend > v)
                    vend = v;

                vcur = vstart;
                vsum = 0;

                // find volume taken up by the segment after it moves down the pipe
                Pipe seg2 = null;

                for (var segIt2 = this.msx.Segments[k].Last; segIt2 != null; segIt2 = segIt.Previous) {
                    // Move to next downstream WQ segment
                    seg2 = segIt2.Value;

                    if (seg2.V == 0.0)
                        continue;

                    vsum += seg2.V;
                    if (vsum >= vstart && vsum <= vend) {
                        for (int m = 1; m <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++) {
                            if (this.msx.Species[m].Type == EnumTypes.SpeciesType.WALL)
                                this.msx.C1[m] += (vsum - vcur) * seg2.C[m];
                        }
                        vcur = vsum;
                    }
                    if (vsum >= vend) break;
                }

                // Update the wall species concentrations in the segment
                for (int m = 1; m <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++) {
                    if (this.msx.Species[m].Type != EnumTypes.SpeciesType.WALL)
                        continue;

                    if (seg2 != null)
                        this.msx.C1[m] += (vend - vcur) * seg2.C[m];

                    seg1.C[m] = this.msx.C1[m] / (vend - vstart);

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
            double cseg, v, vseg;


            // Compute average conc. of segments incident on each node
            //     (for use if there is no transport through the node)
            this.GetIncidentConcen();

            // Reset cumlulative inflow to each node to zero
            Array.Clear(this.volIn, 0, this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE] + 1);

            for (int ij = 0; ij < this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE] + 1; ij++)
                for (int jj = 0; jj < this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + 1; jj++)
                    this.massIn[ij, jj] = 0.0;

            // move mass from first segment of each link into link's downstream node

            for (int k = 1; k <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; k++) {
                int i = this.UP_NODE(k); // upstream node
                int j = this.DOWN_NODE(k); // downstream node
                v = Math.Abs(this.msx.Q[k]) * dt; // flow volume

                // if link volume < flow volume, then transport upstream node's
                // quality to downstream node and remove all link segments

                if (this.Linkvol(k) < v) {
                    Pipe seg = null;
                    this.volIn[j] += v;
                    if (this.msx.Segments[k].Count > 0)
                        seg = this.msx.Segments[k].First.Value; //get(0);
                    for (int m = 1; m <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++) {
                        if (this.msx.Species[m].Type != EnumTypes.SpeciesType.BULK) continue;
                        cseg = this.msx.Node[i].C[m];
                        if (seg != null) cseg = seg.C[m];
                        this.massIn[j,m] += v * cseg;
                    }
                    // Remove all segments in the pipe
                    this.msx.Segments[k].Clear();
                }
                else
                    while (v > 0.0) {
                        // Otherwise remove flow volume from leading segments and accumulate flow mass at
                        // downstream node identify leading segment in pipe
                        Pipe seg = null;

                        if (this.msx.Segments[k].Count > 0)
                            seg = this.msx.Segments[k].First.Value; //get(0);

                        if (seg == null)
                            break;

                        // Volume transported from this segment is minimum of remaining flow volume & segment volume
                        // (unless leading segment is also last segment)

                        vseg = seg.V;
                        vseg = Math.Min(vseg, v);

                        if (this.msx.Segments[k].Count == 1) // if (seg == MSX.LastSeg[k]) vseg = v;
                            vseg = v;

                        //update volume & mass entering downstream node
                        for (int m = 1; m <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++) {
                            if (this.msx.Species[m].Type != EnumTypes.SpeciesType.BULK) continue;
                            cseg = seg.C[m];
                            this.massIn[j,m] += vseg * cseg;
                        }
                        this.volIn[j] += vseg;

                        // Reduce flow volume by amount transported
                        v -= vseg;

                        // If all of segment's volume was transferred, then replace leading segment with the one behind it
                        // (Note that the current seg is recycled for later use.)
                        if (v >= 0.0 && vseg >= seg.V) {
                            if (this.msx.Segments[k].Count > 0)
                                this.msx.Segments[k].RemoveFirst(); //remove(0);
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
            Array.Clear(this.volIn, 0, this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE] + 1);
            for (int i = 0; i < this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE] + 1; i++)
                for (int j = 0; j < this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + 1; j++) {
                    this.massIn[i,j] = 0.0;
                    this.x[i,j] = 0.0;
                }
            // examine each link
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; i++) {
                int jj = this.DOWN_NODE(i); // downstream node
                if (this.msx.Segments[i].Count > 0) // accumulate concentrations
                {
                    for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) {
                        if (this.msx.Species[j].Type == EnumTypes.SpeciesType.BULK)
                            this.massIn[jj, j] += this.msx.Segments[i].First.Value.C[j];
                    }
                    this.volIn[jj]++;
                }
                jj = this.UP_NODE(i); // upstream node
                if (this.msx.Segments[i].Count > 0) // accumulate concentrations
                {
                    for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) {
                        if (this.msx.Species[j].Type == EnumTypes.SpeciesType.BULK)
                            this.massIn[jj, j] += this.msx.Segments[i].Last.Value.C[j];
                        //get(MSX.Segments[k].size()-1).getC()[m];
                    }
                    this.volIn[jj]++;
                }
            }

            // Compute avg. incident concen. at each node
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; i++) {
                if (this.volIn[i] > 0.0) {
                    for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++)
                        this.x[i, j] = this.massIn[i, j] / this.volIn[i];
                }
            }
        }

        ///<summary>
        /// Updates the concentration at each node to the mixture
        /// concentration of the accumulated inflow from connecting pipes.
        /// </summary>
        private void UpdateNodes(long dt) {
            // Examine each node
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; i++) {
                // Node is a junction
                int jj = this.msx.Node[i].Tank;
                if (jj <= 0) {
                    // Add any external inflow (i.e., negative demand) to total inflow volume

                    if (this.msx.D[i] < 0.0)
                        this.volIn[i] -= this.msx.D[i] * dt;

                    // If inflow volume is non-zero, then compute the mixture
                    // concentration resulting at the node
                    if (this.volIn[i] > 0.0) {
                        for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++)
                            this.msx.Node[i].C[j] = this.massIn[i, j] / this.volIn[i];
                    }
                    // Otherwise use the avg. of the concentrations in the links incident on the node
                    else {
                        for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++)
                            this.msx.Node[i].C[j] = this.x[i, j];
                    }

                    // Compute new equilibrium mixture
                    this.chemical.MSXchem_equil(EnumTypes.ObjectTypes.NODE, this.msx.Node[i].C);
                }

                // Node is a tank or reservoir
                else {

                    if (this.msx.Tank[jj].A == 0.0) {
                        // Use initial quality for reservoirs
                        for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++)
                            this.msx.Node[i].C[j] = this.msx.Node[i].C0[j];
                    }
                    else {
                        // otherwise update tank WQ based on mixing model
                        if (this.volIn[i] > 0.0) {
                            for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) {
                                this.msx.C1[j] = this.massIn[i,j] / this.volIn[i];
                            }
                        }
                        else
                            for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++)
                                this.msx.C1[j] = 0.0;

                        switch (this.msx.Tank[jj].MixModel) {
                        case EnumTypes.MixType.MIX1:
                            this.tank.MSXtank_mix1(jj, this.volIn[i], this.msx.C1, dt);
                            break;
                        case EnumTypes.MixType.MIX2:
                            this.tank.MSXtank_mix2(jj, this.volIn[i], this.msx.C1, dt);
                            break;
                        case EnumTypes.MixType.FIFO:
                            this.tank.MSXtank_mix3(jj, this.volIn[i], this.msx.C1, dt);
                            break;
                        case EnumTypes.MixType.LIFO:
                            this.tank.MSXtank_mix4(jj, this.volIn[i], this.msx.C1, dt);
                            break;
                        }
                        for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++)
                            this.msx.Node[i].C[j] = this.msx.Tank[jj].C[j];
                        this.msx.Tank[jj].V = this.msx.Tank[jj].V + this.msx.D[i] * dt;
                    }
                }
            }
        }

        ///<summary>Computes contribution (if any) of mass additions from WQ sources at each node.</summary>
        private void SourceInput(long dt) {
            // Establish a flow cutoff which indicates no outflow from a node
            double qcutoff = 10.0 * Constants.TINY;

            // consider each node
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; i++) {
                // Skip node if no WQ source
                if (this.msx.Node[i].Sources.Count == 0)
                    continue;

                // find total flow volume leaving node

                double volout = this.msx.Node[i].Tank == 0
                    ? this.volIn[i] // Junctions
                    : this.volIn[i] - this.msx.D[i] * dt;

                double qout = volout / dt;

                // evaluate source input only if node outflow > cutoff flow

                if (qout <= qcutoff)
                    continue;

                // Add contribution of each source species
                foreach (Source source  in  this.msx.Node[i].Sources) {
                    this.AddSource(i, source, volout, dt);
                }

                // Compute a new chemical equilibrium at the source node
                this.chemical.MSXchem_equil(EnumTypes.ObjectTypes.NODE, this.msx.Node[i].C);
            }
        }


        ///<summary>Updates concentration of particular species leaving a node that receives external source input.</summary>
        private void AddSource(int n, Source source, double volout, long dt) {
            double massadded, s;

            // Only analyze bulk species
            int m = source.Species;
            massadded = 0.0;
            if (!(source.C0 > 0.0) || this.msx.Species[m].Type != EnumTypes.SpeciesType.BULK) return;
            
            // Mass added depends on type of source

            s = this.GetSourceQual(source);
            switch (source.Type) {
            // Concen. Source : Mass added = source concen. * -(demand)
            case EnumTypes.SourceType.CONCEN:
                // Only add source mass if demand is negative
                if (this.msx.D[n] < 0.0)
                    massadded = -s * this.msx.D[n] * dt;

                // If node is a tank then set concen. to 0.
                // (It will be re-set to true value later on)
                if (this.msx.Node[n].Tank > 0)
                    this.msx.Node[n].C[m] = 0.0;
                break;

            // Mass Inflow Booster Source
            case EnumTypes.SourceType.MASS:
                massadded = s * dt / Constants.LperFT3;
                break;

            // Setpoint Booster Source: Mass added is difference between source & node concen. times outflow volume
            case EnumTypes.SourceType.SETPOINT:
                if (s > this.msx.Node[n].C[m])
                    massadded = (s - this.msx.Node[n].C[m]) * volout;
                break;

            // Flow-Paced Booster Source: Mass added = source concen. times outflow volume
            case EnumTypes.SourceType.FLOWPACED:
                massadded = s * volout;
                break;
            }

            // Adjust nodal concentration to reflect source addition
            this.msx.Node[n].C[m] += massadded / volout;
        }

        ///<summary>Releases outflow from nodes into incident links.</summary>
        private void Release(long dt) {
            int useNewSeg;
            double q, v;
            Pipe seg = null;

            // Examine each link
            for (int i = 1; i <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; i++) {
                // Ignore links with no flow

                if (this.msx.Q[i] == 0.0) {
                    //MSXqual_removeSeg(NewSeg[k]);
                    this.newSeg[i] = null;
                    continue;
                }

                // Find flow volume released to link from upstream node
                // (NOTE: Flow volume is allowed to be > link volume.)
                int n = this.UP_NODE(i);
                q = Math.Abs(this.msx.Q[i]);
                v = q * dt;

                // Place bulk WQ at upstream node in new segment identified for link

                for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) {
                    if (this.msx.Species[j].Type == EnumTypes.SpeciesType.BULK)
                        this.newSeg[i].C[j] = this.msx.Node[n].C[j];
                }

                // If link has no last segment, then we must add a new one
                useNewSeg = 0;
                //seg = MSX.LastSeg[k];
                if (this.msx.Segments[i].Count > 0)
                    seg = this.msx.Segments[i].Last.Value; //get(MSX.Segments[k].size()-1);

                //else
                if (seg == null)
                    useNewSeg = 1;

                // Ostherwise check if quality in last segment differs from that of the new segment
                else if (!this.MSXqual_isSame(seg.C, this.newSeg[i].C))
                    useNewSeg = 1;


                // Quality of last seg & new seg are close simply increase volume of last seg
                if (useNewSeg == 0) {
                    seg.V = seg.V + v;
                    //MSXqual_removeSeg(NewSeg[k]);
                    this.newSeg[i] = null;
                }
                else {
                    // Otherwise add the new seg to the end of the link
                    this.newSeg[i].V = v;
                    this.msx.Segments[i].AddLast(this.newSeg[i]);
                    //MSXqual_addSeg(k, NewSeg[k]);
                }

            } //next link
        }

        ///<summary>Determines source concentration in current time period</summary>
        private double GetSourceQual(Source source) {
            int i;
            long k;
            double c, f = 1.0;

            // Get source concentration (or mass flow) in original units
            c = source.C0;

            // Convert mass flow rate from min. to sec.
            if (source.Type == EnumTypes.SourceType.MASS) c /= 60.0;

            // Apply time pattern if assigned
            i = source.Pattern;

            if (i == 0)
                return c;

            k = (this.msx.Qtime + this.msx.Pstart) / this.msx.Pstep % this.msx.Pattern[i].Length;

            if (k != this.msx.Pattern[i].Interval) {
                if (k < this.msx.Pattern[i].Interval) {
                    this.msx.Pattern[i].Current = 0; //); = MSX.Pattern[i].first;
                    this.msx.Pattern[i].Interval = 0; //interval = 0;
                }
                while (this.msx.Pattern[i].Current != 0 && this.msx.Pattern[i].Interval < k) {
                    this.msx.Pattern[i].Current = this.msx.Pattern[i].Current + 1;
                    this.msx.Pattern[i].Interval = this.msx.Pattern[i].Interval + 1;
                }
            }

            if (this.msx.Pattern[i].Current != 0)
                f = this.msx.Pattern[i].Multipliers[this.msx.Pattern[i].Current];

            return c * f;
        }


        public Pipe CreateSeg(double v, double[] c) {
            var seg = new Pipe();
            seg.C = new double[this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + 1];

            // Assign volume, WQ, & integration time step to the new segment
            seg.V = v;
            for (int m = 1; m <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                seg.C[m] = c[m];

            seg.Hstep = 0.0;
            return seg;
        }



    }

}