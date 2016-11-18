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
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Epanet.Hydraulic.IO {


    ///<summary>Hydraulic binary file reader class.</summary>
    public class HydraulicReader:IEnumerable<AwareStep>, IDisposable {

        private readonly AwareStep.HeaderInfo headerInfo;


        ///<summary>Current hydraulic step snapshot.</summary>
        private AwareStep curStep;

        ///<summary>File input stream.</summary>
        private BinaryReader inputStream;


        public HydraulicReader(BinaryReader reader) {
            this.inputStream = reader;
            this.headerInfo = AwareStep.ReadHeader(reader);
        }

        public HydraulicReader(string hydFile)
            :this(new BinaryReader(File.OpenRead(hydFile))) { }

        /// <summary>
        /// Read step data from file with a given time instant — 
        /// it assumes the requested timestep is the same or after the current one.
        /// </summary>
        /// <param name="time">Step instant.</param>
        /// <returns>Reference to step snapshot.</returns>
        public AwareStep GetStep(long time) {
            if (this.curStep != null) {
                if (this.curStep.Time == time) return this.curStep;
            }
            
            while (this.curStep == null || this.curStep.Time < time)
                this.curStep = new AwareStep(this.inputStream, this.headerInfo);

            return this.curStep.Time >= time ? this.curStep : null;

        }

        /// <summary>Close the inputStream.</summary>
        public void Close() {
            if (this.inputStream != null) {
                ((IDisposable)this.inputStream).Dispose();
                this.inputStream = null;
            }
        }

        /// <summary>Get the epanet hydraulic file version.</summary>
        /// <value>Version number.</value>
        public int Version { get { return this.headerInfo.Version; } }

        /// <summary>Get the number of nodes in the file.</summary>
        /// <value>Number of nodes.</value>
        public int Nodes { get { return this.headerInfo.Nodes; } }

        /// <summary>Get the number of links in the file.</summary>
        /// <value>Number of links.</value>
        public int Links { get { return this.headerInfo.Links; } }

        public long ReportStart { get { return this.headerInfo.Rstart; } }

        public long ReportStep { get { return this.headerInfo.Rstep; } }

        public long Duration { get { return this.headerInfo.Duration; } }

        ///<summary>Get step snapshot iterator.</summary>
        /// <returns>StepSnapshot iterator.</returns>
        public IEnumerator<AwareStep> GetEnumerator() { return this.Steps.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }

        ///<summary>Step snapshot iterator class</summary>
        private IEnumerable<AwareStep> Steps {
            get {
                if (this.inputStream == null)
                    throw new ObjectDisposedException(this.GetType().FullName);

                lock (this.inputStream) {

                    this.inputStream.BaseStream.Position = sizeof(int) * 6;
                    AwareStep stp;

                    do {
                        try {
                            stp = new AwareStep(this.inputStream, this.headerInfo);
                        }
                        catch (IOException e) {
                            throw new SystemException(e.Message);
                        }

                        // if (stp == null) yield break;
                        yield return stp;

                    }
                    while (stp.Step != 0);
                }
            }
        }

        public void Dispose() { this.Close(); }
    }

}