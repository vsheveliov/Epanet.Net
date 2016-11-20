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

namespace Epanet.Network.Structures {

    ///<summary>2D graph used to map volume, pump, efficiency and head loss curves.</summary>
    public class Curve {
        /// <summary>Type of curve</summary>
        public enum CurveType {
            /// <summary>volume curve</summary>
            V_CURVE = 0,

            /// <summary>pump curve</summary>
            P_CURVE = 1,

            /// <summary>efficiency curve</summary>
            E_CURVE = 2,

            /// <summary>head loss curve</summary>
            H_CURVE = 3
        }


        private readonly string id;
        private readonly List<EnPoint> points = new List<EnPoint>();

        public Curve(string id) { this.id = id; }

        /// <summary>Computes intercept and slope of head v. flow curve at current flow.</summary>
        /// <param name="fMap"></param>
        /// <param name="q">Flow value.</param>
        /// <param name="h0">Head at zero flow (y-intercept).</param>
        /// <param name="r">dHead/dFlow (slope).</param>
        public void GetCoeff(FieldsMap fMap, double q, out double h0, out double r) {
            q *= fMap.GetUnits(FieldsMap.FieldType.FLOW);

            int npts = this.points.Count;

            var k2 = 0;
            while (k2 < npts && this.points[k2].X < q) k2++;

            if (k2 == 0) k2++;
            else if (k2 == npts) k2--;
            
            int k1 = k2 - 1;

            r = (this.points[k2].Y - this.points[k1].Y) / (this.points[k2].X - this.points[k1].X);
            h0 = this.points[k1].Y - r * this.points[k1].X;

            h0 = h0 / fMap.GetUnits(FieldsMap.FieldType.HEAD);
            r = r * fMap.GetUnits(FieldsMap.FieldType.FLOW) / fMap.GetUnits(FieldsMap.FieldType.HEAD);

        }

        ///<summary>Curve name.</summary>
        public string Id { get { return this.id; } }

        ///<summary>Curve type.</summary>
        public CurveType Type { get; set; }

        public List<EnPoint> Points { get { return this.points; } }

        /// <summary>Compute the linear interpolation of a 2d cartesian graph.</summary>
        /// <param name="x">The abscissa value.</param>
        /// <returns>The interpolated value.</returns>
        public double this[double x] {
            get {
                var p = this.points;
                int m = this.points.Count - 1;

                if (x <= p[0].X) return p[0].Y;

                for (int i = 1; i <= m; i++) {
                    if (p[i].X >= x) {
                        double dx = p[i].X - p[i - 1].X;
                        double dy = p[i].Y - p[i - 1].Y;
                        if (Math.Abs(dx) < Constants.TINY) return p[i].Y;
                        else return p[i].Y - (p[i].X - x) * dy / dx;
                    }
                }

                return p[m].Y;
            }
        }

        public void Add(double x, double y) { this.points.Add(new EnPoint(x, y)); }
    }

}