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
        public Core[] cores;
        public int[] registers;

        // constructor
        public Processor(int _id, int n_cores, int isntrmem_size)
        {
            id = _id;
            cores = new Core[n_cores];
            isntrmem = new InstructionMemory(isntrmem_size);
            registers = new int[32];
        }

        //Methods
        public void start() {
            //Console.WriteLine("procesador " + id + "tiene " + cores.Length + "cores");
            for (int i = 0; i < cores.Length; i++) {
                cores[i] = new Core(i, this);
                // cambiar a thread por core, no por procsador
                Thread t = new Thread(new ThreadStart(cores[i].start));
                t.Start();
                //Console.WriteLine("creado core  " + i + " en procesador " + id);
            }
        }

        // Intern Classes
        public class Core {
            private int _coreId;
            Processor parent;
            public struct InstructionCache {
                Instruction[] instrsInCache;
                int[] labelsOfInstrs;

                public InstructionCache(int cacheSize) {
                    instrsInCache = new Instruction[cacheSize];
                    labelsOfInstrs = new int[cacheSize];
                    /* 
                       Inicializa las 4 Instrucciones (16 enteros) con 0s.
                       Inicializa las etiquetas de las 4 Instrucciones en direcciones no existentes (-1).
                     */
                    for (int i = 0; i < cacheSize; i++)
                    {
                        instrsInCache[i].operationCod = 0;
                        instrsInCache[i].argument1 = 0;
                        instrsInCache[i].argument2 = 0;
                        instrsInCache[i].argument3 = 0;
                        labelsOfInstrs[i] = -1;
                    }

                }
            }
        
            public struct DataCache {
                enum states { shared, invalid, modified }
                Instruction[] instrsInCache;
                int [] labelsOfInstrs;
                states [] statesOfInstrs;

                public DataCache(int cacheSize) {
                    instrsInCache = new Instruction[cacheSize];
                    labelsOfInstrs = new int[cacheSize];
                    statesOfInstrs = new states[cacheSize];
                    /* 
                       Inicializa las 4 Instrucciones (16 enteros) con 0s.
                       Inicializa los estados de las 4 Instrucciones en Invalidos (I).
                       Inicializa las etiquetas de las 4 Instrucciones en direcciones no existentes (-1).
                     */
                    for (int i = 0; i < cacheSize; i++)
                    {
                        instrsInCache[i].operationCod = 0;
                        instrsInCache[i].argument1    = 0;
                        instrsInCache[i].argument2    = 0;
                        instrsInCache[i].argument3    = 0;
                        statesOfInstrs[i] = states.invalid;
                        labelsOfInstrs[i] = -1;
                    }
                }

            }
            public Core(int _id, Processor prnt ) {
                _coreId = _id;
                parent = prnt;
            }
            public void start() {
                Console.WriteLine("hola desde nucleo  " + _coreId + "/"+ parent.cores.Length+  " en procesador " + parent.id);
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
                for (int i = 0; i < sizeMem; i++) {
                    mem[i] = new Bloque(4);
                }
                lastBlock = 0;
                lastInstr = 0;
            }

            //Methods
            public void insertInstr(Instruction instr) {
                if (lastInstr == 0) {
                    mem[lastBlock].word[0].setValue(instr);
                    lastInstr++;
                }
                if (lastInstr == 1) {
                    mem[lastBlock].word[1].setValue(instr);
                    lastInstr++;
                }
                if (lastInstr == 2) {
                    mem[lastBlock].word[2].setValue(instr);
                    lastInstr++;
                }
                if (lastInstr == 3) {
                    mem[lastBlock].word[3].setValue(instr);
                    lastInstr = 0;
                    if (lastBlock < mem.Length)
                        lastBlock++;
                    else
                        Environment.Exit(0);
                }

            }       
        /*

        public Bloque getBloque(int indexBloque) {
            return mem[indexBloque];

        }
        */

        }

        //Methods
    }
}