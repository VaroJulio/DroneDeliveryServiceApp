using DroneDeliveryLibrary.Core;
using System;
using System.IO;
using System.Threading;

namespace DroneDeliveryServiceApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var continueKey = "N";
                while (continueKey.ToUpper() == "N")
                {
                    StartApp(ref continueKey);
                }

                bool fileExist = File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "deliveries.txt"));

                if (fileExist)
                {
                    Console.WriteLine();
                    Console.WriteLine("File found.");
                    ProcessDroneAndLocationData();
                    Console.WriteLine($"The output file (deliveries_out_[date].txt) is in: {Directory.GetCurrentDirectory()}");
                    FinishExecution();
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("Input file (deliveries.txt) does not exist in the path previously mentioned.");
                    FinishExecution();
                }
            } 
            catch(Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("ERROR:");
                Console.WriteLine("*****************************************");
                Console.WriteLine($"An error occurs executing the app: {ex.Message}");
                Console.WriteLine("*****************************************");
                Console.WriteLine();
                Console.WriteLine("EXCEPTION:");
                Console.WriteLine("*****************************************");
                Console.WriteLine($"{ex}");
                Console.WriteLine("*****************************************");
                FinishExecution();
            }
        }

        private static void StartApp(ref string continueKey)
        {
            Console.Clear();
            Console.WriteLine("Drone Delivery Service");
            Console.WriteLine();
            Console.WriteLine($"Please, put the input file (deliveries.txt) into the path: {Directory.GetCurrentDirectory()}");
            Console.WriteLine();
            Console.Write("Are you ready to continue...(Y/N): ");
            continueKey = Console.ReadKey().KeyChar.ToString();
            if (continueKey.ToUpper() != "N" && continueKey.ToUpper() != "Y")
            {
                Console.Clear();
                Console.WriteLine("Invalid answer.");
                Thread.Sleep(2000);
                StartApp(ref continueKey);
            }
        }

        private static void ProcessDroneAndLocationData()
        {
            var core = new DroneDeliveriesCore();
            var calculedDeliveries = core.CalculateDeliveries().Result;
            core.SaveDeliveriesAsFile(calculedDeliveries).Wait();
        }

        private static void FinishExecution()
        {
            Console.WriteLine();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}


