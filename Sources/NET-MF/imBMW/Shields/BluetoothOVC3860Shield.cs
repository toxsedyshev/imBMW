#if !MF_FRAMEWORK_VERSION_V4_1
using System;
using Microsoft.SPOT;

namespace imBMW.Shields
{
    public class BluetoothOVC3860Shield : ShieldBase
    {
        static BluetoothOVC3860Shield()
        {
            DetectValue = 0.9;
        }
    }
}
#endif