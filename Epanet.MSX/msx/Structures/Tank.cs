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

    public class Tank {
        public Tank(int @params, int species) {
            this.Param = new double[@params];
            this.C = new double[species];
        }

        ///<summary>node index of tank</summary>
        public int Node { get; set; }
      
        ///<summary>integration time step</summary>
        public double Hstep { get; set; }
      
        ///<summary>tank area</summary>
        public double A { get; set; }
      
        ///<summary>initial volume</summary>
        public double V0 { get; set; }
      
        ///<summary>tank volume</summary>
        public double V { get; set; }
    
        ///<summary>type of mixing model</summary>
        public MixType MixModel { get; set; }
     
        ///<summary>mixing compartment size</summary>
        public double VMix { get; set; }
      
        ///<summary>kinetic parameter values</summary>
        public double[] Param { get; set; }
       
        ///<summary>current species concentrations</summary>
        public double[] C { get; set; }
    }

}
