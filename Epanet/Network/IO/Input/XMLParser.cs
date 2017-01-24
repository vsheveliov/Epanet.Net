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
using System.IO;
using System.IO.Compression;
using System.Xml;

using Epanet.Enums;
using Epanet.Network.Structures;
using Epanet.Util;

namespace Epanet.Network.IO.Input {

    public class XmlParser:InputParser {
        private SectType sectionType;
        private XmlReader reader;
        private Network net;
        private readonly bool gzipped;
        
        public XmlParser(bool gzipped) { this.gzipped = gzipped; }

        private void LogException(ErrorCode err, string line) {
            if(err == ErrorCode.Ok)
                return;

            var ex = new InputException(err, this.sectionType, line);

            base.Errors.Add(ex);

            base.LogException(ex);

        }

        public override Network Parse(Network net_, string f) {
            this.net = net_;
            this.FileName = Path.GetFullPath(f);

            XmlReaderSettings settings = new XmlReaderSettings {
                CloseInput = false,
                ValidationType = ValidationType.None,
                CheckCharacters = true,
                IgnoreComments = false,
                IgnoreWhitespace = true,
                
            };

            try {


                Stream stream = this.gzipped
                    ? (Stream)new GZipStream(File.OpenRead(f), CompressionMode.Decompress)
                    : File.OpenRead(f);

                using (stream) {

                    using (this.reader = XmlReader.Create(stream, settings)) {
                        this.reader.ReadToFollowing("network");
                        this.ParsePc(this.reader);
                    }

                    stream.Position = 0;

                    using (this.reader = XmlReader.Create(stream, settings)) {
                        this.reader.ReadToFollowing("network");
                        // this.reader.Read(); // skip "network"
                        // this.reader.MoveToContent(); // If that node is whitespace, skip to content.



                        while (this.reader.Read()) {
                            if (this.reader.NodeType != XmlNodeType.Element || this.reader.IsEmptyElement)
                                continue;

                            try {
                                this.sectionType = (SectType)Enum.Parse(typeof(SectType), reader.Name, true);
                            }
                            catch (ArgumentException) {
                                continue;
                            }


                            var r = this.reader.ReadSubtree();

                            switch (this.sectionType) {
                            case SectType.TITLE:
                                this.ParseTitle(r);
                                break;

                            case SectType.JUNCTIONS:
                                this.ParseJunction(r);
                                break;

                            case SectType.RESERVOIRS:
                            case SectType.TANKS:
                                this.ParseTank(r);
                                break;

                            case SectType.PIPES:
                                this.ParsePipe(r);
                                break;
                            case SectType.PUMPS:
                                this.ParsePump(r);
                                break;
                            case SectType.VALVES:
                                this.ParseValve(r);
                                break;
                            case SectType.CONTROLS:
                                this.ParseControl(r);
                                break;

                            case SectType.RULES:
                                this.ParseRule(r);
                                break;

                            case SectType.DEMANDS:
                                this.ParseDemand(r);
                                break;
                            case SectType.SOURCES:
                                this.ParseSource(r);
                                break;
                            case SectType.EMITTERS:
                                this.ParseEmitter(r);
                                break;
                            case SectType.QUALITY:
                                this.ParseQuality(r);
                                break;
                            case SectType.STATUS:
                                this.ParseStatus(r);
                                break;
                            case SectType.ENERGY:
                                this.ParseEnergy(r);
                                break;
                            case SectType.REACTIONS:
                                this.ParseReact(r);
                                break;
                            case SectType.MIXING:
                                this.ParseMixing(r);
                                break;
                            case SectType.REPORT:
                                this.ParseReport(r);
                                break;
                            case SectType.TIMES:
                                this.ParseTime(r);
                                break;
                            case SectType.OPTIONS:
                                this.ParseOption(r);
                                break;
                            case SectType.COORDINATES:
                                this.ParseCoordinate(r);
                                break;
                            case SectType.VERTICES:
                                this.ParseVertice(r);
                                break;
                            case SectType.LABELS:
                                this.ParseLabel(r);
                                break;
                            }
                        }
                    }
                }
            }
            catch (IOException) {
                throw new ENException(ErrorCode.Err302);
            }

            return this.net;
        }

        private void ParseTitle(XmlReader r) {
          
            int i = Constants.MAXTITLE;
            
            while (r.ReadToFollowing("line") && i-- > 0) {
                string s = r.ReadString();
                this.net.Title.Add(s);
            } 
        }

        private void ParseJunction(XmlReader r) {
            while (r.ReadToFollowing("node")) {
                
                Node node = new Node(r.GetAttribute("name"));

                try {
                    node.Elevation = XmlConvert.ToDouble(r.GetAttribute("elevation") ?? "0");
                }
                catch (ArgumentException) {
                    this.LogException(ErrorCode.Err202, r.ReadOuterXml());
                }


                try {
                    node.Position = new EnPoint(
                        XmlConvert.ToDouble(r.GetAttribute("x") ?? "0"),
                        XmlConvert.ToDouble(r.GetAttribute("y") ?? "0"));
                }
                catch(ArgumentException) {
                    this.LogException(ErrorCode.Err202, r.ReadOuterXml());
                }

                using(var rr = r.ReadSubtree()) {
                    rr.Read();
                    while(rr.Read()) {
                        switch(rr.NodeType) {
                        case XmlNodeType.Element:

                        if(rr.Name.Equals("demand", StringComparison.Ordinal)) {

                            string sx = rr.GetAttribute("base") ?? string.Empty;
                            string sy = rr.GetAttribute("pattern") ?? string.Empty;

                            try {
                                // cur.Add(XmlConvert.ToDouble(sx), XmlConvert.ToDouble(sy));
                            }
                            catch(FormatException) {
                                this.LogException(ErrorCode.Err202, rr.ReadInnerXml());
                            }
                        }

                        break;

                        case XmlNodeType.Comment:
                        node.Comment = rr.Value;
                        break;
                        }
                    }
                }





                try {
                    this.net.Nodes.Add(node);
                }
                catch(ArgumentException) {
                    this.LogException(ErrorCode.Err215, node.Name);
                }


            }
        }

        private void ParseTank(XmlReader r) { throw new NotImplementedException(); }

        private void ParsePipe(XmlReader r) { throw new NotImplementedException(); }

        private void ParsePump(XmlReader r) { throw new NotImplementedException(); }

        private void ParseValve(XmlReader r) { throw new NotImplementedException(); }

        private void ParseControl(XmlReader r) { throw new NotImplementedException(); }

        private void ParseRule(XmlReader r) { throw new NotImplementedException(); }

        private void ParseDemand(XmlReader r) { throw new NotImplementedException(); }

        private void ParseSource(XmlReader r) { throw new NotImplementedException(); }

        private void ParseEmitter(XmlReader r) { throw new NotImplementedException(); }

        private void ParseQuality(XmlReader r) { throw new NotImplementedException(); }

        private void ParseStatus(XmlReader r) { throw new NotImplementedException(); }

        private void ParseEnergy(XmlReader r) { throw new NotImplementedException(); }

        private void ParseReact(XmlReader r) { throw new NotImplementedException(); }

        private void ParseMixing(XmlReader r) { throw new NotImplementedException(); }

        private void ParseReport(XmlReader r) { throw new NotImplementedException(); }

        private void ParseTime(XmlReader r) { throw new NotImplementedException(); }

        private void ParseOption(XmlReader r) { throw new NotImplementedException(); }

        private void ParseCoordinate(XmlReader r) { throw new NotImplementedException(); }

        private void ParseVertice(XmlReader r) { throw new NotImplementedException(); }

        private void ParseLabel(XmlReader r) { throw new NotImplementedException(); }
        

        private void ParsePc(XmlReader r) {
            this.reader.ReadStartElement();
           
            while(!r.EOF) {

                try {
                    this.sectionType = (SectType)Enum.Parse(typeof(SectType), r.Name, true);
                }
                catch (ArgumentException) {
                    this.sectionType = (SectType)(-1);
                }


                try {
                    switch (this.sectionType) {
                    case SectType.PATTERNS:
                        this.ParsePattern(r.ReadSubtree());
                        break;
                    case SectType.CURVES:
                        this.ParseCurve(r.ReadSubtree());
                        break;
                    }
                }
                catch (InputException ex) {
                    base.LogException(ex);
                }

                r.Skip();

            }

            if(this.Errors.Count > 0)
                throw new ENException(ErrorCode.Err200);

        }

        private void ParsePattern(XmlReader r) {
            
            if (!r.ReadToDescendant("pattern")) 
                return;
            
            do {
                if (r.IsEmptyElement) 
                    continue;

                string name = r.GetAttribute("name"); // this.reader["name"];

                if (string.IsNullOrEmpty(name))
                    continue;

                var pat = new Pattern(name);

                using (var rr = r.ReadSubtree()) {
                    rr.Read();
                    while (rr.Read()) {
                        switch (rr.NodeType) {
                        case XmlNodeType.Element:
                            if (rr.Name.Equals("factor", StringComparison.Ordinal)) {

                                string s = rr.GetAttribute("value") ?? string.Empty;

                                try {
                                    pat.Add(XmlConvert.ToDouble(s));
                                }
                                catch (FormatException) {
                                    this.LogException(ErrorCode.Err202, rr.ReadInnerXml());
                                }
                            }

                            break;

                        case XmlNodeType.Comment:
                            pat.Comment = rr.Value;
                            break;
                        }
                    }
                }


                try {
                    this.net.Patterns.Add(pat);
                }
                catch(ArgumentException) {
                    this.LogException(ErrorCode.Err215, pat.Name);
                }


            } while (r.ReadToNextSibling("pattern"));

        }

        private void ParseCurve(XmlReader r) {

            if(!r.ReadToDescendant("curve"))
                return;

            do {
                if(r.IsEmptyElement)
                    continue;

                string name = r.GetAttribute("name"); // this.reader["name"];

                if(string.IsNullOrEmpty(name))
                    continue;
                
                var cur = new Curve(name);
                
                using (var rr = r.ReadSubtree()) {
                    rr.Read();
                    while (rr.Read()) {
                        switch (rr.NodeType) {
                        case XmlNodeType.Element:
                            if (rr.Name.Equals("point", StringComparison.Ordinal)) {

                                string sx = rr.GetAttribute("x") ?? string.Empty;
                                string sy = rr.GetAttribute("y") ?? string.Empty;

                                try {
                                    cur.Add(XmlConvert.ToDouble(sx), XmlConvert.ToDouble(sy));
                                }
                                catch (FormatException) {
                                    this.LogException(ErrorCode.Err202, rr.ReadInnerXml());
                                }
                            }

                            break;

                        case XmlNodeType.Comment:
                            cur.Comment = rr.Value;
                            break;
                        }
                    }
                }

                try {
                    this.net.Curves.Add(cur);
                }
                catch(ArgumentException) {
                    this.LogException(ErrorCode.Err215, cur.Name);
                }

            } while(r.ReadToNextSibling("curve"));

            r.ReadEndElement();

          
        }
    }

}