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
using org.addition.epanet.hydraulic.io;
using org.addition.epanet.msx.Structures;
using org.addition.epanet.util;

namespace org.addition.epanet.msx
{

    public class Quality
    {


        private int UP_NODE(int x)
        {
            return (FlowDir[(x)] == '+') ? MSX.Link[(x)].getN1() : MSX.Link[(x)].getN2();
        }

        private int DOWN_NODE(int x)
        {
            return (FlowDir[(x)] == '+') ? MSX.Link[(x)].getN2() : MSX.Link[(x)].getN1();
        }

        private double LINKVOL(int k)
        {
            return 0.785398*MSX.Link[(k)].getLen()*Math.Pow(MSX.Link[(k)].getDiam(), 2);
        }

        //  External variables
        //--------------------
        private Network MSX; // MSX project data
        private TankMix tank;
        private Chemical chemical;
        private Output @out;
        private ENToolkit2 tk2;

        public void loadDependencies(EpanetMSX epa)
        {
            MSX = epa.getNetwork(); // MSX project data
            tank = epa.getTankMix();
            chemical = epa.getChemical();
            @out = epa.getOutput();
            tk2 = epa.getENToolkit();
        }

        //  Local variables
        //-----------------
        //Pipe            FreeSeg;         // pointer to unused pipe segment
        private Pipe[] NewSeg; // new segment added to each pipe
        private char[] FlowDir; // flow direction for each pipe
        private double[] VolIn; // inflow flow volume to each node
        private double[][] MassIn; // mass inflow of each species to each node
        private double[][] X; // work matrix
        private bool HasWallSpecies; // wall species indicator
        //&bool         OutOfMemory;     // out of memory indicator



        //=============================================================================
        // opens the WQ routing system.
        public EnumTypes.ErrorCodeType MSXqual_open()
        {
            EnumTypes.ErrorCodeType errcode;
            int n;

// --- set flags
            //MSX.QualityOpened = false;
            //MSX.Saveflag = false;
            //OutOfMemory = false;
            HasWallSpecies = false;

// --- initialize array pointers to null

            MSX.C1 = null;
            MSX.Segments = null;
            // MSX.LastSeg = null;
            X = null;
            NewSeg = null;
            FlowDir = null;
            VolIn = null;
            MassIn = null;

// --- open the chemistry system

            errcode = chemical.MSXchem_open();
            if (errcode > 0) return errcode;

// --- allocate a memory pool for pipe segments

            //QualPool = AllocInit();
            //if ( QualPool == null ) return ERR_MEMORY;

// --- allocate memory used for species concentrations

            X = Utilities.createMatrix(MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE] + 1, MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + 1);
            MSX.C1 = new double[MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + 1];
                //(double *) calloc(MSX.Nobjects[ObjectTypes.SPECIES.id]+1, sizeof(double));

// --- allocate memory used for pointers to the first, last,
//     and new WQ segments in each link and tank

            n = MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK] + MSX.Nobjects[(int)EnumTypes.ObjectTypes.TANK] + 1;
            MSX.Segments = new LinkedList<Pipe>[n]; //(Pseg *) calloc(n, sizeof(Pseg));
            for (int i = 0; i < n; i++)
                MSX.Segments[i] = new LinkedList<Pipe>();

            //MSX.LastSeg  = (Pseg *) calloc(n, sizeof(Pseg));
            NewSeg = new Pipe[n]; //(Pseg *) calloc(n, sizeof(Pseg));

// --- allocate memory used flow direction in each link

            FlowDir = new char[n]; //(char *) calloc(n, sizeof(char));

// --- allocate memory used to accumulate mass and volume
//     inflows to each node

            n = MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE] + 1;
            VolIn = new double[n]; //(double *) calloc(n, sizeof(double));
            MassIn = Utilities.createMatrix(n, MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + 1);

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

            for (n = 1; n <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; n++)
            {
                if (MSX.Species[n].getType() == EnumTypes.SpeciesType.WALL) HasWallSpecies = true;
            }
            //if ( errcode == 0)
            //    MSX.QualityOpened = true;
            return (errcode);
        }


        // Re-initializes the WQ routing system.
        public int MSXqual_init()
        {
            int i, n, m;
            int errcode = 0;

            // Initialize node concentrations, tank volumes, & source mass flows
            for (i = 1; i <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; i++)
            {
                for (m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                    MSX.Node[i].getC()[m] = MSX.Node[i].getC0()[m];
            }
            for (i = 1; i <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.TANK]; i++)
            {
                MSX.Tank[i].setHstep(0.0);
                MSX.Tank[i].setV(MSX.Tank[i].getV0());
                n = MSX.Tank[i].getNode();
                for (m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                    MSX.Tank[i].getC()[m] = MSX.Node[n].getC0()[m];
            }
            for (i = 1; i <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.PATTERN]; i++)
            {
                MSX.Pattern[i].setInterval(0);
                MSX.Pattern[i].setCurrent(0); //MSX.Pattern[i]);//first);
            }

            // Check if a separate WQ report is required
            MSX.Rptflag = false;
            n = 0;
            for (i = 1; i <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; i++) n += MSX.Node[i].getRpt() ? 1 : 0;
            for (i = 1; i <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; i++) n += MSX.Link[i].getRpt() ? 1 : 0;
            if (n > 0)
            {
                n = 0;
                for (m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++) n += MSX.Species[m].getRpt();
            }
            if (n > 0) MSX.Rptflag = true;

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
            MSX.Htime = 0; //Hydraulic solution time
            MSX.Qtime = 0; //Quality routing time
            MSX.Rtime = 0; //MSX.Rstart;                //Reporting time
            MSX.Nperiods = 0; //Number fo reporting periods

            // open binary output file if results are to be saved
            //if ( MSX.Saveflag ) errcode = out.MSXout_open();
            return errcode;
        }

        ///<summary>Updates WQ conditions over a single WQ time step.</summary>
        public EnumTypes.ErrorCodeType MSXqual_step(long[] t, long[] tleft)
        {
            long dt, hstep, tstep;
            EnumTypes.ErrorCodeType errcode = 0;

            // Set the shared memory pool to the water quality pool and the overall time step to nominal WQ time step

            //AllocSetPool(QualPool);
            tstep = MSX.Qstep;

            // Repeat until the end of the time step
            do
            {
                // Find the time until the next hydraulic event occurs
                dt = tstep;
                hstep = MSX.Htime - MSX.Qtime;

                // Check if next hydraulic event occurs within the current time step
                if (hstep <= dt)
                {
                    // Reduce current time step to end at next hydraulic event
                    dt = hstep;

                    // route WQ over this time step
                    if (dt > 0)
                        errcode = Utilities.CALL(errcode, transport(dt));

                    MSX.Qtime += dt;

                    // retrieve new hydraulic solution
                    if (MSX.Qtime == MSX.Htime) errcode = Utilities.CALL(errcode, getHydVars());

                    // report results if its time to do so
                    if (MSX.Qtime == MSX.Rtime) // MSX.Saveflag &&
                    {
                        errcode = Utilities.CALL(errcode, @out.MSXout_saveResults());
                        MSX.Rtime += MSX.Rstep;
                        MSX.Nperiods++;
                    }
                }

                    // Otherwise just route WQ over the current time step

                else
                {
                    errcode = Utilities.CALL(errcode, transport(dt));
                    MSX.Qtime += dt;
                }

                // Reduce overall time step by the size of the current time step

                tstep -= dt;

            } while (errcode == 0 && tstep > 0);

            // Update the current time into the simulation and the amount remaining
            t[0] = MSX.Qtime;
            tleft[0] = MSX.Dur - MSX.Qtime;

            // If there's no time remaining, then save the final records to output file
            if (tleft[0] <= 0)
                errcode = Utilities.CALL(errcode, @out.MSXout_saveFinalResults())
            ;

            return errcode;
        }

        //=============================================================================
        // retrieves WQ for species m at node n.
        public double MSXqual_getNodeQual(int j, int m)
        {
            int k;

// --- return 0 for WALL species

            if (MSX.Species[m].getType() == EnumTypes.SpeciesType.WALL) return 0.0;

// --- if node is a tank, return its internal concentration

            k = MSX.Node[j].getTank();
            if (k > 0 && MSX.Tank[k].getA() > 0.0)
            {
                return MSX.Tank[k].getC()[m];
            }

// --- otherwise return node's concentration (which includes
//     any contribution from external sources)

            return MSX.Node[j].getC()[m];
        }

        //=============================================================================
        // Computes average quality in link k.
        public double MSXqual_getLinkQual(int k, int m)
        {
            double vsum = 0.0,
                msum = 0.0;
            //Pipe    seg;

            //seg = MSX.Segments[k][0];
            //while (seg != null)
            foreach (Pipe seg  in  MSX.Segments[k])
            {
                vsum += seg.getV();
                msum += (seg.getC()[m])*(seg.getV());
                //seg = seg->prev;
            }
            if (vsum > 0.0) return (msum/vsum);
            else
            {
                return (MSXqual_getNodeQual(MSX.Link[k].getN1(), m) +
                        MSXqual_getNodeQual(MSX.Link[k].getN2(), m))/2.0;
            }
        }

        //  Closes the WQ routing system.
        private int MSXqual_close()
        {
            int errcode = 0;
            //MSX.QualityOpened = false;
            return errcode;
        }

        // Checks if two sets of concentrations are the same
        public bool MSXqual_isSame(double[] c1, double[] c2)
    {
        int m;
        for (m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
        {
            if (Math.Abs(c1[m] - c2[m]) >= MSX.Species[m].getaTol()) return false;
        }
        return true;
    }

        // Retrieves hydraulic solution and time step for next hydraulic event  from a hydraulics file.
        private EnumTypes.ErrorCodeType getHydVars()
        {
            int n;

            AwareStep step = tk2.getStep((int) MSX.Htime);

            n = MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE];
            for (int i = 0; i < n; i++)
                MSX.D[i + 1] = (float) step.getNodeDemand(i, null, null);

            for (int i = 0; i < n; i++)
                MSX.H[i + 1] = (float) step.getNodeHead(i, null, null);

            n = MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK];

            for (int i = 0; i < n; i++)
                MSX.Q[i + 1] = (float) step.getLinkFlow(i, null, null);

            // update elapsed time until next hydraulic event
            MSX.Htime = step.getTime() + step.getStep();

            // Initialize pipe segments (at time 0) or else re-orient segments to accommodate any flow reversals
            if (MSX.Qtime < MSX.Dur)
            {
                if (MSX.Qtime == 0)
                    initSegs();
                else
                    reorientSegs();
            }

            return 0;
        }

        //=============================================================================
        //transports constituent mass through pipe network
        //    under a period of constant hydraulic conditions.
        private EnumTypes.ErrorCodeType transport(long tstep)
        {
            long qtime, dt;
            EnumTypes.ErrorCodeType errcode = 0;

// --- repeat until time step is exhausted

            qtime = 0;
            while (errcode == 0 &&
                   qtime < tstep)
            {
                // Qstep is nominal quality time step
                dt = Math.Min(MSX.Qstep, tstep - qtime); // get actual time step
                qtime += dt; // update amount of input tstep taken
                errcode = chemical.MSXchem_react(dt); // react species in each pipe & tank
                if (errcode != 0)
                    return errcode;
                advectSegs(dt); // advect segments in each pipe
                accumulate(dt); // accumulate all inflows at nodes
                updateNodes(dt); // update nodal quality
                sourceInput(dt); // compute nodal inputs from sources
                release(dt); // release new outflows from nodes
            }
            return errcode;
        }

        //=============================================================================
        // initializes water quality in pipe segments.
        private void initSegs()
        {
            int j, k, m;
            double v;

// --- examine each link

            for (k = 1; k <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; k++)
            {
                // --- establish flow direction

                FlowDir[k] = '+';
                if (MSX.Q[k] < 0.0) FlowDir[k] = '-';

// --- start with no segments

                //MSX.LastSeg[k] = null;
                MSX.Segments[k].Clear();
                NewSeg[k] = null;

// --- use quality of downstream node for BULK species
//     if no initial link quality supplied

                j = DOWN_NODE(k);
                for (m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                {
                    if (MSX.Link[k].getC0()[m] != Constants.MISSING)
                        MSX.C1[m] = MSX.Link[k].getC0()[m];
                    else if (MSX.Species[m].getType() == EnumTypes.SpeciesType.BULK)
                        MSX.C1[m] = MSX.Node[j].getC0()[m];
                    else MSX.C1[m] = 0.0;
                }

                // --- fill link with a single segment of this quality

                v = LINKVOL(k);
                if (v > 0.0)
                    MSX.Segments[k].AddLast(createSeg(v, MSX.C1));
                //MSXqual_addSeg(k, createSeg(v, MSX.C1));
            }

// --- initialize segments in tanks

            for (j = 1; j <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.TANK]; j++)
            {
                // --- skip reservoirs

                if (MSX.Tank[j].getA() == 0.0) continue;

// --- tank segment pointers are stored after those for links

                k = MSX.Tank[j].getNode();
                for (m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                    MSX.C1[m] = MSX.Node[k].getC0()[m];
                k = MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK] + j;
                //MSX.LastSeg[k] = null;
                MSX.Segments[k].Clear();

// --- add 2 segments for 2-compartment model

                if (MSX.Tank[j].getMixModel() == EnumTypes.MixType.MIX2)
                {
                    v = Math.Max(0, MSX.Tank[j].getV() - MSX.Tank[j].getvMix());
                    //MSXqual_addSeg(k, createSeg(v, MSX.C1));
                    MSX.Segments[k].AddLast(createSeg(v, MSX.C1));
                    v = MSX.Tank[j].getV() - v;
                    //MSXqual_addSeg(k, createSeg(v, MSX.C1));
                    MSX.Segments[k].AddLast(createSeg(v, MSX.C1));
                }

                    // --- add one segment for all other models

                else
                {
                    v = MSX.Tank[j].getV();
                    MSX.Segments[k].AddLast(createSeg(v, MSX.C1));
                    //MSXqual_addSeg(k, createSeg(v, MSX.C1));
                }
            }
        }

        //=============================================================================
        // re-orients pipe segments (if flow reverses).
        private void reorientSegs()
        {
            int k;
            char newdir;
            //Pseg   seg, pseg, nseg;

// --- examine each link

            for (k = 1; k <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; k++)
            {
                // --- find new flow direction

                newdir = '+';
                if (MSX.Q[k] == 0.0) newdir = FlowDir[k];
                else if (MSX.Q[k] < 0.0) newdir = '-';

// --- if direction changes, then reverse the order of segments
//     (first to last) and save new direction

                if (newdir != FlowDir[k])
                {
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
                    MSX.Segments[k].Reverse();
                    FlowDir[k] = newdir;
                }
            }
        }

        ///<summary>Advects WQ segments within each pipe.</summary>

        private void advectSegs(long dt)
        {

            // Examine each link

            for (int k = 1; k <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; k++)
            {
                // Zero out WQ in new segment to be added at entrance of link
                for (int m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                    MSX.C1[m] = 0.0;

                // Get a free segment to add to entrance of link
                NewSeg[k] = createSeg(0.0d, MSX.C1);

                // Skip zero-length links (pumps & valves) & no-flow links
                if (NewSeg[k] == null ||
                    MSX.Link[(k)].getLen() == 0.0 || MSX.Q[k] == 0.0) continue;

                // Find conc. of wall species in new segment to be added and adjust conc.
                // of wall species to reflect shifted positions of existing segments
                if (HasWallSpecies)
                {
                    getNewSegWallQual(k, dt, NewSeg[k]);
                    shiftSegWallQual(k, dt);
                }
            }
        }


        /**
     * Computes wall species concentrations for a new WQ segment that
     * enters a pipe from its upstream node.
     */

        private void getNewSegWallQual(int k, long dt, Pipe newseg)
        {
            //Pipe  seg;
            int m;
            double v, vin, vsum, vadded, vleft;


            if (newseg == null)
                return;

            // Get volume of inflow to link
            v = LINKVOL(k);
            vin = Math.Abs(MSX.Q[k])*dt;
            if (vin > v) vin = v;

            // Start at last (most upstream) existing WQ segment
            vsum = 0.0;
            vleft = vin;
            for (m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
            {
                if (MSX.Species[m].getType() == EnumTypes.SpeciesType.WALL)
                    newseg.getC()[m] = 0.0;
            }

            // repeat while some inflow volume still remains
            
            for(var segIt = this.MSX.Segments[k].Last; segIt != null; segIt = segIt.Previous)
            {
                // Move to next downstream WQ segment
                Pipe seg = segIt.Value;

                // Find volume added by this segment
                vadded = seg.getV();
                if (vadded > vleft) vadded = vleft;

                // Update total volume added and inflow volume remaining

                vsum += vadded;
                vleft -= vadded;

                // Add wall species mass contributed by this segment to new segment
                for (m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                {
                    if (MSX.Species[m].getType() == EnumTypes.SpeciesType.WALL)
                        newseg.getC()[m] += vadded*seg.getC()[m];
                }
            }

            // Convert mass of wall species in new segment to concentration

            if (vsum > 0.0)
            {
                for (m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                {
                    if (MSX.Species[m].getType() == EnumTypes.SpeciesType.WALL) newseg.getC()[m] /= vsum;
                }
            }
        }


        /**
     * Recomputes wall species concentrations in segments that remain
     * within a pipe after flow is advected over current time step.
     */

        private void shiftSegWallQual(int k, long dt)
        {
            double v, vin, vstart, vend, vcur, vsum;

            // Find volume of water displaced in pipe
            v = LINKVOL(k);
            vin = Math.Abs(MSX.Q[k])*dt;
            if (vin > v) vin = v;

            // Set future start position (measured by pipe volume) of original last segment
            vstart = vin;

            // Examine each segment, from upstream to downstream
            for(var segIt = this.MSX.Segments[k].Last; segIt != null; segIt = segIt.Previous)
            {
                // Move to next downstream WQ segment
                Pipe seg1 = segIt.Value;

                // Initialize a "mixture" WQ
                for (int m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                    MSX.C1[m] = 0.0;

                // Find the future end position of this segment

                vend = vstart + seg1.getV();
                if (vend > v)
                    vend = v;

                vcur = vstart;
                vsum = 0;

                // find volume taken up by the segment after it moves down the pipe
                Pipe seg2 = null;

                for(var segIt2 = this.MSX.Segments[k].Last; segIt2 != null; segIt2 = segIt.Previous)
                {
                    // Move to next downstream WQ segment
                    seg2 = segIt2.Value;

                    if (seg2.getV() == 0.0)
                        continue;

                    vsum += seg2.getV();
                    if (vsum >= vstart && vsum <= vend)
                    {
                        for (int m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                        {
                            if (MSX.Species[m].getType() == EnumTypes.SpeciesType.WALL)
                                MSX.C1[m] += (vsum - vcur)*seg2.getC()[m];
                        }
                        vcur = vsum;
                    }
                    if (vsum >= vend) break;
                }

                // Update the wall species concentrations in the segment
                for (int m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                {
                    if (MSX.Species[m].getType() != EnumTypes.SpeciesType.WALL)
                        continue;

                    if (seg2 != null)
                        MSX.C1[m] += (vend - vcur)*seg2.getC()[m];

                    seg1.getC()[m] = MSX.C1[m]/(vend - vstart);

                    if (seg1.getC()[m] < 0.0)
                        seg1.getC()[m] = 0.0;
                }

                // re-start at the current end location

                vstart = vend;
                if (vstart >= v)
                    break;
            }
        }


        ///<summary>accumulates mass inflow at downstream node of each link.</summary>

        private void accumulate(long dt)
        {
            double cseg, v, vseg;


            // Compute average conc. of segments incident on each node
            //     (for use if there is no transport through the node)
            getIncidentConcen();

            // Reset cumlulative inflow to each node to zero
            Array.Clear(VolIn, 0, MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE] + 1);

            for (int ij = 0; ij < MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE] + 1; ij++)
                for (int jj = 0; jj < MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + 1; jj++)
                    MassIn[ij][jj] = 0.0;

            // move mass from first segment of each link into link's downstream node

            for (int k = 1; k <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; k++)
            {
                int i = UP_NODE(k); // upstream node
                int j = DOWN_NODE(k); // downstream node
                v = Math.Abs(MSX.Q[k])*dt; // flow volume

                // if link volume < flow volume, then transport upstream node's
                // quality to downstream node and remove all link segments

                if (LINKVOL(k) < v)
                {
                    Pipe seg = null;
                    VolIn[j] += v;
                    if (MSX.Segments[k].Count > 0)
                        seg = MSX.Segments[k].First.Value; //get(0);
                    for (int m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                    {
                        if (MSX.Species[m].getType() != EnumTypes.SpeciesType.BULK) continue;
                        cseg = MSX.Node[i].getC()[m];
                        if (seg != null) cseg = seg.getC()[m];
                        MassIn[j][m] += v*cseg;
                    }
                    // Remove all segments in the pipe
                    MSX.Segments[k].Clear();
                }
                else
                    while (v > 0.0)
                    {
                        // Otherwise remove flow volume from leading segments and accumulate flow mass at
                        // downstream node identify leading segment in pipe
                        Pipe seg = null;

                        if (MSX.Segments[k].Count > 0)
                            seg = MSX.Segments[k].First.Value; //get(0);

                        if (seg == null)
                            break;

                        // Volume transported from this segment is minimum of remaining flow volume & segment volume
                        // (unless leading segment is also last segment)

                        vseg = seg.getV();
                        vseg = Math.Min(vseg, v);

                        if (MSX.Segments[k].Count == 1) // if (seg == MSX.LastSeg[k]) vseg = v;
                            vseg = v;

                        //update volume & mass entering downstream node
                        for (int m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                        {
                            if (MSX.Species[m].getType() != EnumTypes.SpeciesType.BULK) continue;
                            cseg = seg.getC()[m];
                            MassIn[j][m] += vseg*cseg;
                        }
                        VolIn[j] += vseg;

                        // Reduce flow volume by amount transported
                        v -= vseg;

                        // If all of segment's volume was transferred, then replace leading segment with the one behind it
                        // (Note that the current seg is recycled for later use.)
                        if (v >= 0.0 && vseg >= seg.getV())
                        {
                            if (MSX.Segments[k].Count > 0)
                                MSX.Segments[k].RemoveFirst(); //remove(0);
                            //MSX.Segments[k] = seg->prev;
                            //if (MSX.Segments[k] == null) MSX.LastSeg[k] = null;
                            //MSXqual_removeSeg(seg);
                        }
                            // Otherwise reduce segment's volume
                        else
                        {
                            seg.setV(seg.getV() - vseg);
                        }
                    }
            }
        }


        /**
     * determines average WQ for bulk species in link end segments that are
     * incident on each node.
     */

        private void getIncidentConcen()
        {
            // zero-out memory used to store accumulated totals
            Array.Clear(VolIn, 0, MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE] + 1);
            for (int ij = 0; ij < MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE] + 1; ij++)
                for (int jj = 0; jj < MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + 1; jj++)
                {
                    MassIn[ij][jj] = 0.0;
                    X[ij][jj] = 0.0;
                }
            // examine each link
            for (int k = 1; k <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; k++)
            {
                int j = DOWN_NODE(k); // downstream node
                if (MSX.Segments[k].Count > 0) // accumulate concentrations
                {
                    for (int m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                    {
                        if (MSX.Species[m].getType() == EnumTypes.SpeciesType.BULK)
                            MassIn[j][m] += MSX.Segments[k].First.Value.getC()[m];
                    }
                    VolIn[j]++;
                }
                j = UP_NODE(k); // upstream node
                if (MSX.Segments[k].Count > 0) // accumulate concentrations
                {
                    for (int m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                    {
                        if (MSX.Species[m].getType() == EnumTypes.SpeciesType.BULK)
                            MassIn[j][m] += MSX.Segments[k].Last.Value.getC()[m];
                                //get(MSX.Segments[k].size()-1).getC()[m];
                    }
                    VolIn[j]++;
                }
            }

            // Compute avg. incident concen. at each node
            for (int k = 1; k <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; k++)
            {
                if (VolIn[k] > 0.0)
                {
                    for (int m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                        X[k][m] = MassIn[k][m]/VolIn[k];
                }
            }
        }

        /**
     * Updates the concentration at each node to the mixture
     * concentration of the accumulated inflow from connecting pipes.
      */

        private void updateNodes(long dt)
        {
            int i, j, m;

            // Examine each node
            for (i = 1; i <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; i++)
            {
                // Node is a junction
                j = MSX.Node[i].getTank();
                if (j <= 0)
                {
                    // Add any external inflow (i.e., negative demand) to total inflow volume

                    if (MSX.D[i] < 0.0)
                        VolIn[i] -= MSX.D[i]*dt;

                    // If inflow volume is non-zero, then compute the mixture
                    // concentration resulting at the node
                    if (VolIn[i] > 0.0)
                    {
                        for (m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                            MSX.Node[i].getC()[m] = MassIn[i][m]/VolIn[i];
                    }
                        // Otherwise use the avg. of the concentrations in the links incident on the node
                    else
                    {
                        for (m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                            MSX.Node[i].getC()[m] = X[i][m];
                    }

                    // Compute new equilibrium mixture
                    chemical.MSXchem_equil(EnumTypes.ObjectTypes.NODE, MSX.Node[i].getC());
                }

                    // Node is a tank or reservoir
                else
                {

                    if (MSX.Tank[j].getA() == 0.0)
                    {
                        // Use initial quality for reservoirs
                        for (m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                            MSX.Node[i].getC()[m] = MSX.Node[i].getC0()[m];
                    }
                    else
                    {
                        // otherwise update tank WQ based on mixing model
                        if (VolIn[i] > 0.0)
                        {
                            for (m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                            {
                                MSX.C1[m] = MassIn[i][m]/VolIn[i];
                            }
                        }
                        else
                            for (m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                                MSX.C1[m] = 0.0;

                        switch (MSX.Tank[j].getMixModel())
                        {
                            case EnumTypes.MixType.MIX1:
                                tank.MSXtank_mix1(j, VolIn[i], MSX.C1, dt);
                                break;
                            case EnumTypes.MixType.MIX2:
                                tank.MSXtank_mix2(j, VolIn[i], MSX.C1, dt);
                                break;
                            case EnumTypes.MixType.FIFO:
                                tank.MSXtank_mix3(j, VolIn[i], MSX.C1, dt);
                                break;
                            case EnumTypes.MixType.LIFO:
                                tank.MSXtank_mix4(j, VolIn[i], MSX.C1, dt);
                                break;
                        }
                        for (m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                            MSX.Node[i].getC()[m] = MSX.Tank[j].getC()[m];
                        MSX.Tank[j].setV(MSX.Tank[j].getV() + MSX.D[i]*dt);
                    }
                }
            }
        }

        ///<summary>Computes contribution (if any) of mass additions from WQ sources at each node.</summary>

        private void sourceInput(long dt)
        {
            int n;
            double qout, qcutoff, volout;

            // Establish a flow cutoff which indicates no outflow from a node
            qcutoff = 10.0*Constants.TINY;

            // consider each node
            for (n = 1; n <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.NODE]; n++)
            {
                // Skip node if no WQ source
                if (MSX.Node[n].getSources().Count == 0)
                    continue;

                // find total flow volume leaving node
                if (MSX.Node[n].getTank() == 0)
                    volout = VolIn[n]; // Junctions
                else
                    volout = VolIn[n] - MSX.D[n]*dt; // Tanks

                qout = volout/(double) dt;

                // evaluate source input only if node outflow > cutoff flow

                if (qout <= qcutoff)
                    continue;

                // Add contribution of each source species
                foreach (Source source  in  MSX.Node[n].getSources())
                {
                    addSource(n, source, volout, dt);
                }

                // Compute a new chemical equilibrium at the source node
                chemical.MSXchem_equil(EnumTypes.ObjectTypes.NODE, MSX.Node[n].getC());
            }
        }


        ///<summary>Updates concentration of particular species leaving a node that receives external source input.</summary>

        private void addSource(int n, Source source, double volout, long dt)
        {
            int m;
            double massadded, s;

            // Only analyze bulk species
            m = source.getSpecies();
            massadded = 0.0;
            if (source.getC0() > 0.0 && MSX.Species[m].getType() == EnumTypes.SpeciesType.BULK)
            {

                // Mass added depends on type of source

                s = getSourceQual(source);
                switch (source.getType())
                {
                        // Concen. Source : Mass added = source concen. * -(demand)
                    case EnumTypes.SourceType.CONCEN:
                        // Only add source mass if demand is negative
                        if (MSX.D[n] < 0.0)
                            massadded = -s*MSX.D[n]*dt;

                        // If node is a tank then set concen. to 0.
                        // (It will be re-set to true value later on)
                        if (MSX.Node[n].getTank() > 0)
                            MSX.Node[n].getC()[m] = 0.0;
                        break;

                        // Mass Inflow Booster Source
                    case EnumTypes.SourceType.MASS:
                        massadded = s*dt/Constants.LperFT3;
                        break;

                        // Setpoint Booster Source: Mass added is difference between source & node concen. times outflow volume
                    case EnumTypes.SourceType.SETPOINT:
                        if (s > MSX.Node[n].getC()[m])
                            massadded = (s - MSX.Node[n].getC()[m])*volout;
                        break;

                        // Flow-Paced Booster Source: Mass added = source concen. times outflow volume
                    case EnumTypes.SourceType.FLOWPACED:
                        massadded = s*volout;
                        break;
                }

                // Adjust nodal concentration to reflect source addition
                MSX.Node[n].getC()[m] += massadded/volout;
            }
        }

        ///<summary>Releases outflow from nodes into incident links.</summary>

        private void release(long dt)
        {
            int k, n, m;
            int useNewSeg;
            double q, v;
            Pipe seg = null;

            // Examine each link
            for (k = 1; k <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.LINK]; k++)
            {
                // Ignore links with no flow

                if (MSX.Q[k] == 0.0)
                {
                    //MSXqual_removeSeg(NewSeg[k]);
                    NewSeg[k] = null;
                    continue;
                }

                // Find flow volume released to link from upstream node
                // (NOTE: Flow volume is allowed to be > link volume.)
                n = UP_NODE(k);
                q = Math.Abs(MSX.Q[k]);
                v = q*dt;

                // Place bulk WQ at upstream node in new segment identified for link
                for (m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                {
                    if (MSX.Species[m].getType() == EnumTypes.SpeciesType.BULK)
                        NewSeg[k].getC()[m] = MSX.Node[n].getC()[m];
                }

                // If link has no last segment, then we must add a new one
                useNewSeg = 0;
                //seg = MSX.LastSeg[k];
                if (MSX.Segments[k].Count > 0)
                    seg = MSX.Segments[k].Last.Value; //get(MSX.Segments[k].size()-1);

                //else
                if (seg == null)
                    useNewSeg = 1;

                    // Ostherwise check if quality in last segment differs from that of the new segment
                else if (!MSXqual_isSame(seg.getC(), NewSeg[k].getC()))
                    useNewSeg = 1;


                // Quality of last seg & new seg are close simply increase volume of last seg
                if (useNewSeg == 0)
                {
                    seg.setV(seg.getV() + v);
                    //MSXqual_removeSeg(NewSeg[k]);
                    NewSeg[k] = null;
                }
                else
                {
                    // Otherwise add the new seg to the end of the link
                    NewSeg[k].setV(v);
                    MSX.Segments[k].AddLast(NewSeg[k]);
                    //MSXqual_addSeg(k, NewSeg[k]);
                }

            } //next link
        }

        ///<summary>Determines source concentration in current time period</summary>

        private double getSourceQual(Source source)
        {
            int i;
            long k;
            double c, f = 1.0;

            // Get source concentration (or mass flow) in original units
            c = source.getC0();

            // Convert mass flow rate from min. to sec.
            if (source.getType() == EnumTypes.SourceType.MASS) c /= 60.0;

            // Apply time pattern if assigned
            i = source.getPattern();

            if (i == 0)
                return (c);

            k = ((MSX.Qtime + MSX.Pstart)/MSX.Pstep)%MSX.Pattern[i].getLength();

            if (k != MSX.Pattern[i].getInterval())
            {
                if (k < MSX.Pattern[i].getInterval())
                {
                    MSX.Pattern[i].setCurrent(0); //); = MSX.Pattern[i].first;
                    MSX.Pattern[i].setInterval(0); //interval = 0;
                }
                while (MSX.Pattern[i].getCurrent() != 0 && MSX.Pattern[i].getInterval() < k)
                {
                    MSX.Pattern[i].setCurrent(MSX.Pattern[i].getCurrent() + 1);
                    MSX.Pattern[i].setInterval(MSX.Pattern[i].getInterval() + 1);
                }
            }

            if (MSX.Pattern[i].getCurrent() != 0)
                f = MSX.Pattern[i].getMultipliers()[MSX.Pattern[i].getCurrent()];

            return c*f;
        }

        //
        public Pipe createSeg(double v, double[] c)
        {
            Pipe seg;
            int m;
            seg = new Pipe();
            seg.setC(new double[MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES] + 1]);

            // Assign volume, WQ, & integration time step to the new segment
            seg.setV(v);
            for (m = 1; m <= MSX.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; m++)
                seg.getC()[m] = c[m];

            seg.setHstep(0.0);
            return seg;
        }



    }
}