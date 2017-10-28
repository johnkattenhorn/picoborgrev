using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.Foundation.Diagnostics;

namespace PicoBorgRev
{
    public class ControllerMessageEventArgs : EventArgs
    {
        public string Message;
    }

    public class PicoBorgRev : IDisposable
    {
        private I2cConnectionSettings _settings;

        // Constant values
        private const byte PWM_MAX = 255;

        private const byte I2C_MAX_LEN = 4;
        private const byte I2C_ID_PICOBORG_REV = 0x15;
        private const byte DEFAULT_I2C_ADDRESS = 0x44;

        private const byte COMMAND_SET_LED = 1; // Set the LED status
        private const byte COMMAND_GET_LED = 2; // Get the LED status
        private const byte COMMAND_SET_A_FWD = 3; // Set motor 2 PWM rate in a forwards direction
        private const byte COMMAND_SET_A_REV = 4; // Set motor 2 PWM rate in a reverse direction
        private const byte COMMAND_GET_A = 5; // Get motor 2 direction and PWM rate
        private const byte COMMAND_SET_B_FWD = 6; // Set motor 1 PWM rate in a forwards direction
        private const byte COMMAND_SET_B_REV = 7; // Set motor 1 PWM rate in a reverse direction
        private const byte COMMAND_GET_B = 8; // Get motor 1 direction and PWM rate
        private const byte COMMAND_ALL_OFF = 9; // Switch everything off

        private const byte COMMAND_RESET_EPO = 10
            ; // Resets the EPO flag, use after EPO has been tripped and switch is now clear

        private const byte COMMAND_GET_EPO = 11; // Get the EPO latched flag

        private const byte COMMAND_SET_EPO_IGNORE = 12
            ; // Set the EPO ignored flag, allows the system to run without an EPO

        private const byte COMMAND_GET_EPO_IGNORE = 13; // Get the EPO ignored flag

        private const byte COMMAND_GET_DRIVE_FAULT = 14
            ; // Get the drive fault flag, indicates faults such as short-circuits and under voltage

        private const byte COMMAND_SET_ALL_FWD = 15; // Set all motors PWM rate in a forwards direction
        private const byte COMMAND_SET_ALL_REV = 16; // Set all motors PWM rate in a reverse direction

        private const byte COMMAND_SET_FAILSAFE = 17
            ; // Set the failsafe flag, turns the motors off if communication is interrupted

        private const byte COMMAND_GET_FAILSAFE = 18; // Get the failsafe flag
        private const byte COMMAND_SET_ENC_MODE = 19; // Set the board into encoder or speed mode
        private const byte COMMAND_GET_ENC_MODE = 20; // Get the boards current mode, encoder or speed
        private const byte COMMAND_MOVE_A_FWD = 21; // Move motor 2 forward by n encoder ticks
        private const byte COMMAND_MOVE_A_REV = 22; // Move motor 2 reverse by n encoder ticks
        private const byte COMMAND_MOVE_B_FWD = 23; // Move motor 1 forward by n encoder ticks
        private const byte COMMAND_MOVE_B_REV = 24; // Move motor 1 reverse by n encoder ticks
        private const byte COMMAND_MOVE_ALL_FWD = 25; // Move all motors forward by n encoder ticks
        private const byte COMMAND_MOVE_ALL_REV = 26; // Move all motors reverse by n encoder ticks
        private const byte COMMAND_GET_ENC_MOVING = 27; // Get the status of encoders moving
        private const byte COMMAND_SET_ENC_SPEED = 28; // Set the maximum PWM rate in encoder mode
        private const byte COMMAND_GET_ENC_SPEED = 29; // Get the maximum PWM rate in encoder mode
        private const byte COMMAND_GET_ID = 0x99; // Get the board identifier
        private const byte COMMAND_SET_I2C_ADD = 0xAA; // Set a new I2C address

        private const byte COMMAND_VALUE_FWD = 1; // I2C value representing forward
        private const byte COMMAND_VALUE_REV = 2; // I2C value representing reverse

        private const byte COMMAND_VALUE_ON = 1; // I2C value representing on
        private const byte COMMAND_VALUE_OFF = 0; // I2C value representing off

        public I2cDevice Controller { get; private set; }

        public delegate void ControllerMessageEventHandler(object sender, ControllerMessageEventArgs e);

        public event ControllerMessageEventHandler ControllerMessageReceived;
        public bool IsEnabled { get; private set; } = false;

        public async Task InitializeAsync(byte picoBorgRevDeviceId = DEFAULT_I2C_ADDRESS)
        {
            // Set the I2C address and speed
            _settings = new I2cConnectionSettings(picoBorgRevDeviceId)
            {
                BusSpeed = I2cBusSpeed.StandardMode
            };

            Debug.WriteLine("Initializing PicoBorgRev");
            OnControllerMessageReceived("Initializing PicoBorgRev");

            string aqs = I2cDevice.GetDeviceSelector();

            var dis = await DeviceInformation.FindAllAsync(aqs);
            Controller = await I2cDevice.FromIdAsync(dis.First().Id, _settings);

            if (Controller == null)
            {
                Debug.WriteLine("PicoBorgRev failed to initialize");
                OnControllerMessageReceived("PicoBorgRev failed to initialize");
            }

            Debug.WriteLine("PicoBorgRev successfully initialized");
            OnControllerMessageReceived("PicoBorgRev successfully initialized");

            IsEnabled = true;
        }

        public double GetMotor1()
        {
            byte[] recvBytes = new byte[I2C_MAX_LEN];
            byte[] sendBytes = new byte[] {COMMAND_GET_A};

            try
            {
                Debug.WriteLine("Reading Motor 1");
                OnControllerMessageReceived("Reading Motor 1");

                Controller.Write(sendBytes);
                Controller.Read(recvBytes);

                var power = BitConverter.ToDouble(recvBytes, 0);

                Debug.WriteLine($"Motor 1 Value: {power}");
                OnControllerMessageReceived($"Motor 1 Value: {power}");

                return power;
            }
            catch (Exception)
            {
                // Failed to read...
                Debug.WriteLine("Error reading Motor 1");
                OnControllerMessageReceived("Error reading Motor 1");

                return 0.0;
            }
        }

        public double GetMotor2()
        {
            byte[] recvBytes = new byte[I2C_MAX_LEN];
            byte[] sendBytes = new byte[] {COMMAND_GET_B};

            try
            {
                Debug.WriteLine("Reading Motor 2");
                OnControllerMessageReceived("Reading Motor 2");

                Controller.Write(sendBytes);
                Controller.Read(recvBytes);

                var power = BitConverter.ToDouble(recvBytes, 0);

                Debug.WriteLine($"Motor 2 Value: {power}");
                OnControllerMessageReceived($"Motor 2 Value: {power}");

                return power;
            }
            catch (Exception)
            {
                // Failed to read...
                Debug.WriteLine("Error reading Motor 2");
                OnControllerMessageReceived("Error reading Motor 2");

                return 0.0;
            }
        }

        public void SetMotor1(double power = 0)
        {
            try
            {
                byte command;
                int pwm;

                if (power < 0)
                {
                    command = COMMAND_SET_A_REV;

                    pwm = (int) ((PWM_MAX * power) * -1);

                    if (pwm > PWM_MAX)
                    {
                        pwm = PWM_MAX;
                    }
                }
                else
                {
                    command = COMMAND_SET_A_FWD;

                    pwm = (int) (PWM_MAX * power);

                    if (pwm > PWM_MAX)
                    {
                        pwm = PWM_MAX;
                    }
                }

                Debug.WriteLine($"Setting Motor 1 Power Value: {power}");
                OnControllerMessageReceived($"Setting Motor 1 Power Value: {power}");

                byte[] sendBytes = {command, (byte) pwm};

                Controller.Write(sendBytes);
            }
            catch (Exception)
            {
                // Failed to write...
                Debug.WriteLine("Error setting Motor1 Power");
                OnControllerMessageReceived("Error setting Motor1 Power");
            }
        }

        public void SetMotor2(double power = 0)
        {
            try
            {
                byte command;
                int pwm;

                if (power < 0)
                {
                    command = COMMAND_SET_B_REV;

                    pwm = (int) ((PWM_MAX * power) * -1);

                    if (pwm > PWM_MAX)
                    {
                        pwm = PWM_MAX;
                    }
                }
                else
                {
                    command = COMMAND_SET_B_FWD;

                    pwm = (int) (PWM_MAX * power);

                    if (pwm > PWM_MAX)
                    {
                        pwm = PWM_MAX;
                    }
                }

                Debug.WriteLine($"Setting Motor 2 Power Value: {power}");
                OnControllerMessageReceived($"Setting Motor 2 Power Value: {power}");

                byte[] sendBytes = {command, (byte) pwm};

                Controller.Write(sendBytes);
            }
            catch (Exception)
            {
                // Failed to write...
                Debug.WriteLine("Error setting Motor2 Power");
                OnControllerMessageReceived("Error setting Motor2 Power");
            }
        }

        protected virtual void OnControllerMessageReceived(string message)
        {
            Debug.WriteLine($"Controller Message : {message}");
            ControllerMessageReceived?.Invoke(this, new ControllerMessageEventArgs {Message = message});
        }

        public void Dispose()
        {
            Controller.Dispose();
            IsEnabled = false;
        }
    }
}