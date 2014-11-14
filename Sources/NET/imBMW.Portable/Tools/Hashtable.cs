using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace imBMW.Tools
{
    public class Hashtable : Dictionary<object, object>
    {
        public bool Contains(object key)
        {
            return this.Keys.Contains(key);
        }
    }
}
