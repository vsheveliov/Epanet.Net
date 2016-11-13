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

///<summary>Hydraulic pump structure.</summary>
public class Pump : Link
{
    ///<summary>Type of pump curve.</summary>
    public enum Type{
        ///<summary>Constant horsepower.</summary>
        CONST_HP = 0,
        ///<summary>Power function.</summary>
        POWER_FUNC = 1,
        ///<summary>User-defined custom curve.</summary>
        CUSTOM = 2,
        NOCURVE = 3
    }

    ///<summary>Unit energy cost.</summary>
    private double eCost;
    ///<summary>Effic. v. flow curve reference.</summary>
    private Curve eCurve;
    ///<summary>Energy usage statistics.</summary>
    private double[]  energy = {0,0,0,0,0,0};
    ///<summary>Energy cost pattern.</summary>
    private Pattern ePat;
    ///<summary>Shutoff head (feet)</summary>
    private double h0;
    ///<summary>Head v. flow curve reference.</summary>
    private Curve hCurve;
    ///<summary>Maximum head (feet)</summary>
    private double hMax;
    ///<summary>Flow exponent.</summary>
    private double n;
    ///<summary>Pump curve type.</summary>
    private Type ptype;
    ///<summary>Initial flow (feet^3/s).</summary>
    private double q0;
    ///<summary>Maximum flow (feet^3/s).</summary>
    private double qMax;
    ///<summary>Flow coefficient.</summary>
    private double r;
    ///<summary>Utilization pattern reference.</summary>
    private Pattern uPat;

    public Pump()
    {
        
    }


    public double getEcost() {
        return eCost;
    }

    public Curve getEcurve() {
        return eCurve;
    }

    public double getEnergy(int id) {
        return energy[id];
    }

    public Pattern getEpat() {
        return ePat;
    }

    public double getFlowCoefficient() {
        return r;
    }

    public double getH0() {
        return h0;
    }

    public Curve getHcurve() {
        return hCurve;
    }

    public double getHmax() {
        return hMax;
    }

    public double getN() {
        return n;
    }

    public double getNUFlowCoefficient(PropertiesMap.UnitsType type){
        return NUConvert.revertPower(type,r);
    }


    public double getNUInitialFlow(PropertiesMap.FlowUnitsType type){
        return NUConvert.revertFlow(type,q0);
    }

    public double getNUMaxFlow(PropertiesMap.FlowUnitsType type){
        return NUConvert.revertFlow(type,qMax);
    }

    public double getNUMaxHead(PropertiesMap.UnitsType type){
        return NUConvert.revertDistance(type,hMax);
    }

    public double getNUShutoffHead(PropertiesMap.UnitsType type){
        return NUConvert.revertDistance(type,hMax);
    }

    public Type getPtype() {
        return ptype;
    }

    public double getQ0() {
        return q0;
    }

    public double getQmax() {
        return qMax;
    }

    public Pattern getUpat() {
        return uPat;
    }

    public void setEcost(double ecost) {
        eCost = ecost;
    }

    public void setEcurve(Curve ecurve) {
        this.eCurve = ecurve;
    }

    public void setEnergy(int id, double energy) {
        this.energy[id]=energy;
    }

    public void setEpat(Pattern epat) {
        ePat = epat;
    }

    public void setFlowCoefficient(double r) {
        this.r = r;
    }

    public void setH0(double h0) {
        this.h0 = h0;
    }

    public void setHcurve(Curve hcurve) {
        this.hCurve = hcurve;
    }

    public void setHmax(double hmax) {
        this.hMax = hmax;
    }

    public void setN(double n) {
        this.n = n;
    }

    public void setNUFlowCoefficient(PropertiesMap.UnitsType type, double value){
        r = NUConvert.convertPower(type,value);
    }

    public void setNUInitialFlow(PropertiesMap.FlowUnitsType type, double value){
        q0 = NUConvert.convertFlow(type,value);
    }

    public void setNUMaxFlow(PropertiesMap.FlowUnitsType type, double value){
        qMax = NUConvert.convertFlow(type,value);
    }

    public void setNUMaxHead(PropertiesMap.UnitsType type, double value){
        hMax = NUConvert.convertDistance(type,value);
    }

    public void setNUShutoffHead(PropertiesMap.UnitsType type, double value){
        h0 = NUConvert.convertDistance(type,value);
    }

    public void setPtype(Type ptype) {
        this.ptype = ptype;
    }

    public void setQ0(double q0) {
        this.q0 = q0;
    }

    public void setQmax(double qMax) {
        this.qMax = qMax;
    }

    public void setUpat(Pattern upat) {
        uPat = upat;
    }
}
}