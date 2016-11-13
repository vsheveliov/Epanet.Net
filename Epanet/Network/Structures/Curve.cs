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

namespace org.addition.epanet.network.structures {

    ///<summary>2D graph used to map volume, pump, efficiency and head loss curves.</summary>
    public class Curve {

        ///<summary>Computed curve coefficients.</summary>
        public class Coeffs {
            public double h0; // head at zero flow (y-intercept)

            public double r; // dHead/dFlow (slope)

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

        ///<summary>Curve name.</summary>
        private string id = "";

        ///<summary>Curve type.</summary>
        private CurveType type;

        ///<summary>Curve abscissa values.</summary>
        private readonly List<double> x = new List<double>();


        ///<summary>Curve ordinate values.</summary>
        private readonly List<double> y = new List<double>();

        /**
     * Computes intercept and slope of head v. flow curve at current flow.
     * @param q Flow value.
     * @return
     */

        public Coeffs getCoeff(FieldsMap fMap, double q) {
            double h0;
            double r;
            int k1, k2, npts;

            q *= fMap.getUnits(FieldsMap.Type.FLOW);

            npts = getNpts();

            k2 = 0;
            while (k2 < npts && x[k2] < q) k2++;
            if (k2 == 0) k2++;
            else if (k2 == npts) k2--;
            k1 = k2 - 1;

            r = (y[k2] - y[k1])/(x[k2] - x[k1]);
            h0 = y[k1] - (r)*x[k1];

            h0 = (h0)/fMap.getUnits(FieldsMap.Type.HEAD);
            r = (r)*fMap.getUnits(FieldsMap.Type.FLOW)/fMap.getUnits(FieldsMap.Type.HEAD);

            return new Coeffs(h0, r);
        }

        public string getId() {
            return id;
        }

        /**
     * Get the number of points.
     * @return If the abscissa points count differ from the ordinate it returns -1, otherwise,
     * it returns the abscissa point count.
     */

        public int getNpts() {
            if (x.Count != y.Count) {
                return -1;
            }
            return x.Count;
        }

        public CurveType getType() {
            return type;
        }

        public List<double> getX() {
            return x;
        }

        public List<double> getY() {
            return y;
        }

        public void setId(string Id) {
            this.id = Id;
        }

        public void setType(CurveType Type) {
            this.type = Type;
        }
    }
}