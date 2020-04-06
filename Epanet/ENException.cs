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

namespace Epanet {

    ///<summary>Epanet exception codes handler.</summary>
    public class EnException:Exception {

        ///<summary>Array of arguments to be used in the error string creation.</summary>
        private readonly object[] _arguments;

        ///<summary>Epanet error code.</summary>
        protected readonly ErrorCode _code;

        /// <summary>Get error code.</summary>
        /// <value>Code id.</value>
        public ErrorCode Code => _code;

        ///<summary>Contructor from error code id.</summary>
        ///<param name="code">Error code id.</param>

        public EnException(ErrorCode code) {
            _arguments = null;
            _code = code;
        }

        /// <summary>Contructor from error code id and multiple arguments.</summary>
        ///  <param name="code">Error code id.</param>
        /// <param name="arg">Extra arguments.</param>
        ///  
        public EnException(ErrorCode code, params object[] arg) {
            _code = code;
            _arguments = arg;
        }

        /// <summary>Contructor from error code id and inner exception.</summary>
        ///  <param name="code">Error code id.</param>
        /// <param name="innerException"></param>
        ///  
        public EnException(ErrorCode code, Exception innerException)
            :base(null, innerException) {
            _code = code;
        }

        ///<summary>Handles the exception string conversion.</summary>
        /// <returns>Final error string.</returns>
        public override string Message {
            get {
                string format;
                
                try {
                    format = Properties.Error.ResourceManager.GetString("ERR" + (int)_code);
                }
                catch (Exception) {
                    format = null;
                }


                if (format == null)
                    return string.Format("Unknown error message ({0})", _code);

                return _arguments != null ? string.Format(format, _arguments) : format;
            }
        }
    }

}