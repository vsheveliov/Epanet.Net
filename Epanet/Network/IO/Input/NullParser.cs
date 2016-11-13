/*
 * Copyright (C) 2012  Addition, Lda. (addition at addition dot pt)
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

using System.Diagnostics;
using System.IO;
using org.addition.epanet.log;

namespace org.addition.epanet.network.io.input {

///<summary>Network conversion units only class.</summary>
public class NullParser : InputParser{
    public NullParser(TraceSource log):base(log) {}

    public override Network parse(Network net, string f) {
        adjust(net);
        net.getFieldsMap().prepare(net.getPropertiesMap().getUnitsflag(),
                net.getPropertiesMap().getFlowflag(),
                net.getPropertiesMap().getPressflag(),
                net.getPropertiesMap().getQualflag(),
                net.getPropertiesMap().getChemUnits(),
                net.getPropertiesMap().getSpGrav(),
                net.getPropertiesMap().getHstep());
        convert(net);
        return net;
    }
}
}