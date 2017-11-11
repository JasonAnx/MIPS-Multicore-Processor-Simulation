using System;
using System.IO;


namespace ProyectoArqui {


    // Ctrl k + d     
    //        auto format (VS-only)
    class Computer {
        public static Processor[] processors;
        private static OperatingSystem OS = new OperatingSystem();

        [STAThread]
        static void Main(string[] args) {
            processors = new Processor[2];

            processors[0] = new Processor( /*id*/ 0,/*n_cores*/ 2, /*instmem_size*/ 24);
            processors[1] = new Processor( /*id*/ 1,/*n_cores*/ 1, /*instmem_size*/ 16);
            cargarDatos();
            execute();
            var name = Console.ReadLine();
        }

        public static void cargarDatos() {
            Console.Write("inicio\n");

            OS.allocateInstInMem();
        }

        public static void execute() {
            // inicia los hilos
            foreach (Processor p in processors) {
                // cambiar a thread por core, no por procsador
                p.start();
            }
            // barrera.SignalAndWait();
            Console.WriteLine("Simulacion finalizada. ");

        }



    }

    class OperatingSystem {

        public void allocateInstInMem() {
            string filePath = "0.txt";
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
    }

    class Directory
    {

        // Construye las dos matrices según la cantidad de bloques y caches ingresados
        // Lleva dos matrices:
        // - Una es de dimensiones 2 x cantBloques, lleva en cada fila la etiqueta del bloque y su estado
        // - Otra es de dimensiones cantidadCaches x cantBloques, lleva en cada fila 
        public Directory(int n_blocks, int n_caches)
        {
            block_state_matrix = new string[2, n_blocks];
            caches_matrix = new Boolean[n_caches, n_blocks];
        }

        string[,] block_state_matrix;
        Boolean[,] caches_matrix;
    }
}