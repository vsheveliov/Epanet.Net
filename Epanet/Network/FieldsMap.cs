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

using Epanet.Enums;
using Epanet.Network.IO;
using Epanet.Network.Structures;
using Epanet.Util;

namespace Epanet.Network {

    ///<summary>Units report properties & conversion support class</summary>
    public class FieldsMap {
        ///<summary>Report fields properties.</summary>
        private readonly Dictionary<FieldType, Field> fields;

        ///<summary>Fields units values.</summary>
        private readonly Dictionary<FieldType, double> units;

        ///<summary>Init fields default configuration</summary>
        public FieldsMap() {
            try {
                this.fields = new Dictionary<FieldType, Field>();
                this.units = new Dictionary<FieldType, double>();

                foreach (FieldType type in Enum.GetValues(typeof(FieldType)))
                    this.fields[type] = new Field(type.ParseStr());

                this.GetField(FieldType.FRICTION).Precision = 3;

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
        /// Throws <see cref="ENException"/> 
        /// If specified type not found.
        /// </remarks>
        public Field GetField(FieldType fieldType) {
            Field value;

            if (!this.fields.TryGetValue(fieldType, out value))
                throw new ENException(ErrorCode.Err201, fieldType.ParseStr());

            return value;
        }

        ///<summary>Get conversion value from field type.</summary>
        /// <param name="fieldType">Field type.</param>
        /// <returns>Conversion units value (from user units to system units).</returns>
        /// <remarks>
        /// Throws <see cref="ENException"/> If specified type not found.
        /// </remarks>
        public double GetUnits(FieldType fieldType) {
            double value;
            if (!this.units.TryGetValue(fieldType, out value))
                throw new ENException(ErrorCode.Err201, fieldType.ParseStr());

            return value;
        }

        ///<summary>Update fields and units, after loading the INP.</summary>
        public void Prepare(
            UnitsType targetUnits,
            FlowUnitsType flowFlag,
            PressUnitsType pressFlag,
            QualType qualFlag,
            string chemUnits,
            double spGrav,
            long hstep) {

            double dcf,
                   ccf,
                   qcf,
                   hcf,
                   pcf,
                   wcf;

            if (targetUnits == UnitsType.SI) {

                this.GetField(FieldType.DEMAND).Units = flowFlag.ToString();
                this.GetField(FieldType.ELEV).Units = Keywords.u_METERS;
                this.GetField(FieldType.HEAD).Units = Keywords.u_METERS;

                this.GetField(FieldType.PRESSURE).Units = pressFlag == PressUnitsType.METERS
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
                if (flowFlag == FlowUnitsType.LPM) qcf = Constants.LPMperCFS;
                if (flowFlag == FlowUnitsType.MLD) qcf = Constants.MLDperCFS;
                if (flowFlag == FlowUnitsType.CMH) qcf = Constants.CMHperCFS;
                if (flowFlag == FlowUnitsType.CMD) qcf = Constants.CMDperCFS;
                hcf = Constants.MperFT;
                if (pressFlag == PressUnitsType.METERS) pcf = Constants.MperFT * spGrav;
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
                if (flowFlag == FlowUnitsType.GPM) qcf = Constants.GPMperCFS;
                if (flowFlag == FlowUnitsType.MGD) qcf = Constants.MGDperCFS;
                if (flowFlag == FlowUnitsType.IMGD) qcf = Constants.IMGDperCFS;
                if (flowFlag == FlowUnitsType.AFD) qcf = Constants.AFDperCFS;
                hcf = 1.0;
                pcf = Constants.PSIperFT * spGrav;
                wcf = 1.0;
            }

            this.GetField(FieldType.QUALITY).Units = "";
            ccf = 1.0;

            switch (qualFlag) {
            case QualType.CHEM:
                ccf = 1.0 / Constants.LperFT3;
                this.GetField(FieldType.QUALITY).Units = chemUnits;
                this.GetField(FieldType.REACTRATE).Units = chemUnits + Keywords.t_PERDAY;
                break;
            case QualType.AGE:
                this.GetField(FieldType.QUALITY).Units = Keywords.u_HOURS;
                break;
            case QualType.TRACE:
                this.GetField(FieldType.QUALITY).Units = Keywords.u_PERCENT;
                break;
            }

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
            return fieldType == (FieldType)(-1)
                ? value 
                : value * this.GetUnits(fieldType);
        }

        public double ConvertUnitToSystem(FieldType fieldType, double value) {
            return value / this.GetUnits(fieldType);
        }

        ///<summary>Set conversion value from field type.</summary>
        /// <param name="fieldType">Field type.</param>
        /// <param name="value">Field value.</param>
        private void SetUnits(FieldType fieldType, double value) {
            this.units[fieldType] = value;
        }

    }

}