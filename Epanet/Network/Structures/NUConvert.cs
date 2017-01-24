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

using Epanet.Enums;

namespace Epanet.Network.Structures {

#if NUCONVERT
    public static class NUConvert {
        public static double convertArea(UnitsType type, double value) {
            if (type == UnitsType.SI)
                return value / (Constants.MperFT * Constants.MperFT);

            return value;
        }

        public static double convertDistance(UnitsType type, double value) {
            if (type == UnitsType.SI)
                return value * (1 / Constants.MperFT);

            return value;
        }

        public static double convertFlow(FlowUnitsType flow, double value) {
            switch (flow) {
            case FlowUnitsType.CFS:
                return value * (1 / Constants.LPSperCFS);
            case FlowUnitsType.GPM:
                return value * (1 / Constants.GPMperCFS);
            case FlowUnitsType.MGD:
                return value * (1 / Constants.MGDperCFS);
            case FlowUnitsType.IMGD:
                return value * (1 / Constants.IMGDperCFS);
            case FlowUnitsType.AFD:
                return value * (1 / Constants.AFDperCFS);
            case FlowUnitsType.LPS:
                return value * (1 / Constants.LPSperCFS);
            case FlowUnitsType.LPM:
                return value * (1 / Constants.LPMperCFS);
            case FlowUnitsType.MLD:
                return value * (1 / Constants.MLDperCFS);
            case FlowUnitsType.CMH:
                return value * (1 / Constants.CMHperCFS);
            case FlowUnitsType.CMD:
                return value * (1 / Constants.CMHperCFS);
            }
            return value;
        }

        public static double convertPower(UnitsType type, double value) {
            if (type == UnitsType.SI)
                return value * (1 / Constants.KWperHP);

            return value;
        }

        public static double convertPressure(PressUnitsType type, double SpGrav, double value) {
            switch (type) {
            case PressUnitsType.PSI:
                return value;
            case PressUnitsType.KPA:
                return value / (Constants.KPAperPSI * Constants.PSIperFT * SpGrav);
            case PressUnitsType.METERS:
                return value / (Constants.MperFT * SpGrav);
            }
            return value;
        }

        public static double convertVolume(UnitsType type, double value) {
            if (type == UnitsType.SI)
                return value / (Constants.M3perFT3);

            return value;
        }

        public static double revertArea(UnitsType type, double value) {
            if (type == UnitsType.SI)
                return value * (Constants.MperFT * Constants.MperFT);

            return value;
        }

        public static double revertDistance(UnitsType type, double value) {
            if (type == UnitsType.SI)
                return value * Constants.MperFT;

            return value;
        }

        public static double revertDiameter(UnitsType type, double value) {
            if (type == UnitsType.SI)
                return value * Constants.MMperFT;
            return value * Constants.INperFT;
        }

        public static double revertFlow(FlowUnitsType flow, double value) {
            switch (flow) {
            case FlowUnitsType.CFS:
                return value * Constants.LPSperCFS;
            case FlowUnitsType.GPM:
                return value * Constants.GPMperCFS;
            case FlowUnitsType.MGD:
                return value * Constants.MGDperCFS;
            case FlowUnitsType.IMGD:
                return value * Constants.IMGDperCFS;
            case FlowUnitsType.AFD:
                return value * Constants.AFDperCFS;
            case FlowUnitsType.LPS:
                return value * Constants.LPSperCFS;
            case FlowUnitsType.LPM:
                return value * Constants.LPMperCFS;
            case FlowUnitsType.MLD:
                return value * Constants.MLDperCFS;
            case FlowUnitsType.CMH:
                return value * Constants.CMHperCFS;
            case FlowUnitsType.CMD:
                return value * Constants.CMHperCFS;
            }
            return value;
        }

        public static double revertPower(UnitsType type, double value) {
            if (type == UnitsType.SI)
                return value * Constants.KWperHP;

            return value;
        }

        public static double revertPressure(PressUnitsType type, double SpGrav, double value) {
            switch (type) {
            case PressUnitsType.PSI:
                return value;
            case PressUnitsType.KPA:
                return value * (Constants.KPAperPSI * Constants.PSIperFT * SpGrav);
            case PressUnitsType.METERS:
                return value * (Constants.MperFT * SpGrav);
            }
            return value;
        }

        public static double revertVolume(UnitsType type, double value) {
            if (type == UnitsType.SI)
                return value * (Constants.M3perFT3);

            return value;
        }


    }

#endif

}