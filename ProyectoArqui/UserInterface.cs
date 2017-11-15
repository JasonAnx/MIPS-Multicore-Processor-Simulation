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
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("Enter desired user quantum: ");
            Console.ForegroundColor = ConsoleColor.Black;
            int userQuantum = Convert.ToInt32(Console.ReadLine());
            return userQuantum;
        }

        // Ask if user wants to run simulation in slow mode

        public bool getSlowModeActivated(){
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("Run simulation in slow mode? (Y/N): ");
            Console.ForegroundColor = ConsoleColor.Black;
            string slowMode = Console.ReadLine();
            bool userSlowMode = false; // Default value: 0
            if(slowMode == "Y" || slowMode == "N"){
                if (slowMode == "Y") { userSlowMode = true; }
                else { userSlowMode = false; }
            }
            return userSlowMode;
        }
    }
}
