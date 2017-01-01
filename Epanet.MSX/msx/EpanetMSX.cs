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
        private readonly ENToolkit2 toolkit;
        private readonly Output output;

        [NonSerialized]
        private bool running;

        [NonSerialized]
        private Thread runningThread;

        public Network Network { get { return this.network; } }

        public int NPeriods { get { return this.network.Nperiods; } }

        public long ResultsOffset { get { return this.output.ResultsOffset; } }

        public long QTime { get { return this.network.Qtime; } }

        public string[] GetSpeciesNames() {
            Species[] spe = this.network.Species;
            string[] ret = new string[spe.Length - 1];
            for (int i = 1; i < spe.Length; i++) {
                ret[i - 1] = spe[i].Id;
            }
            return ret;
        }

        public EpanetMSX(ENToolkit2 toolkit) {
            this.reader = new InpReader();
            this.project = new Project();
            this.network = new Network();
            this.report = new Report();
            this.tankMix = new TankMix();

            this.chemical = new Chemical();
            this.quality = new Quality();
            this.toolkit = toolkit;
            this.output = new Output();

            this.reader.LoadDependencies(this);
            this.project.LoadDependencies(this);
            this.report.LoadDependencies(this);
            this.tankMix.LoadDependencies(this);

            this.chemical.LoadDependencies(this);
            this.quality.LoadDependencies(this);
            this.output.LoadDependencies(this);
        }

        public InpReader Reader { get { return this.reader; } }

        public Project Project { get { return this.project; } }

        public Report Report { get { return this.report; } }

        public TankMix TankMix { get { return this.tankMix; } }

        public Chemical Chemical { get { return this.chemical; } }

        public Quality Quality { get { return this.quality; } }

        public ENToolkit2 EnToolkit { get { return this.toolkit; } }

        public Output Output { get { return this.output; } }

        public ErrorCodeType Load(string msxFile) {
            ErrorCodeType err = 0;
            err = Utilities.Call(err, this.project.MSXproj_open(msxFile));
            err = Utilities.Call(err, this.quality.MSXqual_open());
            return err;
        }


        public ErrorCodeType Run(string outFile) {
            ErrorCodeType err = 0;
            bool halted = false;
            if (this.running) throw new InvalidOperationException("Already running");

            this.runningThread = Thread.CurrentThread;
            this.running = true;
            try {
                this.quality.MSXqual_init();

                this.output.MSXout_open(outFile);

                long oldHour = -1, newHour = 0;

                long[] tTemp = new long[1];
                long[] tLeft = new long[1];
                do {
                    if (oldHour != newHour) {
                        //writeCon(string.format("\r  o Computing water quality at hour %-4d", newHour));
                        oldHour = newHour;
                    }
                    err = this.quality.MSXqual_step(tTemp, tLeft);
                    newHour = tTemp[0] / 3600;
                    if (!this.running && tLeft[0] > 0)
                        halted = true;
                }
                while (this.running && err == 0 && tLeft[0] > 0);


            }
            finally {
                this.running = false;
                this.runningThread = null;
            }

            if (halted)
                throw new ENException(ErrorCode.Err1000);

            return err;
        }


        private void WriteCon(string str) { Console.Out.Write(str); }

        public void StopRunning() {
            this.running = false;
            if (this.runningThread != null && this.runningThread.IsAlive)
                this.runningThread.Join(1000);
        }
    }

}