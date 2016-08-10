using System;
using System.Threading.Tasks;

namespace SnakeBot{
    internal class SnakeBotMotion
    {
        private XboxController _xBoxController;
        public AdaPWM ServoController { get; private set; }
        private const int A_DEFAULT = 200;//was 1300
        private const double B_DEFAULT = 3 * Math.PI;
        private const double SPEED_DEFAULT = 0.07; // was 0.05
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

            ServoController.SetDesiredFrequency(60);
        }

        internal async Task StartAsync()
        {
            System.Diagnostics.Stopwatch _sw = new System.Diagnostics.Stopwatch();
            _sw.Start();
            while (true)
            {
                if (_sw.ElapsedMilliseconds <= 20) continue;

                computeMotion();
            }
        }

        internal async Task computeMotion()
        {
            var angleVariation = 2; //was 3200 with original code
             
            RCservo[0] = (alpha * Math.Sin(t) + angleVariation + SERVO_ADJ[3] + gamma);
            RCservo[1] = (alpha * Math.Sin(t + 1 * beta) + angleVariation + SERVO_ADJ[4] + gamma);
            RCservo[2] = (alpha * Math.Sin(t + 2 * beta) + angleVariation + gamma + SERVO_ADJ[5]);
            RCservo[3] = (alpha * Math.Sin(t + 3 * beta) + angleVariation + gamma + SERVO_ADJ[6]);
            RCservo[4] = (alpha * Math.Sin(t + 4 * beta) + angleVariation + gamma + SERVO_ADJ[7]);
            RCservo[5] = (alpha * Math.Sin(t + 5 * beta) + angleVariation + gamma + SERVO_ADJ[8]);
            //RCservo[6] = (alpha * Math.Sin(t + 6 * beta) + 3200 + gamma + SERVO_ADJ[9]);

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

            ServoController.SetServoAngle(0, RCservo[0]);
            ServoController.SetServoAngle(1, RCservo[1]);
            ServoController.SetServoAngle(2, RCservo[2]);
            ServoController.SetServoAngle(3, RCservo[3]);
            ServoController.SetServoAngle(4, RCservo[4]);
        }

    }
}