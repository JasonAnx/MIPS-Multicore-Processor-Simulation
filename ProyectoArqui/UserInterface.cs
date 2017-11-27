using System;
namespace ProyectoArqui
{
    public class UserInterface
    {
        public UserInterface()
        {
        }
           
        // Initialize user interface
        public void init(){
            
            
        }

        // Ask for user quantum and return it

        public int getUserQuantum(){
            Console.WriteLine("Enter desired user quantum: ");
            int userQuantum = Convert.ToInt32(Console.ReadLine());
            return userQuantum;
        }

        // Ask if user wants to run simulation in slow mode

        public bool getSlowModeActivated(){
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
