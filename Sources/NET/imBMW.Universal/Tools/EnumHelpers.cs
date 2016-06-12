using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace imBMW.Tools
{
    public static class EnumHelpers
    {
        public static string ToStringValue(this Enum e)
        {
            return e.ToString();
        }
    }
}
