using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

namespace SnakeBot
{

    /// <summary>
    /// Logic to control steering servo and speed controller
    /// 
    /// Perhaps have a Turn method that has a degrees parameter? As well as a speed method? 
    /// </summary>

    internal sealed class SnakeBotController
    {

        private SparkFunSerial16X2Lcd _display;

        private static SerialDevice _serialDevice;
        private AdaPWM _pwmController; //Controls steering and speed controller

        private DataReader _inputStream;
        private DataWriter _outputStream;

        private DataReader _arduinoDataReader; //IMU and ranging data

        private int _perimeterInInches;

        private double _leftInches;
        private double _centerInches;
        private double _rightInches;

        private double _yaw;
        private double _pitch;
        private double _roll;

        private double _accelX;
        private double _accelY;
        private double _accelZ;

        private SelectedFunction _selectedFunction;

        internal SnakeBotController(SparkFunSerial16X2Lcd display, AdaPWM pwmController, XboxController xboxController)
        {
            _display = display;
            _pwmController = pwmController;
        }

        internal async Task<bool> InitializeAsync()
        {
            _serialDevice = await StartupTask.SerialDeviceHelper.GetSerialDeviceAsync("BCM2836", 115200, new TimeSpan(0, 0, 0, 0, 25), new TimeSpan(0, 0, 0, 0, 25));

            if (_serialDevice == null)
                return false;

            _inputStream = new DataReader(_serialDevice.InputStream) { InputStreamOptions = InputStreamOptions.Partial };
            _outputStream = new DataWriter(_serialDevice.OutputStream);

            return true;
        }
        internal async Task StartAsync()
        {
            var stopwatch = new Stopwatch();
            var displayStopwatch = new Stopwatch();
            stopwatch.Start();
            displayStopwatch.Start();

            while (true)
            {
                if (_arduinoDataReader == null)
                {
                    continue;
                }

                byte startChar = 0x00;
                while (startChar != 0x0a)
                {
                    await _arduinoDataReader.LoadAsync(1).AsTask();
                    startChar = _arduinoDataReader.ReadByte();
                }

                var bytesRead = new List<byte> { 0x00 };

                while (bytesRead.Last() != 0x0d)
                {
                    await _arduinoDataReader.LoadAsync(1).AsTask();
                    bytesRead.Add(_arduinoDataReader.ReadByte());
                }

                try
                {
                    var pingData = Encoding.ASCII.GetString(bytesRead.ToArray());

                    pingData = pingData.Replace("\0", "").Replace("\r", "");

                    if (string.IsNullOrEmpty(pingData)) //#L3198
                        continue;

                    Parse(pingData);

                    if (stopwatch.ElapsedMilliseconds >= 40)
                    {
                        //var e = ImuEvent;
                        //e?.Invoke(null, new ImuDataEventArgs { Yaw = _yaw, Pitch = _pitch, Roll = _roll, AccelX = _accelX, AccelY = _accelY, AccelZ = _accelZ });

                        stopwatch.Restart();
                    }

                    if (_selectedFunction == SelectedFunction.DisplayPing && displayStopwatch.ElapsedMilliseconds >= 50)
                    {
                        await _display.WriteAsync($"{_leftInches} {_centerInches} {_rightInches}", 2);
                        displayStopwatch.Restart();
                    }

                    if (_selectedFunction == SelectedFunction.DisplayYPR && displayStopwatch.ElapsedMilliseconds >= 50)
                    {
                        await _display.WriteAsync($"{_yaw} {_pitch} {_roll}", 2);
                        displayStopwatch.Restart();
                    }

                    if (_selectedFunction == SelectedFunction.DisplayAccel && displayStopwatch.ElapsedMilliseconds >= 50)
                    {
                        await _display.WriteAsync($"{_accelY}", 2);
                        displayStopwatch.Restart();
                    }

                    if (_leftInches <= _perimeterInInches ||
                        _centerInches <= _perimeterInInches ||
                        _rightInches <= _perimeterInInches)
                    {
                        //_isCollisionEvent = true;

                        //_rangeDataEventArgs = new RangeDataEventArgs(_perimeterInInches, _leftInches, _centerInches, _rightInches);

                        //var e = RangingEvent;
                        //e?.Invoke(null, _rangeDataEventArgs);
                    }
                    else
                    {
                        //if (!_isCollisionEvent)
                        //    continue;

                        //_isCollisionEvent = false;

                        //_rangeDataEventArgs = new RangeDataEventArgs(_perimeterInInches, _leftInches, _centerInches, _rightInches);

                        //var e = RangingEvent;
                        //e?.Invoke(null, _rangeDataEventArgs);
                    }
                }
                catch
                {
                    //
                }
            }
        }

        internal void Parse(string data) //Probably a better way...
        {
            try
            {
                double ping;

                if (data.Contains("#YPR"))
                {
                    data = data.Replace("#YPR=", "");

                    var yprArray = data.Split(',');

                    if (yprArray.Length >= 1)
                    {
                        double.TryParse(yprArray[0], out _yaw);
                        _yaw = Math.Round(_yaw, 1);
                    }

                    if (yprArray.Length >= 2)
                    {
                        double.TryParse(yprArray[1], out _pitch);
                        _pitch = Math.Round(_pitch, 1);
                    }

                    if (yprArray.Length >= 3)
                    {
                        double.TryParse(yprArray[2], out _roll);
                        _roll = Math.Round(_roll, 1);
                    }

                    return;
                }

                if (data.Contains("#A-C="))
                {
                    var accelData = data.Replace("#A-C=", "").Split(',');

                    if (accelData.Length >= 1)
                    {
                        double.TryParse(accelData[0], out _accelX);
                        _accelX = Math.Round(_accelX, 1);
                    }

                    if (accelData.Length >= 2)
                    {
                        double.TryParse(accelData[1], out _accelY);
                        _accelY = Math.Round(_accelY, 1);
                    }

                    if (accelData.Length >= 3)
                    {
                        double.TryParse(accelData[2], out _accelZ);
                        _accelZ = Math.Round(_accelZ, 1);
                    }

                    return;
                }

                if (data.Contains("#L"))
                {
                    data = data.Replace("#L", "");

                    if (double.TryParse(data, out ping))
                        _leftInches = GetInchesFromPingDuration(ping);

                    return;
                }
                if (data.Contains("#C"))
                {
                    data = data.Replace("#C", "");

                    if (double.TryParse(data, out ping))
                        _centerInches = GetInchesFromPingDuration(ping);

                    return;
                }
                if (data.Contains("#R"))
                {
                    data = data.Replace("#R", "");

                    if (double.TryParse(data, out ping))
                        _rightInches = GetInchesFromPingDuration(ping);
                }
            }
            catch
            {
                //
            }
        }

        private static double GetInchesFromPingDuration(double duration) //73.746 microseconds per inch
        {
            return Math.Round(duration / 73.746 / 2, 1);
        }

    }
}