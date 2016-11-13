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
using System.Diagnostics;
using org.addition.epanet.network.io;
using org.addition.epanet.network.structures;
using org.addition.epanet.util;

namespace org.addition.epanet.network {

///<summary>Units report properties & conversion support class</summary>
public class FieldsMap {

    /// <summary>Network variables</summary>
    public enum Type {
        ///<summary>nodal elevation</summary>
        ELEV = 0,
        ///<summary>nodal demand flow</summary>
        DEMAND = 1,
        ///<summary>nodal hydraulic head</summary>
        HEAD = 2,
        ///<summary>nodal pressure</summary>
        PRESSURE = 3,
        ///<summary>nodal water quality</summary>
        QUALITY = 4,

        ///<summary>link length</summary>
        LENGTH = 5,
        ///<summary>link diameter</summary>
        DIAM = 6,
        ///<summary>link flow rate</summary>
        FLOW = 7,
        ///<summary>link flow velocity</summary>
        VELOCITY = 8,
        ///<summary>link head loss</summary>
        HEADLOSS = 9,
        ///<summary>avg. water quality in link</summary>
        LINKQUAL = 10,
        ///<summary>link status</summary>
        STATUS = 11,
        ///<summary>pump/valve setting</summary>
        SETTING = 12,
        ///<summary>avg. reaction rate in link</summary>
        REACTRATE = 13,
        ///<summary>link friction factor</summary>
        FRICTION = 14,

        ///<summary>pump power output</summary>
        POWER = 15,
        ///<summary>simulation time</summary>
        TIME = 16,
        ///<summary>tank volume</summary>
        VOLUME = 17,
        ///<summary>simulation time of day</summary>
        CLOCKTIME = 18,
        ///<summary>time to fill a tank</summary>
        FILLTIME = 19,
        ///<summary>time to drain a tank</summary>
        DRAINTIME = 20
    }

    ///<summary>Report fields properties.</summary>
    private Dictionary<Type,Field> fields;

    ///<summary>Fields units values.</summary>
    private Dictionary<Type,double> units;

    ///<summary>Init fields default configuration</summary>
    public FieldsMap()
    {
        try{
            fields = new Dictionary<Type, Field>();
            units = new Dictionary<Type, double>();

            foreach (FieldsMap.Type type in Enum.GetValues(typeof(FieldsMap.Type)))
                setField(type,new Field(type.ParseStr()));

            getField(Type.FRICTION).setPrecision(3);

            for (var i = Type.DEMAND; i <= Type.QUALITY; i++)
                getField(i).setEnabled(true);

            for (var i= Type.FLOW;i<= Type.HEADLOSS; i++)
                getField(i).setEnabled(true);
        }
        catch (ENException e){
            Debug.Print(e.ToString());
        }
    }

    /**
     * Get report field properties from type.
     * @param type Field type.
     * @return Report field.
     * @throws org.addition.epanet.util.ENException If specified type not found.
     */
    public Field getField(Type type) {
        object obj = fields[type];
        if(obj==null)
            throw new ENException( ErrorCode.Err201, type.ParseStr());
        else
            return (Field)obj;
    }

    /**
     * Get conversion value from field type.
     * @param type Field type.
     * @return Conversion units value (from user units to system units)
     * @throws ENException If specified type not found.
     */
    public double getUnits(Type type) {
        object obj = units[type];
        if(obj==null)
            throw new ENException(ErrorCode.Err201,type.ParseStr());
        else
            return (double)obj;
    }

    ///<summary>Update fields and units, after loading the INP.</summary>

    public void prepare(PropertiesMap.UnitsType targetUnits,
                        PropertiesMap.FlowUnitsType flowFlag,
                        PropertiesMap.PressUnitsType pressFlag,
                        PropertiesMap.QualType qualFlag,
                        string ChemUnits,
                        Double SpGrav,
                        long Hstep)
    {
        double  dcf,
                ccf,
                qcf,
                hcf,
                pcf,
                wcf;

        if (targetUnits == PropertiesMap.UnitsType.SI)
        {
            getField(Type.DEMAND).setUnits(flowFlag.ToString());
            getField(Type.ELEV).setUnits(Keywords.u_METERS);
            getField(Type.HEAD).setUnits(Keywords.u_METERS);

            if (pressFlag == PropertiesMap.PressUnitsType.METERS)
                getField(Type.PRESSURE).setUnits(Keywords.u_METERS);
            else
                getField(Type.PRESSURE).setUnits(Keywords.u_KPA);

            getField(Type.LENGTH).setUnits(Keywords.u_METERS);
            getField(Type.DIAM).setUnits(Keywords.u_MMETERS);
            getField(Type.FLOW).setUnits(flowFlag.ToString());
            getField(Type.VELOCITY).setUnits(Keywords.u_MperSEC);
            getField(Type.HEADLOSS).setUnits("m"+Keywords.u_per1000M);
            getField(Type.FRICTION).setUnits("");
            getField(Type.POWER).setUnits(Keywords.u_KW);

            dcf = 1000.0* Constants.MperFT;
            qcf = Constants.LPSperCFS;
            if (flowFlag == PropertiesMap.FlowUnitsType.LPM) qcf = Constants.LPMperCFS;
            if (flowFlag == PropertiesMap.FlowUnitsType.MLD) qcf = Constants.MLDperCFS;
            if (flowFlag == PropertiesMap.FlowUnitsType.CMH) qcf = Constants.CMHperCFS;
            if (flowFlag == PropertiesMap.FlowUnitsType.CMD) qcf = Constants.CMDperCFS;
            hcf = Constants.MperFT;
            if (pressFlag == PropertiesMap.PressUnitsType.METERS) pcf = Constants.MperFT*SpGrav;
            else pcf = Constants.KPAperPSI*Constants.PSIperFT*SpGrav;
            wcf = Constants.KWperHP;
        }
        else
        {
            getField(Type.DEMAND).setUnits(flowFlag.ToString());
            getField(Type.ELEV).setUnits(Keywords.u_FEET);
            getField(Type.HEAD).setUnits(Keywords.u_FEET);

            getField(Type.PRESSURE).setUnits(Keywords.u_PSI);
            getField(Type.LENGTH).setUnits(Keywords.u_FEET);
            getField(Type.DIAM).setUnits(Keywords.u_INCHES);
            getField(Type.FLOW).setUnits(flowFlag.ToString());
            getField(Type.VELOCITY).setUnits(Keywords.u_FTperSEC);
            getField(Type.HEADLOSS).setUnits("ft"+Keywords.u_per1000FT);
            getField(Type.FRICTION).setUnits("");
            getField(Type.POWER).setUnits(Keywords.u_HP);


            dcf = 12.0;
            qcf = 1.0;
            if (flowFlag == PropertiesMap.FlowUnitsType.GPM) qcf = Constants.GPMperCFS;
            if (flowFlag == PropertiesMap.FlowUnitsType.MGD) qcf = Constants.MGDperCFS;
            if (flowFlag == PropertiesMap.FlowUnitsType.IMGD)qcf = Constants.IMGDperCFS;
            if (flowFlag == PropertiesMap.FlowUnitsType.AFD) qcf = Constants.AFDperCFS;
            hcf = 1.0;
            pcf = Constants.PSIperFT*SpGrav;
            wcf = 1.0;
        }
        getField(Type.QUALITY).setUnits("");
        ccf = 1.0;
        if (qualFlag == PropertiesMap.QualType.CHEM)
        {
            ccf = 1.0/Constants.LperFT3;
            getField(Type.QUALITY).setUnits(ChemUnits);
            getField(Type.REACTRATE).setUnits(ChemUnits + Keywords.t_PERDAY);
        }
        else if (qualFlag == PropertiesMap.QualType.AGE)
            getField(Type.QUALITY).setUnits(Keywords.u_HOURS);
        else if (qualFlag == PropertiesMap.QualType.TRACE)
            getField(Type.QUALITY).setUnits(Keywords.u_PERCENT);

        setUnits(Type.DEMAND,qcf);
        setUnits(Type.ELEV,hcf);
        setUnits(Type.HEAD,hcf);
        setUnits(Type.PRESSURE,pcf);
        setUnits(Type.QUALITY,ccf);
        setUnits(Type.LENGTH,hcf);
        setUnits(Type.DIAM,dcf);
        setUnits(Type.FLOW,qcf);
        setUnits(Type.VELOCITY,hcf);
        setUnits(Type.HEADLOSS,hcf);
        setUnits(Type.LINKQUAL,ccf);
        setUnits(Type.REACTRATE,ccf);
        setUnits(Type.FRICTION,1.0);
        setUnits(Type.POWER,wcf);
        setUnits(Type.VOLUME,hcf*hcf*hcf);

        if (Hstep < 1800)
        {
            setUnits(Type.TIME,1.0/60.0);
            getField(Type.TIME).setUnits(Keywords.u_MINUTES);
        }
        else
        {
            setUnits(Type.TIME,1.0/3600.0);
            getField(Type.TIME).setUnits(Keywords.u_HOURS);
        }
    }

    /**
     * Revert system units to user units.
     * @param type Field type.
     * @param value Value to be converted.
     * @return Value in user units.
     */

    public double revertUnit(FieldsMap.Type type, double value) {
        return type != (Type)(-1) ? value * getUnits(type) : value;
    }

    public double convertUnitToSystem(Type type,double value) {
        return value/getUnits(type);
    }

    /**
     * Set field properties.
     * @param type Field type.
     * @param value Report field reference.
     */
    private void setField(Type type,Field value) {
        fields[type] = value;
    }

    /**
     * Set conversion value from field type.
     * @param type Field type.
     * @param value Field value.
     */
    private void setUnits(Type type,Double value) {
        units[type] = value;
    }

}
}