#if !MF_FRAMEWORK_VERSION_V4_1
using System;
using Microsoft.SPOT;

namespace imBMW.Shields
{
    public class ShieldBase
    {
        public static double DetectValue { get; protected set; }
    }
}
#endif