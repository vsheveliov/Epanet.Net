using System;

using Epanet.Enums;

namespace Epanet.Network.Structures {

    public class Junction : Node {
        public Junction(string name):base(name) {}

        public override void ConvertUnits(Network nw) {
            FieldsMap fMap = nw.FieldsMap;

            // ... convert elevation & initial quality units
            Elevation /= fMap.GetUnits(FieldType.ELEV);
            C0 /= fMap.GetUnits(FieldType.QUALITY);

            // ... if no demand categories exist, add primary demand to list
            if (Demands.Count == 0) { Demands.Add(PrimaryDemand); }

            // ... convert flow units for base demand in each demand category
            double qcf = fMap.GetUnits(FieldType.DEMAND);

            foreach(Demand d in Demands) {
                d.Base /= qcf;
            }

            // ... convert emitter flow units
            if (Ke > 0.0) {
                double ucf = Math.Pow(fMap.GetUnits(FieldType.FLOW), nw.QExp) / fMap.GetUnits(FieldType.PRESSURE);

                Ke = ucf / Math.Pow(Ke, nw.QExp);
            }

        }

    }
}
