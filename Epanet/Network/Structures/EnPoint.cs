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

namespace Epanet.Network.Structures {

    ///<summary>Simple 2d point.</summary>

    public struct EnPoint : IComparable<EnPoint>, IEquatable<EnPoint> {
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

        public int CompareTo(EnPoint other) {
            var cmp = this.x.CompareTo(other.x);
            if (cmp != 0) return cmp;

            return this.y.CompareTo(other.y);
        }

        public bool Equals(EnPoint other) {
            bool ex = (this.x == other.x) || (double.IsNaN(this.x) && double.IsNaN(other.x));
            bool ey = (this.y == other.y) || (double.IsNaN(this.y) && double.IsNaN(other.y)); 
            
            return ex && ey;
        }
        
        public override bool Equals(object obj) {
            if (!(obj is EnPoint)) return false;

            return this.Equals((EnPoint)obj);
        }
        
        public override int GetHashCode() {
            return this.x.GetHashCode() ^ this.y.GetHashCode();
        }
        
        public override string ToString() { return "EnPoint{" + "x=" + this.x + ", y=" + this.y + '}'; }
    }

}