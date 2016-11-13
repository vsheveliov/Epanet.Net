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

namespace org.addition.epanet.hydraulic.io {


///<summary>Hydraulic binary file reader class.</summary>
public class HydraulicReader : IEnumerable<AwareStep> {

    private AwareStep.HeaderInfo headerInfo;


    ///<summary>Current hydraulic step snapshot.</summary>
    private AwareStep curStep;

    ///<summary>File input stream.</summary>
    private BinaryReader inputStream;


    public HydraulicReader(BinaryReader inputStream) {
        this.inputStream = inputStream;
        headerInfo = AwareStep.readHeader(inputStream);
    }

    public HydraulicReader(string hydFile)
        : this(new BinaryReader(File.OpenRead(hydFile))) {
    }

    /**
     * Read step data from file with a given time instant — it assumes the requested timestep is the same or after the current one.
     *
     * @param time Step instant.
     * @return Reference to step snapshot.
     * @throws IOException
     */
    public AwareStep getStep(long time) {
        if (curStep != null) {
            if (curStep.getTime() == time) return curStep;
        }
        while (curStep==null || curStep.getTime() < time)
            curStep = new AwareStep(inputStream, headerInfo);
        return curStep.getTime() >= time ? curStep : null;

    }

    /**
     * Close the inputStream.
     *
     * @throws IOException
     */
    public void close() {
        if (inputStream != null)
            ((IDisposable)inputStream).Dispose();
    }


    /**
     * Get the epanet hydraulic file version.
     *
     * @return Version number.
     */
    public int getVersion() {
        return headerInfo.version;
    }

    /**
     * Get the number of nodes in the file.
     *
     * @return Number of nodes.
     */
    public int getNodes() {
        return headerInfo.nodes;
    }

    /**
     * Get the number of links in the file.
     *
     * @return Number of links.
     */
    public int getLinks() {
        return headerInfo.links;
    }

    /**
     *
     * @return
     */
    public long getReportStart() {
        return headerInfo.rstart;
    }

    /**
     *
     * @return
     */
    public long getReportStep() {
        return headerInfo.rstep;
    }

    /**
     *
     * @return
     */
    public long getDuration() {
        return headerInfo.duration;
    }

    /**
     * Get step snapshot iterator.
     *
     * @return StepSnapshot iterator.
     */
    public IEnumerator<AwareStep> GetEnumerator() {
        return Steps.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }

    ///<summary>Step snapshot iterator class</summary>
    private IEnumerable<AwareStep> Steps {
        get {
            if(this.inputStream == null)
                throw new ObjectDisposedException(this.GetType().FullName);

            lock(this.inputStream) {

                this.inputStream.BaseStream.Position = sizeof(int) * 6;
                AwareStep stp;

                do {
                    try {
                        stp = new AwareStep(this.inputStream, this.headerInfo);
                    }
                    catch(IOException e) {
                        throw new SystemException(e.Message);
                    }

                    // if (stp == null) yield break;
                    yield return stp;

                } while(stp.getStep() != 0);
            }
        }
    }

    
}
}