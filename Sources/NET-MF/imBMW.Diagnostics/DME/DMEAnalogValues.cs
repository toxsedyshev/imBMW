using System;

namespace imBMW.Diagnostics.DME
{
    public class DMEAnalogValues
    {
        public DMEAnalogValues(DateTime loggerStarted)
        {
            Time = DateTime.Now;
            TimeSpan = Time - loggerStarted;
        }

        public DMEAnalogValues()
        {
            Time = DateTime.Now;
        }

        public override string ToString()
        {
            return String.Concat(Time, " RPM:", RPM);
        }

        /// <summary>
        /// Time of log entry
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Since logger started
        /// </summary>
        public TimeSpan TimeSpan { get; set; }

        /// <summary>
        /// 1/min
        /// </summary>
        public int RPM { get; set; }

        /// <summary>
        /// km/h
        /// </summary>
        public int Speed { get; set; }

        /// <summary>
        /// Degree
        /// </summary>
        public double Pedal { get; set; }

        /// <summary>
        /// Degree
        /// </summary>
        public double Throttle { get; set; }

        /// <summary>
        /// kg/h
        /// </summary>
        public double AirMass { get; set; }

        /// <summary>
        /// Celsius
        /// </summary>
        public double IntakeTemp { get; set; }

        /// <summary>
        /// Celsius
        /// </summary>
        public double CoolantTemp { get; set; }

        /// <summary>
        /// Celsius
        /// </summary>
        public double OilTemp { get; set; }

        /// <summary>
        /// Celsius
        /// </summary>
        public double CoolantRadiatorTemp { get; set; }

        /// <summary>
        /// Degree
        /// </summary>
        public double IgnitionAngle { get; set; }

        /// <summary>
        /// ms
        /// </summary>
        public double InjectionTime { get; set; }

        /// <summary>
        /// %
        /// Idle Control Valve PWM Integrator = Closing?
        /// </summary>
        public double ISAPWM_IS { get; set; }

        /// <summary>
        /// %
        /// Idle Control Valve PWM Steller = Opening?
        /// </summary>
        public double ISAPWM_ISA { get; set; }

        /// <summary>
        /// Degree
        /// </summary>
        public double VanosPositionIntake { get; set; }

        /// <summary>
        /// Degree
        /// </summary>
        public double VanosPositionExhaust { get; set; }

        /// <summary>
        /// Volts
        /// </summary>
        public double VoltageKL15 { get; set; }

        /// <summary>
        /// Number
        /// </summary>
        public double LambdaIntegrator1 { get; set; }

        /// <summary>
        /// Number
        /// </summary>
        public double LambdaIntegrator2 { get; set; }
        
        /// <summary>
        /// %
        /// </summary>
        public double LambdaHeatingBeforeCats1 { get; set; }

        /// <summary>
        /// %
        /// </summary>
        public double LambdaHeatingBeforeCats2 { get; set; }

        /// <summary>
        /// %
        /// </summary>
        public double LambdaHeatingAfterCats1 { get; set; }

        /// <summary>
        /// %
        /// </summary>
        public double LambdaHeatingAfterCats2 { get; set; }

        /// <summary>
        /// mg/HUB = mg per stroke
        /// </summary>
        public double AirMassPerStroke { get; set; }

        /// <summary>
        /// Volts
        /// </summary>
        public double KnockSensor2 { get; set; }

        /// <summary>
        /// Volts
        /// </summary>
        public double KnockSensor5 { get; set; }

        /// <summary>
        /// %
        /// </summary>
        public double ElectricFanSpeed { get; set; }

        /// <summary>
        /// hPa = bar*1000
        /// </summary>
        public int AtmosphericPressure { get; set; }

        /// <summary>
        /// Volts
        /// </summary>
        public double VoltageBattery { get; set; }

        /// <summary>
        /// hPa = bar*1000
        /// </summary>
        public int IntakePressure { get; set; }

        /// <summary>
        /// Air/Fuel
        /// </summary>
        public double AFR { get; set; }

        /// <summary>
        /// Number
        /// </summary>
        public double WideBandLambda { get; set; }

        public virtual string GenerateLogString()
        {
            throw new NotImplementedException();
        }
    }
}
