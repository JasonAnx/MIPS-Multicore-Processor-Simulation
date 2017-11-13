using System;
using System.IO;
using System.Threading;
using System.Collections;

namespace ProyectoArqui {

    /// <summary>
    /// Processor class, containing the Core, SharedMemory and InstructionMemory classes.
    /// </summary>
    public partial class Processor {

        /// fields
        public InstructionMemory isntrmem;
        public SharedMemory shrmem;

        public int id { get; } // externaly read-only

        private Core[] cores;

        public Queue contextQueue;

        // constructor
        public Processor(int _id, int n_cores, int isntrmem_size) {
            id = _id;
            cores = new Core[n_cores];
            for (int i = 0; i < cores.Length; i++) {
                cores[i] = new Core(i, this);
            }
            isntrmem = new InstructionMemory(isntrmem_size);
            //hardcodeado con un tamaño para probar hay que ponerlo como parametro
            shrmem = new SharedMemory(16);
        }

        public int getCoreCount() {
            return cores.Length;
        }

        //Methods
        public void start() {
            //Console.WriteLine("procesador " + id + "tiene " + cores.Length + "cores");
            foreach (Core c in cores) {
                Thread t = new Thread(new ThreadStart(c.start));
                t.Start();
            }
        }

        // Intern Classes

        public class SharedMemory {
            private static Mutex mutex = new Mutex();
            static Bloque[] shMem;
            /* 
               Recordar que la memoria compartida de P0 es de 16 (0-15)
               y la de P1 es de 8 (16-23).  
            */
            public SharedMemory(int sizeMem) {
                shMem = new Bloque[sizeMem];
                for (int i = 0; i < sizeMem; i++) {
                    shMem[i] = new Bloque(4);
                }
            }

            public Bloque getBloque(int numBloque, Processor proc) {
                Bloque returnBloque = new Bloque(4);
                if (proc.id == 0 && numBloque < 16) {
                    returnBloque.setValue(shMem[numBloque]);
                }
                if (proc.id == 1 && numBloque >= 16) {
                    returnBloque.setValue(shMem[numBloque - 16]);

                }
                if ((proc.id == 0 && numBloque >= 16) || (proc.id == 1 && numBloque < 16)) {
                    Console.WriteLine("The Block " + numBloque + " does not belong to the shared memory of processor " + proc.id + ".");
                    returnBloque.generateErrorBloque();
                }
                return returnBloque;
            }

            public bool insertBloque(int numBloque, Bloque block, Processor proc) {
                bool inserted = false;
                if (proc.id == 0 && numBloque < 16) {
                    //shMem[numBloque].setValue(block);
                    shMem[numBloque].setValue(block);
                    inserted = true;
                }
                if (proc.id == 1 && numBloque >= 16) {
                    shMem[numBloque - 16].setValue(block);
                    inserted = true;
                }
                if ((proc.id == 0 && numBloque >= 16) || (proc.id == 1 && numBloque < 16)) {
                    Console.WriteLine("The Block " + numBloque + " does not belong to the shared memory of processor " + proc.id + ".");
                }
                return inserted;

            }
        }

        public class InstructionMemory {
            //Attributes
            Bloque[] mem;
            public int lastBlock;
            public int lastInstr;

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
                if (lastInstr > 3) {
                    lastInstr = 0;
                    lastBlock++;
                    if (lastBlock > mem.Length)
                        Environment.Exit(0);
                }
                mem[lastBlock].word[lastInstr].setValue(instr);
                lastInstr++;
            }
            /*

            public Bloque getBloque(int indexBloque) {
                return mem[indexBloque];

            }
            */

        }

        /// <summary>
        /// the Core of a Processor. 
        /// </summary>
        public partial class Core {
            // we used a partial class to define the class methods on another
            // file and so, keep this file shorter and more readable
            private int _coreId;
            public int getId() { return _coreId; }
            Processor parent;
            InstructionCache instructionsCache;
            DataCache dataCache;

            public int[] registers;

            public Core(int _id, Processor prnt) {
                _coreId = _id;
                parent = prnt;
                registers = new int[32];
                instructionsCache = new InstructionCache(4);
                dataCache = new DataCache(4);
            }

            // Create a new Context struct, save current context and insert it in the contextQueue

            public void saveCurrentContext()
            {
                int currentThreadId = getId();
                int[] registerValues = registers;
                // Todav¨ªa esto no se mide
                float currentThreadExecutionTime = 0;
                bool threadIsFinalized = false;

                Context currentContext = new Context(currentThreadId, currentThreadExecutionTime, registerValues, threadIsFinalized);
                parent.contextQueue.Enqueue(currentContext);
            }

            // Loads last Context in Context queue

            public void loadNewContext()
            {
                // Loads last Context in Queue
                Context newContext = (Context)parent.contextQueue.Dequeue();
                // Only load register values for now
                int[] newRegisterValues = newContext.getRegisterValues();
                registers = newRegisterValues;
            }

            // Saves current Context in auxiliary variable, dequeues and loads last Context in contextQueue
            // Enqueues old Context in contextQueue

            public void contextSwitch(){

                // Store old Context

                saveCurrentContext();

                // Load new Context

                loadNewContext();
            }

            // 
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
                    for (int i = 0; i < cacheSize; i++) {
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
                int[] labelsOfInstrs;
                states[] statesOfInstrs;

                public DataCache(int cacheSize) {
                    instrsInCache = new Instruction[cacheSize];
                    labelsOfInstrs = new int[cacheSize];
                    statesOfInstrs = new states[cacheSize];
                    /* 
                       Inicializa las 4 Instrucciones (16 enteros) con 0s.
                       Inicializa los estados de las 4 Instrucciones en Invalidos (I).
                       Inicializa las etiquetas de las 4 Instrucciones en direcciones no existentes (-1).
                     */
                    for (int i = 0; i < cacheSize; i++) {
                        instrsInCache[i].operationCod = 0;
                        instrsInCache[i].argument1 = 0;
                        instrsInCache[i].argument2 = 0;
                        instrsInCache[i].argument3 = 0;
                        statesOfInstrs[i] = states.invalid;
                        labelsOfInstrs[i] = -1;
                    }
                } // EO constructor

                public Instruction fetchInstruction( /*params*/ ) {
                    // TODO
                    return new Instruction(); // remove
                }
            }

        }
        //Methods
    }




}