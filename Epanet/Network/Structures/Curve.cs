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
using System.Collections;
using System.Collections.Generic;

using Epanet.Enums;

namespace Epanet.Network.Structures {

    ///<summary>2D graph used to map volume, pump, efficiency and head loss curves.</summary>
    public class Curve : Element, IEnumerable<EnPoint> {
        private readonly List<EnPoint> points = new List<EnPoint>();

        public Curve(string name):base(name) { }

        /// <summary>Computes intercept and slope of head v. flow curve at current flow.</summary>
        /// <param name="fMap"></param>
        /// <param name="q">Flow value.</param>
        /// <param name="h0">Head at zero flow (y-intercept).</param>
        /// <param name="r">dHead/dFlow (slope).</param>
        public void GetCoeff(FieldsMap fMap, double q, out double h0, out double r) {
            q *= fMap.GetUnits(FieldType.FLOW);

            int npts = points.Count;

            var k2 = 0;
            while (k2 < npts && points[k2].X < q) k2++;

            if (k2 == 0) k2++;
            else if (k2 == npts) k2--;
            
            int k1 = k2 - 1;

            r = (points[k2].Y - points[k1].Y) / (points[k2].X - points[k1].X);
            h0 = points[k1].Y - r * points[k1].X;

            h0 = h0 / fMap.GetUnits(FieldType.HEAD);
            r = r * fMap.GetUnits(FieldType.FLOW) / fMap.GetUnits(FieldType.HEAD);

        }

        ///<summary>Curve type.</summary>
        public CurveType Type { get; set; } //TODO: parse it correctly

        public List<EnPoint> Points { get { return points; } }

        /// <summary>Compute the linear interpolation of a 2d cartesian graph.</summary>
        /// <param name="x">The abscissa value.</param>
        /// <returns>The interpolated value.</returns>
        public double Interpolate(double x) {
            var p = points;
            int m = points.Count - 1;

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

        public override ElementType ElementType {
            get { return ElementType.Curve; }
        }

        #region partial implementation of IList<EnPoint>

        public void Add(double x, double y) { points.Add(new EnPoint(x, y)); }

        IEnumerator IEnumerable.GetEnumerator() { return points.GetEnumerator(); }
        public IEnumerator<EnPoint> GetEnumerator() { return points.GetEnumerator(); }
        
        public int Count { get { return points.Count; } }
        public EnPoint this[int index] {
            get { return points[index]; }
        }

        #endregion

    }

}