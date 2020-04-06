using Epanet.Enums;

namespace Epanet.Network.Structures {

    public class Reservoir:Node {
        public Reservoir(string name):base(name) { }

        public override void ConvertUnits(Network nw) {
            // FIXME:Tanks and reservoirs here?

            FieldsMap fMap = nw.FieldsMap;

            // ... convert from user to internal units
            double ucfLength = fMap.GetUnits(FieldType.ELEV);
            Elevation /= ucfLength;
            H0 = Elevation + H0 / ucfLength;
            Hmin = Elevation + Hmin / ucfLength;
            Hmax = Elevation + Hmax / ucfLength;
            V0 /= fMap.GetUnits(FieldType.VOLUME);
            Vmin /= fMap.GetUnits(FieldType.VOLUME);
            Vmax /= fMap.GetUnits(FieldType.VOLUME);
            Kb /= Constants.SECperDAY;
            // tk.Volume = tk.V0;
            C = C0;
            V1Max *= Vmax;
        }

        public override NodeType NodeType => NodeType.RESERV;

        ///<summary>Species concentration.</summary>
        public double C { get; set; }

        ///<summary>Initial water elev.</summary>
        public double H0 { get; set; }

        ///<summary>Maximum water elev (feet).</summary>
        public double Hmax { get; set; }

        ///<summary>Minimum water elev (feet).</summary>
        public double Hmin { get; set; }

        ///<summary>Reaction coeff. (1/days).</summary>
        public double Kb { get; set; }

        ///<summary>Type of mixing model</summary>
        public MixType MixModel { get; set; }

        ///<summary>Fixed grade time pattern.</summary>
        public Pattern Pattern { get; set; }

        ///<summary>Initial volume (feet^3).</summary>
        public double V0 { get; set; }

        ///<summary>Mixing compartment size</summary>
        public double V1Max { get; set; }

        ///<summary>Fixed grade time pattern</summary>
        public Curve Vcurve { get; set; }

        ///<summary>Maximum volume (feet^3).</summary>
        public double Vmax { get; set; }

        ///<summary>Minimum volume (feet^3).</summary>
        public double Vmin { get; set; }

#if NUCONVERT

        public double GetNuInitHead(UnitsType type) { return NUConvert.RevertDistance(type, H0); }

        public double GetNuInitVolume(UnitsType type) { return NUConvert.RevertVolume(type, V0); }

        public double GetNuMaximumHead(UnitsType type)
        {
            return NUConvert.RevertDistance(type, Hmax);
        }

        public double GetNuMaxVolume(UnitsType type) { return NUConvert.RevertVolume(type, Vmax); }

        public double GetNuMinimumHead(UnitsType type)
        {
            return NUConvert.RevertDistance(type, Hmin);
        }

        public double GetNuMinVolume(UnitsType type) { return NUConvert.RevertVolume(type, Vmin); }

        public void SetNuMinVolume(UnitsType type, double value)
        {
            Vmin = NUConvert.ConvertVolume(type, value);
        }


        public double GetNuMixCompartimentSize(UnitsType type)
        {
            return NUConvert.RevertVolume(type, V1Max);
        }


        public void SetNuInitHead(UnitsType type, double value)
        {
            H0 = NUConvert.RevertDistance(type, value);
        }

        public void SetNuInitVolume(UnitsType type, double value)
        {
            V0 = NUConvert.ConvertVolume(type, value);
        }


        public void SetNuMaximumHead(UnitsType type, double value)
        {
            Hmax = NUConvert.RevertDistance(type, value);
        }

        public void SetNuMaxVolume(UnitsType type, double value)
        {
            Vmax = NUConvert.ConvertVolume(type, value);
        }

        public void SetNuMinimumHead(UnitsType type, double value)
        {
            Hmin = NUConvert.ConvertArea(type, value);
        }

        public void SetNuMixCompartimentSize(UnitsType type, double value)
        {
            V1Max = NUConvert.ConvertVolume(type, value);
        }

#endif

    }

}
