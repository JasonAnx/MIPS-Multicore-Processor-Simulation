using System;
using System.Threading;


namespace ProyectoArqui
{
    // Ctrl k + d     
    //        auto format (VS-only)
    class Computer
    {

        // atributes
        /* private atr */
        private static int clock, quantum;
        /* public atr */
        internal static Processor[] processors;
        public static UserInterface userInterface = new UserInterface();

        public static Barrier bsync;
        public const int block_size = 4;
        public const int p0_sharedmem_size = 16;
        public const int p1_sharedmem_size = 8;
        /************************** MAIN **************************/

        [STAThread]
        static void Main(string[] args)
        {
            OperatingSystem.log("Booting");
            processors = new Processor[2];
            processors[0] = new Processor( /*id*/ 0,/*n_cores*/ 2, /*instmem_size*/ 24, /*sharedmem_size*/ p0_sharedmem_size);
            processors[1] = new Processor( /*id*/ 1,/*n_cores*/ 1, /*instmem_size*/ 16, /*sharedmem_size*/ p1_sharedmem_size);

            OperatingSystem.log("There is a total of " + getGlobalCoreCount() + " cores in this virtual machine");

            // Ask user for quantum and slow mode
            OperatingSystem.userQuantum = userInterface.getUserQuantum();
            OperatingSystem.log("Quantum set to " + OperatingSystem.userQuantum);
            OperatingSystem.slowModeActivated = userInterface.getSlowModeActivated();

            // Sync Barrier
            bsync = new Barrier(getGlobalCoreCount(), (b) =>
            {
                clock++;
                quantum--;
                if (OperatingSystem.slowModeActivated)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("[Barrier Message]: Threads Syncronized");
                    Console.WriteLine("                   Press any key to continue");
                    Console.ResetColor();
                    Console.ReadLine();
                }
            });
            checkBarrierIntegrity();

            loadData();
            OperatingSystem.log("Starting Cores");
            execute();

            // link event on threads finish
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);

        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            OperatingSystem.log(" --- Results ---");
            foreach (Processor p in processors)
            {
                p.printArchivedContexts();
            }
            Console.ResetColor();
            OperatingSystem.log("Finished. Press any key to exit.");
            Console.ReadLine();
        }

        /************************** MAIN **************************/

        public static void checkBarrierIntegrity()
        {
            if (bsync == null) Environment.Exit(10);
            if (bsync.ParticipantCount != getGlobalCoreCount())
            {
                Environment.Exit(10);
            }
        }


        public static int getGlobalCoreCount()
        {
            int totalCores = 0;
            foreach (Processor p in processors)
            {
                totalCores += p.getCoreCount();
            }
            return totalCores;
        }

        public static void loadData()
        {
            OperatingSystem.allocateInstInMem();
        }

        public static void execute()
        {
            // inicia los hilos
            foreach (Processor p in processors)
            {
                p.start();
            }
        }

        public static bool tryBlockHomeDirectory(int dirBlock)
        {
            bool dirBlocked = false;
            if (dirBlock < p0_sharedmem_size)
            {
                //bloquear directorio de P0
                dirBlocked = true; /* si se logra*/
            }
            else
            {
                //bloquear directorio de P1
                dirBlocked = true; /* si se logra*/
            }
            return dirBlocked;
        }

        public static Processor.DirectoryProc getHomeDirectory(int dirBlock)
        {
            if (dirBlock < p0_sharedmem_size)
            {
                return processors[0].dir;
            }
            else
            {
                //bloquear directorio de P1
                return processors[1].dir;
            }
        }

    }

}