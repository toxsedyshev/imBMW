using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace imBMW.Tools
{
    public class Hashtable : Dictionary<object, object>
    {
        public bool Contains(string key)
        {
            return ContainsKey(key);
        }
    }
}
