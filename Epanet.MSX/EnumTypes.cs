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

namespace org.addition.epanet.msx
{
    public class EnumTypes
    {
        /// <summary>Pipe surface area units</summary>
        public enum AreaUnitsType
        {
            /// <summary>square feet</summary>
            FT2 = 0,

            /// <summary>square meters</summary>
            M2 = 1,

            /// <summary>square centimeters</summary>
            CM2 = 2,
        }

        /// <summary>Degree of coupling for solving DAE's</summary>
        public enum CouplingType
        {
            /// <summary>no coupling between alg. & diff. eqns.</summary>
            NO_COUPLING,

            /// <summary>full coupling between alg. & diff. eqns.</summary>
            FULL_COUPLING
        }

        /// <summary>Error codes (501-515)</summary>
        public enum ErrorCodeType
        {
            ERR_FIRST = 500,
            ERR_MEMORY = 501,
            ERR_NO_EPANET_FILE = 502,
            ERR_OPEN_MSX_FILE = 503,
            ERR_OPEN_HYD_FILE = 504,
            ERR_READ_HYD_FILE = 505,
            ERR_MSX_INPUT = 506,
            ERR_NUM_PIPE_EXPR = 507,
            ERR_NUM_TANK_EXPR = 508,
            ERR_INTEGRATOR_OPEN = 509,
            ERR_NEWTON_OPEN = 510,
            ERR_OPEN_OUT_FILE = 511,
            ERR_IO_OUT_FILE = 512,
            ERR_INTEGRATOR = 513,
            ERR_NEWTON = 514,
            ERR_INVALID_OBJECT_TYPE = 515,
            ERR_INVALID_OBJECT_INDEX = 516,
            ERR_UNDEFINED_OBJECT_ID = 517,
            ERR_INVALID_OBJECT_PARAMS = 518,
            ERR_MSX_NOT_OPENED = 519,
            ERR_MSX_OPENED = 520,
            ERR_OPEN_RPT_FILE = 521,
            ERR_MAX = 522,
        }

        /// <summary>Types of math expressions</summary>
        public enum ExpressionType
        {
            /// <summary>no expression</summary>
            NO_EXPR,

            /// <summary>reaction rate</summary>
            RATE,

            /// <summary>simple formula</summary>
            FORMULA,

            /// <summary>equilibrium expression</summary>
            EQUIL
        }

        /// <summary>File modes</summary>
        public enum FileModeType
        {
            SCRATCH_FILE,
            SAVED_FILE,
            USED_FILE
        }

        /// <summary>Flow units</summary>
        public enum FlowUnitsType
        {
            /// <summary>cubic feet per second</summary>
            CFS = 0,

            /// <summary>gallons per minute</summary>
            GPM = 1,

            /// <summary>million gallons per day</summary>
            MGD = 2,

            /// <summary>imperial million gal. per day</summary>
            IMGD = 3,

            /// <summary>acre-feet per day</summary>
            AFD = 4,

            /// <summary>liters per second</summary>
            LPS = 5,

            /// <summary>liters per minute</summary>
            LPM = 6,

            /// <summary>megaliters per day</summary>
            MLD = 7,

            /// <summary>cubic meters per hour</summary>
            CMH = 8,

            /// <summary>cubic meters per day</summary>
            CMD = 9,
        }


        /// <summary>Hydraulic variables</summary>
        public enum HydVarType
        {
            /// <summary>link diameter</summary>
            DIAMETER = 1,

            /// <summary>link flow rate</summary>
            FLOW = 2,

            /// <summary>link flow velocity</summary>
            VELOCITY = 3,

            /// <summary>Reynolds number</summary>
            REYNOLDS = 4,

            /// <summary>link shear velocity</summary>
            SHEAR = 5,

            /// <summary>friction factor</summary>
            FRICTION = 6,

            /// <summary>area/volume</summary>
            AREAVOL = 7,

            /// <summary>roughness</summary>
            ROUGHNESS = 8,
            MAX_HYD_VARS = 9
        }

        public enum MSXConstants
        {
            MSX_NODE = 0,
            MSX_LINK = 1,
            MSX_TANK = 2,
            MSX_SPECIES = 3,
            MSX_TERM = 4,
            MSX_PARAMETER = 5,
            MSX_CONSTANT = 6,
            MSX_PATTERN = 7,
            MSX_BULK = 0,
            MSX_WALL = 1,
            MSX_NOSOURCE = -1,
            MSX_CONCEN = 0,
            MSX_MASS = 1,
            MSX_SETPOINT = 2,
            MSX_FLOWPACED = 3,
        }

        /// <summary>Concentration mass units</summary>
        public enum MassUnitsType
        {
            /// <summary>milligram</summary>
            MG,

            /// <summary>microgram</summary>
            UG,

            /// <summary>mole</summary>
            MOLE,

            /// <summary>millimole</summary>
            MMOLE
        }

        /// <summary>Tank mixing regimes</summary>
        public enum MixType
        {
            /// <summary>1-compartment model</summary>
            MIX1 = 0,

            /// <summary>2-compartment model</summary>
            MIX2 = 1,

            /// <summary>First in, first out model</summary>
            FIFO = 2,

            /// <summary>Last in, first out model</summary>
            LIFO = 3,
        }

        /// <summary>Object types</summary>
        public enum ObjectTypes
        {
            NODE = 0,
            LINK = 1,
            TANK = 2,
            SPECIES = 3,
            TERM = 4,
            PARAMETER = 5,
            CONSTANT = 6,
            PATTERN = 7,
            MAX_OBJECTS = 8
        }

        /// <summary>Analysis options</summary>
        public enum OptionType
        {
            AREA_UNITS_OPTION,
            RATE_UNITS_OPTION,
            SOLVER_OPTION,
            COUPLING_OPTION,
            TIMESTEP_OPTION,
            RTOL_OPTION,
            ATOL_OPTION
        }

        /// <summary>Reaction rate time units</summary>
        public enum RateUnitsType
        {
            /// <summary>seconds</summary>
            SECONDS = 0,

            /// <summary>minutes</summary>
            MINUTES = 1,

            /// <summary>hours</summary>
            HOURS = 2,

            /// <summary>days</summary>
            DAYS = 3,
        }

        /// <summary>Input data file sections</summary>
        public enum SectionType
        {
            s_TITLE = 0,
            s_SPECIES = 1,
            s_COEFF = 2,
            s_TERM = 3,
            s_PIPE = 4,
            s_TANK = 5,
            s_SOURCE = 6,
            s_QUALITY = 7,
            s_PARAMETER = 8,
            s_PATTERN = 9,
            s_OPTION = 10,
            s_REPORT = 11,
        }

        /// <summary>ODE solver options</summary>
        public enum SolverType
        {
            /// <summary>Euler</summary>
            EUL,

            /// <summary>5th order Runge-Kutta</summary>
            RK5,

            /// <summary>2nd order Rosenbrock</summary>
            ROS2
        }

        /// <summary>Type of source quality input</summary>
        public enum SourceType
        {
            /// <summary>inflow concentration</summary>
            CONCEN,

            /// <summary>mass inflow booster</summary>
            MASS,

            /// <summary>setpoint booster</summary>
            SETPOINT,

            /// <summary>flow paced booster</summary>
            FLOWPACED
        }

        /// <summary>Types of water quality species</summary>
        public enum SpeciesType
        {
            /// <summary>bulk flow species</summary>
            BULK,

            /// <summary>pipe wall attached species</summary>
            WALL
        }

        /// <summary>Time series statistics</summary>
        public enum TstatType
        {
            /// <summary>full time series</summary>
            SERIES,

            /// <summary>time-averages</summary>
            AVGERAGE,

            /// <summary>minimum values</summary>
            MINIMUM,

            /// <summary>maximum values</summary>
            MAXIMUM,

            /// <summary>max - min values</summary>
            RANGE
        }

        /// <summary>Unit system</summary>
        public enum UnitSystemType
        {
            /// <summary>US</summary>
            US,

            /// <summary>SI (metric)</summary>
            SI
        }

        /// <summary>Measurement unit types</summary>
        public enum UnitsType
        {
            /// <summary>length</summary>
            LENGTH_UNITS = 0,

            /// <summary>pipe diameter</summary>
            DIAM_UNITS = 1,

            /// <summary>surface area</summary>
            AREA_UNITS = 2,

            /// <summary>volume</summary>
            VOL_UNITS = 3,

            /// <summary>flow</summary>
            FLOW_UNITS = 4,

            /// <summary>concentration volume</summary>
            CONC_UNITS = 5,

            /// <summary>reaction rate time units</summary>
            RATE_UNITS = 6,
            MAX_UNIT_TYPES = 7,
        }
    }
}