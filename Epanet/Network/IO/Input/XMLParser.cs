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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

using Epanet.Enums;
using Epanet.Log;
using Epanet.Network.Structures;
using Epanet.Util;

namespace Epanet.Network.IO.Input {

    public class XmlParser:InputParser {
        private XmlReader reader;
        private readonly bool gzipped;

        public XmlParser(bool gzipped) { this.gzipped = gzipped; }

        protected void LogException(SectType section, ErrorCode err, string line, IList<string> tokens) {
            if(err == ErrorCode.Ok)
                return;

            string arg = section == SectType.OPTIONS ? line : tokens[0];

            EpanetParseException parseException = new EpanetParseException(
                err,
                this.reader.,
                this.FileName,
                section.ReportStr(),
                arg);

            base.Errors.Add(parseException);

            this.Log.Error(parseException.ToString());

        }

        public override Network Parse(Network net, string f) {
            this.FileName = Path.GetFullPath(f);

            try {
                Stream stream = this.gzipped
                    ? (Stream)new GZipStream(File.OpenRead(f), CompressionMode.Decompress)
                    : File.OpenRead(f);

                XmlReaderSettings settings = new XmlReaderSettings {
                    CloseInput = true,
                    ValidationType = ValidationType.None,
                    CheckCharacters = true,
                    IgnoreComments = true,
                    IgnoreWhitespace = true
                };
                

                using (reader = XmlReader.Create(stream, settings)) {
                    reader.MoveToContent();

                    ParsePc(net);

                }


            }
            catch (IOException) {
                throw new ENException(ErrorCode.Err302);
            }

            return net;
        }

        private void ParsePc(Network net) {
            while (this.reader.Read()) {
                if (this.reader.NodeType != XmlNodeType.Element) continue;

                SectType sectionType;
                try {
                    sectionType = (SectType)Enum.Parse(typeof(SectType), this.reader.Name, true);
                }
                catch (ArgumentException) {
                    sectionType = (SectType)(-1);
                }


                try {
                switch (sectionType) {
                case SectType.PATTERNS:
                    this.ParsePattern(net);
                    break;
                case SectType.CURVES:
                    this.ParseCurve(net);
                    break;
                }
                }
                catch(ENException e) {
                    this.LogException(sectionType, e.Code, this.reader.Name, null);
                }

                    

            }

            if(this.Errors.Count > 0)
                throw new ENException(ErrorCode.Err200);

        }


        protected void ParsePattern(Network net) {
            this.reader.ReadElementString();
            Pattern pat = net.GetPattern(tok[0]);

            if(pat == null) {
                pat = new Pattern(tok[0]);
                net.Patterns.Add(pat);
            }

            for(int i = 1; i < tok.Length; i++) {
                double x;

                if(!tok[i].ToDouble(out x))
                    throw new ENException(ErrorCode.Err202);

                pat.Add(x);
            }
        }

        protected void ParseCurve(Network net) {
            Curve cur = net.GetCurve(tok[0]);

            if(cur == null) {
                cur = new Curve(tok[0]);
                net.Curves.Add(cur);
            }

            double x, y;

            if(!tok[1].ToDouble(out x) || !tok[2].ToDouble(out y))
                throw new ENException(ErrorCode.Err202);

            cur.Add(x, y);
        }
    }

}