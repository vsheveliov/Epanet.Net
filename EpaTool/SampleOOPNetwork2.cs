/*
    Copyright (C) 2015 Addition, Lda. (addition at addition dot pt) *
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version. *
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details. *
    You should have received a copy of the GNU General Public License
    along with this program. If not, see http://www.gnu.org/licenses/. 
 */

using System;
using System.Diagnostics;
using System.IO;

using Epanet.Enums;
using Epanet.Hydraulic;
using Epanet.Hydraulic.IO;
using Epanet.Network;
using Epanet.Network.IO.Input;
using Epanet.Network.Structures;
using Epanet.Util;

namespace Epanet {


    public class SampleOOPNetwork2 {
        public static void main(string[] args) {
            int i;

            Network.Network network = new Network.Network();

            //Tank
            Tank tank = new Tank("0") {Elevation = 210};
            network.Nodes.Add(tank);

            //Nodes
            Node[] node = new Node[7];

            for (i = 1; i < 7; i++)
                node[i] = new Node(i.ToString());

            node[1].Elevation = 150;
            node[1].InitDemand = 100;
            node[2].Elevation = 160;
            node[2].InitDemand = 100;
            node[3].Elevation = 155;
            node[3].InitDemand = 120;
            node[4].Elevation = 150;
            node[4].InitDemand = 270;
            node[5].Elevation = 165;
            node[5].InitDemand = 330;
            node[6].Elevation = 160;
            node[6].InitDemand = 200;

            //Links
            Link[] pipe = new Link[8];
            for (i = 0; i < 8; i++) {
                pipe[i] = new Link(i.ToString()) {Lenght = 1000};
            }
            pipe[0].FirstNode = tank;
            pipe[0].SecondNode = node[1];
            pipe[1].FirstNode = node[1];
            pipe[1].SecondNode = node[2];
            pipe[2].FirstNode = node[1];
            pipe[2].SecondNode = node[3];
            pipe[3].FirstNode = node[3];
            pipe[3].SecondNode = node[4];
            pipe[4].FirstNode = node[3];
            pipe[4].SecondNode = node[5];
            pipe[5].FirstNode = node[5];
            pipe[5].SecondNode = node[6];
            pipe[6].FirstNode = node[2];
            pipe[6].SecondNode = node[4];
            pipe[7].FirstNode = node[4];
            pipe[7].SecondNode = node[6];

            for (i = 1; i < 7; i++) { network.Nodes.Add(node[i]); }
            for (i = 0; i < 8; i++) { network.Links.Add(pipe[i]); }

            //Prepare Network
            TraceSource log = new TraceSource(typeof(SampleOOPNetwork2).FullName, SourceLevels.All);
            NullParser nP = (NullParser)InputParser.Create(FileType.NULL_FILE, log);
            Debug.Assert(nP != null);
            nP.Parse(network, null);

            //// Simulate hydraulics
            string hydFile = Path.GetTempFileName(); // ("hydSim", "bin");
            HydraulicSim hydSim = new HydraulicSim(network, log);
            hydSim.Simulate(hydFile);

            // Read hydraulic results
            PropertiesMap pMap = network.PropertiesMap;
            HydraulicReader hydReader = new HydraulicReader(hydFile);

            for (long time = pMap.RStart; time <= pMap.Duration; time += pMap.RStep) {
                AwareStep step = hydReader.GetStep((int)time);
                Console.WriteLine("Time : " + step.Time.GetClockTime() + ", nodes heads : ");

                i = 0;
                foreach (Node inode in network.Nodes)
                    Console.Write("{0:F2}\t", step.GetNodeHead(i++, inode, null));

                Console.WriteLine();
            }

            hydReader.Close();
        }

    }

}
