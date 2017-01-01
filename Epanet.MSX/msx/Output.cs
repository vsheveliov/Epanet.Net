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

using System.IO;

namespace Epanet.MSX {

    public class Output {

        public void LoadDependencies(EpanetMSX epa) {
            this.msx = epa.Network;
            this.quality = epa.Quality;
        }

        ///<summary>MSX project data</summary>
        private Network msx;

        ///<summary>Offset byte where results begin</summary>
        public long ResultsOffset;
        ///<summary>Bytes per time period used by all nodes</summary>
        private long nodeBytesPerPeriod;
        ///<summary>Bytes per time period used by all links</summary>
        private static long _linkBytesPerPeriod;
        private Quality quality;

        private BinaryWriter outStream;



        /// <summary>Opens an MSX binary output file.</summary>
        public int MSXout_open(string output) {
            var stream = File.OpenWrite(output);
            this.outStream = new BinaryWriter(stream);

            // write initial results to file
            this.msx.Nperiods = 0;
            this.MSXout_saveInitialResults(stream);
            return 0;
        }

        // Saves general information to beginning of MSX binary output file.
        private int MSXout_saveInitialResults(Stream output) {
            //MSX.OutFile.close();
            //MSX.OutFile.openAsBinaryWritter(); // rewind

            //DataOutputStream dout = (DataOutputStream)MSX.OutFile.getFileIO();

            //try {
            //    outStream.writeInt(Constants.MAGICNUMBER);               //Magic number
            //    outStream.writeInt(Constants.VERSION);                   //Version number
            //    outStream.writeInt(MSX.Nobjects[ObjectTypes.NODE.id]);   //Number of nodes
            //    outStream.writeInt(MSX.Nobjects[ObjectTypes.LINK.id]);   //Number of links
            //    outStream.writeInt(MSX.Nobjects[ObjectTypes.SPECIES.id]);//Number of species
            //    outStream.writeInt((int)MSX.Rstep);                      //Reporting step size
            //
            //    for (int m=1; m<=MSX.Nobjects[ObjectTypes.SPECIES.id]; m++){
            //        int n = MSX.Species[m].getId().length();
            //        outStream.writeInt(n);                               //Length of species ID
            //        writeString(outStream,MSX.Species[m].getId(),n);     //Species ID string
            //    }
            //
            //    for (int m=1; m<=MSX.Nobjects[ObjectTypes.SPECIES.id]; m++){
            //        writeString(outStream,MSX.Species[m].getUnits(),Constants.MAXUNITS); //Species mass units
            //    }
            //
            //} catch (IOException e) {
            //    return 0;
            //}

            //outStream.close();
            this.ResultsOffset = 0; // output.length();
            this.outStream = new BinaryWriter(output);


            this.nodeBytesPerPeriod = this.msx.Nobjects[(int)ObjectTypes.NODE]
                                      * this.msx.Nobjects[(int)ObjectTypes.SPECIES] * 4;
            _linkBytesPerPeriod = this.msx.Nobjects[(int)ObjectTypes.LINK]
                                  * this.msx.Nobjects[(int)ObjectTypes.SPECIES] * 4;

            return 0;
        }



        /// <summary>
        /// Saves computed species concentrations for each node and link at the
        /// current time period to the temporary MSX binary output file (which
        /// will be the same as the permanent MSX binary file if time series
        /// values were specified as the reported statistic, which is the
        /// default case).
        /// </summary>
        public ErrorCodeType MSXout_saveResults() {

            double x;
            //DataOutputStream dout = (DataOutputStream)MSX.TmpOutFile.getFileIO();
            for (int i = 1; i <= this.msx.Nobjects[(int)ObjectTypes.SPECIES]; i++) {
                for (int j = 1; j <= this.msx.Nobjects[(int)ObjectTypes.NODE]; j++) {
                    x = this.quality.MSXqual_getNodeQual(j, i);
                    //if(j==462){
                    //    System.out.println("462 : " + x);
                    //}
                    //if(j==79){
                    //    System.out.println("79 : " + x);
                    //}
                    try {
                        this.outStream.Write((float)x); //fwrite(&x, sizeof(REAL4), 1, MSX.TmpOutFile.file);
                    }
                    catch (IOException) {}
                }
            }
            for (int i = 1; i <= this.msx.Nobjects[(int)ObjectTypes.SPECIES]; i++) {
                for (int j = 1; j <= this.msx.Nobjects[(int)ObjectTypes.LINK]; j++) {
                    x = this.quality.MSXqual_getLinkQual(j, i);
                    try {
                        this.outStream.Write((float)x); //fwrite(&x, sizeof(REAL4), 1, MSX.TmpOutFile.file);
                    }
                    catch (IOException) {}
                }
            }
            return 0;
        }

        /// <summary>
        /// Saves any statistical results plus the following information to the end of the MSX binary output file:
        /// <list type="bullet">
        ///     <item>byte offset into file where WQ results for each time period begins,</item>
        ///     <item>total number of time periods written to the file,</item>
        ///     <item>any error code generated by the analysis (0 if there were no errors),</item>
        ///     <item>the Magic Number to indicate that the file is complete.</item> 
        /// </list>
        /// </summary>
        public ErrorCodeType MSXout_saveFinalResults() {
            int magic = Constants.MAGICNUMBER;
            ErrorCodeType err = 0;

            // Save statistical results to the file
            //if ( MSX.Statflag != TstatType.SERIES )
            //    err = saveStatResults(out);

            if (err > 0)
                return err;

            // Write closing records to the file
            try {
                this.outStream.Write((int)this.ResultsOffset);
                this.outStream.Write((int)this.msx.Nperiods);
                this.outStream.Write((int)this.msx.ErrCode);
                this.outStream.Write((int)magic);
            }
            catch (IOException) {}
            return 0;
        }

        /// <summary>Retrieves a result for a specific node from the MSX binary output file.</summary>
        public float MSXout_getNodeQual(BinaryReader raf, int k, int j, int m) {
            float c = 0.0f;
            long bp = this.ResultsOffset + k * (this.nodeBytesPerPeriod + _linkBytesPerPeriod);
            bp += ((m - 1) * this.msx.Nobjects[(int)ObjectTypes.NODE] + (j - 1)) * 4;

            try {
                raf.BaseStream.Seek(bp, SeekOrigin.Begin);
                c = raf.ReadSingle();
            }
            catch (IOException) {}

            return c;
        }

        /// <summary>Retrieves a result for a specific link from the MSX binary output file.</summary>
        public float MSXout_getLinkQual(BinaryReader raf, int k, int j, int m) {
            float c = 0.0f;
            long bp = this.ResultsOffset + ((k + 1) * this.nodeBytesPerPeriod) + (k * _linkBytesPerPeriod);
            bp += ((m - 1) * this.msx.Nobjects[(int)ObjectTypes.LINK] + (j - 1)) * 4;

            try {
                raf.BaseStream.Position = bp;
                c = raf.ReadSingle();
            }
            catch (IOException) {}
            return c;
        }
    }
}