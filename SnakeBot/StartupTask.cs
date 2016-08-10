using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace SnakeBot
{
    public sealed class StartupTask : IBackgroundTask
    {
        private static BackgroundTaskDeferral _deferral;

        internal static readonly SerialDeviceHelper SerialDeviceHelper = new SerialDeviceHelper();

        private SparkFunSerial16X2Lcd _display;
        private XboxController _xboxController;
        private Gps _gps;
        private NtripClient _ntripClient;
        private Navigator _navigator; 
        private SnakeBotController _snakeBotController;
        private AdaPWM _pwmController;
        private IoTClient _ioTClient;

        private readonly List<Task> _initializeTasks = new List<Task>();
        private readonly List<Task> _startTasks = new List<Task>();
        private SnakeBotMotion _snakeBotMotion;

        //TODO : Wire up "start switch" to GPIO pin. This is used on the starting line, needs to be in the AutonoceptorController class.
        //TODO : All the SerialDevices will need to be identified and associated with the piece of hardware they control. The Identifier (serial number) will be used to find the correct device 

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral(); //Doing this will ensure this BackgroundTask runs until all Tasks are complete. In our case, it will run forever since all the StartAsync tasks are while(true) {//do stuff;}

            await SerialDeviceHelper.ListAvailableSerialDevices(); //This is helpful in finding the identifier associate to each USB/Serial adapter. 

            _display = new SparkFunSerial16X2Lcd(); //LCD Display that will show useful debug messages
            //_ioTClient = new IoTClient(_display); //We send data to the Azure IoT hub with this. Yaw/Pitch/Roll/Accel/Speed/Heading/Range data/Lat/Lon
            _pwmController = new AdaPWM();
            _xboxController = new XboxController(_display); //For testing and saving waypoints to disk
            _snakeBotMotion = new SnakeBotMotion(_xboxController, _display);
            _snakeBotController = new SnakeBotController(_display, _pwmController, _xboxController); //The brains of the operation
            //_ntripClient = new NtripClient("10.0.0.14", 8000, "", "", "", _display); //The counter part to this, is the RtkGpsBase code. The RtkGpsBase code runs on a seperate PI and just serves up GPS correction data. Of course this IP will probably change.
            //_gps = new Gps(_display, _ntripClient); //!
            //_navigator = new Navigator(_snakeBotController, _display, _gps); //This works like the xbox controller, but automated

            //_initializeTasks.Add(_display.InitializeAsync());
            _initializeTasks.Add(_snakeBotController.InitializeAsync());
            //_initializeTasks.Add(_pwmController.InitializeAsync());
            _initializeTasks.Add(_snakeBotMotion.InitializeAsync());
            _initializeTasks.Add(_xboxController.InitializeAsync());
            //_initializeTasks.Add(_ntripClient.InitializeAsync());
            //_initializeTasks.Add(_gps.InitializeAsync());
            //_initializeTasks.Add(_ioTClient.InitializeAsync());
            //_initializeTasks.Add(_navigator.InitializeAsync()); //This is actually started/stopped via the AutonoceptorController

            //_startTasks.Add(_ioTClient.StartAsync());
            //_startTasks.Add(_ntripClient.StartAsync());
            //_startTasks.Add(_gps.StartAsync());
            //_startTasks.Add(_snakeBotController.StartAsync());
            _startTasks.Add(_snakeBotMotion.StartAsync());

            

            await Task.WhenAll(_initializeTasks.ToArray());

            await Task.WhenAll(_startTasks.ToArray()); //These tasks are all while(true) {} so they will never finish, allowing this background task to run forever.

        }
    }
}
