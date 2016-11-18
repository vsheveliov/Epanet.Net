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

namespace Epanet.Network.Structures {

    ///<summary>Simple 2d point.</summary>

    public struct EnPoint {
        public static readonly EnPoint Invalid = new EnPoint(double.NaN, double.NaN);
        private readonly double x;
        private readonly double y;

        public EnPoint(double x, double y) {

            this.x = x;
            this.y = y;
        }

        public bool IsInvalid { get { return double.IsNaN(this.x) || double.IsNaN(this.y); } }

        ///<summary>Absciss coordinate.</summary>
        public double X { get { return this.x; } }

        ///<summary>Ordinate coordinate.</summary>
        public double Y { get { return this.y; } }

        public override string ToString() { return "Point{" + "x=" + this.x + ", y=" + this.y + '}'; }
    }

}