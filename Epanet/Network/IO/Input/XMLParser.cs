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
                                    ParseReservoir(r);
                                    break;

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

                                case SectType.PATTERNS:
                                case SectType.CURVES:
                                    break;

                                case SectType.QUALITY:
                                    ParseQuality(r);
                                    break;

                                case SectType.STATUS:
                                    ParseStatus(r);
                                    break;

                                case SectType.ROUGHNESS:
                                    // TODO add ParseRoughness 
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

                                case SectType.BACKDROP:
                                // TODO add ParseRoughness 
                                    break;

                                case SectType.TAGS:

                                case SectType.END:
                                    break;
                                // TODO add etc
                            }
                        }
                    }
                }
            }
            catch (IOException) {
                throw new EnException(ErrorCode.Err302);
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
            if (!r.ReadToDescendant("node"))
                return;

            // while (r.ReadToFollowing("node"))

            do {
                string name = r.GetAttribute("name");

                Node node = new Junction(name);

                try {
                    net.Nodes.Add(node);
                }
                catch (ArgumentException) {
                    LogException(ErrorCode.Err215, name);
                    continue;
                }

                string el = r.GetAttribute("elevation");

                if (string.IsNullOrEmpty(el)) {
                    LogException(ErrorCode.Err201, r.Value);
                    continue;
                }

                try {
                    node.Elevation = XmlConvert.ToDouble(el);
                }
                catch (FormatException) {
                    LogException(ErrorCode.Err202, name);
                    continue;
                }

                string sx = r.GetAttribute("x");
                string sy = r.GetAttribute("y");

                if (!string.IsNullOrEmpty(sx) && !string.IsNullOrEmpty(sy)) {
                    try {
                        node.Coordinate = new EnPoint(XmlConvert.ToDouble(sx), XmlConvert.ToDouble(sy));
                    }
                    catch (FormatException) {
                        LogException(ErrorCode.Err202, node.Name);
                        continue;
                    }
                }


                using (var rr = r.ReadSubtree()) {

                    rr.Read();

                    while (rr.Read()) {
                        switch (rr.NodeType) {
                            case XmlNodeType.Element:

                                if (rr.Name.Equals("demand", StringComparison.Ordinal)) {
                                    string att = rr.GetAttribute("base");

                                    if (!string.IsNullOrEmpty(att)) {
                                        try {
                                            node.PrimaryDemand.Base = XmlConvert.ToDouble(att.Trim());
                                        }
                                        catch (FormatException) {
                                            LogException(ErrorCode.Err202, name);
                                            continue;
                                        }
                                    }

                                    att = rr.GetAttribute("pattern");

                                    if (!string.IsNullOrEmpty(att)) {
                                        Pattern p = net.GetPattern(att.Trim());
                                        if (p == null) {
                                            LogException(ErrorCode.Err205, name);
                                            continue;
                                        }

                                        node.PrimaryDemand.Pattern = p;
                                    }

                                }
                                else if (rr.Name.Equals("tag", StringComparison.Ordinal)) {
                                    string s = rr.ReadString();
                                    if (!string.IsNullOrEmpty(s)) node.Tag = s.Trim();
                                }

                                break;

                            case XmlNodeType.Comment:
                                node.Comment = rr.Value;
                                break;
                        }
                    }
                }

            } while (r.ReadToNextSibling("node"));
        }

        private void ParseReservoir(XmlReader r) {

            if (!r.ReadToDescendant("node"))
                return;

            // while (r.ReadToFollowing("node"))

            do
            {
                string name = r.GetAttribute("name");

                Tank node = new Tank(name);

                try
                {
                    net.Nodes.Add(node);
                }
                catch (ArgumentException)
                {
                    LogException(ErrorCode.Err215, name);
                    continue;
                }

                string el = r.GetAttribute("elevation");
                
                if (string.IsNullOrEmpty(el))
                {
                    LogException(ErrorCode.Err201, r.Value);
                    continue;
                }

                try
                {
                    node.Elevation = XmlConvert.ToDouble(el);
                }
                catch (FormatException)
                {
                    LogException(ErrorCode.Err202, name);
                    continue;
                }


                string pattern = r.GetAttribute("pattern");

                if(!string.IsNullOrEmpty(pattern)) {
                    Pattern p = net.GetPattern(pattern.Trim());
                    if(p == null) {
                        LogException(ErrorCode.Err205, name);
                        continue;
                    }

                    node.Pattern = p;
                }

                string sx = r.GetAttribute("x");
                string sy = r.GetAttribute("y");

                if (!string.IsNullOrEmpty(sx) && !string.IsNullOrEmpty(sy))
                {
                    try
                    {
                        node.Coordinate = new EnPoint(XmlConvert.ToDouble(sx), XmlConvert.ToDouble(sy));
                    }
                    catch (FormatException)
                    {
                        LogException(ErrorCode.Err202, node.Name);
                        continue;
                    }
                }


                using (var rr = r.ReadSubtree())
                {

                    rr.Read();

                    while (rr.Read())
                    {
                        switch (rr.NodeType)
                        {
                            case XmlNodeType.Element:

                                if (rr.Name.Equals("demand", StringComparison.Ordinal))
                                {
                                    string att = rr.GetAttribute("base");

                                    if (!string.IsNullOrEmpty(att))
                                    {
                                        try
                                        {
                                            node.PrimaryDemand.Base = XmlConvert.ToDouble(att.Trim());
                                        }
                                        catch (FormatException)
                                        {
                                            LogException(ErrorCode.Err202, name);
                                            continue;
                                        }
                                    }

                                    att = rr.GetAttribute("pattern");

                                    if (!string.IsNullOrEmpty(att))
                                    {
                                        Pattern p = net.GetPattern(att.Trim());
                                        if (p == null)
                                        {
                                            LogException(ErrorCode.Err205, name);
                                            continue;
                                        }

                                        node.PrimaryDemand.Pattern = p;
                                    }

                                }
                                else if (rr.Name.Equals("tag", StringComparison.Ordinal))
                                {
                                    string s = rr.ReadString();
                                    if (!string.IsNullOrEmpty(s)) node.Tag = s.Trim();
                                }

                                break;

                            case XmlNodeType.Comment:
                                node.Comment = rr.Value;
                                break;
                        }
                    }
                }

            } while (r.ReadToNextSibling("node"));
            
        }

        private void ParseTank(XmlReader r) {

            if (!r.ReadToDescendant("node"))
                return;

            // while (r.ReadToFollowing("node"))

            do
            {
                string name = r.GetAttribute("name");

                Tank node = new Tank(name);

                try
                {
                    net.Nodes.Add(node);
                }
                catch (ArgumentException)
                {
                    LogException(ErrorCode.Err215, name);
                    continue;
                }

                string el = r.GetAttribute("elevation");



                if (string.IsNullOrEmpty(el))
                { 
                    // Reservour
                    string pattern = r.GetAttribute("pattern");

                    if (!string.IsNullOrEmpty(pattern)) {
                        // TODO
                    }


                }
                else {
                    // Tank
                    // TODO
                }


                try
                {
                    node.Elevation = XmlConvert.ToDouble(el);
                }
                catch (FormatException)
                {
                    LogException(ErrorCode.Err202, name);
                    continue;
                }

                string sx = r.GetAttribute("x");
                string sy = r.GetAttribute("y");

                if (!string.IsNullOrEmpty(sx) && !string.IsNullOrEmpty(sy))
                {
                    try
                    {
                        node.Coordinate = new EnPoint(XmlConvert.ToDouble(sx), XmlConvert.ToDouble(sy));
                    }
                    catch (FormatException)
                    {
                        LogException(ErrorCode.Err202, node.Name);
                        continue;
                    }
                }


                using (var rr = r.ReadSubtree())
                {

                    rr.Read();

                    while (rr.Read())
                    {
                        switch (rr.NodeType)
                        {
                            case XmlNodeType.Element:

                                if (rr.Name.Equals("demand", StringComparison.Ordinal))
                                {
                                    string att = rr.GetAttribute("base");

                                    if (!string.IsNullOrEmpty(att))
                                    {
                                        try
                                        {
                                            node.PrimaryDemand.Base = XmlConvert.ToDouble(att.Trim());
                                        }
                                        catch (FormatException)
                                        {
                                            LogException(ErrorCode.Err202, name);
                                            continue;
                                        }
                                    }

                                    att = rr.GetAttribute("pattern");

                                    if (!string.IsNullOrEmpty(att))
                                    {
                                        Pattern p = net.GetPattern(att.Trim());
                                        if (p == null)
                                        {
                                            LogException(ErrorCode.Err205, name);
                                            continue;
                                        }

                                        node.PrimaryDemand.Pattern = p;
                                    }

                                }
                                else if (rr.Name.Equals("tag", StringComparison.Ordinal))
                                {
                                    string s = rr.ReadString();
                                    if (!string.IsNullOrEmpty(s)) node.Tag = s.Trim();
                                }

                                break;

                            case XmlNodeType.Comment:
                                node.Comment = rr.Value;
                                break;
                        }
                    }
                }

            } while (r.ReadToNextSibling("node"));

        }

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
                throw new EnException(ErrorCode.Err200);

        }

        private void ParsePattern(XmlReader r) {
            
            if (!r.ReadToDescendant("pattern")) 
                return;
            
            do {
                if (r.IsEmptyElement) 
                    continue;

                string name = r.GetAttribute("name"); 

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

                string name = r.GetAttribute("name"); 

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