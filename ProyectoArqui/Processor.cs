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

        public void archiveContext(Context ctx)
        {
            finishedTds.Enqueue(ctx);
        }

        public void printArchivedContexts()
        {
            string s = "";
            while (finishedTds.Count > 0)
            {
                Context ct = finishedTds.Dequeue();
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

        public void invalidate(int procId, int coreId, int dirBloqueCache, int dirBloque)
        {
            foreach (Core c in cores)
            {
                //Para que no se invalide a si mismo
                if (c.getParentId() != procId || c.getId() != coreId)
                {
                    //Revisa si el bloque está en cache
                    if (c.GetDataCache().labelsOfWords[dirBloqueCache] == dirBloque)
                    {
                        //Bloquea la cache 
                        lock (c.GetDataCache())
                        {
                            //Invalida cache
                            c.GetDataCache().statesOfWords[dirBloqueCache] = c.GetDataCache().setInvalid();
                        }
                    }
                    /*Si es el procesador 0 puede escribir en las filas 0 y 1 de los directorios
                      que representan las caches de los dos cores del proc*/
                    if (procId == 0)
                    {
                        dir.getCacheMatrix()[c.getId(), dirBloqueCache] = false;

                    }
                    /*Si el procesador 1 su core 0 esta represetado por la fila '2' del directorio*/
                    else
                    {
                        dir.getCacheMatrix()[2, dirBloqueCache] = false;
                    }
                    //Solo pone U en el dir del otro proc
                    if (c.getParentId() != procId)
                    {
                        dir.getStates()[dirBloqueCache] = DirectoryProc.dirStates.U;
                    }
                }
            }
        }

        /*Necesita recibir el id del proc y del core desde los cuales se está revisando para que no 
        se cuente a si mismo*/
        public bool isBlockOnAnotherCache(int procId, int coreId, int dirBloqueCache, int dirBloque)
        {
            bool isOnAnotherCache = false;
            int c = 0;
            while (c < getCoreCount() && !isOnAnotherCache)
            {
                //Para que no se cuente a si mismo
                if (cores[c].getParentId() != procId || cores[c].getId() != coreId)
                {
                    if (cores[c].GetDataCache().labelsOfWords[dirBloqueCache] == dirBloque)
                    {
                        isOnAnotherCache = true;
                    }
                }
                c++;
            }
            return isOnAnotherCache;
        }


        // Intern Classes

        public class SharedMemory
        {
            Bloque<int>[] shMem;
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
                    shMem[i].word[0] = 0;
                    shMem[i].word[1] = 0;
                    shMem[i].word[2] = 0;
                    shMem[i].word[3] = 0;
                }
            }

            public Bloque<int> getBloque(int numBloque)
            {
                
                if (numBloque < Computer.p0_sharedmem_size)
                {
                    return Computer.processors[0].shrmem.getShMem()[numBloque];
                }
               else
                {
                    return Computer.processors[1].shrmem.getShMem()[numBloque - Computer.p0_sharedmem_size];

                }
            }

            public Bloque<int>[] getShMem()
            {
                return shMem;
            }

            public bool insertBloque(int numBloque, Bloque<int> block)
            {
                bool inserted = false;
                if (numBloque < Computer.p0_sharedmem_size)
                {
                    Computer.processors[0].shrmem.getShMem()[numBloque].setValue(block);
                    inserted = true;
                }
                else
                {
                    Computer.processors[1].shrmem.getShMem()[numBloque - Computer.p0_sharedmem_size].setValue(block);
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
            public enum dirStates { U, S, M }
            dirStates[] block_states;
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
                block_states = new dirStates[n_blocks];
                caches_matrix = new Boolean[n_caches, n_blocks];
            }


            public dirStates[] getStates()
            {
                return block_states;
            }

            public dirStates getStateOfBlock(int dirBlock)
            {
                if (dirBlock < Computer.p0_sharedmem_size)
                {
                    return block_states[dirBlock];
                }
                else
                {
                    return block_states[dirBlock - Computer.p0_sharedmem_size];
                }

            }

            public string toString()
            {
                string s = "";
                for (int i = 0; i < block_states.Length; i++)
                {
                    s += i + ", " + block_states[i] + ", ";
                    for (int j = 0; j < n_caches; j++)
                    {
                        s += caches_matrix[j, i] + " ";
                    }
                    s += "\n";
                }
                return s;
            }

            public Boolean[,] getCacheMatrix()
            {
                return caches_matrix;
            }
 
            // Set state to U
            public void setUState(int dirBloque)
            {
                block_states[dirBloque] = dirStates.U;
            }

            public Processor getParent()
            {
                return parent;
            }

            public int getIdOfCacheWithBlock(int dirBloque)
            {
                int i = 0;
                while (i < n_caches)
                {
                    if (caches_matrix[dirBloque, i] == true)
                    {
                        return i;
                    }
                    i++;
                }
                return -1;
            }
            // Set specific matrix position to true
            public void setMatrixState(int procParentId, int numCache, int dirBloque, bool value)
            {
                if (procParentId == 1)
                    numCache = 2;

                if (dirBloque < Computer.p0_sharedmem_size)
                {
                    Computer.processors[0].dir.caches_matrix[numCache, dirBloque] = value;
                }
                else 
                {
                    Computer.processors[1].dir.caches_matrix[numCache, dirBloque - Computer.p0_sharedmem_size] = value;
                }
            }

            public bool isBlockOnAnotherCache(int myProc ,int myCache, int dirBlock)
            {
                bool isOnAnother = false;
                if (myProc == 1)
                {
                    myCache = 2;
                }
                int i = 0;
                while (i < numCaches)
                {
                    if (i != myCache && caches_matrix[i, dirBlock] == true)
                    {
                        isOnAnother = true;
                    }
                    i++;
                }
                return isOnAnother;
            }
            public void setState(int bloque, dirStates state)
            {
                if (bloque < Computer.p0_sharedmem_size)
                {
                    Computer.processors[0].dir.block_states[bloque] = state;
                }
                else
                {
                    bloque = bloque - Computer.p0_sharedmem_size;
                    Computer.processors[1].dir.block_states[bloque] = state;
                }
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
                    catch (InvalidOperationException)
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

            public DataCache GetDataCache()
            {
                return this.dataCache;
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
                public enum states { shared, invalid, modified }
                public Bloque<int>[] data;
                public int[] labelsOfWords;
                public states[] statesOfWords;

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

                public DataCache getDataCache(int i)
                {
                    if (i >= 2)
                    {
                        return Computer.processors[1].cores[0].dataCache;
                    }
                    else
                    {
                        return Computer.processors[0].cores[i].dataCache;
                    }
                }

                public DataCache GetDataCacheWithBlock(int procId, int coreId, int dirCache, int dirBloqueCache)
                {
                    DataCache cache = new DataCache();
                    for (int i = 0; i < cacheSize; i++)
                    {
                        cache.data[i].word[0] = 1;
                        cache.data[i].word[1] = 1;
                        cache.data[i].word[2] = 1;
                        cache.data[i].word[3] = 1;
                    }

                    if (Computer.processors[0].cores[0].dataCache.labelsOfWords[dirBloqueCache] == dirCache &&
                        (procId != 0 || coreId != 0))
                    {
                        return Computer.processors[0].cores[0].dataCache;
                    }
                    else
                    if (Computer.processors[0].cores[1].dataCache.labelsOfWords[dirBloqueCache] == dirCache &&
                        (procId != 0 || coreId != 1))
                    {
                        return Computer.processors[0].cores[0].dataCache;
                    }
                    else
                    if (Computer.processors[1].cores[0].dataCache.labelsOfWords[dirBloqueCache] == dirCache &&
                        (procId != 1 || coreId != 0))
                    {
                        return Computer.processors[0].cores[0].dataCache;
                    }
                    else
                    {
                        return cache;
                    }

                }

                public states setInvalid()
                {
                    return states.invalid;
                }

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
                            //Console.WriteLine("this is hit");
                            return data[dirBloqueCache].word[dirPalabra];
                        }
                        else // miss
                        {
                            //Console.WriteLine("this is miss");
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
                    // si solo esta compartirdo, se pone 0 en dir y u si no hay mas
                    {
                        // Block the home directory of the victim block
                        // 5 o 1
                        lock (Computer.getHomeDirectory(dirBloque))
                        {
                            //+40 or +16 (esta suma es cuando se inserta en memoria)
                            c.parent.shrmem.insertBloque(dirBloque, data[dirBloqueCache]);
                            if (statesOfWords[dirBloqueCache] == states.shared)
                            {
                                Computer.getHomeDirectory(dirBloque).setMatrixState(c.parent.id, c._coreId, dirBloque, false);
                            }
                            //Si el bloque no esta en otra cache, pone en U el estado de ese bloque en el directorio
                            if (!Computer.getHomeDirectory(dirBloqueCache).isBlockOnAnotherCache(c.parent.id, c._coreId, dirBloqueCache))
                            //if (!Computer.isBlockOnAnotherCache(c.parent.id, c._coreId, dirBloqueCache, dirBloque))
                            {
                                Computer.getHomeDirectory(dirBloque).setState(dirBloque, DirectoryProc.dirStates.U);
                            }

                        }
                    }
                    // Allocate
                    lock (Computer.getHomeDirectory(dirBloque))
                    {
                        /*Se supoe que esto revisa si el estado del bloque en el dir es M*/
                        if (Computer.getHomeDirectory(dirBloque).getStateOfBlock(dirBloque) == DirectoryProc.dirStates.M)
                        {
                            int numCache = Computer.getHomeDirectory(dirBloque).getIdOfCacheWithBlock(dirBloque);
                            /*Bloquea la cache de datos que tenga el bloque*/
                            //Hacer un metodo para bloquear esa cache
                            lock (GetDataCacheWithBlock(c.parent.id, c._coreId, dirBloque, dirBloqueCache))
                            {
                                /*Guarda el bloque desde la cache bloqueda a la cache compartida*/
                                c.parent.shrmem.insertBloque(dirBloque, GetDataCacheWithBlock(c.parent.id, c._coreId, dirBloque, dirBloqueCache).data[dirBloqueCache]);
                                /*Cambia el estado de bloque guardado a shared en la cache compartida*/
                                GetDataCacheWithBlock(c.parent.id, c._coreId, dirBloque, dirBloqueCache).statesOfWords[dirBloqueCache] = states.shared;
                            }
                        }
                        /*guarda en mi cache el bloque desde memoria compartida*/
                        data[dirBloqueCache] = c.parent.shrmem.getBloque(dirBloque);
                        labelsOfWords[dirBloqueCache] = dirBloque;
                        statesOfWords[dirBloqueCache] = states.shared;
                        c.parent.dir.setMatrixState(c.parent.id, c._coreId, dirBloque, true);
                        c.parent.dir.setState(dirBloque, DirectoryProc.dirStates.S);
                    }
                }

                public bool storeData(int program_counter, int dato, Core c)
                {
                    int dirBloque = program_counter / (Computer.block_size * 4);
                    int dirBloqueCache = dirBloque % 4;
                    int dirPalabra = program_counter % (Computer.block_size * 4) / data.Length;
                    bool stored = false;
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
                                //return data[dirBloqueCache].word[dirPalabra];
                                data[dirBloqueCache].word[dirPalabra] = dato;
                                stored = true;
                                return stored;
                            }
                            //El bloque esta en estado C
                            else if (statesOfWords[dirBloqueCache] == states.shared)
                            {
                                //Invalidacion
                                Computer.invalidateBlockInCache(c.parent.id, c._coreId, dirBloqueCache, dirBloque);
                                data[dirBloqueCache].word[dirPalabra] = dato;
                            }
                            stored = true;
                            return stored;

                        }
                        else // miss
                        {
                            missStore(program_counter, dato, c);
                            stored = true;
                            return stored;
                        }
                    }

                }

                public void missStore(int program_counter, int dato, Core c)
                {
                    int dirBloque = program_counter / (Computer.block_size * 4);
                    int dirBloqueCache = dirBloque % 4;
                    int dirPalabra = program_counter % (Computer.block_size * 4) / data.Length;

                    if (statesOfWords[dirBloqueCache] == states.modified ||
                       statesOfWords[dirBloqueCache] == states.shared)
                    // si solo esta compartirdo, se pone 0 en dir y u si no hay mas
                    {
                        // Block the home directory of the victim block
                        // 5 o 1
                        lock (Computer.getHomeDirectory(dirBloque))
                        {
                            //+40 or +16
                            c.parent.shrmem.insertBloque(dirBloque, data[dirBloqueCache]);
                            //pone false en la posicion de la cache en el directorio
                            Computer.getHomeDirectory(dirBloque).setMatrixState(c.parent.id, c._coreId, dirBloqueCache, false);
                            //Si el bloque no esta en otra cache, pone en U el estado de ese bloque en el directorio
                            if (!Computer.getHomeDirectory(dirBloqueCache).isBlockOnAnotherCache(c.parent.id, c._coreId, dirBloqueCache))
                            {
                                //solo debría hacerlo si esta compartido
                                Computer.getHomeDirectory(dirBloque).setState(dirBloque, DirectoryProc.dirStates.U);
                            }

                        }
                    }
                    // Allocate
                    lock (Computer.getHomeDirectory(dirBloque))
                    {
                        /*Se supoe que esto revisa si el estado del bloque en el dir es M*/
                        if (Computer.getHomeDirectory(dirBloque).getStateOfBlock(dirBloque) == DirectoryProc.dirStates.M ||
                            Computer.getHomeDirectory(dirBloque).getStateOfBlock(dirBloque) == DirectoryProc.dirStates.S)
                        {
                            /*Bloquea la cache de datos que tenga el bloque*/
                            lock (GetDataCacheWithBlock(c.parent.id, c._coreId, dirBloque, dirBloqueCache))
                            {
                                if (GetDataCacheWithBlock(c.parent.id, c._coreId, dirBloque, dirBloqueCache).statesOfWords[dirBloqueCache] == states.modified)
                                {
                                    /*Guarda el bloque desde la cache bloqueda a la mem compartida*/
                                    c.parent.shrmem.insertBloque(dirBloque, GetDataCacheWithBlock(c.parent.id, c._coreId, dirBloque, dirBloqueCache).data[dirBloqueCache]);
                                    /*Guarda el bloque en mi cache*/
                                    data[dirBloqueCache] = GetDataCacheWithBlock(c.parent.id, c._coreId, dirBloque, dirBloqueCache).data[dirBloqueCache];
                                    /*Bloque de la otra cache lo marca invalido*/
                                    GetDataCacheWithBlock(c.parent.id, c._coreId, dirBloque, dirBloqueCache).statesOfWords[dirBloqueCache] = states.shared;

                                    Computer.getHomeDirectory(dirBloque).setMatrixState(c.parent.id, c._coreId, dirBloqueCache, false);
                                    //Si el bloque no esta en otra cache, pone en U el estado de ese bloque en el directorio
                                    if (!Computer.getHomeDirectory(dirBloqueCache).isBlockOnAnotherCache(c.parent.id, c._coreId, dirBloqueCache))
                                    {
                                        //solo deberia hacerlo si esta compartido
                                        Computer.getHomeDirectory(dirBloque).setState(dirBloque, DirectoryProc.dirStates.U);
                                    }
                                }


                            }
                        }
                        /*guarda en mi cache el bloque desde memoria compartida*/
                        data[dirBloqueCache] = c.parent.shrmem.getBloque(dirBloqueCache);
                        labelsOfWords[dirBloqueCache] = dirBloque;
                        c.parent.dir.setMatrixState(c.parent.id, c._coreId, dirBloque, true);
                        c.parent.dir.setState(dirBloque, DirectoryProc.dirStates.S);
                    }
                }

            }

        }
        //Methods
    }
}