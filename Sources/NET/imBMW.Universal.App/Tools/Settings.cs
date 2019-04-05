using imBMW.Universal.App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Windows.Storage;
using System.ComponentModel;
using Newtonsoft.Json;

namespace imBMW.Universal.App.Tools
{
    public class Settings
    {
        static Settings instance;
        ApplicationDataContainer settings;

        public event PropertyChangedEventHandler PropertyChanged;

        Settings()
        {
            settings = ApplicationData.Current.RoamingSettings;
        }
        
        public List<GaugeSlot> Gauges
        {
            get
            {
                var value = GetValueOrDefault<List<GaugeSlot>>();
                return value ?? DefaultGauges;
            }
            set
            {
                AddOrUpdateValue(value);
            }
        }

        #region Default gauges

        List<GaugeSlot> DefaultGauges
        {
            get
            {
                return new List<GaugeSlot>
                {
                    new GaugeSlot(GaugeType.RPM, GaugeType.AirMassPerStroke),
                    new GaugeSlot(GaugeType.AFR),
                    new GaugeSlot(GaugeType.IntakePressure, GaugeType.IntakeTemperatureAfterCooler),
                    new GaugeSlot(GaugeType.FuelPressure, GaugeType.OilPressure),
                    new GaugeSlot(GaugeType.CoolerInTemperature, GaugeType.CoolerOutTemperature),
                    new GaugeSlot(GaugeType.OilTemperature, GaugeType.CoolantTemperature),
                    new GaugeSlot(GaugeType.IgnitionAngle, GaugeType.InjectionTime),
                    new GaugeSlot(GaugeType.IsMethanolInjecting, GaugeType.IsMethanolFailsafe),
                };
                /*return new List<GaugeSlot>
                {
                    new GaugeSlot(GaugeType.CoolantTemperature, GaugeType.CoolantTemperature),
                    new GaugeSlot(GaugeType.CoolantRadiatorTemperature, GaugeType.ElectricFanSpeed),
                    new GaugeSlot(GaugeType.AFR),
                    new GaugeSlot(GaugeType.IntakePressure),
                    new GaugeSlot(GaugeType.IntakeTemperature, GaugeType.VoltageBattery),
                    new GaugeSlot(GaugeType.Throttle, GaugeType.AirMass),
                    new GaugeSlot(GaugeType.Consumption1, GaugeType.Consumption2),
                    new GaugeSlot(GaugeType.SpeedLimit, GaugeType.Range)
                };*/
            }
        }

        #endregion

        #region Settings utils

        /// <summary>
        /// Update a setting value for our application. If the setting does not
        /// exist, then add the setting.
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        bool AddOrUpdateValue(Object value, [CallerMemberName]string Key = "")
        {
            bool valueChanged = false;

            // If the key exists
            if (settings.Values.ContainsKey(Key))
            {
                // If the value has changed
                if (settings.Values[Key] != value)
                {
                    // Store the new value
                    settings.Values[Key] = value;
                    valueChanged = true;
                }
            }
            // Otherwise create the key.
            else
            {
                settings.Values.Add(Key, value);
                valueChanged = true;
            }
            if (valueChanged)
            {
                OnPropertyChanged(Key);
            }
            return valueChanged;
        }

        /// <summary>
        /// Get the current value of the setting, or if it is not found, set the 
        /// setting to the default setting.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        T GetValueOrDefault<T>(Func<T> defaultValueCreator, [CallerMemberName]string Key = "")
        {
            T value;

            // If the key exists, retrieve the value.
            if (settings.Values.ContainsKey(Key))
            {
                value = (T)settings.Values[Key];
            }
            // Otherwise, use the default value.
            else
            {
                if (defaultValueCreator == null)
                {
                    value = default(T);
                }
                else
                {
                    value = defaultValueCreator();
                }
                settings.Values.Add(Key, value);
            }
            return value;
        }

        T GetValueOrDefault<T>([CallerMemberName]string Key = "")
        {
            return GetValueOrDefault<T>(null, Key);
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static Settings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Settings();
                }
                return instance;
            }
        }

        #endregion
    }
}
