using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace imBMW.Universal.App.Models
{
    public enum GaugeType
    {
        Custom,

        // BC
        Consumption1,
        Consumption2,
        SpeedLimit,
        AverageSpeed,
        Range,
        ArrivalDistance,
        ArrivalTime,
        CoolantTemperatureBC,
        OutsideTemperature,
        Voltage,
        SpeedBC,
        RPMBC,

        // DME
        RPM,
        Speed,
        Throttle,
        Pedal,
        AFR,
        WideBandLambda,
        IntakePressure,
        FuelPressure,
        OilPressure,
        IsMethanolInjecting,
        IsMethanolFailsafe,
        AirMass,
        AirMassPerStroke,
        IgnitionAngle,
        InjectionTime,
        OilTemperature,
        IntakeTemperature,
        IntakeTemperatureAfterCooler,
        CoolerInTemperature,
        CoolerOutTemperature,
        CoolantTemperature,
        CoolantRadiatorTemperature,
        ElectricFanSpeed,
        ISAPWM_IS,
        ISAPWM_ISA,
        KnockSensor2,
        KnockSensor5,
        LambdaIntegrator1,
        LambdaIntegrator2,
        LambdaHeatingAfterCats1,
        LambdaHeatingAfterCats2,
        LambdaHeatingBeforeCats1,
        LambdaHeatingBeforeCats2,
        VanosPositionExhaust,
        VanosPositionIntake,
        AtmosphericPressure,
        VoltageBattery,
        VoltageKL15
    }
}
