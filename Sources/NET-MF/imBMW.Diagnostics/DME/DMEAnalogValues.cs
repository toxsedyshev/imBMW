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
        public float Pedal { get; protected set; }

        /// <summary>
        /// Degree
        /// </summary>
        public float Throttle { get; protected set; }

        /// <summary>
        /// kg/h
        /// </summary>
        public float AirMass { get; protected set; }

        /// <summary>
        /// Celsius
        /// </summary>
        public float IntakeTemp { get; protected set; }

        /// <summary>
        /// Celsius
        /// </summary>
        public float CoolantTemp { get; protected set; }

        /// <summary>
        /// Celsius
        /// </summary>
        public float OilTemp { get; protected set; }

        /// <summary>
        /// Celsius
        /// </summary>
        public float CoolantRadiatorTemp { get; protected set; }

        /// <summary>
        /// Degree
        /// </summary>
        public float IgnitionAngle { get; protected set; }

        /// <summary>
        /// ms
        /// </summary>
        public float InjectionTime { get; protected set; }

        /// <summary>
        /// %
        /// Idle Control Valve PWM Integrator = Closing?
        /// </summary>
        public float ISAPWM_IS { get; protected set; }

        /// <summary>
        /// %
        /// Idle Control Valve PWM Steller = Opening?
        /// </summary>
        public float ISAPWM_ISA { get; protected set; }

        /// <summary>
        /// Degree
        /// </summary>
        public float VanosPositionIntake { get; protected set; }

        /// <summary>
        /// Degree
        /// </summary>
        public float VanosPositionExhaust { get; protected set; }

        /// <summary>
        /// Volts
        /// </summary>
        public float VoltageKL15 { get; protected set; }

        /// <summary>
        /// Number
        /// </summary>
        public float LambdaIntegrator1 { get; protected set; }

        /// <summary>
        /// Number
        /// </summary>
        public float LambdaIntegrator2 { get; protected set; }

        /// <summary>
        /// %
        /// </summary>
        public float LambdaIntegratorV1 { get; protected set; }

        /// <summary>
        /// %
        /// </summary>
        public float LambdaIntegratorV2 { get; protected set; }

        /// <summary>
        /// %
        /// </summary>
        public float LambdaHeatingBeforeCats1 { get; protected set; }

        /// <summary>
        /// %
        /// </summary>
        public float LambdaHeatingBeforeCats2 { get; protected set; }

        /// <summary>
        /// %
        /// </summary>
        public float LambdaHeatingAfterCats1 { get; protected set; }

        /// <summary>
        /// %
        /// </summary>
        public float LambdaHeatingAfterCats2 { get; protected set; }

        /// <summary>
        /// mg/HUB = mg per stroke
        /// </summary>
        public float AirFlowPerStroke { get; protected set; }

        /// <summary>
        /// Volts
        /// </summary>
        public float KnockSensor2 { get; protected set; }

        /// <summary>
        /// Volts
        /// </summary>
        public float KnockSensor5 { get; protected set; }

        /// <summary>
        /// %
        /// </summary>
        public float ElectricFanSpeed { get; protected set; }

        /// <summary>
        /// hPa = bar*1000
        /// </summary>
        public float AtmosphericPressure { get; protected set; }

        /// <summary>
        /// Volts
        /// </summary>
        public float VoltageBattery { get; protected set; }

        /// <summary>
        /// hPa = bar*1000
        /// </summary>
        public float IntakePressure { get; set; }

        /// <summary>
        /// Air/Fuel
        /// </summary>
        public float AFR { get; set; }

    }
}
