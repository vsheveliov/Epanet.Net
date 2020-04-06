using System;

using Epanet.Enums;

namespace Epanet.Network.Structures {

    public class Pipe :Link{
        public Pipe(string name):base(name) {}

        public override LinkType LinkType => LinkType.PIPE;

        public bool HasCheckValve { get; set; }

        public override void InitResistance(FormType formflag, double hexp) {
            FlowResistance = Constants.CSMALL;
            double d = Diameter;

            switch (formflag) {
                case FormType.HW:
                    FlowResistance = 4.727 * Lenght / Math.Pow(Kc, hexp) / Math.Pow(d, 4.871);
                    break;
                case FormType.DW:
                    FlowResistance = Lenght / 2.0 / 32.2 / d / Math.Pow(Math.PI * Math.Pow(d, 2) / 4.0, 2);
                    break;
                case FormType.CM:
                    FlowResistance = Math.Pow(4.0 * Kc / (1.49 * Math.PI * d * d), 2)
                                     * Math.Pow(d / 4.0, -1.333) * Lenght;
                    break;
            }
        }

        public override void ConvertUnits(Network nw) {
            FieldsMap fMap = nw.FieldsMap;

            if (nw.FormFlag == FormType.DW)
                Kc /= 1000.0 * fMap.GetUnits(FieldType.ELEV);

            Diameter /= fMap.GetUnits(FieldType.DIAM);
            Lenght /= fMap.GetUnits(FieldType.LENGTH);

            Km = 0.02517 * Km / Math.Pow(Diameter, 2) / Math.Pow(Diameter, 2);

            Kb /= Constants.SECperDAY;
            Kw /= Constants.SECperDAY;
        }


    }
}
