using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace ProyectoArqui
{

    /// <summary>
    /// Processor class, containing the Core, SharedMemory and InstructionMemory classes.
    /// </summary>
    public partial class Processor
    {

        /// fields
        public InstructionMemory isntrmem;
        public SharedMemory shrmem;
        public DirectoryProc dir;

        public int id { get; } // externaly read-only

        private Core[] cores;

        public Queue<Context> contextQueue = new Queue<Context>();
        public Queue<Context> finishedTds = new Queue<Context>();


        public const int numCaches = 3;

        // constructor
        public Processor(int _id, int n_cores, int isntrmem_size, int shrmem_size)
        {
            id = _id;
            cores = new Core[n_cores];
            for (int i = 0; i < cores.Length; i++)
            {
                cores[i] = new Core(i, this);
            }
            isntrmem = new InstructionMemory(isntrmem_size);
            shrmem = new SharedMemory(shrmem_size, this);
            dir = new DirectoryProc(shrmem_size, numCaches);
        }

        public int getCoreCount()
        {
            return cores.Length;
        }

        public void archiveContext (Context ctx)
        {
            finishedTds.Enqueue(ctx);
        }

        public void printArchivedContexts()
        {
            string s = "";
            while (finishedTds.Count > 0)
            {
                Context ct =  finishedTds.Dequeue();
                s += ct.registersToString();
            }
            Console.WriteLine(s);
        }

        public void createContext(int ip, string thread_id)
        {
            Context currentContext = new Context(ip, thread_id);
            contextQueue.Enqueue(currentContext);
            Console.WriteLine("Created Context " + thread_id +
                " on processor " + id + " with pc " +
                currentContext.instruction_pointer
                );
        }

        //Methods
        public void start()
        {
            //Console.WriteLine("procesador " + id + "tiene " + cores.Length + "cores");
            foreach (Core c in cores)
            {
                Thread t = new Thread(new ThreadStart(c.start));
                t.Start();
            }
        }

        // Intern Classes

        public class SharedMemory
        {
            static Bloque<int>[] shMem;
            Processor parent;
            /* 
               Recordar que la memoria compartida de P0 es de 16 (0-15)
               y la de P1 es de 8 (16-23).  
            */
            public SharedMemory(int sizeMem, Processor prnt)
            {
                parent = prnt;
                shMem = new Bloque<int>[sizeMem];
                for (int i = 0; i < sizeMem; i++)
                {
                    shMem[i] = new Bloque<int>(Computer.block_size);
                }
            }

            public Bloque<int> getBloque(int numBloque)
            {
                Console.WriteLine("Fix this method");
                Environment.Exit(67);
                Bloque<int> returnBloque = new Bloque<int>(Computer.block_size);
                if (parent.id == 0 && numBloque < 16)
                {
                    //returnBloque.setValue(shMem[numBloque]);
                }
                if (parent.id == 1 && numBloque >= 16)
                {
                    //returnBloque.setValue(shMem[numBloque - 16]);

                }
                if ((parent.id == 0 && numBloque >= 16) || (parent.id == 1 && numBloque < 16))
                {
                    Console.WriteLine("The Block " + numBloque + " does not belong to the shared memory of processor " + parent.id + ".");
                    //returnBloque.generateErrorBloque();
                }
                return returnBloque;
            }

            public bool insertBloque(int numBloque, Bloque<int> block)
            {
                Console.WriteLine("Fix this method");
                Environment.Exit(68);
                bool inserted = false;
                if (parent.id == 0 && numBloque < 16)
                {
                    //shMem[numBloque].setValue(block);
                    inserted = true;
                }
                if (parent.id == 1 && numBloque >= 16)
                {
                    //shMem[numBloque - 16].setValue(block);
                    inserted = true;
                }
                if ((parent.id == 0 && numBloque >= 16) || (parent.id == 1 && numBloque < 16))
                {
                    Console.WriteLine("The Block " + numBloque + " does not belong to the shared memory of processor " + parent.id + ".");
                }
                return inserted;

            }
        }

        public class InstructionMemory
        {
            //Attributes
            Bloque<Instruction>[] mem;
            public int lastBlock;
            public int lastInstr;

            //Constructor
            public InstructionMemory(int sizeMem)
            {
                mem = new Bloque<Instruction>[sizeMem];
                for (int i = 0; i < sizeMem; i++)
                {
                    mem[i] = new Bloque<Instruction>(4);
                }
                lastBlock = 0;
                lastInstr = 0;
            }

            //Methods
            public void insertInstr(Instruction instr)
            {
                if (lastInstr > 3)
                {
                    lastInstr = 0;
                    lastBlock++;
                    if (lastBlock > mem.Length)
                        Environment.Exit(0);
                }
                mem[lastBlock].word[lastInstr].setValue(instr);
                lastInstr++;
            }

            public int getLength()
            {
                return mem.Length;
            }

            public Bloque<Instruction> getBloque(int indexBloque)
            {
                // TODO ask
                lock (mem)
                {
                    return mem[indexBloque];
                }
            }


        }

        public class DirectoryProc
        {
            public enum dirStates { C, M, U }
            int[] block_labels;
            dirStates[] block_states;
            string[,] block_state_matrix;
            Boolean[,] caches_matrix;
            // Construye las dos matrices según la cantidad de bloques y caches ingresados
            // Lleva dos matrices:
            // - Una es de dimensiones 2 x cantBloques, lleva en cada fila la etiqueta del bloque y su estado
            // - Otra es de dimensiones cantidadCaches x cantBloques, lleva en cada fila 
            public DirectoryProc(int n_blocks, int n_caches)
            {
                block_labels = new int[n_blocks];
                block_states = new dirStates[n_blocks];
                block_state_matrix = new string[2, n_blocks];
                caches_matrix = new Boolean[n_caches, n_blocks];
            }

            public int[] getLabels()
            {
                return block_labels;
            }

            public dirStates[] getStates()
            {
                return block_states;
            }

            public Boolean[,] getCacheMatrix()
            {
                return caches_matrix;
            }

        }

        /// <summary>
        /// the Core of a Processor. 
        /// </summary>
        private partial class Core
        {
            // we used a partial class to define the class methods on another
            // file and so, keep this file shorter and more readable


            public int[] registers;

            public Core(int _id, Processor prnt)
            {
                _coreId = _id;
                parent = prnt;
                instructionsCache = new InstructionCache(4);
                dataCache = new DataCache(4);
            }

            // Loads last Context in Context queue

            public bool loadContext()
            {
                // Loads last Context in Queue
                lock (parent.contextQueue)
                {
                    try
                    {
                        Context loadedContext = (Context)parent.contextQueue.Dequeue();
                        // Only load register values for now
                        //int[] newRegisterValues = loadedContext.getRegisterValues();

                        this.currentContext = loadedContext;
                        registers = loadedContext.getRegisterValues();

                        //log("context loaded with ip " + loadedContext.instr_pointer);
                        //Console.WriteLine(
                        //    "Loaded Context " + loadedContext.id +
                        //    " on core " + this.getId() + " processor " + parent.id
                        //    );
                        return true;
                    }
                    catch (InvalidOperationException )
                    {
                        //log("\t\t>> Contxt queue empty");
                        //Console.ReadLine();
                        return false;
                    }
                }

            }

            // Saves current Context in contextQueue
            // Loads new Context

            public void contextSwitch()
            {

                // Store old Context

                saveCurrentContext();

                // Load new Context

                loadContext();
            }

            // 
            public class InstructionCache
            {
                Bloque<Instruction>[] instrsInCache;
                int[] labelsOfInstrs;

                public InstructionCache(int cacheSize)
                {
                    instrsInCache = new Bloque<Instruction>[cacheSize];
                    labelsOfInstrs = new int[cacheSize];
                    /* 
                       Inicializa las 4 Instrucciones (16 enteros) con 0s.
                       Inicializa las etiquetas de las 4 Instrucciones en direcciones no existentes (-1).
                     */
                    for (int i = 0; i < cacheSize; i++)
                    {
                        instrsInCache[i] = new Bloque<Instruction>(Computer.block_size);
                        //llena todos las instrucciones de los bloques con -1
                        //data[i].generateErrorBloque();
                        labelsOfInstrs[i] = -1;
                    }
                } // EO constr

                public Instruction fetchInstruction(int program_counter, Core c)
                {
                    int dirBloque = program_counter / (Computer.block_size * 4);
                    int dirBloqueCache = dirBloque % 4;
                    int dirPalabra = program_counter % (Computer.block_size * 4) / instrsInCache.Length;

                    if (dirBloqueCache > labelsOfInstrs.Length || dirBloqueCache < 0)
                    {
                        c.log("Error: wrong block direction : " + dirBloqueCache);
                        Environment.Exit(33);
                    }

                    if (labelsOfInstrs[dirBloqueCache] == dirBloque)
                    {
                        return instrsInCache[dirBloqueCache].word[dirPalabra];
                    }
                    else
                    {
                        miss(program_counter, c);
                        return instrsInCache[dirBloqueCache].word[dirPalabra];
                    }
                }
                public void miss(int program_counter, Core c)
                {
                    int dirBloque = program_counter / (Computer.block_size * 4);
                    Bloque<Instruction> blk = c.parent.isntrmem.getBloque(dirBloque);

                    int dirBloqueCache = dirBloque % 4;
                    instrsInCache[dirBloqueCache] = blk;
                    labelsOfInstrs[dirBloqueCache] = dirBloque;

                    //data
                    //c.parent.isntrmem.
                }
            }





            public class DataCache
            {
                enum states { shared, invalid, modified }
                Bloque<int>[] data;
                int[] labelsOfInstrs;
                states[] statesOfInstrs;

                public DataCache(int cacheSize)
                {
                    data = new Bloque<int>[cacheSize];
                    labelsOfInstrs = new int[cacheSize];
                    statesOfInstrs = new states[cacheSize];
                    /* 
                       Inicializa los 4 Bloques con 0s.
                       Inicializa los estados en Invalidos (I).
                       Inicializa las etiquetas de los 4 Bloques en direcciones no existentes (-1).
                     */
                    for (int i = 0; i < cacheSize; i++)
                    {
                        data[i] = new Bloque<int>(Computer.block_size);
                        statesOfInstrs[i] = states.invalid;
                        labelsOfInstrs[i] = -1;
                    }
                } // EO constructor

                public void allocate(int dirBloque, Core c)
                {
                    /*Se supone que en esa función se deberia bloquear el directorio primero*/
                    if (Computer.tryBlockHomeDirectory(dirBloque))
                    {
                        /* Se revisa el estado del bloque en el directorio*/
                        if (Computer.getHomeDirectory(dirBloque).getStates()[dirBloque] == DirectoryProc.dirStates.M)
                        {
                            /*Bloquear cache*/
                            /*Bloquear bus*/
                            /* Esto se supone que inserta el bloque en la memoria compartida*/
                            c.parent.shrmem.insertBloque(dirBloque, data[dirBloque]);
                            statesOfInstrs[dirBloque] = states.shared;
                            data[dirBloque] = c.parent.shrmem.getBloque(dirBloque);
                        }
                        else
                        {
                            /*En caso de que no este en estado M*/
                        }
                    }
                    else
                    {
                        /*En caso de que no pueda bloquear directorio*/
                    }
                }
            }

        }
        //Methods
    }
}