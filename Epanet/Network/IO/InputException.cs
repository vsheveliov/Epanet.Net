using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Epanet.Enums;

namespace Epanet.Network.IO {
    class InputException:ENException {
     
        private readonly SectType _section;
        private readonly string _arg;
        public SectType Section { get { return this._section; } }
        public string Arg { get { return this._arg; } }

        public InputException(ErrorCode code, SectType section, string arg) : base(code) {
            this._section = section;
            this._arg = arg;
        }
        
        public override string Message {
            get {
                string fmt;

                try {
                    fmt = Properties.Error.ResourceManager.GetString("ERR" + (int)this._code);
                }
                catch(Exception) {
                    fmt = null;
                }

                return fmt == null 
                    ? string.Format("Unknown error #({0})", (int)this._code)
                    : string.Format(fmt, this._section.ReportStr(), this.Arg);
            }
        }
        
    }
}
