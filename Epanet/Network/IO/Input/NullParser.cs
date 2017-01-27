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

namespace Epanet.Network.IO.Input {

    ///<summary>Network conversion units only class.</summary>
    public class NullParser:InputParser {

        public override Network Parse(Network nw, string f) {
            net = nw ?? new Network();

            AdjustData(net);
            net.FieldsMap.Prepare(
                   net.UnitsFlag,
                   net.FlowFlag,
                   net.PressFlag,
                   net.QualFlag,
                   net.ChemUnits,
                   net.SpGrav,
                   net.HStep);

            Convert();

            return net;
        }
    }

}