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
        // number of blocks in a cache
        public const int cacheSize = 4;


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
            dir = new DirectoryProc(shrmem_size, numCaches, this);
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

            Console.WriteLine("Created Context [" + thread_id +
                "] on processor " + id + " with pc " +
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
                    shMem[i].word[0] = 1;
                    shMem[i].word[1] = 1;
                    shMem[i].word[2] = 1;
                    shMem[i].word[3] = 1;
                }
            }

            public Bloque<int> getBloque(int numBloque)
            {
                Bloque<int> returnBloque = new Bloque<int>(Computer.block_size);
                if (parent.id == 0 && numBloque < 16)
                {
                    returnBloque.setValue(shMem[numBloque]);
                }
                if (parent.id == 1 && numBloque >= 16)
                {
                    returnBloque.setValue(shMem[numBloque - 16]);

                }
                if ((parent.id == 0 && numBloque >= 16) || (parent.id == 1 && numBloque < 16))
                {
                    //Console.WriteLine("The Block " + numBloque + " does not belong to the shared memory of processor " + parent.id + ".");
                    returnBloque.word[0] = 1;
                    returnBloque.word[1] = 1;
                    returnBloque.word[2] = 1;
                    returnBloque.word[3] = 1;
                }
                return returnBloque;
            }

            public bool insertBloque(int numBloque, Bloque<int> block)
            {
                bool inserted = false;
                if (parent.id == 0 && numBloque < 16)
                {
                    shMem[numBloque].setValue(block);
                    inserted = true;
                }
                if (parent.id == 1 && numBloque >= 16)
                {
                    shMem[numBloque - 16].setValue(block);
                    inserted = true;
                }
                /*if ((parent.id == 0 && numBloque >= 16) || (parent.id == 1 && numBloque < 16))
                {
                    Console.WriteLine("The Block " + numBloque + " does not belong to the shared memory of processor " + parent.id + ".");
                }*/
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
            int n_caches;
            Processor parent;

            // Construye las dos matrices seg�n la cantidad de bloques y caches ingresados
            // Lleva dos matrices:
            // - Una es de dimensiones 2 x cantBloques, lleva en cada fila la etiqueta del bloque y su estado
            // - Otra es de dimensiones cantidadCaches x cantBloques, lleva en cada fila 
            public DirectoryProc(int n_blocks, int n_caches, Processor prnt)
            {
                parent = prnt;
                this.n_caches = n_caches;
                block_labels = new int[n_blocks];
                block_states = new dirStates[n_blocks];
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
            // Set specific cache row to false in caches matrix
            public void setCacheMatrixToFalse(int dirBloque)
            {
                for (int i = 0; i < n_caches; i++)
                {
                    caches_matrix[dirBloque, i] = false;
                }
            }
            // Set state to U
            public void setUState(int dirBloque)
            {
                block_states[dirBloque] = dirStates.U;
            }

            public Processor getParent() {
                return parent;
            }

            public int getIdOfCacheWithBlock(int dirBloque)
            {
                int i = 0;
                while (i < n_caches) {
                    if (caches_matrix[dirBloque, i] == true) {
                        return i;
                    }
                    i++;
                }
                return -1;
            }
            // Set specific matrix position to true
            public void setNStateTrue(int numCache, int dirBloque){
                caches_matrix[numCache, dirBloque] = true;
            }

            public bool isBlockOnAnotherCache(int dirBlock, int myCache)
            {
                bool isOnAnother = false;
                int i = 0;
                while (i < numCaches) {
                    if (i != myCache && caches_matrix[i, dirBlock] == true) {
                        isOnAnother = true;
                    }
                    i++;
                }
                return isOnAnother;
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
                instructionsCache = new InstructionCache();
                dataCache = new DataCache();
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
                Bloque<Instruction>[] blocks;
                int[] labelsOfInstrs;

                public InstructionCache()
                {
                    blocks = new Bloque<Instruction>[cacheSize];
                    labelsOfInstrs = new int[cacheSize];
                    /* 
                       Inicializa las 4 Instrucciones (16 enteros) con 0s.
                       Inicializa las etiquetas de las 4 Instrucciones en direcciones no existentes (-1).
                     */
                    for (int i = 0; i < cacheSize; i++)
                    {
                        blocks[i] = new Bloque<Instruction>(Computer.block_size);
                        //llena todos las instrucciones de los bloques con -1
                        //data[i].generateErrorBloque();
                        labelsOfInstrs[i] = -1;
                    }
                } // EO constr

                public Instruction fetchInstruction(int program_counter, Core c)
                {
                    int dirBloque = program_counter / (Computer.block_size * 4);
                    int dirBloqueCache = dirBloque % 4;
                    int dirPalabra = program_counter % (Computer.block_size * 4) / blocks.Length;

                    if (dirBloqueCache > labelsOfInstrs.Length || dirBloqueCache < 0)
                    {
                        c.log("Error: wrong block direction : " + dirBloqueCache);
                        Environment.Exit(33);
                    }

                    if (labelsOfInstrs[dirBloqueCache] == dirBloque)
                    {
                        return blocks[dirBloqueCache].word[dirPalabra];
                    }
                    else
                    {
                        miss(program_counter, c);
                        return blocks[dirBloqueCache].word[dirPalabra];
                    }
                }
                public void miss(int program_counter, Core c)
                {
                    int dirBloque = program_counter / (Computer.block_size * 4);
                    Bloque<Instruction> blk = c.parent.isntrmem.getBloque(dirBloque);

                    int dirBloqueCache = dirBloque % 4;
                    blocks[dirBloqueCache] = blk;
                    labelsOfInstrs[dirBloqueCache] = dirBloque;

                    //data
                    //c.parent.isntrmem.
                }
            }





            public class DataCache
            {
                enum states { shared, invalid, modified }
                Bloque<int>[] data;
                int[] labelsOfWords;
                states[] statesOfWords;

                public DataCache()
                {
                    data = new Bloque<int>[cacheSize];
                    labelsOfWords = new int[cacheSize];
                    statesOfWords = new states[cacheSize];
                    /* 
                       Inicializa los Bloques con 0s.
                       Inicializa los estados en Invalidos (I).
                       Inicializa las etiquetas de los 4 Bloques en direcciones no existentes (-1).
                     */
                    for (int i = 0; i < cacheSize; i++)
                    {
                        data[i] = new Bloque<int>(Computer.block_size);
                        statesOfWords[i] = states.invalid;
                        labelsOfWords[i] = -1;
                    }
                } // EO constructor


                public int fetchData(int program_counter, Core c)
                {
                    int dirBloque = program_counter / (Computer.block_size * 4);
                    int dirBloqueCache = dirBloque % 4;
                    int dirPalabra = program_counter % (Computer.block_size * 4) / data.Length;

                    // TODO: Request exclusive access to cache (block cache)
                    // cambiar nombre de inst in cache
                    lock (data)
                    {
                        // Area critica
                        if (dirBloqueCache > data.Length || dirBloqueCache < 0)
                        {
                            c.log("Error: wrong block direction : " + dirBloqueCache);
                            Environment.Exit(33);
                        }

                        if (labelsOfWords[dirBloqueCache] == dirBloque &&
                            statesOfWords[dirBloqueCache] != states.invalid
                            ) // hit
                        {
                            Console.WriteLine("this is hit");
                            return data[dirBloqueCache].word[dirPalabra];
                        }
                        else // miss
                        {
                            Console.WriteLine("this is miss");
                            miss(program_counter, c);
                            return data[dirBloqueCache].word[dirPalabra];
                        }
                    }
                    //Access to cache granted:
                }

                public void miss(int program_counter, Core c)
                {
                    int dirBloque = program_counter / (Computer.block_size * 4);
                    int dirBloqueCache = dirBloque % 4;

                    if (statesOfWords[dirBloqueCache] == states.modified ||
                       statesOfWords[dirBloqueCache] == states.shared)
                    {
                        // Block the home directory of the victim block
                        lock (Computer.getHomeDirectory(dirBloque))
                        {
                            //+40 or +16
                            c.parent.shrmem.insertBloque(dirBloque, data[dirBloqueCache]);
                            Computer.getHomeDirectory(dirBloqueCache).getCacheMatrix()[c._coreId, dirBloqueCache] = false;
                            //Si el bloque no esta en otra cache, pone en U el estado de ese bloque en el directorio
                            if (!Computer.getHomeDirectory(dirBloqueCache).isBlockOnAnotherCache(c._coreId, dirBloqueCache))
                            {
                                Computer.getHomeDirectory(dirBloqueCache).setUState(dirBloqueCache);
                            }
                            
                        }
                    }
                    // Allocate
                    lock (Computer.getHomeDirectory(dirBloqueCache)) {
                        /*Se supoe que esto revisa si el estado del bloque en el dir es M*/
                        if (Computer.getHomeDirectory(dirBloqueCache).getStates()[dirBloqueCache] == DirectoryProc.dirStates.M) {
                            int numCache = Computer.getHomeDirectory(dirBloqueCache).getIdOfCacheWithBlock(dirBloqueCache);
                            /*Bloquea la cache de datos que tenga el bloque*/
                            lock (c.parent.cores[numCache].dataCache) {
                                /*Guarda el bloque desde la cache bloqueda a la cache compartida*/
                                c.parent.shrmem.insertBloque(dirBloque, c.parent.cores[numCache].dataCache.data[dirBloqueCache]);
                                /*Cambia el estado de bloque guardado a shared en la cache compartida*/
                                c.parent.cores[numCache].dataCache.statesOfWords[dirBloqueCache] = states.shared;
                            }
                        }
                        /*guarda en mi cache el bloque desde memoria compartida*/
                        data[dirBloqueCache] = c.parent.shrmem.getBloque(dirBloque);
                        labelsOfWords[dirBloqueCache] = dirBloque;
                        statesOfWords[dirBloqueCache] = states.shared;
                        c.parent.dir.setNStateTrue(c._coreId, dirBloqueCache);
                    }
                }
                /*
                public int storeData(int program_counter, Core c)
                {
                    int dirBloque = program_counter / (Computer.block_size * 4);
                    int dirBloqueCache = dirBloque % 4;
                    int dirPalabra = program_counter % (Computer.block_size * 4) / data.Length;

                    lock (data)
                    {
                        // Area critica
                        if (dirBloqueCache > data.Length || dirBloqueCache < 0)
                        {
                            c.log("Error: wrong block direction : " + dirBloqueCache);
                            Environment.Exit(33);
                        }

                        if (labelsOfWords[dirBloqueCache] == dirBloque) // hit
                        {
                            //El bloque esta en estado M
                            if (statesOfWords[dirBloqueCache] == states.modified)
                            {
                                return data[dirBloqueCache].word[dirPalabra];
                            }
                            else {
                                //El bloque esta en estado C
                                if (statesOfWords[dirBloqueCache] == states.shared)
                                {
                                    //Invalidacion
                                    //Bloquea el directorio casa del bloque
                                    lock (Computer.getHomeDirectory(dirBloqueCache))
                                    {


                                    }

                                }
                            }
                            
                        }
                        else // miss
                        {
                            miss(program_counter, c);
                            return data[dirBloqueCache].word[dirPalabra];
                        }
                    }

                }*/

            }

        }
        //Methods
    }
}