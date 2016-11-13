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
using System.Collections.Generic;

namespace org.addition.epanet.network.structures
{

///<summary>Hydraulic node structure  (junction)</summary>

    public class Node : IComparable<Node>
{
    ///<summary>Node id string.</summary>
    private string id;
    ///<summary>Node elevation(foot).</summary>
    private double elevation;

    ///<summary>Node demand list.</summary>
    private readonly List<Demand> demand;
    ///<summary>Water quality source.</summary>
    private Source source;
    ///<summary>Initial species concentrations.</summary>
    private double[] C0;
    ///<summary>Emitter coefficient.</summary>
    private double Ke;
    ///<summary>Node reporting flag.</summary>
    private bool rptFlag;
    ///<summary>Node position.</summary>
    private Point position;

    [NonSerialized] 
    private double initDemand;
    ///<summary>Node comment.</summary>
    private String comment;

    public String getComment()
    {
        return comment;
    }

    public void setComment(String value)
    {
        this.comment = value;
    }

    public double getInitDemand()
    {
        return initDemand;
    }

    public void setInitDemand(double value)
    {
        this.initDemand = value;
    }

    //public NodeType getType() {
    //    return type;
    //}
    //
    //public void setType(NodeType type) {
    //    this.type = type;
    //}

    public Node()
    {
        C0 = new double[1];
        comment = "";
        initDemand = 0;
        //type = NodeType.JUNC;
        demand = new List<Demand>();
        position = new Point();
    }

    public Point getPosition()
    {
        return position;
    }

    public void setPosition(Point value)
    {
        this.position = value;
    }

    public String getId()
    {
        return id;
    }

    public void setId(string value)
    {
        this.id = value;
    }

    public double getElevation()
    {
        return elevation;
    }

    public void setElevation(double value)
    {
        this.elevation = value;
    }

    public List<Demand> getDemand()
    {
        return demand;
    }

    public Source getSource()
    {
        return source;
    }

    public void setSource(Source value)
    {
        this.source = value;
    }

    public double[] getC0()
    {
        return C0;
    }

    public void setC0(double[] c0)
    {
        C0 = c0;
    }

    public double getKe()
    {
        return Ke;
    }

    public void setKe(double ke)
    {
        Ke = ke;
    }

    public bool isRptFlag()
    {
        return rptFlag;
    }

    public void setReportFlag(bool value)
    {
        this.rptFlag = value;
    }

    public double getNUElevation(PropertiesMap.UnitsType units)
    {
        return NUConvert.revertDistance(units, elevation);
    }

    public void setNUElevation(PropertiesMap.UnitsType units, double elev)
    {
        elevation = NUConvert.convertDistance(units, elev);
    }

//    @Override
//    public bool equals(Object o) {
//        if (this == o) return true;
//        if (o == null || getClass() != o.getClass()) return false;
//
//        Node node = (Node) o;
//
//        if (id != null ? !id.equals(node.id) : node.id != null) return false;
//
//        return true;
//    }

    public override int GetHashCode()
    {
        return string.IsNullOrEmpty(id) ? 0 : id.GetHashCode();
    }

    public int CompareTo(Node o)
    {
        return string.Compare(id, o.id, StringComparison.OrdinalIgnoreCase);
    }
}
}