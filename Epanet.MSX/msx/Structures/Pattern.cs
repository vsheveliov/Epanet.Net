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

using System.Collections.Generic;

namespace org.addition.epanet.msx.Structures {

// Time Pattern object
public class Pattern {
    private string          id;             // pattern ID
    private long            interval;       // current time interval
    private int             current;        // current multiplier
    private List<double>    multipliers;    // list of multipliers

    public int getLength(){
        return multipliers.Count;
    }

    public void setId(string text){
        id = text;
    }

    public string getId() {
        return id;
    }

    public List<double> getMultipliers(){
        return multipliers;
    }

    public long getInterval() {
        return interval;
    }

    public void setInterval(long interval_) {
        this.interval = interval_;
    }

    public int getCurrent() {
        return current;
    }

    public void setCurrent(int current) {
        this.current = current;
    }

    public Pattern() {
        this.id = "";
        multipliers = new List<double>();
        current = 0;
        interval = 0;
    }

    public Pattern clone(){
        return new Pattern
        {
            id = id,
            current = current,
            multipliers = new List<double>(multipliers),
            interval = interval
        };
    }
}
}