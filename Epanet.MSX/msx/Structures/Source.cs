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

namespace Epanet.MSX.Structures {

    public class Source {
        ///<summary>species index</summary>
        public int Species { set; get; }
       
        ///<summary>base concentration</summary>
        public double C0 { get; set; }
      
        ///<summary>time pattern index</summary>
        public int Pattern { get; set; }
      
        ///<summary>actual mass flow rate</summary>
        public double MassRate { get; set; }

        ///<summary>sourceType</summary>
        public EnumTypes.SourceType Type { get; set; }

        public Source() {
            this.Type = EnumTypes.SourceType.CONCEN;
            this.Species = 0;
            this.C0 = 0;
            this.Pattern = 0;
            this.MassRate = 0;
        }
    }

}