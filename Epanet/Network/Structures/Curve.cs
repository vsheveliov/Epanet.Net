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

        ///<summary>Computed curve coefficients.</summary>
        public class Coeffs {
            public readonly double h0; // head at zero flow (y-intercept)

            public readonly double r; // dHead/dFlow (slope)

            public Coeffs(double h0, double r) {
                this.h0 = h0;
                this.r = r;
            }
        }

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

        
        private readonly string _id;
        private readonly List<double> _x = new List<double>();
        private readonly List<double> _y = new List<double>();

        public Curve(string id) { this._id = id; }

        /// <summary>Computes intercept and slope of head v. flow curve at current flow.</summary>
        /// <param name="fMap"></param>
        /// <param name="q">Flow value.</param>
        public Coeffs getCoeff(FieldsMap fMap, double q) {
            double h0;
            double r;
            int k1, k2, npts;

            q *= fMap.GetUnits(FieldsMap.FieldType.FLOW);

            npts = this.Npts;

            k2 = 0;
            while (k2 < npts && this._x[k2] < q) k2++;
            if (k2 == 0) k2++;
            else if (k2 == npts) k2--;
            k1 = k2 - 1;

            r = (this._y[k2] - this._y[k1])/(this._x[k2] - this._x[k1]);
            h0 = this._y[k1] - (r)*this._x[k1];

            h0 = (h0)/fMap.GetUnits(FieldsMap.FieldType.HEAD);
            r = (r)*fMap.GetUnits(FieldsMap.FieldType.FLOW)/fMap.GetUnits(FieldsMap.FieldType.HEAD);

            return new Coeffs(h0, r);
        }

        ///<summary>Curve name.</summary>
        public string Id { get { return this._id; } }

        /// <summary>Get the number of points.</summary>
        /// <value>
        ///   If the abscissa points count differ from the ordinate it returns -1, otherwise,
        ///   it returns the abscissa point count.
        /// </value>
        public int Npts { get { return this._x.Count != this._y.Count ? -1 : this._x.Count; } }

        ///<summary>Curve type.</summary>
        public CurveType Type { get; set; }

        ///<summary>Curve abscissa values.</summary>
        public List<double> X { get { return this._x; } }

        ///<summary>Curve ordinate values.</summary>
        public List<double> Y { get { return this._y; } }

  
        /// <summary>Compute the linear interpolation of a 2d cartesian graph.</summary>
        /// <param name="xx">The abscissa value.</param>
        /// <returns>The interpolated value.</returns>
        public double LinearInterpolator(double xx) {
            var x = this._x;
            var y = this._y;
            var n = this.Npts;

            int    k,m;
            double  dx,dy;

            m = n - 1;
            if (xx <= x[0]) return y[0];
            for (k=1; k<=m; k++)
            {
                if (x[k] >= xx)
                {
                    dx = x[k]-x[k-1];
                    dy = y[k]-y[k-1];
                    if (Math.Abs(dx) < Constants.TINY) return y[k];
                    else return y[k] - (x[k]-xx)*dy/dx;
                }
            }
            return y[m];
        }
    }
}