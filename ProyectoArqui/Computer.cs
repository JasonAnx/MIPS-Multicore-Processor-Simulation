using System;
using System.IO;
using System.Threading;

namespace ProyectoArqui {


    // Ctrl k + d     
    //        auto format (VS-only)
    class Computer {

        // atributes
        /* private atr */
        private static int clock, quantum;
        private static OperatingSystem OS = new OperatingSystem();
        /* public atr */
        public static Processor[] processors;
        public static Barrier bsync;

        [STAThread]
        static void Main(string[] args) {
            OperatingSystem.log("Started.");
            processors = new Processor[2];

            processors[0] = new Processor( /*id*/ 0,/*n_cores*/ 2, /*instmem_size*/ 24);
            processors[1] = new Processor( /*id*/ 1,/*n_cores*/ 1, /*instmem_size*/ 16);

            // Sync Barrier
            bsync = new Barrier(getGlobalCoreCount(), (b) => {
                clock++;
                quantum--;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[Barrier Message]: Threads Syncronized");
                Console.ResetColor();
            });
            checkBarrierIntegrity();

            Console.WriteLine("theres a total of " + getGlobalCoreCount() + " cores in this virtual machine");

            loadData();
            execute();
            var name = Console.ReadLine();
        }

        public static void checkBarrierIntegrity() {
            if (bsync == null) Environment.Exit(10);
            if (bsync.ParticipantCount != getGlobalCoreCount()) {
                Environment.Exit(10);
            }
        }


        public static int getGlobalCoreCount() {
            int totalCores = 0;
            foreach (Processor p in processors) {
                totalCores += p.cores.Length;
            }
            return totalCores;
        }

        public static void loadData() {
            OS.allocateInstInMem();
        }

        public static void execute() {
            // inicia los hilos
            foreach (Processor p in processors) {
                p.start();
            }
        }



    }

    class OperatingSystem {

        public void allocateInstInMem() {
            string filePath = "0.txt";
            try {
                string[] lines = File.ReadAllLines(filePath);
                for (int line = 0; line < lines.Length; line++) {
                    string[] instructionParts = lines[line].Split(' ');
                    Instruction inst = new Instruction(int.Parse(instructionParts[0]),
                                                        int.Parse(instructionParts[1]),
                                                        int.Parse(instructionParts[2]),
                                                        int.Parse(instructionParts[3]));
                    // ojo para el proc 1
                    Computer.processors[0].isntrmem.insertInstr(inst);
                }
                //Console.WriteLine(memoria.getBloque(5).word0.operation);
            }
            catch (FileNotFoundException e) {
                logError("File not found: " + filePath);
                logError("Could not load program");
                Environment.Exit(10);
                //Console.WriteLine("An error occurred: '{0}'", e);
            }
        }
        public static void log(string s) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[OS Message]: " + s);
            Console.ResetColor();
        }
        public static void logError(string s) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[OS Message]: " + s);
            Console.ResetColor();
        }
    }

    class Directory {

        // Construye las dos matrices según la cantidad de bloques y caches ingresados
        // Lleva dos matrices:
        // - Una es de dimensiones 2 x cantBloques, lleva en cada fila la etiqueta del bloque y su estado
        // - Otra es de dimensiones cantidadCaches x cantBloques, lleva en cada fila 
        public Directory(int n_blocks, int n_caches) {
            block_state_matrix = new string[2, n_blocks];
            caches_matrix = new Boolean[n_caches, n_blocks];
        }

        string[,] block_state_matrix;
        Boolean[,] caches_matrix;
    }
}