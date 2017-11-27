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
                s += ct.clockTicksToString();
            }
            Console.WriteLine(s);
        }

        public void printDataCaches()
        {
            string s = "";
            foreach (Core c in cores)
            {
                s += c.myDataCacheToString(c);
            }
            Console.WriteLine(s);
        }

        public void printSharedMem()
        {
            string s = "Shared Memory\nProcessor 0:\n";
            for (int i = 0; i < Computer.p0_sharedmem_size+Computer.p1_sharedmem_size; i++) {
                if (i == 16)
                { s += "\nProcessor 1:\n"; }
                if (i != 0 && i != 16 && i % 4 == 0)
                { s += "\n"; }
                s += "Block " + i + ": " + shrmem.getBloque(i).toString() + " ";
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
                // Bloquea la cache 
                lock (c.GetDataCache())
                {
                    //Revisa si el bloque está en cache
                    if (c.GetDataCache().labelsOfWords[dirBloqueCache] == dirBloque)
                    {
                        //Invalida el bloque en la cache
                        c.GetDataCache().statesOfWords[dirBloqueCache] = c.GetDataCache().Invalid();


                        DirectoryProc homedir = Computer.getHomeDirectory(dirBloque);
                        lock (homedir)
                        {
                            c.setMatrixState(c, dirBloque, false);
                            if (!c.isBlockOnAnotherCache(dirBloque))
                            {
                                //solo debría hacerlo si esta compartido
                                homedir.setState(dirBloque, DirectoryProc.dirStates.U);
                            }
                        }
                    }
                }
            }
        } // EO invalidate

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

            public int getShMemLenght()
            {
                return shMem.Length;
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

        /// <summary>
        /// the Core of a Processor. 
        /// </summary>
        private partial class Core
        {
            // we used a partial class to define the class methods on another
            // file and so, keep this file shorter and more readable

            int ticks;
            public int[] registers;

            public Core(int _id, Processor prnt)
            {
                _coreId = _id;
                parent = prnt;
                ticks = 0;
                instructionsCache = new InstructionCache();
                dataCache = new DataCache();
            }

            public void addTicksForAccessDir(int dirParentId)
            {
                //si estoy acceso directorio propio
                if (dirParentId == getParentId())
                    this.ticks += 1;
                //si acceso a un dir remoto
                else {
                    this.ticks += 5;
                }
            }

            //Recibe el numero de bloque que va a ser escrito o leido desde memoria local o remota
            //Necesita el id del procesador para saber si el acceso va a ser local o remoto
            public void addTicksForAccessShMem(int procId, int numWriteBlock)
            {
                //si es memoria local
                if (procId == 0 && numWriteBlock < Computer.p0_sharedmem_size ||
                    procId == 1 && numWriteBlock >= Computer.p0_sharedmem_size)
                    this.ticks += 16;
                //si es memoria remota
                else
                {
                    this.ticks += 40;
                }
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

            public bool isBlockOnAnotherCache(int dirBlock)
            {
                DirectoryProc home = Computer.getHomeDirectory(dirBlock);

                lock (home)
                {
                    int i = 0;
                    int thisCore = Computer.getCoreCountBefore(parent) + getId();
                    int f = home.getCacheMatrix().GetLength(1);
                    log("i am core " + thisCore + " and f = " + f);
                    while (i < Computer.getGlobalCoreCount())
                    {

                        //Console.ReadLine();
                        log("dir " + home.getParent().id + "[" + i + ", " + dirBlock % f + "] == " + home.getCacheMatrix()[i, dirBlock % f]);
                        if (home.getCacheMatrix()[i, dirBlock % f] == true && i != thisCore)
                        {
                            log("cache holding the needed block found on core " + i);
                            return true;
                        }
                        i++;
                    }
                }
                log("No cache holding the needed block was found");
                return false;
            }

            // Set specific matrix position to true
            public void setMatrixState(Core c, int dirBloque, bool value)
            {
                DirectoryProc home = Computer.getHomeDirectory(dirBloque);

                lock (home)
                {
                    int thisCore = Computer.getCoreCountBefore(c.parent) + c.getId();
                    int f = home.getCacheMatrix().GetLength(1);

                    Console.WriteLine(" caches_matrix[ " + thisCore + ", " + dirBloque % f + "] = " + value);

                    if (home.getCacheMatrix()[thisCore, dirBloque % f] == value)
                    {
                        OperatingSystem.logError(" caches_matrix[ " + thisCore + ", " + dirBloque % f + "] is already" + value);
                        Console.ReadLine();
                    }

                    home.getCacheMatrix()[thisCore, dirBloque % f] = value;
                }
            }

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

            public string myDataCacheToString(Core c)
            {
                string dataCache = "Data cache from processor "+c.getParentId()+ ", core "+ c.getId()+":\n";
                for (int i = 0; i < 3; i++) {
                    if (i == 0)
                        { dataCache += "Labels: "; }
                    if (i == 1)
                        { dataCache += "Data  : "; }
                    if (i == 2)
                        { dataCache += "States: "; }
                    for (int j = 0; j < cacheSize; j++)
                    {
                        if (i == 0)
                            { dataCache += c.GetDataCache().labelsOfWords[j] + " "; }

                        if (i == 1)
                            { dataCache += "Block " + j + ": " + c.GetDataCache().data[j].toString() + " "; }

                        if (i == 2)
                            { dataCache += c.GetDataCache().statesOfWords[j] + " "; }
                    }
                    dataCache += "\n";
                }
                return dataCache;
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

                public DataCache GetDataCacheWithBlock(int procId, int coreId, int dirBloque, int dirBloqueCache, DirectoryProc _home_dir_)
                {
                    int f = _home_dir_.getCacheMatrix().GetLength(1);

                    for (int i = 0; i < _home_dir_.getCacheMatrix().GetLength(0); i++)
                    {
                        if (_home_dir_.getCacheMatrix()[i, dirBloque % f] == true)
                        {
                            OperatingSystem.log("Found Core with modif block on cache " + i);
                            if (i == 2)
                                return Computer.processors[1].cores[i].GetDataCache();
                            return Computer.processors[0].cores[i].GetDataCache();
                        };
                    }
                    // should never get here
                    OperatingSystem.logError("Error: cache with reported modified was block not found");
                    Environment.Exit(22434);
                    return new DataCache();
                }

                public states Invalid()
                {
                    return states.invalid;
                }

                public int fetchData(int program_counter, Core c)
                {
                    int dirBloque = program_counter / (Computer.block_size * 4);
                    int dirBloqueCache = dirBloque % 4;
                    int dirPalabra = program_counter % (Computer.block_size * 4) / data.Length;


                    if (dirBloqueCache > data.Length || dirBloqueCache < 0)
                    {
                        c.log("Error: wrong block direction : " + dirBloqueCache);
                        Environment.Exit(33);
                    }

                    // Area critica
                    lock (c.dataCache)
                    {
                        // si est'a en esta cache ( no es invalido ), retornarlo
                        if (labelsOfWords[dirBloqueCache] == dirBloque &&
                            statesOfWords[dirBloqueCache] != states.invalid) // hit
                        {
                            Console.WriteLine("this is hit on load block " + dirBloque);
                            return data[dirBloqueCache].word[dirPalabra];
                        }
                        else // miss
                        {
                            Console.WriteLine("this is miss on load block " + dirBloque);
                            miss(program_counter, c);
                            return data[dirBloqueCache].word[dirPalabra];
                        }
                    }
                }

                public void miss(int program_counter, Core c)
                {
                    int dirBloque = program_counter / (Computer.block_size * 4);
                    int dirBloqueCache = dirBloque % 4;

                    DirectoryProc _home_dir_ = Computer.getHomeDirectory(dirBloque);

                    OperatingSystem.log("Proc " + c.parent.id + " dir LOAD block " + dirBloque + " >  BEFORE \n" + _home_dir_.toString());

                    // save the currently-in-this-cache block if its modified
                    if (statesOfWords[dirBloqueCache] == states.modified)
                    {
                        
                        lock (c.parent.shrmem)
                        {
                            //+40 o +16 cuando se inserta o se trae de memoria, 16 si es local, 40 si es remota 
                            c.addTicksForAccessShMem(c.getParentId(), dirBloque);
                            c.parent.shrmem.insertBloque(dirBloque, data[dirBloqueCache]);
                        }
                        // Block the home directory of the victim block
                        
                        lock (_home_dir_)
                        {
                            // Se suman 5 o 1 en caso de que sea remote o local, respectivamente
                            c.addTicksForAccessDir(_home_dir_.getParent().id);
                            // ponerlo en 0 en el dir
                            c.setMatrixState(c, dirBloque, false);

                            //Si el bloque no esta en otra cache, pone en U el estado de ese bloque en el directorio
                            if (c.isBlockOnAnotherCache(dirBloque))
                            {
                                // siempre deberia anular
                                OperatingSystem.logError("block should not be in another cache");
                                Environment.Exit(3456);
                            }
                            _home_dir_.setState(dirBloque, DirectoryProc.dirStates.U);

                        }
                    }
                    // si solo esta compartirdo, se pone 0 en dir y u si no hay mas
                    if (statesOfWords[dirBloqueCache] == states.shared)
                    {
                        OperatingSystem.logError("shared code 639");
                        Environment.Exit(33);

                        // poner en 0 en su columna del dir
                        //    Computer.getHomeDirectory(dirBloque).setMatrixState(c.parent.id, c._coreId, dirBloque, false);
                        // si no esta en otra cache, lo pone en U
                        //if (!Computer.getHomeDirectory(dirBloqueCache).isBlockOnAnotherCache(c.parent.id, c._coreId, dirBloqueCache))


                    }
                    // Allocate
                    lock (_home_dir_)
                    {
                        // Se suman 5 o 1 en caso de que sea remote o local, respectivamente
                        c.addTicksForAccessDir(_home_dir_.getParent().id);
                        // si está en otra cache, traerselo de ahí en vez de memoria
                        if (_home_dir_.getStateOfBlock(dirBloque) == DirectoryProc.dirStates.M)
                        {
                            /*Bloquea la cache de datos que tenga el bloque*/
                            DataCache HomeCache = GetDataCacheWithBlock(c.parent.id, c._coreId, dirBloque, dirBloqueCache, _home_dir_);
                            lock (HomeCache)
                            {
                                //Console.Write("trayendo el bloque de cache " + HomeCache.id)
                                /*Guarda el bloque desde la cache bloqueda a la mem compartida */
                                lock (c.parent.shrmem)
                                {
                                    //+40 o +16 cuando se inserta o se trae de memoria, 16 si es local, 40 si es remota 
                                    c.addTicksForAccessShMem(c.getParentId(), dirBloque);
                                    // revisar
                                    c.parent.shrmem.insertBloque(dirBloque, HomeCache.data[dirBloqueCache]);
                                }
                                /*Cambia el estado de bloque guardado a shared en la cache compartida */
                                HomeCache.statesOfWords[dirBloqueCache] = states.shared;
                                //_home_dir_.setState(dirBloque, DirectoryProc.dirStates.S);

                            }
                        }

                        /*guarda en mi cache el bloque desde memoria compartida*/

                        // 
                        lock (c.parent.shrmem)
                        {
                            //+40 o +16 cuando se inserta o se trae de memoria, 16 si es local, 40 si es remota 
                            c.addTicksForAccessShMem(c.getParentId(), dirBloque);

                            data[dirBloqueCache] = c.parent.shrmem.getBloque(dirBloque);
                            labelsOfWords[dirBloqueCache] = dirBloque;
                            statesOfWords[dirBloqueCache] = states.shared;
                        }

                        c.setMatrixState(c, dirBloque, true);
                        _home_dir_.setState(dirBloque, DirectoryProc.dirStates.S);
                        //Console.WriteLine("set " + dirBloque + "on dir " + _home_.getParent().id);
                        OperatingSystem.log("Proc " + c.parent.id + " dir LOAD After \n" + _home_dir_.toString());
                    }
                }

                public bool storeData(int program_counter, int dato, Core c)
                {
                    int dirBloque = program_counter / (Computer.block_size * 4);
                    int dirBloqueCache = dirBloque % 4;
                    int dirPalabra = program_counter % (Computer.block_size * 4) / data.Length;
                    bool stored = false;

                    if (dirBloqueCache > data.Length || dirBloqueCache < 0)
                    {
                        c.log("Error: wrong block direction : " + dirBloqueCache);
                        Environment.Exit(33);
                    }

                    lock (c.dataCache)
                    {
                        // Area critica
                        if (labelsOfWords[dirBloqueCache] == dirBloque) // hit
                        {
                            Console.WriteLine("this is hit on store block " + dirBloque);
                            // El bloque esta en estado M
                            if (statesOfWords[dirBloqueCache] == states.modified)
                            {
                                //return data[dirBloqueCache].word[dirPalabra];
                                data[dirBloqueCache].word[dirPalabra] = dato;
                                statesOfWords[dirBloqueCache] = states.modified;
                                stored = true;
                                return stored;
                            }
                            //El bloque esta en estado Compartido
                            else if (statesOfWords[dirBloqueCache] == states.shared)
                            {
                                //Invalidacion
                                Computer.invalidateInOtherCaches(c.parent.id, c._coreId, dirBloqueCache, dirBloque);

                                data[dirBloqueCache].word[dirPalabra] = dato;
                                statesOfWords[dirBloqueCache] = states.modified;

                                DirectoryProc _home_dir_ = Computer.getHomeDirectory(dirBloque);
                                lock (_home_dir_)
                                {
                                    c.setMatrixState(c, dirBloque, true);
                                    _home_dir_.setState(dirBloque, DirectoryProc.dirStates.M);
                                }

                                stored = true;
                                return stored;
                            }
                            else // invalid
                            {
                                Console.WriteLine("this is miss on store block  + dirBloque");
                                missStore(program_counter, dato, c);
                                statesOfWords[dirBloqueCache] = states.modified;
                                stored = true;
                                return stored;
                            }

                        }
                        else // miss
                        {
                            Console.WriteLine("this is miss on store block " + dirBloque);
                            missStore(program_counter, dato, c);
                            statesOfWords[dirBloqueCache] = states.modified;
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

                    DirectoryProc _home_dir_ = Computer.getHomeDirectory(dirBloque);

                    OperatingSystem.log("Proc " + c.parent.id + " dir STORE block " + dirBloque + " > BEFORE \n" + _home_dir_.toString());
                    OperatingSystem.log("currently in cache block " + labelsOfWords[dirBloqueCache] + " status is " + statesOfWords[dirBloqueCache]);

                    if (statesOfWords[dirBloqueCache] == states.modified)
                    {
                        /* if the currently--in--this--cache block is modified, save it to mem first, 
                         * then remove it from cache. And then, clear its position in its directory
                        */

                        // Block the home directory of the victim block
                        // 5 o 1
                        lock (_home_dir_)
                        {
                            // Se suman 5 o 1 en caso de que sea remote o local, respectivamente
                            c.addTicksForAccessDir(_home_dir_.getParent().id);
                            //+40 or +16
                            lock (c.parent.shrmem)
                            {
                                //+40 o +16 cuando se inserta o se trae de memoria, 16 si es local, 40 si es remota 
                                c.addTicksForAccessShMem(c.getParentId(), dirBloque);

                                c.parent.shrmem.insertBloque(dirBloque, data[dirBloqueCache]);
                            }

                            //pone false en la posicion de la cache en el directorio
                            c.setMatrixState(c, labelsOfWords[dirBloqueCache], false);

                            //Si el bloque no esta en otra cache, pone en U el estado de ese bloque en el directorio
                            if (!c.isBlockOnAnotherCache(dirBloque))
                            {
                                //solo debría hacerlo si esta compartido
                                _home_dir_.setState(labelsOfWords[dirBloqueCache], DirectoryProc.dirStates.U);
                            }

                        }
                    }
                    else if (statesOfWords[dirBloqueCache] == states.shared)
                    {
                        /* if the currently--in--this--cache block is shared, clear its position in its directory
                        */
                        OperatingSystem.logError("shared on missStore");
                        lock (_home_dir_)
                        {
                            // Se suman 5 o 1 en caso de que sea remote o local, respectivamente
                            c.addTicksForAccessDir(_home_dir_.getParent().id);

                            c.setMatrixState(c, labelsOfWords[dirBloqueCache], false);

                            if (!c.isBlockOnAnotherCache(dirBloque))
                            {
                                //solo debría hacerlo si esta compartido
                                _home_dir_.setState(labelsOfWords[dirBloqueCache], DirectoryProc.dirStates.U);
                            }
                        }
                    }

                    // Allocate
                    lock (_home_dir_)
                    {
                        if (_home_dir_.getStateOfBlock(dirBloque) == DirectoryProc.dirStates.M)
                        {
                            /* si está compartido en otra cache, traerlo de ah'i, invalidarlo ahi y actualizar
                             * su posicion en su directorio */

                            /*Bloquea la cache de datos que tenga el bloque*/
                            DataCache datahome = GetDataCacheWithBlock(c.parent.id, c._coreId, dirBloque, dirBloqueCache, _home_dir_);
                            lock (datahome)
                            {
                                if (datahome.statesOfWords[dirBloqueCache] == states.modified)
                                {
                                    /*Guarda el bloque desde la cache bloqueda a la mem compartida*/
                                    lock (c.parent.shrmem)
                                    {
                                        //+40 o +16 cuando se inserta o se trae de memoria, 16 si es local, 40 si es remota 
                                        c.addTicksForAccessShMem(c.getParentId(), dirBloque);

                                        c.parent.shrmem.insertBloque(dirBloque, datahome.data[dirBloqueCache]);
                                    }
                                    /*Guarda el bloque en mi cache*/
                                    data[dirBloqueCache] = datahome.data[dirBloqueCache];

                                    /*Bloque de la otra cache lo marca invalido*/
                                    datahome.statesOfWords[dirBloqueCache] = states.invalid;

                                    OperatingSystem.logError("ojo aca puede dar error");
                                    // TODO se debe poner false en su core, no en este (c)
                                    c.setMatrixState(c, labelsOfWords[dirBloqueCache], false);

                                    // dado que estaba modificado, no deberia estar en ninguna otra cache
                                    //Si el bloque no esta en otra cache, pone en U el estado de ese bloque en el directorio
                                    if (!c.isBlockOnAnotherCache(dirBloque))
                                    {
                                        //solo deberia hacerlo si esta compartido
                                        _home_dir_.setState(dirBloque, DirectoryProc.dirStates.U);
                                    }
                                    else
                                    {
                                        //Invalidacion
                                        OperatingSystem.logError("was on another cache");
                                        Console.ReadLine();
                                        //Computer.invalidateInOtherCaches(c.parent.id, c._coreId, dirBloqueCache, dirBloque);
                                    }
                                }


                            }
                        }
                        else if (_home_dir_.getStateOfBlock(dirBloque) == DirectoryProc.dirStates.S)
                        {
                            /* si est'a compartido en otra(s) cache, invalidarlo */
                        }

                        /*guarda en mi cache el bloque desde memoria compartida*/
                        lock (c.parent.shrmem)
                        {
                            //+40 o +16 cuando se inserta o se trae de memoria, 16 si es local, 40 si es remota 
                            c.addTicksForAccessShMem(c.getParentId(), dirBloque);

                            data[dirBloqueCache] = c.parent.shrmem.getBloque(dirBloque);
                            labelsOfWords[dirBloqueCache] = dirBloque;
                            statesOfWords[dirBloqueCache] = states.modified;
                        }


                        c.setMatrixState(c, dirBloque, true);
                        _home_dir_.setState(dirBloque, DirectoryProc.dirStates.M);
                        OperatingSystem.log("Proc " + c.parent.id + " dir STORE AFTER \n" + _home_dir_.toString());
                    }
                } // EO miss Store

            } //EO class dataCache

        }
        //Methods
    }
}