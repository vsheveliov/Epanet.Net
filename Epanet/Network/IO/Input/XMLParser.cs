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
        private SectType _sectionType;
        private XmlReader _reader;
        private readonly bool _gzipped;
        
        public XmlParser(bool gzipped) { _gzipped = gzipped; }

        private void LogException(ErrorCode err, string line) {
            if(err == ErrorCode.Ok)
                return;

            var ex = new InputException(err, _sectionType, line);

            base.errors.Add(ex);

            base.LogException(ex);

        }

        public override Network Parse(Network nw, string f) {
            net = nw ?? new Network();

            XmlReaderSettings settings = new XmlReaderSettings {
                CloseInput = false,
                ValidationType = ValidationType.None,
                CheckCharacters = true,
                IgnoreComments = false,
                IgnoreWhitespace = true,
                
            };

            try {


                Stream stream = _gzipped
                    ? (Stream)new GZipStream(File.OpenRead(f), CompressionMode.Decompress)
                    : File.OpenRead(f);

                using (stream) {

                    using (_reader = XmlReader.Create(stream, settings)) {
                        _reader.ReadToFollowing("network");
                        ParsePc(_reader);
                    }

                    stream.Position = 0;

                    using (_reader = XmlReader.Create(stream, settings)) {
                        _reader.ReadToFollowing("network");
                        // this.reader.Read(); // skip "network"
                        // this.reader.MoveToContent(); // If that node is whitespace, skip to content.



                        while (_reader.Read()) {
                            if (_reader.NodeType != XmlNodeType.Element || _reader.IsEmptyElement)
                                continue;

                            try {
                                _sectionType = (SectType)Enum.Parse(typeof(SectType), _reader.Name, true);
                            }
                            catch (ArgumentException) {
                                continue;
                            }


                            var r = _reader.ReadSubtree();

                            switch (_sectionType) {
                            case SectType.TITLE:
                                ParseTitle(r);
                                break;

                            case SectType.JUNCTIONS:
                                ParseJunction(r);
                                break;

                            case SectType.RESERVOIRS:
                            case SectType.TANKS:
                                ParseTank(r);
                                break;

                            case SectType.PIPES:
                                ParsePipe(r);
                                break;
                            case SectType.PUMPS:
                                ParsePump(r);
                                break;
                            case SectType.VALVES:
                                ParseValve(r);
                                break;
                            case SectType.CONTROLS:
                                ParseControl(r);
                                break;

                            case SectType.RULES:
                                ParseRule(r);
                                break;

                            case SectType.DEMANDS:
                                ParseDemand(r);
                                break;
                            case SectType.SOURCES:
                                ParseSource(r);
                                break;
                            case SectType.EMITTERS:
                                ParseEmitter(r);
                                break;
                            case SectType.QUALITY:
                                ParseQuality(r);
                                break;
                            case SectType.STATUS:
                                ParseStatus(r);
                                break;
                            case SectType.ENERGY:
                                ParseEnergy(r);
                                break;
                            case SectType.REACTIONS:
                                ParseReact(r);
                                break;
                            case SectType.MIXING:
                                ParseMixing(r);
                                break;
                            case SectType.REPORT:
                                ParseReport(r);
                                break;
                            case SectType.TIMES:
                                ParseTime(r);
                                break;
                            case SectType.OPTIONS:
                                ParseOption(r);
                                break;
                            case SectType.COORDINATES:
                                ParseCoordinate(r);
                                break;
                            case SectType.VERTICES:
                                ParseVertice(r);
                                break;
                            case SectType.LABELS:
                                ParseLabel(r);
                                break;
                            }
                        }
                    }
                }
            }
            catch (IOException) {
                throw new ENException(ErrorCode.Err302);
            }

            return net;
        }

        private void ParseTitle(XmlReader r) {
          
            int i = Constants.MAXTITLE;
            
            while (r.ReadToFollowing("line") && i-- > 0) {
                string s = r.ReadString();
                net.Title.Add(s);
            } 
        }

        private void ParseJunction(XmlReader r) {
            while (r.ReadToFollowing("node")) {
                
                Node node = new Node(r.GetAttribute("name"));

                try {
                    node.Elevation = XmlConvert.ToDouble(r.GetAttribute("elevation") ?? "0");
                }
                catch (ArgumentException) {
                    LogException(ErrorCode.Err202, r.ReadOuterXml());
                }


                try {
                    node.Position = new EnPoint(
                        XmlConvert.ToDouble(r.GetAttribute("x") ?? "0"),
                        XmlConvert.ToDouble(r.GetAttribute("y") ?? "0"));
                }
                catch(ArgumentException) {
                    LogException(ErrorCode.Err202, r.ReadOuterXml());
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
                                LogException(ErrorCode.Err202, rr.ReadInnerXml());
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
                    net.Nodes.Add(node);
                }
                catch(ArgumentException) {
                    LogException(ErrorCode.Err215, node.Name);
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
            _reader.ReadStartElement();
           
            while(!r.EOF) {

                try {
                    _sectionType = (SectType)Enum.Parse(typeof(SectType), r.Name, true);
                }
                catch (ArgumentException) {
                    _sectionType = (SectType)(-1);
                }


                try {
                    switch (_sectionType) {
                    case SectType.PATTERNS:
                        ParsePattern(r.ReadSubtree());
                        break;
                    case SectType.CURVES:
                        ParseCurve(r.ReadSubtree());
                        break;
                    }
                }
                catch (InputException ex) {
                    base.LogException(ex);
                }

                r.Skip();

            }

            if(errors.Count > 0)
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
                                    LogException(ErrorCode.Err202, rr.ReadInnerXml());
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
                    net.Patterns.Add(pat);
                }
                catch(ArgumentException) {
                    LogException(ErrorCode.Err215, pat.Name);
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
                                    LogException(ErrorCode.Err202, rr.ReadInnerXml());
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
                    net.Curves.Add(cur);
                }
                catch(ArgumentException) {
                    LogException(ErrorCode.Err215, cur.Name);
                }

            } while(r.ReadToNextSibling("curve"));

            r.ReadEndElement();

          
        }
    }

}