using System;
using System.IO;
using System.Threading;

namespace ProyectoArqui {

    /// <summary>
    /// Processor class, containing the Core, SharedMemory and InstructionMemory classes.
    /// </summary>
    public class Processor {

        /// fields
        public InstructionMemory isntrmem;
        public SharedMemory sharedMemory;
        private int id; // processor id
        public static Core[] cores;

        // constructor
        public Processor(int _id, int n_cores, int isntrmem_size) {
            id = _id;
            cores = new Core[n_cores];
            isntrmem = new InstructionMemory(isntrmem_size);
        }

        //Methods
        public void start() {
            for (int i = 0; i < cores.Length; i++) {
                cores[i] = new Core(i);
                // cambiar a thread por core, no por procsador
                Thread t = new Thread(new ThreadStart(cores[i].start));
                t.Start();
            }
        }

        // Intern Classes
        public class Core {
            private int _coreId;
            public struct InstructionCache {
            }
            public struct DataCache {
            }
            public Core(int _id) {
                _coreId = _id;
            }
            public void start() {
                Console.WriteLine("hola desde hilo  " + _coreId);
            }

        }
        public class SharedMemory { }

        public class InstructionMemory {
            //Attributes
            Bloque[] mem;
            int lastBlock;
            int lastInstr;

            //Constructor
            public InstructionMemory(int sizeMem) {
                mem = new Bloque[sizeMem];
                lastBlock = 0;
                lastInstr = 0;
            }

            //Methods
            public void insertInstr(Instruction instr) {
                if (lastInstr == 0) {
                    mem[lastBlock].word0.setValues(instr);
                    lastInstr++;
                }
                if (lastInstr == 1) {
                    mem[lastBlock].word1.setValues(instr);
                    lastInstr++;
                }
                if (lastInstr == 2) {
                    mem[lastBlock].word2.setValues(instr);
                    lastInstr++;
                }
                if (lastInstr == 3) {
                    mem[lastBlock].word3.setValues(instr);
                    lastInstr = 0;
                    if (lastBlock < mem.Length)
                        lastBlock++;
                    else
                        Environment.Exit(0);
                }

            }

            public Bloque getBloque(int indexBloque) {
                return mem[indexBloque];
            }

        }
    }
}