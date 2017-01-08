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

using System.Collections.Generic;

namespace Epanet.Network.Structures {

    ///<summary>Rule source code class.</summary>
    public class Rule:Element {
        private readonly List<string> code = new List<string>();

        public Rule(string name) : base(name) { }

        public List<string> Code { get { return this.code; } }
  
        public override ElementType ElementType { get { return ElementType.Rule;} }

  
    }

}