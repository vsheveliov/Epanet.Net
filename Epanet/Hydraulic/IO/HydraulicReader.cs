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

        private readonly AwareStep.HeaderInfo _headerInfo;


        ///<summary>Current hydraulic step snapshot.</summary>
        private AwareStep _curStep;

        ///<summary>File input stream.</summary>
        private BinaryReader _inputStream;


        public HydraulicReader(BinaryReader reader) {
            _inputStream = reader;
            _headerInfo = AwareStep.ReadHeader(reader);
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
            if (_curStep != null) {
                if (_curStep.Time == time) return _curStep;
            }
            
            while (_curStep == null || _curStep.Time < time)
                _curStep = new AwareStep(_inputStream, _headerInfo);

            return _curStep.Time >= time ? _curStep : null;

        }

        /// <summary>Close the inputStream.</summary>
        public void Close() {
            if (_inputStream != null) {
                ((IDisposable)_inputStream).Dispose();
                _inputStream = null;
            }
        }

        /// <summary>Get the epanet hydraulic file version.</summary>
        /// <value>Version number.</value>
        public int Version { get { return _headerInfo.version; } }

        /// <summary>Get the number of nodes in the file.</summary>
        /// <value>Number of nodes.</value>
        public int Nodes { get { return _headerInfo.nodes; } }

        /// <summary>Get the number of links in the file.</summary>
        /// <value>Number of links.</value>
        public int Links { get { return _headerInfo.links; } }

        public long ReportStart { get { return _headerInfo.rstart; } }

        public long ReportStep { get { return _headerInfo.rstep; } }

        public long Duration { get { return _headerInfo.duration; } }

        ///<summary>Get step snapshot iterator.</summary>
        /// <returns>StepSnapshot iterator.</returns>
        public IEnumerator<AwareStep> GetEnumerator() { return Steps.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        ///<summary>Step snapshot iterator class</summary>
        private IEnumerable<AwareStep> Steps {
            get {
                if (_inputStream == null)
                    throw new ObjectDisposedException(GetType().FullName);

                lock (_inputStream) {

                    _inputStream.BaseStream.Position = sizeof(int) * 6;
                    AwareStep stp;

                    do {
                        try {
                            stp = new AwareStep(_inputStream, _headerInfo);
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

        public void Dispose() { Close(); }
    }

}