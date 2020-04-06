using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Epanet.Enums
{
    internal sealed class KeywordAttribute : Attribute
    {
        public KeywordAttribute(string keyword) { Keyword = keyword; }

        public string Keyword { get; }
    }



}
