using System;
namespace ProyectoArqui
{
    public class UserInterface
    {
        // Ask for user quantum and return it

        public uint getUserQuantum()
        {
            Console.WriteLine("Enter desired quantum: ");
            try
            {
                uint userQuantum = Convert.ToUInt32(Console.ReadLine());
                return userQuantum;
            }
            catch (ArgumentException) { OperatingSystem.logError(" ArgumentException ", true); }
            catch (OverflowException) { OperatingSystem.logError(" Overflow: number is too big or negative", true); }
            catch (FormatException) { OperatingSystem.logError(" FormatException: could not convert input to a valid number ", true); }
            return 0;
        }

        // Ask if user wants to run simulation in slow mode

        public bool getSlowModeActivated()
        {
            string slowMode = "";
            bool userSlowMode = false; // Default value: 0
            while (slowMode != "Y" && slowMode != "y" && slowMode != "N" && slowMode != "n")
            {
                Console.WriteLine("Run simulation in slow mode? (Y/N): ");
                slowMode = Console.ReadLine();
                if (slowMode == "Y" || slowMode == "y")
                {
                    userSlowMode = true;
                }
            }
            return userSlowMode;
        }

        // Ask if user wants setup the ShMem with 0s or 1s
        public bool getValueForShMem() {
            string valueShMem = "";
            bool valueforShMem = false; // Default value: 0
            while (valueShMem != "Y" && valueShMem != "y" && valueShMem != "N" && valueShMem != "n")
            {
                Console.WriteLine("Shared Memory is initialized with 0's by default");
                Console.WriteLine("Initialize Shared Memory with 1's ?  (Y/N):");
                valueShMem = Console.ReadLine();
                if (valueShMem == "Y" || valueShMem == "y")
                {
                    valueforShMem = true;
                }
            }
            return valueforShMem;
        }
    }
}
