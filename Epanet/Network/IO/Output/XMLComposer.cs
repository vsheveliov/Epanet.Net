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

using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Xml.Serialization;
using org.addition.epanet.network.structures;
using org.addition.epanet.util;

namespace org.addition.epanet.network.io.output {

public class XMLComposer : OutputComposer {
    private readonly bool gzip;

    public XMLComposer(bool gzip) {
        this.gzip = gzip;
    }

    public override void Composer(Network net, string f) {
        try {

            var fs = new FileStream(
                f,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read,
                0x1000,
                FileOptions.SequentialScan);

            using (fs)
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("", "");

                Stream stream = this.gzip ? (Stream)new GZipStream(fs, CompressionMode.Compress) : fs;
                //var x = new XmlSerializer(typeof(Network), new Type[] {});
                var x = new XmlSerializer(typeof(Network), new[] {
                    //typeof (Control),
                    //typeof (Curve),
                    //typeof (Demand),
                    //typeof (EnPoint),
                    //typeof (Field),
                    typeof (Label),
                    typeof (Link),
                    typeof (Node),
                    //typeof (Pattern),
                    //typeof (Pump),
                    //typeof (Rule),
                    //typeof (Source),
                    //typeof (Tank),
                    //typeof (Valve)
                });

                x.Serialize(stream, net); // TODO: Check, wether we can use Stream here
            }
    
        } catch (IOException e) {
            Debug.Print(e.ToString());
            throw new ENException(ErrorCode.Err308);
        }
    }
}
}