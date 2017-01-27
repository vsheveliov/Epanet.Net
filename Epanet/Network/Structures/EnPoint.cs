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
        private readonly double _x;
        private readonly double _y;

        public EnPoint(double x, double y) {

            _x = x;
            _y = y;
        }

        public bool IsInvalid { get { return double.IsNaN(_x) || double.IsNaN(_y); } }

        ///<summary>Absciss coordinate.</summary>
        public double X { get { return _x; } }

        ///<summary>Ordinate coordinate.</summary>
        public double Y { get { return _y; } }

        public int CompareTo(EnPoint other) {
            var cmp = _x.CompareTo(other._x);
            return cmp == 0 ? _y.CompareTo(other._y) : cmp;
        }

        public bool Equals(EnPoint other) {
            bool ex = (_x == other._x) || (double.IsNaN(_x) && double.IsNaN(other._x));
            bool ey = (_y == other._y) || (double.IsNaN(_y) && double.IsNaN(other._y)); 
            
            return ex && ey;
        }
        
        public override bool Equals(object obj) {
            if (!(obj is EnPoint)) return false;

            return Equals((EnPoint)obj);
        }
        
        public override int GetHashCode() {
            return _x.GetHashCode() ^ _y.GetHashCode();
        }
        
        public override string ToString() { return "EnPoint{" + "x=" + _x + ", y=" + _y + '}'; }
    }

}