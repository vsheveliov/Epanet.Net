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
using System.Threading;
using Epanet.MSX.Structures;
using Epanet.Util;

namespace Epanet.MSX {

    public class EpanetMSX {
        private readonly InpReader reader;
        private readonly Project project;
        private readonly Network network;
        private readonly Report report;
        private readonly TankMix tankMix;

        private readonly Chemical chemical;
        private readonly Quality quality;
        private readonly EnToolkit2 toolkit;
        private readonly Output output;

        [NonSerialized]
        private bool running;

        [NonSerialized]
        private Thread runningThread;

        public Network Network { get { return network; } }

        public int NPeriods { get { return network.Nperiods; } }

        public long ResultsOffset { get { return output.ResultsOffset; } }

        public long QTime { get { return network.Qtime; } }

        public string[] GetSpeciesNames() {
            Species[] spe = network.Species;
            string[] ret = new string[spe.Length - 1];
            for (int i = 1; i < spe.Length; i++) {
                ret[i - 1] = spe[i].Id;
            }
            return ret;
        }

        public EpanetMSX(EnToolkit2 toolkit) {
            reader = new InpReader();
            project = new Project();
            network = new Network();
            report = new Report();
            tankMix = new TankMix();

            chemical = new Chemical();
            quality = new Quality();
            this.toolkit = toolkit;
            output = new Output();

            reader.LoadDependencies(this);
            project.LoadDependencies(this);
            report.LoadDependencies(this);
            tankMix.LoadDependencies(this);

            chemical.LoadDependencies(this);
            quality.LoadDependencies(this);
            output.LoadDependencies(this);
        }

        public InpReader Reader { get { return reader; } }

        public Project Project { get { return project; } }

        public Report Report { get { return report; } }

        public TankMix TankMix { get { return tankMix; } }

        public Chemical Chemical { get { return chemical; } }

        public Quality Quality { get { return quality; } }

        public EnToolkit2 EnToolkit { get { return toolkit; } }

        public Output Output { get { return output; } }

        public ErrorCodeType Load(string msxFile) {
            ErrorCodeType err = 0;
            err = Utilities.Call(err, project.MSXproj_open(msxFile));
            err = Utilities.Call(err, quality.MSXqual_open());
            return err;
        }


        public ErrorCodeType Run(string outFile) {
            ErrorCodeType err;
            bool halted = false;
            if (running) throw new InvalidOperationException("Already running");

            runningThread = Thread.CurrentThread;
            running = true;
            try {
                quality.MSXqual_init();

                output.MSXout_open(outFile);

                long oldHour = -1, newHour = 0;

                long[] tTemp = new long[1];
                long[] tLeft = new long[1];
                do {
                    if (oldHour != newHour) {
                        // Console.WriteLine("\r  o Computing water quality at hour {0,-1}", newHour);
                        oldHour = newHour;
                    }
                    err = quality.MSXqual_step(tTemp, tLeft);
                    newHour = tTemp[0] / 3600;
                    if (!running && tLeft[0] > 0)
                        halted = true;
                }
                while (running && err == 0 && tLeft[0] > 0);


            }
            finally {
                running = false;
                runningThread = null;
            }

            if (halted)
                throw new ENException(ErrorCode.Err1000);

            return err;
        }

        public void StopRunning() {
            running = false;
            if (runningThread != null && runningThread.IsAlive)
                runningThread.Join(1000);
        }
    }

}