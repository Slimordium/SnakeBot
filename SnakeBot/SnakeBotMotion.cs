using System;
using System.Threading.Tasks;

namespace SnakeBot{
    internal class SnakeBotMotion
    {
        private XboxController _xBoxController;
        public AdaPWM ServoController { get; private set; }
        private const int A_DEFAULT = 200;//was 1300
        private const double B_DEFAULT = 3 * Math.PI;
        private const double SPEED_DEFAULT = 0.1; // was 0.05
        private const int C_DEFAULT = 0;

        //use volatile keyword to avoid problems with optimizer
        private double a = A_DEFAULT;
        private double b = B_DEFAULT;
        private double c = C_DEFAULT;

        private double alpha;
        private double gamma;
        private double beta;
        private double speed = 0;
        private double prev_speed = SPEED_DEFAULT;
        private double t = 0;
        private int num_segments = 8;
        private Double[] RCservo = new Double[10];
        private Double[] SERVO_ADJ = new Double[10];
        private SparkFunSerial16X2Lcd _display;

        public SnakeBotMotion(XboxController xBoxController, SparkFunSerial16X2Lcd display)
        {
            _xBoxController = xBoxController;
            _display = display;

            //load default values
            a = A_DEFAULT;
            b = B_DEFAULT;
            c = C_DEFAULT;
            gamma = -c / num_segments;
            beta = b / num_segments;
            alpha = a * Math.Abs(Math.Sin(beta));
            speed = 0;
        }

        internal async Task InitializeAsync()
        {
            //Connect / Create instance of the I2C / PWM device here. Follow patterns elsewhere
            ServoController = new AdaPWM();
            await ServoController.InitializeAsync();

            
        }

        internal async Task StartAsync()
        {
            await Task.Delay(5000);

            System.Diagnostics.Stopwatch _sw = new System.Diagnostics.Stopwatch();
            _sw.Start();

            while (true)
            {
                if (_sw.ElapsedMilliseconds <= 2)
                    continue;

                _sw.Restart();

                await computeMotion();
            }
        }

        internal async Task computeMotion()
        {
            RCservo[0] = (alpha * Math.Sin(t) + gamma);
            RCservo[1] = (alpha * Math.Sin(t + 1 * beta) + gamma);
            RCservo[2] = (alpha * Math.Sin(t + 2 * beta) + gamma);
            RCservo[3] = (alpha * Math.Sin(t + 3 * beta) + gamma);
            RCservo[4] = (alpha * Math.Sin(t + 4 * beta) + gamma);
            RCservo[5] = (alpha * Math.Sin(t + 5 * beta) + gamma);
            RCservo[6] = (alpha * Math.Sin(t + 6 * beta) + gamma);
            RCservo[7] = (alpha * Math.Sin(t + 7 * beta) + gamma);

            //increment speed
            speed = SPEED_DEFAULT;

            t += speed; //increment time, wrap around if necessary to prevent overflow
            if (t > 2 * Math.PI)
            {
                t = 0;
            }
            else if (t < 0)
            {
                t = 2 * Math.PI;
            }

            //await _display.WriteAsync("RCservo[0]: " + RCservo[0]);
            //await _display.WriteAsync("RCservo[1]: " + RCservo[1]);
            //await _display.WriteAsync("RCservo[2]: " + RCservo[2]);
            //await _display.WriteAsync("RCservo[3]: " + RCservo[3]);
            //await _display.WriteAsync("RCservo[4]: " + RCservo[4]);
            //await _display.WriteAsync("speed: " + speed);
            //await _display.WriteAsync("t: " + t);

            //ServoController.SetServoAngle(0, RCservo[0]);

            ServoController.SetServoAngle(0, RCservo[0]);
            ServoController.SetServoAngle(1, RCservo[1]);
            ServoController.SetServoAngle(2, RCservo[2]);
            ServoController.SetServoAngle(3, RCservo[3]);
            ServoController.SetServoAngle(4, RCservo[4]);
            ServoController.SetServoAngle(5, RCservo[5]);
            ServoController.SetServoAngle(6, RCservo[6]);
            ServoController.SetServoAngle(7, RCservo[7]);

        }

    }
}