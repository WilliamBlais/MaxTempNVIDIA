// MaxTempNVIDIA by github.com/williamblais
// Based on TwistedMexi/CudaManager

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CUDA_Manager.NVext.Hardware.Nvidia;
using CUDA_Manager.NVext.Hardware;
using System.Threading;
namespace MaxTempNVIDIA
{
    class Program
    {
        private static int[] MaxTemperatureGPU = new int[25] { 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70, 70 };
        private static bool cancel = false;

        static void Main(string[] args)
        {
            for (int i = 0; i < args.Count(); i++)
            {
                MaxTemperatureGPU[i] = Int32.Parse(args[i]);
            }

            Program Program = new Program();
            Console.WriteLine("MaxTempNVIDIA has started. Ctrl-C to end");

            var workerThread = new Thread(() => Worker(Program));
            workerThread.Start();

            var autoResetEvent = new AutoResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                // cancel the cancellation to allow the program to shutdown cleanly
                eventArgs.Cancel = true;
                autoResetEvent.Set();
            };

            // main blocks here waiting for ctrl-C
            autoResetEvent.WaitOne();
            cancel = true;
            Console.WriteLine("Now shutting down");
            Program.restoreGPUAutoFan();


        }

        private static void Worker(Program Program)
        {
            while (!cancel)
            {
                Console.Clear();
                Console.WriteLine("MaxTempNVIDIA - Fan Speed Control Based On Max Temperature.");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("WARNING: This program sets your fan speed to manual mode, closing it without pressing Ctrl-C will lock your GPU's fan speed on the last value.");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine();
                Program.controlGPU();
                Thread.Sleep(5000);
            }
            Console.WriteLine("Worker thread ending");
        }


        private void restoreGPUAutoFan()
        {
            NvidiaGroup gpus = new NvidiaGroup();
            foreach (NvidiaGPU gpu in gpus.Hardware)
            {
                ISensor FanSensor = gpu.Sensors.Single(s => s.SensorType == SensorType.Control);
                FanController(FanSensor, true, 0);
            }
        }

        private void writeValue(float? Value, bool isTemperature)
        {
            string Extention = "";
            if (isTemperature)
            {
                Extention = "°C";
            }
            else
            {
                Extention = "%";
            }
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write(Value + Extention);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private void controlGPU()
        {
            NvidiaGroup gpus = new NvidiaGroup();
            foreach (NvidiaGPU gpu in gpus.Hardware)
            {

                ISensor TemperatureSensor = gpu.Sensors.Single(s => s.SensorType == SensorType.Temperature);
                ISensor FanSensor = gpu.Sensors.Single(s => s.SensorType == SensorType.Control);

                Console.Write(gpu.adapterIndex + " : " + gpu.Name);
                Console.Write(" | Max Temp Target: ");
                writeValue(MaxTemperatureGPU[gpu.adapterIndex], true);
                Console.Write(" | Current Temp: ");
                writeValue(TemperatureSensor.Value, true);
                Console.Write(" | Fan Speed: ");
                writeValue(FanSensor.Value, false);
                determineFanSpeed(FanSensor, TemperatureSensor, gpu.adapterIndex);
                Console.WriteLine();
            }
        }

        private void determineFanSpeed(ISensor FanSensor, ISensor TemperatureSensor, int adapterIndex)
        {

            int IdealFanSpeed = (int)(((TemperatureSensor.Value / (float)MaxTemperatureGPU[adapterIndex])) * 100);
            int CurrentFanSpeed = (int)FanSensor.Value;

            // If the difference between IdealFanSpeed and the current fan speed is greater than 5% exept between 95-100
            float Difference = Math.Abs(IdealFanSpeed - CurrentFanSpeed);
            Console.Write(" | IdealFanSpeed: ");
            writeValue(IdealFanSpeed, false);
            Console.Write(" | Difference: ");
            writeValue(Difference, false);

            if (Difference >= 5 || IdealFanSpeed >= 100 - 5)
            {
                FanController(FanSensor, false, IdealFanSpeed);
            }
        }


        private void FanController(ISensor fan, bool auto, int value)
        {
            if (auto)
                fan.Control.SetAuto();
            else if (value != 0)
            {
                if (fan.Control.MaxSoftwareValue < value)
                    value = (int)fan.Control.MaxSoftwareValue;
                else if (fan.Control.MinSoftwareValue > value)
                    value = (int)fan.Control.MinSoftwareValue;

                fan.Control.SetSoftware(value);
            }
            else
            {
                fan.Control.SetDefault();
            }
        }

    }
}