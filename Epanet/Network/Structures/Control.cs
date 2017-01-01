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

using Epanet.Enums;

namespace Epanet.Network.Structures {

    ///<summary>Control statement</summary>
    public class Control {
        ///<summary>Control grade.</summary>
        public double Grade { get; set; }

        ///<summary>Assigned link reference.</summary>
        public Link Link { get; set; }

        ///<summary>Assigned node reference.</summary>
        public Node Node { get; set; }

        ///<summary>New link setting.</summary>
        public double Setting { get; set; }

        ///<summary>New link status.</summary>
        public StatType Status { get; set; }

        ///<summary>Control time (in seconds).</summary>
        public long Time { get; set; }

        ///<summary>Control type</summary>
        public ControlType Type { get; set; }
    }

}