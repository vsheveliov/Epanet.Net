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
        private readonly Dictionary<FieldType, Field> _fields;

        ///<summary>Fields units values.</summary>
        private readonly Dictionary<FieldType, double> _units;

        ///<summary>Init fields default configuration</summary>
        public FieldsMap() {
            try {
                _fields = new Dictionary<FieldType, Field>();
                _units = new Dictionary<FieldType, double>();

                foreach (FieldType type in Enum.GetValues(typeof(FieldType)))
                    _fields[type] = new Field(type.ParseStr());

                GetField(FieldType.FRICTION).Precision = 3;

                for (var i = FieldType.DEMAND; i <= FieldType.QUALITY; i++)
                    GetField(i).Enabled = true;

                for (var i = FieldType.FLOW; i <= FieldType.HEADLOSS; i++)
                    GetField(i).Enabled = true;
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

            if (!_fields.TryGetValue(fieldType, out value))
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
            if (!_units.TryGetValue(fieldType, out value))
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

                GetField(FieldType.DEMAND).Units = flowFlag.ToString();
                GetField(FieldType.ELEV).Units = Keywords.u_METERS;
                GetField(FieldType.HEAD).Units = Keywords.u_METERS;

                GetField(FieldType.PRESSURE).Units = pressFlag == PressUnitsType.METERS
                    ? Keywords.u_METERS
                    : Keywords.u_KPA;

                GetField(FieldType.LENGTH).Units = Keywords.u_METERS;
                GetField(FieldType.DIAM).Units = Keywords.u_MMETERS;
                GetField(FieldType.FLOW).Units = flowFlag.ToString();
                GetField(FieldType.VELOCITY).Units = Keywords.u_MperSEC;
                GetField(FieldType.HEADLOSS).Units = "m" + Keywords.u_per1000M;
                GetField(FieldType.FRICTION).Units = "";
                GetField(FieldType.POWER).Units = Keywords.u_KW;

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
                GetField(FieldType.DEMAND).Units = flowFlag.ToString();
                GetField(FieldType.ELEV).Units = Keywords.u_FEET;
                GetField(FieldType.HEAD).Units = Keywords.u_FEET;

                GetField(FieldType.PRESSURE).Units = Keywords.u_PSI;
                GetField(FieldType.LENGTH).Units = Keywords.u_FEET;
                GetField(FieldType.DIAM).Units = Keywords.u_INCHES;
                GetField(FieldType.FLOW).Units = flowFlag.ToString();
                GetField(FieldType.VELOCITY).Units = Keywords.u_FTperSEC;
                GetField(FieldType.HEADLOSS).Units = "ft" + Keywords.u_per1000FT;
                GetField(FieldType.FRICTION).Units = "";
                GetField(FieldType.POWER).Units = Keywords.u_HP;


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

            GetField(FieldType.QUALITY).Units = "";
            ccf = 1.0;

            switch (qualFlag) {
            case QualType.CHEM:
                ccf = 1.0 / Constants.LperFT3;
                GetField(FieldType.QUALITY).Units = chemUnits;
                GetField(FieldType.REACTRATE).Units = chemUnits + Keywords.t_PERDAY;
                break;
            case QualType.AGE:
                GetField(FieldType.QUALITY).Units = Keywords.u_HOURS;
                break;
            case QualType.TRACE:
                GetField(FieldType.QUALITY).Units = Keywords.u_PERCENT;
                break;
            }

            SetUnits(FieldType.DEMAND, qcf);
            SetUnits(FieldType.ELEV, hcf);
            SetUnits(FieldType.HEAD, hcf);
            SetUnits(FieldType.PRESSURE, pcf);
            SetUnits(FieldType.QUALITY, ccf);
            SetUnits(FieldType.LENGTH, hcf);
            SetUnits(FieldType.DIAM, dcf);
            SetUnits(FieldType.FLOW, qcf);
            SetUnits(FieldType.VELOCITY, hcf);
            SetUnits(FieldType.HEADLOSS, hcf);
            SetUnits(FieldType.LINKQUAL, ccf);
            SetUnits(FieldType.REACTRATE, ccf);
            SetUnits(FieldType.FRICTION, 1.0);
            SetUnits(FieldType.POWER, wcf);
            SetUnits(FieldType.VOLUME, hcf * hcf * hcf);

            if (hstep < 1800) {
                SetUnits(FieldType.TIME, 1.0 / 60.0);
                GetField(FieldType.TIME).Units = Keywords.u_MINUTES;
            }
            else {
                SetUnits(FieldType.TIME, 1.0 / 3600.0);
                GetField(FieldType.TIME).Units = Keywords.u_HOURS;
            }
        }

        /// <summary>Revert system units to user units.</summary>
        ///  <param name="fieldType">Field type.</param>
        /// <param name="value">Value to be converted.</param>
        /// <returns>Value in user units.</returns>
        public double RevertUnit(FieldType fieldType, double value) {
            return fieldType == (FieldType)(-1)
                ? value 
                : value * GetUnits(fieldType);
        }

        public double ConvertUnitToSystem(FieldType fieldType, double value) {
            return value / GetUnits(fieldType);
        }

        ///<summary>Set conversion value from field type.</summary>
        /// <param name="fieldType">Field type.</param>
        /// <param name="value">Field value.</param>
        private void SetUnits(FieldType fieldType, double value) {
            _units[fieldType] = value;
        }

    }

}