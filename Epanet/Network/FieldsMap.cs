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
        public enum FieldType {
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
        private readonly Dictionary<FieldType, Field> _fields;

        ///<summary>Fields units values.</summary>
        private readonly Dictionary<FieldType, double> _units;

        ///<summary>Init fields default configuration</summary>
        public FieldsMap() {
            try {
                this._fields = new Dictionary<FieldType, Field>();
                this._units = new Dictionary<FieldType, double>();

                foreach (FieldType type in Enum.GetValues(typeof(FieldType)))
                    this.SetField(type, new Field(type.ParseStr()));

                this.GetField(FieldType.FRICTION).SetPrecision(3);

                for (var i = FieldType.DEMAND; i <= FieldType.QUALITY; i++)
                    this.GetField(i).Enabled = true;

                for (var i = FieldType.FLOW; i <= FieldType.HEADLOSS; i++)
                    this.GetField(i).Enabled = true;
            }
            catch (ENException e) {
                Debug.Print(e.ToString());
            }
        }

        ///<summary>Get report field properties from type.</summary>
        /// <param name="fieldType">Field type.</param>
        /// <returns>Report field.</returns>
        /// <remarks>
        /// Throws <see cref="org.addition.epanet.util.ENException"/> 
        /// If specified type not found.
        /// </remarks>
        public Field GetField(FieldType fieldType) {
            object obj = this._fields[fieldType];

            if (obj == null)
                throw new ENException(ErrorCode.Err201, fieldType.ParseStr());
            else
                return (Field)obj;
        }

        ///<summary>Get conversion value from field type.</summary>
        /// <param name="fieldType">Field type.</param>
        /// <returns>Conversion units value (from user units to system units).</returns>
        /// <remarks>
        /// Throws <see cref="ENException"/> If specified type not found.
        /// </remarks>
        public double GetUnits(FieldType fieldType) {
            object obj = this._units[fieldType];
            if (obj == null)
                throw new ENException(ErrorCode.Err201, fieldType.ParseStr());
            else
                return (double)obj;
        }

        ///<summary>Update fields and units, after loading the INP.</summary>

        public void Prepare(
            PropertiesMap.UnitsType targetUnits,
            PropertiesMap.FlowUnitsType flowFlag,
            PropertiesMap.PressUnitsType pressFlag,
            PropertiesMap.QualType qualFlag,
            string chemUnits,
            double spGrav,
            long hstep) {
            double dcf,
                   ccf,
                   qcf,
                   hcf,
                   pcf,
                   wcf;

            if (targetUnits == PropertiesMap.UnitsType.SI) {
                this.GetField(FieldType.DEMAND).Units = flowFlag.ToString();
                this.GetField(FieldType.ELEV).Units = Keywords.u_METERS;
                this.GetField(FieldType.HEAD).Units = Keywords.u_METERS;

                this.GetField(FieldType.PRESSURE).Units = pressFlag == PropertiesMap.PressUnitsType.METERS
                    ? Keywords.u_METERS
                    : Keywords.u_KPA;

                this.GetField(FieldType.LENGTH).Units = Keywords.u_METERS;
                this.GetField(FieldType.DIAM).Units = Keywords.u_MMETERS;
                this.GetField(FieldType.FLOW).Units = flowFlag.ToString();
                this.GetField(FieldType.VELOCITY).Units = Keywords.u_MperSEC;
                this.GetField(FieldType.HEADLOSS).Units = "m" + Keywords.u_per1000M;
                this.GetField(FieldType.FRICTION).Units = "";
                this.GetField(FieldType.POWER).Units = Keywords.u_KW;

                dcf = 1000.0 * Constants.MperFT;
                qcf = Constants.LPSperCFS;
                if (flowFlag == PropertiesMap.FlowUnitsType.LPM) qcf = Constants.LPMperCFS;
                if (flowFlag == PropertiesMap.FlowUnitsType.MLD) qcf = Constants.MLDperCFS;
                if (flowFlag == PropertiesMap.FlowUnitsType.CMH) qcf = Constants.CMHperCFS;
                if (flowFlag == PropertiesMap.FlowUnitsType.CMD) qcf = Constants.CMDperCFS;
                hcf = Constants.MperFT;
                if (pressFlag == PropertiesMap.PressUnitsType.METERS) pcf = Constants.MperFT * spGrav;
                else pcf = Constants.KPAperPSI * Constants.PSIperFT * spGrav;
                wcf = Constants.KWperHP;
            }
            else {
                this.GetField(FieldType.DEMAND).Units = flowFlag.ToString();
                this.GetField(FieldType.ELEV).Units = Keywords.u_FEET;
                this.GetField(FieldType.HEAD).Units = Keywords.u_FEET;

                this.GetField(FieldType.PRESSURE).Units = Keywords.u_PSI;
                this.GetField(FieldType.LENGTH).Units = Keywords.u_FEET;
                this.GetField(FieldType.DIAM).Units = Keywords.u_INCHES;
                this.GetField(FieldType.FLOW).Units = flowFlag.ToString();
                this.GetField(FieldType.VELOCITY).Units = Keywords.u_FTperSEC;
                this.GetField(FieldType.HEADLOSS).Units = "ft" + Keywords.u_per1000FT;
                this.GetField(FieldType.FRICTION).Units = "";
                this.GetField(FieldType.POWER).Units = Keywords.u_HP;


                dcf = 12.0;
                qcf = 1.0;
                if (flowFlag == PropertiesMap.FlowUnitsType.GPM) qcf = Constants.GPMperCFS;
                if (flowFlag == PropertiesMap.FlowUnitsType.MGD) qcf = Constants.MGDperCFS;
                if (flowFlag == PropertiesMap.FlowUnitsType.IMGD) qcf = Constants.IMGDperCFS;
                if (flowFlag == PropertiesMap.FlowUnitsType.AFD) qcf = Constants.AFDperCFS;
                hcf = 1.0;
                pcf = Constants.PSIperFT * spGrav;
                wcf = 1.0;
            }
            this.GetField(FieldType.QUALITY).Units = "";
            ccf = 1.0;
            if (qualFlag == PropertiesMap.QualType.CHEM) {
                ccf = 1.0 / Constants.LperFT3;
                this.GetField(FieldType.QUALITY).Units = chemUnits;
                this.GetField(FieldType.REACTRATE).Units = chemUnits + Keywords.t_PERDAY;
            }
            else if (qualFlag == PropertiesMap.QualType.AGE)
                this.GetField(FieldType.QUALITY).Units = Keywords.u_HOURS;
            else if (qualFlag == PropertiesMap.QualType.TRACE)
                this.GetField(FieldType.QUALITY).Units = Keywords.u_PERCENT;

            this.SetUnits(FieldType.DEMAND, qcf);
            this.SetUnits(FieldType.ELEV, hcf);
            this.SetUnits(FieldType.HEAD, hcf);
            this.SetUnits(FieldType.PRESSURE, pcf);
            this.SetUnits(FieldType.QUALITY, ccf);
            this.SetUnits(FieldType.LENGTH, hcf);
            this.SetUnits(FieldType.DIAM, dcf);
            this.SetUnits(FieldType.FLOW, qcf);
            this.SetUnits(FieldType.VELOCITY, hcf);
            this.SetUnits(FieldType.HEADLOSS, hcf);
            this.SetUnits(FieldType.LINKQUAL, ccf);
            this.SetUnits(FieldType.REACTRATE, ccf);
            this.SetUnits(FieldType.FRICTION, 1.0);
            this.SetUnits(FieldType.POWER, wcf);
            this.SetUnits(FieldType.VOLUME, hcf * hcf * hcf);

            if (hstep < 1800) {
                this.SetUnits(FieldType.TIME, 1.0 / 60.0);
                this.GetField(FieldType.TIME).Units = Keywords.u_MINUTES;
            }
            else {
                this.SetUnits(FieldType.TIME, 1.0 / 3600.0);
                this.GetField(FieldType.TIME).Units = Keywords.u_HOURS;
            }
        }

        /// <summary>Revert system units to user units.</summary>
        ///  <param name="fieldType">Field type.</param>
        /// <param name="value">Value to be converted.</param>
        /// <returns>Value in user units.</returns>
        public double RevertUnit(FieldType fieldType, double value) {
            return fieldType != (FieldType)(-1) ? value * this.GetUnits(fieldType) : value;
        }

        public double ConvertUnitToSystem(FieldType fieldType, double value) { return value / this.GetUnits(fieldType); }

        ///<summary>Set field properties.</summary>
        /// <param name="fieldType">Field type.</param>
        /// <param name="value">Report field reference.</param>
        private void SetField(FieldType fieldType, Field value) {
            this._fields[fieldType] = value;
        }

        ///<summary>Set conversion value from field type.</summary>
        /// <param name="fieldType">Field type.</param>
        /// <param name="value">Field value.</param>
        private void SetUnits(FieldType fieldType, double value) {
            this._units[fieldType] = value;
        }

    }

}