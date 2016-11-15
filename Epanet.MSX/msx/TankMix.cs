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
using Epanet.MSX.Structures;

namespace Epanet.MSX {

    public class TankMix {

        private Chemical chemical;
        private Network msx;
        private Quality quality;

        public void LoadDependencies(EpanetMSX epa) {
            this.chemical = epa.Chemical;
            this.msx = epa.Network;
            this.quality = epa.Quality;
        }

        ///<summary>
        /// Computes new WQ at end of time step in a completely mixed tank
        /// (after contents have been reacted). 
        /// </summary>
        public void MSXtank_mix1(int i, double vIn, double[] cIn, long dt) {
            // blend inflow with contents
            int n = this.msx.Tank[i].Node;
            int k = this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK] + i;
            Pipe seg = this.msx.Segments[k].First.Value;

            for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) {
                if (this.msx.Species[j].Type != EnumTypes.SpeciesType.BULK)
                    continue;

                double c = seg.C[j];

                if (this.msx.Tank[i].V > 0.0)
                    c += (cIn[j] - c) * vIn / this.msx.Tank[i].V;
                else
                    c = cIn[j];

                c = Math.Max(0.0, c);
                seg.C[j] = c;
                this.msx.Tank[i].C[j] = c;
            }

            // update species equilibrium
            if (vIn > 0.0)
                this.chemical.MSXchem_equil(EnumTypes.ObjectTypes.NODE, this.msx.Tank[i].C);
        }


        ///<summary>2-compartment tank model</summary>
        public void MSXtank_mix2(int i, double vIn, double[] cIn, long dt) {
            long tstep, // Actual time step taken
                 tstar; // Time to fill or drain a zone
        
            double c, c1, c2; // Species concentrations

            // Find inflows & outflows
            int n = this.msx.Tank[i].Node;
            double qNet = this.msx.D[n]; // Net flow rate
            double qIn = vIn / dt; // Inflow rate
            double qOut = qIn - qNet; // Outflow rate

            // Get segments for each zone
            int k = this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK] + i;
            Pipe seg1 = this.msx.Segments[k].First.Value; // Mixing zone segment
            Pipe seg2 = this.msx.Segments[k].Last.Value; // Ambient zone segment

            // Case of no net volume change
            if (Math.Abs(qNet) < Constants.TINY)
                return;

            // Case of net filling (qNet > 0)
            if (qNet > 0.0) {
                // Case where ambient zone empty & mixing zone filling
                if (seg2.V <= 0.0) {
                    // Time to fill mixing zone
                    tstar = (long)((this.msx.Tank[i].VMix - (seg1.V)) / qNet);
                    tstep = Math.Min(dt, tstar);

                    for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) {
                        if (this.msx.Species[j].Type != EnumTypes.SpeciesType.BULK) continue;

                        // --- new quality in mixing zone
                        c = seg1.C[j];
                        if (seg1.V > 0.0) seg1.C[j] += qIn * tstep * (cIn[j] - c) / (seg1.V);
                        else seg1.C[j] = cIn[j];
                        seg1.C[j] = Math.Max(0.0, seg1.C[j]);
                        seg2.C[j] = 0.0;
                    }

                    // New volume of mixing zone
                    seg1.V = seg1.V + qNet * tstep;

                    // Time during which ambient zone fills
                    dt -= tstep;
                }

                // Case where mixing zone full & ambient zone filling
                if (dt > 1) {
                    for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) {
                        if (this.msx.Species[j].Type != EnumTypes.SpeciesType.BULK) continue;

                        // --- new quality in mixing zone
                        c1 = seg1.C[j];
                        seg1.C[j] += qIn * dt * (cIn[j] - c1) / (seg1.V);
                        seg1.C[j] = Math.Max(0.0, seg1.C[j]);

                        // --- new quality in ambient zone
                        c2 = seg2.C[j];
                        if (seg2.V <= 0.0)
                            seg2.C[j] = seg1.C[j];
                        else
                            seg2.C[j] += qNet * dt * ((seg1.C[j]) - c2) / (seg2.V);
                        seg2.C[j] = Math.Max(0.0, seg2.C[j]);
                    }

                    // New volume of ambient zone
                    seg2.V = seg2.V + qNet * dt;
                }
                if (seg1.V > 0.0) this.chemical.MSXchem_equil(EnumTypes.ObjectTypes.NODE, seg1.C);
                if (seg2.V > 0.0) this.chemical.MSXchem_equil(EnumTypes.ObjectTypes.NODE, seg2.C);
            }

            // Case of net emptying (qnet < 0)
            else if (qNet < 0.0 && seg1.V > 0.0) {
                // Case where mixing zone full & ambient zone draining
                if ((seg2.V) > 0.0) {

                    // Time to drain ambient zone
                    tstar = (long)(seg2.V / -qNet);
                    tstep = Math.Min(dt, tstar);

                    for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) {
                        if (this.msx.Species[j].Type != EnumTypes.SpeciesType.BULK) continue;
                        c1 = seg1.C[j];
                        c2 = seg2.C[j];

                        // New mizing zone quality (affected by both external inflow
                        // and drainage from the ambient zone
                        seg1.C[j] += (qIn * cIn[j] - qNet * c2 - qOut * c1) * tstep / (seg1.V);
                        seg1.C[j] = Math.Max(0.0, seg1.C[j]);
                    }

                    // New ambient zone volume
                    seg2.V = seg2.V + qNet * tstep;
                    seg2.V = Math.Max(0.0, seg2.V);

                    // Time during which mixing zone empties
                    dt -= tstep;
                }

                // Case where ambient zone empty & mixing zone draining
                if (dt > 1) {
                    for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) {
                        if (this.msx.Species[j].Type != EnumTypes.SpeciesType.BULK) continue;

                        // New mixing zone quality (affected by external inflow only)
                        c = seg1.C[j];
                        seg1.C[j] += qIn * dt * (cIn[j] - c) / (seg1.V);
                        seg1.C[j] = Math.Max(0.0, seg1.C[j]);
                        seg2.C[j] = 0.0;
                    }

                    // New volume of mixing zone
                    seg1.V = seg1.V + qNet * dt;
                    seg1.V = Math.Max(0.0, seg1.V);
                }
                if (seg1.V > 0.0) this.chemical.MSXchem_equil(EnumTypes.ObjectTypes.NODE, seg1.C);
            }

            // Use quality of mixed compartment (seg1) to represent quality
            // of tank since this is where outflow begins to flow from
            for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++)
                this.msx.Tank[i].C[j] = seg1.C[j];
        }

        ///<summary>
        /// Computes concentrations in the segments that form a
        /// first-in-first-out (FIFO) tank model.
        /// </summary>
        public void MSXtank_mix3(int i, double vIn, double[] cIn, long dt) {
            
            // Find inflows & outflows

            int k = this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK] + i;
            int n = this.msx.Tank[i].Node;
            double vNet = this.msx.D[n] * dt;
            double vOut = vIn - vNet;

            // Initialize outflow volume & concentration

            var vSum = 0.0;
            for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) this.msx.C1[j] = 0.0;

            // Withdraw flow from first segment
            while (vOut > 0.0) {
                if (this.msx.Segments[k].Count == 0) break;

                // --- get volume of current first segment
                Pipe seg = this.msx.Segments[k].First.Value;

                double vSeg = seg.V;
                vSeg = Math.Min(vSeg, vOut);
                if (seg == this.msx.Segments[k].Last.Value)
                    vSeg = vOut; //[MSX.Segments[k].size()-1]       //TODO pode ser simplificado para getSize()==1

                // --- update mass & volume removed
                vSum += vSeg;
                for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) {
                    this.msx.C1[j] += (seg.C[j]) * vSeg;
                }

                // --- decrease vOut by volume of first segment
                vOut -= vSeg;

                // --- remove segment if all its volume is consumed
                if (vOut >= 0.0 && vSeg >= seg.V) {
                    this.msx.Segments[k].RemoveFirst(); //.remove(0);
                }

                // --- otherwise just adjust volume of first segment
                else seg.V = seg.V - vSeg;
            }

            // Use quality from first segment to represent overall
            // quality of tank since this is where outflow flows from

            for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) {
                if (vSum > 0.0) this.msx.Tank[i].C[j] = this.msx.C1[j] / vSum;
                else this.msx.Tank[i].C[j] = this.msx.Segments[k].First.Value.C[j]; //MSX.Segments[k][0].getC()[m];
            }

            // Add new last segment for new flow entering tank
            if (vIn > 0.0) {
                // Quality is the same, so just add flow volume to last seg
                k = this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK] + i;
                Pipe seg = null;
                if (this.msx.Segments[k].Count > 0)
                    seg = this.msx.Segments[k].Last.Value; //get(MSX.Segments[k].size()-1);

                if (seg != null && this.quality.MSXqual_isSame(seg.C, cIn)) seg.V = seg.V + vIn;

                // Otherwise add a new seg to tank
                else {
                    seg = this.quality.CreateSeg(vIn, cIn);
                    //quality.MSXqual_addSeg(k, seg);
                    this.msx.Segments[k].AddLast(seg);
                }
            }
        }

        ///<summary>Last In-First Out (LIFO) tank model</summary>
        public void MSXtank_mix4(int i, double vIn, double[] cIn, long dt) {
            

            // Find inflows & outflows

            int k = this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK] + i;
            int n = this.msx.Tank[i].Node;
            double vNet = this.msx.D[n] * dt;
            double vOut = vIn - vNet;

            // keep track of total volume & mass removed from tank

            var vSum = 0.0;
            for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) this.msx.C1[j] = 0.0;

            // if tank filling, then create a new last segment
            if (vNet > 0.0) {

                // inflow quality = last segment quality so just expand last segment
                Pipe seg = null;
                if (this.msx.Segments[k].Count > 0)
                    seg = this.msx.Segments[k].Last.Value; //[MSX.Segments[k].size()-1];

                if (seg != null && this.quality.MSXqual_isSame(seg.C, cIn)) seg.V = seg.V + vNet;

                // otherwise add a new last segment to tank

                else {
                    seg = this.quality.CreateSeg(vNet, cIn);
                    this.msx.Segments[k].AddLast(seg);
                }

                // quality of tank is that of inflow

                for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++)
                    this.msx.Tank[i].C[j] = cIn[j];

            }

            // if tank emptying then remove last segments until vNet consumed

            else if (vNet < 0.0) {

                // keep removing volume from last segments until vNet is removed
                vNet = -vNet;
                while (vNet > 0.0) {
                    // --- get volume of current last segment
                    Pipe seg = null;
                    if (this.msx.Segments[k].Count > 0)
                        seg = this.msx.Segments[k].Last.Value; //get(MSX.Segments[k].size()-1);

                    if (seg == null) break;

                    double vSeg = seg.V;
                    vSeg = Math.Min(vSeg, vNet);
                    if (seg == this.msx.Segments[k].First.Value) vSeg = vNet;

                    // update mass & volume removed
                    vSum += vSeg;
                    for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++)
                        this.msx.C1[j] += (seg.C[j]) * vSeg;

                    // reduce vNet by volume of last segment
                    vNet -= vSeg;

                    // remove segment if all its volume is used up
                    if (vNet >= 0.0 && vSeg >= seg.V) {
                        this.msx.Segments[k].RemoveLast(); //remove(MSX.Segments[k].size()-1);
                        //MSX.LastSeg[k] = seg->prev;
                        //if ( MSX.LastSeg[k] == NULL ) MSX.Segments[k] = NULL;
                        //MSXqual_removeSeg(seg);
                    }

                    // otherwise just reduce volume of last segment
                    else {
                        seg.V = seg.V - vSeg;
                    }
                }

                // tank quality is mixture of flow released and any inflow

                for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) {
                    vSum = vSum + vIn;
                    if (vSum > 0.0)
                        this.msx.Tank[i].C[j] = (this.msx.C1[j] + cIn[j] * vIn) / vSum;
                }
            }
        }


    }

}