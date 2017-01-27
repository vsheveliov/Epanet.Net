using System;

using Epanet.Enums;

namespace Epanet.Network.IO {
    class InputException:ENException {
     
        private readonly SectType _section;
        private readonly string _arg;
        public SectType Section { get { return _section; } }
        public string Arg { get { return _arg; } }

        public InputException(ErrorCode code, SectType section, string arg) : base(code) {
            _section = section;
            _arg = arg;
        }
        
        public override string Message {
            get {
                string fmt;

                try {
                    fmt = Properties.Error.ResourceManager.GetString("ERR" + (int)code);
                }
                catch(Exception) {
                    fmt = null;
                }

                return fmt == null 
                    ? string.Format("Unknown error #({0})", (int)code)
                    : string.Format(fmt, _section.ReportStr(), Arg);
            }
        }
        
    }
}
