namespace Epanet.Enums {

    ///<summary>Rule variables.</summary>
    public enum Varwords {
        CLOCKTIME,
        DEMAND,
        DRAINTIME,
        FILLTIME,
        FLOW,
        GRADE,
        HEAD,
        LEVEL,
        POWER,
        PRESSURE,
        SETTING,
        STATUS,
        TIME
    }

    ///<summary>Rule values types.</summary>
    public enum Values {
        IS_NUMBER = 0,
        IS_OPEN = 1,
        IS_CLOSED = 2,
        IS_ACTIVE = 3

    }

    /// <summary>Unit system.</summary>
    public enum UnitsType {
        /// <summary>SI (metric)</summary>
        SI,
        /// <summary>US</summary>
        US
    }

    /// <summary>Time series statistics.</summary>
    public enum 
        TStatType {
        /// <summary>none</summary>
        SERIES,
        /// <summary>time-averages</summary>
        AVG,
        /// <summary>minimum values</summary>
        MIN,
        /// <summary>maximum values</summary>
        MAX,
        /// <summary>max - min values</summary>
        RANGE
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

    /// <summary>Status report options.</summary>
    public enum StatFlag {
        NO = 0,
        YES = 1,
        FULL = 2
    }

    ///<summary>Source type</summary>
    public enum SourceType {
        ///<summary>Inflow concentration.</summary>
        CONCEN = 0,

        ///<summary>Mass inflow booster.</summary>
        MASS = 1,

        ///<summary>Setpoint booster.</summary>
        SETPOINT = 2,

        ///<summary>Flow paced booster.</summary>
        FLOWPACED = 3,
    }

    ///<summary>Available section types.</summary>
    public enum SectType {
        TITLE = 0,
        JUNCTIONS = 1,
        RESERVOIRS = 2,
        TANKS = 3,
        PIPES = 4,
        PUMPS = 5,
        VALVES = 6,
        CONTROLS = 7,
        RULES = 8,
        DEMANDS = 9,
        SOURCES = 10,
        EMITTERS = 11,
        PATTERNS = 12,
        CURVES = 13,
        QUALITY = 14,
        STATUS = 15,
        ROUGHNESS = 16,
        ENERGY = 17,
        REACTIONS = 18,
        MIXING = 19,
        REPORT = 20,
        TIMES = 21,
        OPTIONS = 22,
        COORDINATES = 23,
        VERTICES = 24,
        LABELS = 25,
        BACKDROP = 26,
        TAGS = 27,
        END = 28,
    }

    ///<summary>Rule statements.</summary>
    public enum Rulewords {
        AND,
        ELSE,
        ERROR,
        IF,
        OR,
        PRIORITY,
        RULE,
        THEN
    }

    /// <summary>Reporting flag.</summary>
    public enum ReportFlag {
        FALSE = 0,
        SOME = 2,
        TRUE = 1
    }

    ///<summary>Range limits.</summary>
    public enum RangeType {
        ///<summary>lower limit</summary>
        LOW = 0,

        ///<summary>upper limit</summary>
        HI = 1,

        ///<summary>precision</summary>
        PREC = 2
    }

    /// <summary>Water quality analysis option.</summary>
    public enum QualType {
        /// <summary>No quality analysis.</summary>
        NONE = 0,
        /// <summary>Analyze a chemical.</summary>
        CHEM = 1,
        /// <summary>Analyze water age.</summary>
        AGE = 2,
        /// <summary>Trace % of flow from a source</summary>
        TRACE = 3
    }

    ///<summary>Type of pump curve.</summary>
    public enum PumpType {
        ///<summary>Constant horsepower.</summary>
        CONST_HP = 0,
        ///<summary>Power function.</summary>
        POWER_FUNC = 1,
        ///<summary>User-defined custom curve.</summary>
        CUSTOM = 2,
        NOCURVE = 3
    }

    /// <summary>Pressure units.</summary>
    public enum PressUnitsType {
        /// <summary>pounds per square inch</summary>
        PSI,
        /// <summary>kiloPascals</summary>
        KPA,
        /// <summary>meters</summary>
        METERS
    }

    ///<summary>Rule operators.</summary>
    public enum Operators {
        ABOVE,
        BELOW,
        EQ,
        GE,
        GT,
        IS,
        LE,
        LT,
        NE,
        NOT
    }

    ///<summary>Rule object types.</summary>
    public enum Objects {
        JUNC,
        LINK,
        NODE,
        PIPE,
        PUMP,
        RESERV,
        SYSTEM,
        TANK,
        VALVE
    }

    /// <summary>Type of node.</summary>
    public enum NodeType {
        /// <summary>junction</summary>
        JUNC = 0,
        /// <summary>reservoir</summary>
        RESERV = 1,
        /// <summary>tank</summary>
        TANK = 2
    }

    /// <summary>Tank mixing regimes.</summary>
    public enum MixType {
        /// <summary>1-compartment model</summary>
        MIX1 = 0,
        /// <summary>2-compartment model</summary>
        MIX2 = 1,
        /// <summary>First in, first out model</summary>
        FIFO = 2,
        /// <summary> Last in, first out model</summary>
        LIFO = 3,
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

    /// <summary>Hydraulics solution option.</summary>
    public enum HydType {
        /// <summary>Use from previous run.</summary>
        USE,
        /// <summary>Save after current run.</summary>
        SAVE,
        /// <summary>Use temporary file.</summary>
        SCRATCH
    }

    /// <summary>Head loss formula.</summary>
    public enum FormType {
        /// <summary>Hazen-Williams</summary>
        HW,
        /// <summary>Darcy-Weisbach</summary>
        DW,
        /// <summary>Chezy-Manning</summary>
        CM,
    }

    /// <summary>Flow units.</summary>
    public enum FlowUnitsType {
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
        CMD = 9
    }

    ///<summary>Available files types.</summary>
    public enum FileType {
        NET_FILE,
        INP_FILE,
        NULL_FILE,
        XML_FILE,
        XML_GZ_FILE,
    }

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

    /// <summary>Type of curve</summary>
    public enum CurveType {
        /// <summary>volume curve</summary>
        Volume = 0,

        /// <summary>pump curve</summary>
        Pump = 1,

        /// <summary>efficiency curve</summary>
        Efficiency = 2,

        /// <summary>head loss curve</summary>
        HeadLoss = 3
    }

    ///<summary>Control condition type</summary>
    public enum ControlType {
        /// <summary>act when grade below set level</summary>
        LOWLEVEL = 0,
        /// <summary>act when grade above set level</summary>
        HILEVEL = 1,
        /// <summary>act when set time reached</summary>
        TIMER = 2,
        /// <summary>act when time of day occurs</summary>
        TIMEOFDAY = 3,
    }

}
