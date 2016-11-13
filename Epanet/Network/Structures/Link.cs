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

namespace org.addition.epanet.network.structures {

///<summary>Hydraulic link structure (pipe)</summary>
public class Link : IComparable<Link> {
    /**
     * Init links flow resistance values.
     *
     * @param formflag
     * @param hexp
     * @throws org.addition.epanet.util.ENException
     *
     */
    public void initResistance(PropertiesMap.FormType formflag, Double hexp) {
        double e, d, L;
        this.setFlowResistance(Constants.CSMALL);
        switch ((LinkType)this.getType()) {
            case LinkType.CV:
            case LinkType.PIPE:
                e = this.getRoughness();
                d = this.getDiameter();
                L = this.getLenght();
                switch (formflag) {
                    case PropertiesMap.FormType.HW:
                        this.setFlowResistance(4.727 * L / Math.Pow(e, hexp) / Math.Pow(d, 4.871));
                        break;
                    case PropertiesMap.FormType.DW:
                        this.setFlowResistance(L / 2.0 / 32.2 / d / Math.Pow(Constants.PI * Math.Pow(d, 2) / 4.0, 2));
                        break;
                    case PropertiesMap.FormType.CM:
                        this.setFlowResistance(Math.Pow(4.0 * e / (1.49 * Constants.PI * d * d), 2) *
                                Math.Pow((d / 4.0), -1.333) * L);
                        break;
                }
                break;

            case LinkType.PUMP:
                this.setFlowResistance(Constants.CBIG);
                break;
        }
    }

        /// <summary>Type of link</summary>
    public enum LinkType {
        /// <summary>Pipe with check valve.</summary>
        CV = 0,
        /// <summary>Regular pipe.</summary>
        PIPE = 1,
        /// <summary>Pump.</summary>
        PUMP = 2,
        /// <summary>Pressure reducing valve.</summary>
        PRV = 3,
        /// <summary>Pressure sustaining valve.</summary>
        PSV = 4,
        /// <summary>Pressure breaker valve.</summary>
        PBV = 5,
        /// <summary>Flow control valve.</summary>
        FCV = 6,
        /// <summary>Throttle control valve.</summary>
        TCV = 7,
        /// <summary>General purpose valve.</summary>
        GPV = 8
    }

    ///<summary>Link/Tank/Pump status</summary>
    public enum StatType {
        /// <summary>Pump cannot deliver head (closed).</summary>
        XHEAD = 0,
        /// <summary>Temporarily closed.</summary>
        TEMPCLOSED = 1,
        /// <summary>Closed.</summary>
        CLOSED = 2,
        /// <summary>Open.</summary>
        OPEN = 3,
        /// <summary>Valve active (partially open).</summary>
        ACTIVE = 4,
        /// <summary>Pump exceeds maximum flow.</summary>
        XFLOW = 5,
        /// <summary>FCV cannot supply flow.</summary>
        XFCV = 6,
        /// <summary>Valve cannot supply pressure.</summary>
        XPRESSURE = 7,
        /// <summary>Tank filling.</summary>
        FILLING = 8,
        /// <summary>Tank emptying.</summary>
        EMPTYING = 9,
    }

    ///<summary>Initial species concentrations.</summary>
    private double[] c0;
    ///<summary>Link comment (parsed from INP or excel file)</summary>
    private String comment;
    ///<summary>Link diameter (feet).</summary>
    private double diameter;
    ///<summary>First node.</summary>
    private Node first;
    ///<summary>Link name.</summary>
    private string ID;
    ///<summary>Bulk react. coeff.</summary>
    private double kb;
    ///<summary>Minor loss coeff.</summary>
    private double km;
    ///<summary>Wall react. coeff.</summary>
    private double kw;
    ///<summary>Link length (feet).</summary>
    private double lenght;
    ///<summary>Kinetic parameter values.</summary>
    private double[] param;

    ///<summary>Flow resistance.</summary>
    private double resistance;
    ///<summary>Roughness factor.</summary>
    private double roughness;
    ///<summary>Link report flag.</summary>
    private bool rptFlag;
    ///<summary>Second node.</summary>
    private Node second;
    ///<summary>Link status.</summary>
    private StatType status;
    ///<summary>Link subtype.</summary>
    private LinkType type;
    ///<summary>List of points for link path rendering.</summary>
    private List<Point> vertices;

    public Link() {
        comment = "";
        vertices = new List<Point>();
        type = LinkType.CV;
        status = StatType.XHEAD;
    }

    public double[] getC0() {
        return c0;
    }

    public String getComment() {
        return comment;
    }

    public double getDiameter() {
        return diameter;
    }

    public Node getFirst() {
        return first;
    }

    public double getFlowResistance() {
        return resistance;
    }

    public String getId() {
        return ID;
    }

    public double getKb() {
        return kb;
    }

    public double getKm() {
        return km;
    }

    public double getKw() {
        return kw;
    }

    public double getLenght() {
        return lenght;
    }

    public double getNUDiameter(PropertiesMap.UnitsType type) {
        return NUConvert.revertDiameter(type, diameter);
    }

    public double getNULength(PropertiesMap.UnitsType type) {
        return NUConvert.revertDistance(type, lenght);
    }

    public double getNURoughness(PropertiesMap.FlowUnitsType fType, PropertiesMap.PressUnitsType pType, double SpGrav) {
        switch (getType()) {
            case LinkType.FCV:
                return NUConvert.revertFlow(fType, roughness);
            case LinkType.PRV:
            case LinkType.PSV:
            case LinkType.PBV:
                return NUConvert.revertPressure(pType, SpGrav, roughness);
        }
        return roughness;
    }

    public double[] getParam() {
        return param;
    }

    public double getRoughness() {
        return roughness;
    }

    public Node getSecond() {
        return second;
    }

    public StatType getStat() {
        return status;
    }

    public LinkType getType() {
        return type;
    }

    public List<Point> getVertices() {
        return vertices;
    }

    public bool isRptFlag() {
        return rptFlag;
    }

    public void setC0(double[] c0) {
        this.c0 = c0;
    }

    public void setComment(String comment) {
        this.comment = comment;
    }

    public void setDiameter(double diameter) {
        this.diameter = diameter;
    }

    public void setDiameterAndUpdate(double diameter, org.addition.epanet.network.Network net) {
        double realkm = km * Math.Pow(this.diameter, 4.0) / 0.02517;
        this.diameter = diameter;
        km = 0.02517 * realkm / Math.Pow(diameter, 4);
        initResistance(net.getPropertiesMap().getFormflag(), net.getPropertiesMap().getHexp());
    }


    public void setFirst(Node n1) {
        first = n1;
    }

    public void setFlowResistance(double r) {
        this.resistance = r;
    }

    public void setId(String id) {
        ID = id;
    }

    public void setKb(double kb) {
        this.kb = kb;
    }

    public void setKm(double km) {
        this.km = km;
    }

    public void setKw(double kw) {
        this.kw = kw;
    }

    public void setLenght(double len) {
        this.lenght = len;
    }

    public void setNUDiameter(PropertiesMap.UnitsType type, double value) {
        diameter = NUConvert.convertDistance(type, value);
    }

    public void setNULenght(PropertiesMap.UnitsType type, double value) {
        lenght = NUConvert.convertDistance(type, value);
    }

    public void setParam(double[] param) {
        this.param = param;
    }

    public void setReportFlag(bool rptFlag) {
        this.rptFlag = rptFlag;
    }

    public void setRoughness(double kc) {
        this.roughness = kc;
    }

    public void setSecond(Node n2) {
        second = n2;
    }

    public void setStatus(StatType stat) {
        this.status = stat;
    }

    public void setType(LinkType type) {
        this.type = type;
    }

//    @Override
//    public bool equals(Object o) {
//        if (this == o) return true;
//        if (o == null || getClass() != o.getClass()) return false;
//
//        Link link = (Link) o;
//
//        if (ID != null ? !ID.equals(link.ID) : link.ID != null) return false;
//
//        return true;
//    }

    public override int GetHashCode() {
        return ID != null ? ID.GetHashCode() : 0;
    }

    public int CompareTo(Link o)
    {
        return string.Compare(ID, o.ID, StringComparison.Ordinal);
    }
}
}