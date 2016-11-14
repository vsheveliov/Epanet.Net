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
using org.addition.epanet.msx.Structures;

namespace org.addition.epanet.msx {

    public class TankMix {

        private Chemical chemical;
        private Network msx;
        private Quality quality;

        public void LoadDependencies(EpanetMSX epa) {
            chemical = epa.Chemical;
            this.msx = epa.Network;
            quality = epa.Quality;
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
                if (this.msx.Species[j].getType() != EnumTypes.SpeciesType.BULK)
                    continue;

                double c = seg.getC()[j];

                if (this.msx.Tank[i].V > 0.0)
                    c += (cIn[j] - c) * vIn / this.msx.Tank[i].V;
                else
                    c = cIn[j];

                c = Math.Max(0.0, c);
                seg.getC()[j] = c;
                this.msx.Tank[i].C[j] = c;
            }

            // update species equilibrium
            if (vIn > 0.0)
                chemical.MSXchem_equil(EnumTypes.ObjectTypes.NODE, this.msx.Tank[i].C);
        }


        ///<summary>2-compartment tank model</summary>
        public void MSXtank_mix2(int i, double vIn, double[] cIn, long dt) {
            long tstep,
                 // Actual time step taken
                 tstar; // Time to fill or drain a zone
            double qIn,
                   // Inflow rate
                   qOut,
                   // Outflow rate
                   qNet; // Net flow rate
            double c, c1, c2; // Species concentrations
            Pipe seg1,
                 // Mixing zone segment
                 seg2; // Ambient zone segment

            // Find inflows & outflows
            int n = this.msx.Tank[i].Node;
            qNet = this.msx.D[n];
            qIn = vIn / dt;
            qOut = qIn - qNet;

            // Get segments for each zone
            int k = this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK] + i;
            seg1 = this.msx.Segments[k].First.Value; //get(0);
            seg2 = this.msx.Segments[k].Last.Value; //get(MSX.Segments[k].size()-1);

            // Case of no net volume change
            if (Math.Abs(qNet) < Constants.TINY)
                return;

            // Case of net filling (qNet > 0)
            else if (qNet > 0.0) {
                // Case where ambient zone empty & mixing zone filling
                if (seg2.getV() <= 0.0) {
                    // Time to fill mixing zone
                    tstar = (long)((this.msx.Tank[i].VMix - (seg1.getV())) / qNet);
                    tstep = Math.Min(dt, tstar);

                    for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) {
                        if (this.msx.Species[j].getType() != EnumTypes.SpeciesType.BULK) continue;

                        // --- new quality in mixing zone
                        c = seg1.getC()[j];
                        if (seg1.getV() > 0.0) seg1.getC()[j] += qIn * tstep * (cIn[j] - c) / (seg1.getV());
                        else seg1.getC()[j] = cIn[j];
                        seg1.getC()[j] = Math.Max(0.0, seg1.getC()[j]);
                        seg2.getC()[j] = 0.0;
                    }

                    // New volume of mixing zone
                    seg1.setV(seg1.getV() + qNet * tstep);

                    // Time during which ambient zone fills
                    dt -= tstep;
                }

                // Case where mixing zone full & ambient zone filling
                if (dt > 1) {
                    for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) {
                        if (this.msx.Species[j].getType() != EnumTypes.SpeciesType.BULK) continue;

                        // --- new quality in mixing zone
                        c1 = seg1.getC()[j];
                        seg1.getC()[j] += qIn * dt * (cIn[j] - c1) / (seg1.getV());
                        seg1.getC()[j] = Math.Max(0.0, seg1.getC()[j]);

                        // --- new quality in ambient zone
                        c2 = seg2.getC()[j];
                        if (seg2.getV() <= 0.0)
                            seg2.getC()[j] = seg1.getC()[j];
                        else
                            seg2.getC()[j] += qNet * dt * ((seg1.getC()[j]) - c2) / (seg2.getV());
                        seg2.getC()[j] = Math.Max(0.0, seg2.getC()[j]);
                    }

                    // New volume of ambient zone
                    seg2.setV(seg2.getV() + qNet * dt);
                }
                if (seg1.getV() > 0.0) chemical.MSXchem_equil(EnumTypes.ObjectTypes.NODE, seg1.getC());
                if (seg2.getV() > 0.0) chemical.MSXchem_equil(EnumTypes.ObjectTypes.NODE, seg2.getC());
            }

            // Case of net emptying (qnet < 0)
            else if (qNet < 0.0 && seg1.getV() > 0.0) {
                // Case where mixing zone full & ambient zone draining
                if ((seg2.getV()) > 0.0) {

                    // Time to drain ambient zone
                    tstar = (long)(seg2.getV() / -qNet);
                    tstep = Math.Min(dt, tstar);

                    for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) {
                        if (this.msx.Species[j].getType() != EnumTypes.SpeciesType.BULK) continue;
                        c1 = seg1.getC()[j];
                        c2 = seg2.getC()[j];

                        // New mizing zone quality (affected by both external inflow
                        // and drainage from the ambient zone
                        seg1.getC()[j] += (qIn * cIn[j] - qNet * c2 - qOut * c1) * tstep / (seg1.getV());
                        seg1.getC()[j] = Math.Max(0.0, seg1.getC()[j]);
                    }

                    // New ambient zone volume
                    seg2.setV(seg2.getV() + qNet * tstep);
                    seg2.setV(Math.Max(0.0, seg2.getV()));

                    // Time during which mixing zone empties
                    dt -= tstep;
                }

                // Case where ambient zone empty & mixing zone draining
                if (dt > 1) {
                    for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) {
                        if (this.msx.Species[j].getType() != EnumTypes.SpeciesType.BULK) continue;

                        // New mixing zone quality (affected by external inflow only)
                        c = seg1.getC()[j];
                        seg1.getC()[j] += qIn * dt * (cIn[j] - c) / (seg1.getV());
                        seg1.getC()[j] = Math.Max(0.0, seg1.getC()[j]);
                        seg2.getC()[j] = 0.0;
                    }

                    // New volume of mixing zone
                    seg1.setV(seg1.getV() + qNet * dt);
                    seg1.setV(Math.Max(0.0, seg1.getV()));
                }
                if (seg1.getV() > 0.0) chemical.MSXchem_equil(EnumTypes.ObjectTypes.NODE, seg1.getC());
            }

            // Use quality of mixed compartment (seg1) to represent quality
            // of tank since this is where outflow begins to flow from
            for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++)
                this.msx.Tank[i].C[j] = seg1.getC()[j];
        }

        ///<summary>
        /// Computes concentrations in the segments that form a
        /// first-in-first-out (FIFO) tank model.
        /// </summary>
        public void MSXtank_mix3(int i, double vIn, double[] cIn, long dt) {
            double vNet, vOut, vSeg, vSum;
            Pipe seg;

            // Find inflows & outflows

            int k = this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK] + i;
            int n = this.msx.Tank[i].Node;
            vNet = this.msx.D[n] * dt;
            vOut = vIn - vNet;

            // Initialize outflow volume & concentration

            vSum = 0.0;
            for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) this.msx.C1[j] = 0.0;

            // Withdraw flow from first segment
            while (vOut > 0.0) {
                if (this.msx.Segments[k].Count == 0) break;

                // --- get volume of current first segment
                seg = this.msx.Segments[k].First.Value; //[0];

                vSeg = seg.getV();
                vSeg = Math.Min(vSeg, vOut);
                if (seg == this.msx.Segments[k].Last.Value)
                    vSeg = vOut; //[MSX.Segments[k].size()-1]       //TODO pode ser simplificado para getSize()==1

                // --- update mass & volume removed
                vSum += vSeg;
                for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) {
                    this.msx.C1[j] += (seg.getC()[j]) * vSeg;
                }

                // --- decrease vOut by volume of first segment
                vOut -= vSeg;

                // --- remove segment if all its volume is consumed
                if (vOut >= 0.0 && vSeg >= seg.getV()) {
                    this.msx.Segments[k].RemoveFirst(); //.remove(0);
                }

                // --- otherwise just adjust volume of first segment
                else seg.setV(seg.getV() - vSeg);
            }

            // Use quality from first segment to represent overall
            // quality of tank since this is where outflow flows from

            for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) {
                if (vSum > 0.0) this.msx.Tank[i].C[j] = this.msx.C1[j] / vSum;
                else this.msx.Tank[i].C[j] = this.msx.Segments[k].First.Value.getC()[j]; //MSX.Segments[k][0].getC()[m];
            }

            // Add new last segment for new flow entering tank
            if (vIn > 0.0) {
                // Quality is the same, so just add flow volume to last seg
                k = this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK] + i;
                seg = null;
                if (this.msx.Segments[k].Count > 0)
                    seg = this.msx.Segments[k].Last.Value; //get(MSX.Segments[k].size()-1);

                if (seg != null && quality.MSXqual_isSame(seg.getC(), cIn)) seg.setV(seg.getV() + vIn);

                // Otherwise add a new seg to tank
                else {
                    seg = quality.CreateSeg(vIn, cIn);
                    //quality.MSXqual_addSeg(k, seg);
                    this.msx.Segments[k].AddLast(seg);
                }
            }
        }

        ///<summary>Last In-First Out (LIFO) tank model</summary>
        public void MSXtank_mix4(int i, double vIn, double[] cIn, long dt) {
            double vOut, vNet, vSum, vSeg;
            Pipe seg;

            // Find inflows & outflows

            int k = this.msx.Nobjects[(int)EnumTypes.ObjectTypes.LINK] + i;
            int n = this.msx.Tank[i].Node;
            vNet = this.msx.D[n] * dt;
            vOut = vIn - vNet;

            // keep track of total volume & mass removed from tank

            vSum = 0.0;
            for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++) this.msx.C1[j] = 0.0;

            // if tank filling, then create a new last segment
            if (vNet > 0.0) {

                // inflow quality = last segment quality so just expand last segment

                seg = null;
                if (this.msx.Segments[k].Count > 0)
                    seg = this.msx.Segments[k].Last.Value; //[MSX.Segments[k].size()-1];

                if (seg != null && quality.MSXqual_isSame(seg.getC(), cIn)) seg.setV(seg.getV() + vNet);

                // otherwise add a new last segment to tank

                else {
                    seg = quality.CreateSeg(vNet, cIn);
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
                    seg = null;
                    if (this.msx.Segments[k].Count > 0)
                        seg = this.msx.Segments[k].Last.Value; //get(MSX.Segments[k].size()-1);

                    if (seg == null) break;

                    vSeg = seg.getV();
                    vSeg = Math.Min(vSeg, vNet);
                    if (seg == this.msx.Segments[k].First.Value) vSeg = vNet;

                    // update mass & volume removed
                    vSum += vSeg;
                    for (int j = 1; j <= this.msx.Nobjects[(int)EnumTypes.ObjectTypes.SPECIES]; j++)
                        this.msx.C1[j] += (seg.getC()[j]) * vSeg;

                    // reduce vNet by volume of last segment
                    vNet -= vSeg;

                    // remove segment if all its volume is used up
                    if (vNet >= 0.0 && vSeg >= seg.getV()) {
                        this.msx.Segments[k].RemoveLast(); //remove(MSX.Segments[k].size()-1);
                        //MSX.LastSeg[k] = seg->prev;
                        //if ( MSX.LastSeg[k] == NULL ) MSX.Segments[k] = NULL;
                        //MSXqual_removeSeg(seg);
                    }

                    // otherwise just reduce volume of last segment
                    else {
                        seg.setV(seg.getV() - vSeg);
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