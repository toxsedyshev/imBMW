using System;
using Microsoft.SPOT;
using System.Collections;
using Microsoft.SPOT.Hardware;

namespace imBMW.Tools
{
    public class HardwareButton
    {
        static ArrayList buttons = new ArrayList();

        /// <summary>
        /// Toggle button callback handler.
        /// </summary>
        /// <param name="pressed">True if Pin is short to GND.</param>
        public delegate void ToggleHandler(bool pressed);

        public delegate void Action();

        public static void OnPress(Cpu.Pin pin, Action callback)
        {
            var btn = new InterruptPort(pin, true, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeLow);
            btn.OnInterrupt += (s, e, t) => callback();
            buttons.Add(btn);
        }

        public static void Toggle(Cpu.Pin pin, ToggleHandler callback, bool fire = true)
        {
            var btn = new InterruptPort(pin, true, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeBoth);
            btn.OnInterrupt += (s, e, t) => callback(!btn.Read());
            buttons.Add(btn);
            if (fire)
            {
                callback(!btn.Read());
            }
        }
    }
}
