using System;
using System.IO;
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
        private static OperatingSystem OS = new OperatingSystem();
        /* public atr */
        public static UserInterface userInterface = new UserInterface();
        public static Processor[] processors;
        public static Barrier bsync;
        public const int block_size = 4;
        public const int p0_sharedmem_size = 16;
        public const int p1_sharedmem_size = 8;
        /************************** MAIN **************************/

        [STAThread]
        static void Main(string[] args)
        {
            OperatingSystem.log("Started.");
            // Ask user for quantum and slow mode
            OS.userQuantum = userInterface.getUserQuantum();
            OS.slowModeActivated = userInterface.getSlowModeActivated();

            processors = new Processor[2];

            processors[0] = new Processor( /*id*/ 0,/*n_cores*/ 2, /*instmem_size*/ 24, /*sharedmem_size*/ p0_sharedmem_size);
            processors[1] = new Processor( /*id*/ 1,/*n_cores*/ 1, /*instmem_size*/ 16, /*sharedmem_size*/ p1_sharedmem_size);

            // Sync Barrier
            bsync = new Barrier(getGlobalCoreCount(), (b) =>
            {
                clock++;
                quantum--;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[Barrier Message]: Threads Syncronized");
                Console.ResetColor();
            });
            checkBarrierIntegrity();

            OperatingSystem.log("There is a total of " + getGlobalCoreCount() + " cores in this virtual machine");

            loadData();
            execute();

            var name = Console.ReadLine();
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
            OS.allocateInstInMem();
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
            else {
                //bloquear directorio de P1
                dirBlocked = true; /* si se logra*/
            }
            return dirBlocked;
        }

        public static Processor.DirectoryProc getHomeDirectory(int dirBlock) {
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

    class OperatingSystem
    {
        public int userQuantum;
        public bool slowModeActivated;
        public void allocateInstInMem()
        {
            for (int numProcessor = 0; numProcessor < 2; numProcessor++)
            {
                string folderPath = "p" + numProcessor;
                string[] files = System.IO.Directory.GetFiles(folderPath);
                foreach (string filePath in files)
                {
                    try
                    {
                        Console.WriteLine(filePath);
                        string[] lines = File.ReadAllLines(filePath);
                        for (int line = 0; line < lines.Length; line++)
                        {
                            string[] instructionParts = lines[line].Split(' ');
                            Instruction inst = new Instruction(int.Parse(instructionParts[0]),
                                                                int.Parse(instructionParts[1]),
                                                                int.Parse(instructionParts[2]),
                                                                int.Parse(instructionParts[3]));
                            try
                            {
                                Computer.processors[numProcessor].isntrmem.insertInstr(inst);
                            }
                            catch
                            {
                                logError("Unable to insert instruction " + inst.printValue() +
                                          " on instruction memory of processor " + numProcessor);
                            }


                        }
                        //Console.WriteLine(memoria.getBloque(5).word0.operation);
                    }
                    catch (FileNotFoundException e)
                    {
                        logError("File not found: " + filePath);
                        logError("Could not load program");
                        Environment.Exit(11);
                        //Console.WriteLine("An error occurred: '{0}'", e);
                    }
                }
                //Console.WriteLine(memoria.getBloque(5).word0.operation);
            }
        }

        public static void log(string s)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[OS Message]: " + s);
            Console.ResetColor();
        }

        public static void logError(string s, bool halt = false)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[OS Message]: " + s);
            if (halt)
            {
                Console.WriteLine("\tProgram Halted. Press any key to exit");
                Console.ReadLine();
            }
            else Console.ResetColor();
        }
    }
}