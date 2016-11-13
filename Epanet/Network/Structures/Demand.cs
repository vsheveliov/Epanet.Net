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

namespace org.addition.epanet.network.structures {

///<summary>Node demand category.</summary>
public class Demand {
    ///<summary>Baseline demand (Feet^3/t)</summary>
    private double @base;
    ///<summary>Pattern reference.</summary>
    private Pattern pattern;


    public Demand(double @base, Pattern pattern) {
        this.@base = @base;
        this.pattern = pattern;
    }

    public double getBase() {
        return @base;
    }

    public double getBaseNU(PropertiesMap.FlowUnitsType units) {
        return NUConvert.revertFlow(units,@base);
    }

    public Pattern getPattern() {
        return pattern;
    }

    public void setBase(double @base) {
        this.@base = @base;
    }


    public void setBaseNU(PropertiesMap.FlowUnitsType units,double value) {
        @base = NUConvert.convertFlow(units,value);
    }

    public void setPattern(Pattern pattern) {
        this.pattern = pattern;
    }
}
}