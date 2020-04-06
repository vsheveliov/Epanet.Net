using System;

using Epanet.Enums;

namespace Epanet.Network.IO {
    class InputException:EnException {
     
        private readonly SectType _section;
        public string Arg { get; }

        public InputException(ErrorCode code, SectType section, string arg) : base(code) {
            _section = section;
            Arg = arg;
        }
        
        public override string Message {
            get {
                string fmt;

                try {
                    fmt = Properties.Error.ResourceManager.GetString("ERR" + (int)_code);
                }
                catch(Exception) {
                    fmt = null;
                }

                return fmt == null 
                    ? string.Format("Unknown error #({0})", (int)_code)
                    : string.Format(fmt, _section.ToString(), Arg);
            }
        }
        
    }
}
