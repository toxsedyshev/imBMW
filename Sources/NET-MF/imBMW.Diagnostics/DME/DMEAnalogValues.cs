using System;
using Microsoft.SPOT;

namespace imBMW.Diagnostics.DME
{
    public class DMEAnalogValues
    {
        /// <summary>
        /// 1/min
        /// </summary>
        public int RPM { get; protected set; }

        /// <summary>
        /// km/h
        /// </summary>
        public int Speed { get; protected set; }

        /// <summary>
        /// Degree
        /// </summary>
        public double Pedal { get; protected set; }

        /// <summary>
        /// Degree
        /// </summary>
        public double Throttle { get; protected set; }

        /// <summary>
        /// kg/h
        /// </summary>
        public double AirMass { get; protected set; }

        /// <summary>
        /// Celsius
        /// </summary>
        public double IntakeTemp { get; protected set; }

        /// <summary>
        /// Celsius
        /// </summary>
        public double CoolantTemp { get; protected set; }

        /// <summary>
        /// Celsius
        /// </summary>
        public double OilTemp { get; protected set; }

        /// <summary>
        /// Celsius
        /// </summary>
        public double CoolantRadiatorTemp { get; protected set; }

        /// <summary>
        /// Degree
        /// </summary>
        public double IgnitionAngle { get; protected set; }

        /// <summary>
        /// ms
        /// </summary>
        public double InjectionTime { get; protected set; }

        /// <summary>
        /// %
        /// Idle Control Valve PWM Integrator = Closing?
        /// </summary>
        public double ISAPWM_IS { get; protected set; }

        /// <summary>
        /// %
        /// Idle Control Valve PWM Steller = Opening?
        /// </summary>
        public double ISAPWM_ISA { get; protected set; }

        /// <summary>
        /// Degree
        /// </summary>
        public double VanosPositionIntake { get; protected set; }

        /// <summary>
        /// Degree
        /// </summary>
        public double VanosPositionExhaust { get; protected set; }

        /// <summary>
        /// Volts
        /// </summary>
        public double VoltageKL15 { get; protected set; }

        /// <summary>
        /// Number
        /// </summary>
        public double LambdaIntegrator1 { get; protected set; }

        /// <summary>
        /// Number
        /// </summary>
        public double LambdaIntegrator2 { get; protected set; }
        
        /// <summary>
        /// %
        /// </summary>
        public double LambdaHeatingBeforeCats1 { get; protected set; }

        /// <summary>
        /// %
        /// </summary>
        public double LambdaHeatingBeforeCats2 { get; protected set; }

        /// <summary>
        /// %
        /// </summary>
        public double LambdaHeatingAfterCats1 { get; protected set; }

        /// <summary>
        /// %
        /// </summary>
        public double LambdaHeatingAfterCats2 { get; protected set; }

        /// <summary>
        /// mg/HUB = mg per stroke
        /// </summary>
        public double AirMassPerStroke { get; protected set; }

        /// <summary>
        /// Volts
        /// </summary>
        public double KnockSensor2 { get; protected set; }

        /// <summary>
        /// Volts
        /// </summary>
        public double KnockSensor5 { get; protected set; }

        /// <summary>
        /// %
        /// </summary>
        public double ElectricFanSpeed { get; protected set; }

        /// <summary>
        /// hPa = bar*1000
        /// </summary>
        public double AtmosphericPressure { get; protected set; }

        /// <summary>
        /// Volts
        /// </summary>
        public double VoltageBattery { get; protected set; }

        /// <summary>
        /// hPa = bar*1000
        /// </summary>
        public double IntakePressure { get; set; }

        /// <summary>
        /// Air/Fuel
        /// </summary>
        public double AFR { get; set; }

    }
}
