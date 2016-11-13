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

namespace org.addition.epanet.msx {

public class MsxReader {
    private readonly long nodeBytesPerPeriod;
    private readonly long linkBytesPerPeriod;
    private readonly long resultsOffset;
    private readonly int nNodes;
    private readonly int nLinks;

    BinaryReader ouputRaf;

    public MsxReader(int nodes, int links, int species, long resultsOffset) {
        this.nLinks = links;
        this.nNodes = nodes;
        this.resultsOffset = resultsOffset;
        nodeBytesPerPeriod = nNodes * species * 4;
        linkBytesPerPeriod = nLinks * species * 4;
    }

    public void open(string output) {
        ouputRaf = new BinaryReader(File.OpenRead(output));

    }

    public void close() {
        ouputRaf.Close();
    }
    public float getNodeQual(int period, int node, int specie)
    {
        float c=0.0f;
        long bp = resultsOffset + period * (nodeBytesPerPeriod + linkBytesPerPeriod);
        bp += ((specie-1)*nNodes + (node-1)) * 4;

        try {
            ouputRaf.BaseStream.Position = bp;
            c = ouputRaf.ReadSingle();
        } catch (IOException) {}

        return c;
    }

    // retrieves a result for a specific link from the MSX binary output file.
    public float getLinkQual(int period, int node, int specie)
    {
        float c=0.0f;
        long bp = resultsOffset + ((period+1)* nodeBytesPerPeriod) + (period* linkBytesPerPeriod);
        bp += ((specie-1)*nLinks + (node-1)) * 4;

        try {
            ouputRaf.BaseStream.Position = bp;
            c = ouputRaf.ReadSingle();
        } catch (IOException) {}

        return c;
    }
}
}