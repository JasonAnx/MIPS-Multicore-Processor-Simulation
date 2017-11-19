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

        public Core getCore(int i)
        {
            return cores[i];
        }

        // Intern Classes

        public class SharedMemory
        {
            private static Mutex mutex = new Mutex();
            static Bloque[] shMem;
            Processor parent;
            /* 
               Recordar que la memoria compartida de P0 es de 16 (0-15)
               y la de P1 es de 8 (16-23).  
            */
            public SharedMemory(int sizeMem, Processor prnt)
            {
                parent = prnt;
                shMem = new Bloque[sizeMem];
                for (int i = 0; i < sizeMem; i++)
                {
                    shMem[i] = new Bloque(Computer.block_size);
                }
            }

            public Bloque getBloque(int numBloque)
            {
                Bloque returnBloque = new Bloque(Computer.block_size);
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
                    Console.WriteLine("The Block " + numBloque + " does not belong to the shared memory of processor " + parent.id + ".");
                    returnBloque.generateErrorBloque();
                }
                return returnBloque;
            }

            public bool insertBloque(int numBloque, Bloque block)
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
            Bloque[] mem;
            public int lastBlock;
            public int lastInstr;

            //Constructor
            public InstructionMemory(int sizeMem)
            {
                mem = new Bloque[sizeMem];
                for (int i = 0; i < sizeMem; i++)
                {
                    mem[i] = new Bloque(4);
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

            public Bloque getBloque(int indexBloque)
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

            // Construye las dos matrices según la cantidad de bloques y caches ingresados
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

            public int getCacheWithBlock(int dirBloque)
            {
                int i = 0;
                while (i < n_caches) {
                    if (caches_matrix[dirBloque, i] == true) {
                        return i;
                    }
                }
                return -1;
            }
        }

        /// <summary>
        /// the Core of a Processor. 
        /// </summary>
        public partial class Core
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
                    catch (InvalidOperationException e)
                    {
                        log("\t\t>> Contxt queue empty: " + e.HelpLink);
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
            public struct InstructionCache
            {
                Bloque[] instrsInCache;
                int[] labelsOfInstrs;

                public InstructionCache(int cacheSize)
                {
                    instrsInCache = new Bloque[cacheSize];
                    labelsOfInstrs = new int[cacheSize];
                    /* 
                       Inicializa las 4 Instrucciones (16 enteros) con 0s.
                       Inicializa las etiquetas de las 4 Instrucciones en direcciones no existentes (-1).
                     */
                    for (int i = 0; i < cacheSize; i++)
                    {
                        instrsInCache[i] = new Bloque(Computer.block_size);
                        //llena todos las instrucciones de los bloques con -1
                        instrsInCache[i].generateErrorBloque();
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
                    Bloque blk = c.parent.isntrmem.getBloque(dirBloque);

                    int dirBloqueCache = dirBloque % 4;
                    instrsInCache[dirBloqueCache] = blk;
                    labelsOfInstrs[dirBloqueCache] = dirBloque;

                    //instrsInCache
                    //c.parent.isntrmem.
                }
            }





            public class DataCache
            {
                enum states { shared, invalid, modified }
                Bloque[] wordsInCache;
                int[] labelsOfWords;
                states[] statesOfWords;

                public DataCache(int cacheSize)
                {
                    wordsInCache = new Bloque[cacheSize];
                    labelsOfWords = new int[cacheSize];
                    statesOfWords = new states[cacheSize];
                    /* 
                       Inicializa los 4 Bloques con 0s.
                       Inicializa los estados en Invalidos (I).
                       Inicializa las etiquetas de los 4 Bloques en direcciones no existentes (-1).
                     */
                    for (int i = 0; i < cacheSize; i++)
                    {
                        wordsInCache[i] = new Bloque(Computer.block_size);
                        statesOfWords[i] = states.invalid;
                        labelsOfWords[i] = -1;
                    }
                } // EO constructor


                public Instruction fetchData(int program_counter, Core c)
                {
                    int dirBloque = program_counter / (Computer.block_size * 4);
                    int dirBloqueCache = dirBloque % 4;
                    int dirPalabra = program_counter % (Computer.block_size * 4) / wordsInCache.Length;

                    // TODO: Request exclusive access to cache (block cache)
                    // cambiar nombre de inst in cache
                    lock (this.wordsInCache)
                    {
                        // Area critica
                        if (dirBloqueCache > labelsOfWords.Length || dirBloqueCache < 0)
                        {
                            c.log("Error: wrong block direction : " + dirBloqueCache);
                            Environment.Exit(33);
                        }

                        if (labelsOfWords[dirBloqueCache] == dirBloque) // hit
                        {
                            return wordsInCache[dirBloqueCache].word[dirPalabra];
                        }
                        else // miss
                        {
                            miss(program_counter, c);
                            return wordsInCache[dirBloqueCache].word[dirPalabra];
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
                            c.parent.shrmem.insertBloque(dirBloque, wordsInCache[dirBloqueCache]);
                            Computer.getHomeDirectory(dirBloque).setCacheMatrixToFalse(dirBloque);
                            Computer.getHomeDirectory(dirBloque).setUState(dirBloque);
                        }
                    }
                    // Allocate
                    lock (Computer.getHomeDirectory(dirBloque)) {
                        /*Se supone que esto devuelve el indice de la matriz de bools donde hay un true
                         Esto, para poder acceder a la cache
                         */
                        int numCache = Computer.getHomeDirectory(dirBloque).getCacheWithBlock(dirBloque);
                        /*Se supoe que esto revisa si el bloque está M es esa cache*/
                        if (c.parent.getCore(numCache).dataCache.statesOfWords[dirBloqueCache] == states.modified) {
                            /*Bloquea la cache de datos que tenga el bloque*/
                            lock (c.parent.getCore(numCache).dataCache) {
                                /*Guarda el bloque desde la cache bloqueda a la cache compartida*/
                                c.parent.shrmem.insertBloque(dirBloque, c.parent.getCore(numCache).dataCache.wordsInCache[dirBloqueCache]);
                                /*Cambia el estado de bloque guardado a shared en la cache compartida*/
                                c.parent.getCore(numCache).dataCache.statesOfWords[dirBloqueCache] = states.shared;
                            }
                        }
                        /*guarda en MI cache el bloque desde memoria compartida*/
                        wordsInCache[dirBloqueCache] = c.parent.shrmem.getBloque(dirBloque);
                    }


                    ////// codigo viejo
                    /*
                    int dirBloque = program_counter / (Computer.block_size * 4);
                    Bloque blk = c.parent.isntrmem.getBloque(dirBloque);

                    int dirBloqueCache = dirBloque % 4;
                    instrsInCache[dirBloqueCache] = blk;
                    labelsOfInstrs[dirBloqueCache] = dirBloque;

                    //instrsInCache
                    //c.parent.isntrmem.
                    */
                }

            }

        }
        //Methods
    }
}