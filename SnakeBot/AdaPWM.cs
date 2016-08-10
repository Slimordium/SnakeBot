﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;

namespace SnakeBot
{
    internal sealed class AdaPWM
    {
        // Serial is Pca9685
        private readonly I2CDevice _pca9685;
        private double _actualFrequency;
        private readonly double _maxFrequency = 1000;
        private readonly double _minFrequency = 40;
        private readonly int _pinCount = 16;

        /// <summary>
        /// Adafruit 12bit, 16 channel, I2C PWM controller.
        /// Adapted from the Adafruit library for the Arduino
        /// </summary>
        internal AdaPWM()
        {
            _pca9685 = new I2CDevice();
        }

        internal async Task InitializeAsync()
        {
            await _pca9685.Initialize();


            Reset();
            //else
            //{
            //    Debug.WriteLine("Could not find Pca9685");
            //    return;
            //}
            //Self test

            //TestPins();

            
        }

        public void TestPins()
        {
            for (byte pwmnum = 8; pwmnum < 10; pwmnum++)
            {

                for (ushort i = 4096; i > 0; i -= 4)
                {
                    SetPin(pwmnum, i);
                }
            }
            ushort number = 2500;
            //SetAllPwm(number, 0);
        }

        /// <summary>
        /// toggles the pulse for a given pin; note: to completely shut off an LED, send in a value of 4096
        /// </summary>
        /// <param name="pin">the pin between 0 and 15</param>
        /// <param name="dutyCycle">value between 0-4095; if maximum value exceeded, 4095 will be used</param>
        /// <param name="invertPolarity"></param>
        public void SetPin(int pin, double dutyCycle, bool invertPolarity = true)
        {
            // Clamp value between 0 and 4095 inclusive. 
            dutyCycle = Math.Min(dutyCycle, 4095);
            var value = (ushort)dutyCycle;
            var channel = (byte)pin;
            if (channel > _pinCount - 1)
                throw new ArgumentOutOfRangeException(nameof(channel), "Channel must be between 0 and 15");
            if (invertPolarity)
            {
                // Special value for signal fully on/off.
                switch (value)
                {
                    case 0:
                        SetPwm(channel, 4095, 0);
                        break;
                    case 4095:
                        SetPwm(channel, 0, 4095);
                        break;
                    case 4096:
                        SetPwm(channel, 0, 4096);
                        break;
                    default:
                        SetPwm(channel, 0, (ushort)(4095 - value));
                        break;
                }
            }
            else
            {
                // Special value for signal fully on/off. 
                switch (value)
                {
                    case 4095:
                        SetPwm(channel, 4095, 0);
                        break;
                    case 4096:
                        SetPwm(channel, 4096, 0);
                        break;
                    case 0:
                        SetPwm(channel, 0, 4095);
                        break;
                    default:
                        SetPwm(channel, 0, value);
                        break;
                }
            }
        }

        /// <summary>
        /// toggles the pulse for all pins; note: to completely shut off an LED, send in a value of 4096
        /// </summary>
        /// <param name="value">value between 0-4095; if maximum value exceeded, 4095 will be used</param>
        /// <param name="invertPolarity"></param>
        public void SetPulseParameters(double value, bool invertPolarity = false)
        {
            // Clamp value between 0 and 4095 inclusive. 
            value = Math.Min(value, 4095);
            if (invertPolarity)
            {
                // Special value for signal fully on.
                switch ((ushort)value)
                {
                    case 0:
                        SetAllPwm(4095, 0);
                        break;
                    case 4095:
                        SetAllPwm(0, 4095);
                        break;
                    case 4096:
                        SetAllPwm(0, 4096);
                        break;
                    default:
                        SetAllPwm(0, (ushort)(4095 - value));
                        break;
                }
            }
            else
            {
                // Special value for signal fully on. 
                switch ((ushort)value)
                {
                    case 4095:
                        SetAllPwm(4095, 0);
                        break;
                    case 4096:
                        SetAllPwm(4096, 0);
                        break;
                    case 0:
                        SetAllPwm(0, 4095);
                        break;
                    default:
                        SetAllPwm(0, (ushort)value);
                        break;
                }
            }
        }

        public static double Map(double x, double inMin, double inMax, double outMin, double outMax)
        {
            var r = (x - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
            return r;
        }

        double mapMin = -1, mapMax = 20;
        public void SetServoAngle(int servoNumber, double newAngle)
        {
            var servoAngle = Math.Round(Map(newAngle, -180, 180, 3820, 3720), 1);

            Debug.WriteLine($"{servoNumber} - {servoAngle}");
            
            SetPin(servoNumber, servoAngle);
        }

        internal void SetPwm(byte channel, ushort on, ushort off)
        {
            _pca9685.Write((byte)(Registers.LED0_ON_L + 4 * channel), (byte)(on & 0xFF));
            _pca9685.Write((byte)(Registers.LED0_ON_H + 4 * channel), (byte)(on >> 8));
            _pca9685.Write((byte)(Registers.LED0_OFF_L + 4 * channel), (byte)(off & 0xFF));
            _pca9685.Write((byte)(Registers.LED0_OFF_H + 4 * channel), (byte)(off >> 8));
        }

        internal void SetAllPwm(ushort on, ushort off)
        {
            //_pca9685.Write((byte)Registers.ALL_LED_ON_L, (byte)(on & 0xFF));
            //_pca9685.Write((byte)Registers.ALL_LED_ON_H, (byte)(on >> 8));
            //_pca9685.Write((byte)Registers.ALL_LED_OFF_L, (byte)(off & 0xFF));
            //_pca9685.Write((byte)Registers.ALL_LED_OFF_H, (byte)(off >> 8));
        }

        internal async Task Reset()
        {
            _pca9685.Write((byte)Registers.MODE1, 0x0); // reset the device
            Debug.WriteLine($"PCA9685 Frequency {SetDesiredFrequency(53)}");

            //Center all servos
            SetPin(0, 3800);
            await Task.Delay(20);
            SetPin(1, 3800);
            await Task.Delay(20);
            SetPin(2, 3800);
            await Task.Delay(20);
            SetPin(3, 3800);
            await Task.Delay(20);
            SetPin(4, 3800);
            await Task.Delay(20);
            SetPin(5, 3800);
            await Task.Delay(20);
            SetPin(6, 3800);
            await Task.Delay(20);
            SetPin(7, 3800);
            await Task.Delay(20);
            SetPin(8, 3800);
            await Task.Delay(20);
        }

        internal double SetDesiredFrequency(double frequency)
        {
            frequency = Math.Min(frequency, _maxFrequency);
            frequency = Math.Max(frequency, _minFrequency);

            frequency *= 0.9f;  // Correct for overshoot in the frequency setting (see issue #11).
            var prescaleval = 25000000d;
            prescaleval /= 4096;
            prescaleval /= frequency;
            prescaleval -= 1;
            var prescale = (byte)Math.Floor(prescaleval + 0.5f);
            byte[] readBuffer;
            _pca9685.Read(1, (byte)Registers.MODE1, out readBuffer);
            var oldmode = readBuffer[0];
            var newmode = (byte)((oldmode & 0x7F) | 0x10); //sleep
            _pca9685.Write((byte)Registers.MODE1, newmode);
            _pca9685.Write((byte)Registers.PRESCALE, prescale);
            _pca9685.Write((byte)Registers.MODE1, oldmode);
            Task.Delay(5).Wait();
            _pca9685.Write((byte)Registers.MODE1, (byte)(oldmode | 0xa1));
            _actualFrequency = frequency;
            return _actualFrequency;
        }
    }

    public enum Registers
    {
        MODE1 = 0x00,
        MODE2 = 0x01,
        SUBADR1 = 0x02,
        SUBADR2 = 0x03,
        SUBADR3 = 0x04,
        PRESCALE = 0xFE,
        LED0_ON_L = 0x06,
        LED0_ON_H = 0x07,
        LED0_OFF_L = 0x08,
        LED0_OFF_H = 0x09,
        ALL_LED_ON_L = 0xFA,
        ALL_LED_ON_H = 0xFB,
        ALL_LED_OFF_L = 0xFC,
        ALL_LED_OFF_H = 0xFD
    }

    public enum Bits
    {
        RESTART = 0x80,
        SLEEP = 0x10,
        ALLCALL = 0x01,
        INVRT = 0x10,
        OUTDRV = 0x04
    }
}
