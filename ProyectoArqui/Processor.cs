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

        //Cola de contexto que se utiliza durante la simulacion
        public Queue<Context> contextQueue = new Queue<Context>();
        //Cola de contexto para guardar los hilillos finalizados
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

        //Imprime los contextos archivados (se archivan para que no se pierdan una vez que se acaban)
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

        //Imprime las caches de datos
        public void printDataCaches()
        {
            string s = "";
            foreach (Core c in cores)
            {
                s += c.myDataCacheToString(c);
            }
            Console.WriteLine(s);
        }

        //Imprime la memoria compartida
        public void printSharedMem()
        {
            OperatingSystem.log("Shared Memory");
            string s = "\nProcessor 0:\n";
            for (int i = 0; i < Computer.p0_sharedmem_size + Computer.p1_sharedmem_size; i++)
            {
                if (i == 16)
                { s += "\nProcessor 1:\n"; }
                if (i != 0 && i != 16 && i % 4 == 0)
                { s += "\n"; }
                s += "Block " + i + ": " + shrmem.getBloque(i).toString() + "\t";
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
                //Crea un hilo de ejecucion para cada nucleo
                Thread t = new Thread(new ThreadStart(c.start));
                t.Start();
            }
        }

        //Metodo que invalida el resto de caches que tengan "mi bloque"
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

                        //Obtiene directorio casa del bloque
                        DirectoryProc homedir = Computer.getHomeDirectory(dirBloque);
                        lock (homedir)
                        {
                            //Suma 1 o 5 por el acceso a directorio
                            c.addTicksForAccessDir(homedir.getParent().id);
                            //lo remueve del directorio
                            c.GetDataCache().setMatrixState(c, dirBloque, false);
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
                int valueSh = 0;
                if (OperatingSystem.valueShMem)
                {
                    valueSh = 1;
                }
                parent = prnt;
                shMem = new Bloque<int>[sizeMem];
                //Inicializa la memoria compartida con el valor especificado al principio 
                for (int i = 0; i < sizeMem; i++)
                {
                    shMem[i] = new Bloque<int>(Computer.block_size);
                    shMem[i].word[0] = valueSh;
                    shMem[i].word[1] = valueSh;
                    shMem[i].word[2] = valueSh;
                    shMem[i].word[3] = valueSh;
                }
            }

            //Devuelve el bloque en la posicion numBloque
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

            //Retorna el arreglo de datos de la memoria compartida local
            public Bloque<int>[] getShMem()
            {
                return shMem;
            }

            ////Retorna el tamano de la memoria compartida local
            public int getShMemLenght()
            {
                return shMem.Length;
            }

            //Inserta el bloque "block" en la posicion numBloque 
            public bool insertBloque(int numBloque, Bloque<int> block)
            {
                bool inserted = false;
                //ShMem de proc 0
                if (numBloque < Computer.p0_sharedmem_size)
                {
                    Computer.processors[0].shrmem.getShMem()[numBloque].setValue(block);
                    inserted = true;
                }
                //ShMem de proc 1
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

            public int[] registers;

            public Core(int _id, Processor prnt)
            {
                _coreId = _id;
                parent = prnt;
                instructionsCache = new InstructionCache();
                dataCache = new DataCache();
            }

            public void addTicksForAccessDir(int dirParentId)
            {
                //si estoy acceso directorio propio
                if (dirParentId == getParentId())
                    this.currentContext.addClockTicks(1);
                //si acceso a un dir remoto
                else
                {
                    this.currentContext.addClockTicks(5);
                }
            }

            //Suma 16 o 40 ciclos de reloj. Cuando se accede a Shared Mem local o remota  
            public void addTicksForAccessShMem(int numWriteBlock)
            {
                //si es memoria local
                if (getParentId() == 0 && numWriteBlock < Computer.p0_sharedmem_size ||
                    getParentId() == 1 && numWriteBlock >= Computer.p0_sharedmem_size)
                    this.currentContext.addClockTicks(16);
                //si es memoria remota
                else
                {
                    this.currentContext.addClockTicks(40);
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
            //Verifica que un bloque no este en otra cache. Esto para poder marca en el directorio estado U
            public bool isBlockOnAnotherCache(int dirBlock)
            {
                DirectoryProc home = Computer.getHomeDirectory(dirBlock);

                lock (home)
                {
                    int i = 0;
                    int thisCore = Computer.getCoreCountBefore(parent) + getId();
                    int f = home.getCacheMatrix().GetLength(1);
                    //log("i am core " + thisCore + " and f = " + f);
                    while (i < Computer.getGlobalCoreCount())
                    {

                        //Console.ReadLine();
                        //log("dir " + home.getParent().id + "[" + i + ", " + dirBlock % f + "] == " + home.getCacheMatrix()[i, dirBlock % f]);
                        if (home.getCacheMatrix()[i, dirBlock % f] == true && i != thisCore)
                        {
                            //log("cache holding the needed block found on core " + i);
                            return true;
                        }
                        i++;
                    }
                }
                //log("No cache holding the needed block was found");
                return false;
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

            //Imprime la cache de datos
            public string myDataCacheToString(Core c)
            {
                string dataCache = "\nData cache from processor " + c.getParentId() + ", core " + c.getId() + ":\n";
                for (int i = 0; i < 3; i++)
                {
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

                //Almacena las instrucciones en la cache de instrucciones
                public Instruction fetchInstruction(int program_counter, Core c)
                {
                    int dirBloque = program_counter / (Computer.block_size * 4);
                    int dirBloqueCache = dirBloque % 4;
                    int dirPalabra = program_counter % (Computer.block_size * 4) / blocks.Length;
                    //No deberia entrar aqui
                    if (dirBloqueCache > labelsOfInstrs.Length || dirBloqueCache < 0)
                    {
                        c.log("Error: wrong block direction : " + dirBloqueCache);
                        Environment.Exit(33);
                    }
                    //hit
                    if (labelsOfInstrs[dirBloqueCache] == dirBloque)
                    {
                        //una palabra (instruccion)
                        return blocks[dirBloqueCache].word[dirPalabra];
                    }
                    //miss 
                    else
                    {
                        miss(program_counter, c);
                        return blocks[dirBloqueCache].word[dirPalabra];
                    }
                }
                public void miss(int program_counter, Core c)
                {
                    //Trae la el bloque de la memoria de instrucciones
                    int dirBloque = program_counter / (Computer.block_size * 4);
                    Bloque<Instruction> blk = c.parent.isntrmem.getBloque(dirBloque);
                    
                    int dirBloqueCache = dirBloque % 4;
                    //Asigna el bloque en la cache
                    blocks[dirBloqueCache] = blk;
                    labelsOfInstrs[dirBloqueCache] = dirBloque;

                    //data
                    //c.parent.isntrmem.
                }
            }

            public class DataCache
            {
                /// <summary>
                /// La DataCache esta compuesta por sus datos, las etiquetas de bloque y los estados de bloques 
                /// </summary>
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

                //public DataCache getDataCache(int i)
                //{
                //    if (i >= 2)
                //    {
                //        return Computer.processors[1].cores[0].dataCache;
                //    }
                //    else
                //    {
                //        return Computer.processors[0].cores[i].dataCache;
                //    }
                //}

                // Set specific matrix position to true
                public void setMatrixState(Core c, int dirBloque, bool value)
                {
                    DirectoryProc home = Computer.getHomeDirectory(dirBloque);

                    lock (home)
                    {
                        int thisCore = Computer.getCoreCountBefore(c.parent) + c.getId();
                        int f = home.getCacheMatrix().GetLength(1);

                        //Console.WriteLine(" caches_matrix[ " + thisCore + ", " + dirBloque % f + "] = " + value);

                        // should never happen
                        //if (home.getCacheMatrix()[thisCore, dirBloque % f] == value)
                        //{
                        //    OperatingSystem.logError(" caches_matrix[ " + thisCore + ", " + dirBloque % f + "] is already" + value);
                        //    Console.ReadLine();
                        //}

                        home.getCacheMatrix()[thisCore, dirBloque % f] = value;
                    }
                }

                //Devuelve el core que tenga el bloque con la direccion dirBloque
                public Core GetCoreWithBlock(int dirBloque, int dirBloqueCache, DirectoryProc _home_dir_)
                {
                    int f = _home_dir_.getCacheMatrix().GetLength(1);

                    for (int i = 0; i < _home_dir_.getCacheMatrix().GetLength(0); i++)
                    {
                        if (_home_dir_.getCacheMatrix()[i, dirBloque % f] == true)
                        {
                            //OperatingSystem.log("Found Core with modif block on cache " + i);
                            if (i == 2)
                                return Computer.processors[1].cores[i];
                            return Computer.processors[0].cores[i];
                        };
                    }
                    // should never get here
                    OperatingSystem.logError("Error: cache with reported modified was block not found");
                    Environment.Exit(22434);
                    return null;
                }

                public states Invalid()
                {
                    return states.invalid;
                }
                
                //LW
                public int? fetchData(int program_counter, Core c)
                {
                    //Convierte el program_counter a direccion de caches de dato
                    int dirBloque = program_counter / (Computer.block_size * 4);
                    int dirBloqueCache = dirBloque % 4;
                    int dirPalabra = program_counter % (Computer.block_size * 4) / data.Length;

                    if (dirBloqueCache > data.Length || dirBloqueCache < 0)
                    {
                        c.log("Error: wrong block direction : " + dirBloqueCache);
                        Environment.Exit(33);
                    }

                    // Area critica
                    if (Monitor.TryEnter(c.dataCache))
                    {
                        try
                        {
                            // si esta en esta cache ( no es invalido ), retornarlo
                            if (labelsOfWords[dirBloqueCache] == dirBloque &&
                                statesOfWords[dirBloqueCache] != states.invalid) // hit
                            {
                                c.currentContext.addClockTicks(1);
                                //Console.WriteLine("this is hit on load block " + dirBloque);
                                return data[dirBloqueCache].word[dirPalabra];
                            }
                            else // miss
                            {
                                //Console.WriteLine("this is miss on load block " + dirBloque);
                                if (miss(program_counter, c))
                                    return data[dirBloqueCache].word[dirPalabra];
                                else return null;
                            }
                        }
                        finally
                        {
                            Monitor.Exit(c.dataCache);
                        }
                    }
                    else
                    {
                        return null;
                    }
                }

                public bool miss(int program_counter, Core thisCore)
                {

                    int dirBloque = program_counter / (Computer.block_size * 4);
                    int dirBloqueCache = dirBloque % 4;

                    // save the currently-in-this-cache block if its modified
                    if (statesOfWords[dirBloqueCache] == states.modified)
                    {
                        DirectoryProc inCacheBlockDir = Computer.getHomeDirectory(labelsOfWords[dirBloqueCache]);

                        lock (thisCore.parent.shrmem)
                        {
                            thisCore.addTicksForAccessShMem(dirBloque);
                            thisCore.parent.shrmem.insertBloque(labelsOfWords[dirBloqueCache], data[dirBloqueCache]);
                        }
                        // Block the home directory of the victim block
                        lock (inCacheBlockDir)
                        {
                            thisCore.addTicksForAccessDir(inCacheBlockDir.getParent().id);

                            // ponerlo en 0 en el dir
                            setMatrixState(thisCore, labelsOfWords[dirBloqueCache], false);

                            //Si el bloque no esta en otra cache, pone en U el estado de ese bloque en el directorio
                            if (thisCore.isBlockOnAnotherCache(labelsOfWords[dirBloqueCache]))
                            {
                                // siempre deberia anular
                                OperatingSystem.logError("block should not be in another cache");
                                Environment.Exit(3456);
                            }
                            inCacheBlockDir.setState(labelsOfWords[dirBloqueCache], DirectoryProc.dirStates.U);
                        }
                    }
                    // si solo esta compartirdo, se pone 0 en dir y u si no hay mas
                    else if (statesOfWords[dirBloqueCache] == states.shared)
                    {
                        //OperatingSystem.logError("shared code 639");

                        DirectoryProc inCacheBlockDir = Computer.getHomeDirectory(labelsOfWords[dirBloqueCache]);
                        //Environment.Exit(33);
                        lock (inCacheBlockDir)
                        {
                            thisCore.addTicksForAccessDir(inCacheBlockDir.getParent().id);
                            setMatrixState(thisCore, labelsOfWords[dirBloqueCache], false);
                            if (!thisCore.isBlockOnAnotherCache(labelsOfWords[dirBloqueCache]))
                            {
                                inCacheBlockDir.setState(labelsOfWords[dirBloqueCache], DirectoryProc.dirStates.U);
                            }
                        }

                        // poner en 0 en su columna del dir
                        //    Computer.getHomeDirectory(dirBloque).setMatrixState(c.parent.id, c._coreId, dirBloque, false);
                        // si no esta en otra cache, lo pone en U
                        //if (!Computer.getHomeDirectory(dirBloqueCache).isBlockOnAnotherCache(c.parent.id, c._coreId, dirBloqueCache))

                    }


                    DirectoryProc toFetchBlockDir = Computer.getHomeDirectory(dirBloque);

                    if (OperatingSystem.slowModeActivated)
                        OperatingSystem.log("Proc " + thisCore.parent.id + " dir LOAD block " + dirBloque + " >  BEFORE \n" + toFetchBlockDir.toString());


                    // Allocate
                    lock (toFetchBlockDir)
                    {
                        thisCore.addTicksForAccessDir(toFetchBlockDir.getParent().id);
                        // Se suman 5 o 1 en caso de que sea remote o local, respectivamente
                        // si está en otra cache, traerselo de ahí en vez de memoria
                        if (toFetchBlockDir.getStateOfBlock(dirBloque) == DirectoryProc.dirStates.M)
                        {
                            /*Bloquea la cache de datos que tenga el bloque*/
                            Core cacheOwner = GetCoreWithBlock(dirBloque, dirBloqueCache, toFetchBlockDir);

                            if (Monitor.TryEnter(cacheOwner.dataCache))
                            {
                                try
                                {
                                    /*Guarda el bloque desde la cache bloqueda a la mem compartida */
                                    lock (thisCore.parent.shrmem)
                                    {
                                        thisCore.addTicksForAccessShMem(dirBloque);

                                        // revisar
                                        thisCore.parent.shrmem.insertBloque(
                                            cacheOwner.dataCache.labelsOfWords[dirBloqueCache],
                                            cacheOwner.dataCache.data[dirBloqueCache]
                                            );
                                    }
                                    /*Cambia el estado de bloque guardado a shared en la cache compartida */
                                    cacheOwner.dataCache.statesOfWords[dirBloqueCache] = states.shared;
                                    //_home_dir_.setState(dirBloque, DirectoryProc.dirStates.S);
                                }
                                finally
                                {
                                    Monitor.Exit(cacheOwner.dataCache);
                                }
                            }
                            else
                            {
                                return false;
                            }
                        }

                        /*guarda en mi cache el bloque desde memoria compartida*/
                        lock (thisCore.parent.shrmem)
                        {
                            thisCore.addTicksForAccessShMem(dirBloque);

                            data[dirBloqueCache] = thisCore.parent.shrmem.getBloque(dirBloque);
                            labelsOfWords[dirBloqueCache] = dirBloque;
                            statesOfWords[dirBloqueCache] = states.shared;
                        }

                        setMatrixState(thisCore, dirBloque, true);
                        toFetchBlockDir.setState(dirBloque, DirectoryProc.dirStates.S);
                        //Console.WriteLine("set " + dirBloque + "on dir " + _home_.getParent().id);
                        if (OperatingSystem.slowModeActivated)
                            OperatingSystem.log("Proc " + thisCore.parent.id + " dir LOAD After \n" + toFetchBlockDir.toString());
                        return true;
                    }
                }

                //SW recibe el program counter y el dato que se desea almacenar
                public bool storeData(int program_counter, int dato, Core thisCore)
                {
                    //Convierte el program_counter a direccion de caches de dato
                    int dirBloque = program_counter / (Computer.block_size * 4);
                    int dirBloqueCache = dirBloque % 4;
                    int dirPalabra = program_counter % (Computer.block_size * 4) / data.Length;
                    bool stored = false;
                    
                    //No deberia entrar aqui
                    if (dirBloqueCache > data.Length || dirBloqueCache < 0)
                    {
                        thisCore.log("Error: wrong block direction : " + dirBloqueCache);
                        Environment.Exit(33);
                    }

                    // Area critica
                    lock (thisCore.dataCache)
                    {
                        if (labelsOfWords[dirBloqueCache] == dirBloque)
                        {
                            // if the block is in this cache with state M
                            if (statesOfWords[dirBloqueCache] == states.modified) // store hit
                            {
                                //Console.WriteLine("this is hit on store block " + dirBloque);
                                //return data[dirBloqueCache].word[dirPalabra];
                                data[dirBloqueCache].word[dirPalabra] = dato;
                                statesOfWords[dirBloqueCache] = states.modified;
                                thisCore.currentContext.addClockTicks(1);
                                stored = true;
                                return stored;
                            }
                            // if the block is in this cache with state S
                            else if (statesOfWords[dirBloqueCache] == states.shared) // store almost hit
                            {
                                //Console.WriteLine("this is hit on store block " + dirBloque + ", state Shared");

                                // invalidate in all other caches
                                Computer.invalidateBlockInOtherCaches(thisCore.parent.id, thisCore._coreId, dirBloqueCache, dirBloque);

                                data[dirBloqueCache].word[dirPalabra] = dato;
                                statesOfWords[dirBloqueCache] = states.modified;
                                //Una vez que invalidad marca como modificado en el directorio propio
                                DirectoryProc _home_dir_ = Computer.getHomeDirectory(dirBloque);
                                lock (_home_dir_)
                                {
                                    thisCore.addTicksForAccessDir(_home_dir_.getParent().id);

                                    setMatrixState(thisCore, dirBloque, true);
                                    _home_dir_.setState(dirBloque, DirectoryProc.dirStates.M);
                                }
                                thisCore.currentContext.addClockTicks(1);
                                stored = true;
                                return stored;
                            }
                            else // Si el bloque esta invalido
                            {
                                //Console.WriteLine("this is miss on store block  + dirBloque");
                                if (missStore(program_counter, dato, thisCore))
                                {
                                    statesOfWords[dirBloqueCache] = states.modified;
                                    stored = true;
                                    return stored;

                                }
                                else return false;
                            }

                        }
                        else // miss
                        {
                            //Console.WriteLine("this is miss on store block " + dirBloque);

                            if (missStore(program_counter, dato, thisCore))
                            {
                                //Una vez que resulve el fallo y trae el bloque de mem lo marca como modificado
                                statesOfWords[dirBloqueCache] = states.modified;
                                stored = true;
                                return stored;

                            }
                            else return false;
                        }
                    }

                }

                public bool missStore(int program_counter, int dato, Core thisCore)
                {
                    //Convierte el program_counter a direccion de caches de dato
                    int dirBloque = program_counter / (Computer.block_size * 4);
                    int dirBloqueCache = dirBloque % 4;
                    int dirPalabra = program_counter % (Computer.block_size * 4) / data.Length;

                    /* if the currently--in--this--cache block is modified,
                     * we have to save it first before proceding with the store
                     * */
                    if (statesOfWords[dirBloqueCache] == states.modified)
                    {
                        /* if the currently--in--this--cache block is modified, save it to mem first, 
                         * then remove it from cache. And then, clear its position in its directory
                        */
                        DirectoryProc inCacheBlockDir = Computer.getHomeDirectory(labelsOfWords[dirBloqueCache]);

                        // Block the home directory of the victim block
                        // 5 o 1
                        lock (inCacheBlockDir)
                        {
                            thisCore.addTicksForAccessDir(inCacheBlockDir.getParent().id);

                            // Se suman 5 o 1 en caso de que sea remote o local, respectivamente
                            lock (thisCore.parent.shrmem)
                            {
                                thisCore.addTicksForAccessShMem(dirBloque);

                                thisCore.parent.shrmem.insertBloque(labelsOfWords[dirBloqueCache], data[dirBloqueCache]);
                            }

                            //pone false en la posicion de la cache en el directorio
                            setMatrixState(thisCore, labelsOfWords[dirBloqueCache], false);

                            //Si el bloque no esta en otra cache, pone en U el estado de ese bloque en el directorio
                            if (!thisCore.isBlockOnAnotherCache(labelsOfWords[dirBloqueCache]))
                            {
                                //solo debría hacerlo si esta compartido
                                inCacheBlockDir.setState(labelsOfWords[dirBloqueCache], DirectoryProc.dirStates.U);
                            }

                        }
                    }
                    /* if the currently--in--this--cache block is shared, 
                     * clear its position in its directory before writing
                    */
                    else if (statesOfWords[dirBloqueCache] == states.shared)
                    {
                        DirectoryProc inCacheBlockDir = Computer.getHomeDirectory(labelsOfWords[dirBloqueCache]);
                        lock (inCacheBlockDir)
                        {
                            thisCore.addTicksForAccessDir(inCacheBlockDir.getParent().id);

                            // set as false in dir
                            setMatrixState(thisCore, labelsOfWords[dirBloqueCache], false);

                            // if there are no more blocks shared, set dir state as U
                            if (!thisCore.isBlockOnAnotherCache(labelsOfWords[dirBloqueCache]))
                            {
                                //solo debría hacerlo si esta compartido
                                inCacheBlockDir.setState(labelsOfWords[dirBloqueCache], DirectoryProc.dirStates.U);
                            }
                        }
                    }

                    //Directorio que tenga el bloque
                    DirectoryProc toFetchBlockDir = Computer.getHomeDirectory(dirBloque);

                    if (OperatingSystem.slowModeActivated)
                    {
                        OperatingSystem.log("Proc " + thisCore.parent.id + " dir STORE block " + dirBloque + " > BEFORE \n" + toFetchBlockDir.toString());
                    }

                    // Allocate
                    lock (toFetchBlockDir)
                    {
                        thisCore.addTicksForAccessDir(toFetchBlockDir.getParent().id);

                        if (toFetchBlockDir.getStateOfBlock(dirBloque) == DirectoryProc.dirStates.M)
                        {
                            /* si está compartido en otra cache, traerlo de ah'i, invalidarlo ahi y actualizar
                             * su posicion en su directorio */

                            /*Bloquea la cache de datos que tenga el bloque*/
                            Core cacheOwner = GetCoreWithBlock(dirBloque, dirBloqueCache, toFetchBlockDir);

                            if (Monitor.TryEnter(cacheOwner.dataCache))
                            {
                                try
                                {
                                    if (cacheOwner.dataCache.statesOfWords[dirBloqueCache] != states.modified)
                                    {
                                        OperatingSystem.logError("Irregularity > block marked as M on dir, not M on cache");
                                        Environment.Exit(7676767);
                                    }
                                    /*Guarda el bloque desde la cache bloqueda a la mem compartida*/
                                    lock (thisCore.parent.shrmem)
                                    {
                                        thisCore.parent.shrmem.insertBloque(
                                            cacheOwner.dataCache.labelsOfWords[dirBloqueCache],
                                            cacheOwner.dataCache.data[dirBloqueCache]
                                            );
                                    }
                                    /*Guarda el bloque en mi cache*/
                                    this.data[dirBloqueCache] = cacheOwner.dataCache.data[dirBloqueCache];

                                    /*Bloque de la otra cache lo marca invalido*/
                                    cacheOwner.dataCache.statesOfWords[dirBloqueCache] = states.invalid;
                                    // y actualiza el directorio correxpondiente 
                                    setMatrixState(cacheOwner, cacheOwner.GetDataCache().labelsOfWords[dirBloqueCache], false);

                                    // dado que estaba modificado, no deberia estar en ninguna otra cache
                                    if (!thisCore.isBlockOnAnotherCache(dirBloque))
                                    {
                                        //Si el bloque no esta en otra cache, pone en U el estado de ese bloque en el directorio
                                        toFetchBlockDir.setState(dirBloque, DirectoryProc.dirStates.U);
                                    }
                                    else
                                    {
                                        //Invalidacion
                                        OperatingSystem.logError("ERROR: the block was on another cache");
                                        Console.ReadLine();
                                    }

                                }
                                finally
                                {
                                    Monitor.Exit(cacheOwner.dataCache);
                                }
                            }
                            else
                            {
                                return false;
                            }

                        }
                        /* si el bloque que se quiere escribir esta compartido en otra(s) cache(s)
                         * hay que invalidar todas sus ocurrencias
                         */
                        else if (toFetchBlockDir.getStateOfBlock(dirBloque) == DirectoryProc.dirStates.S)
                        {
                            //OperatingSystem.logError("Implement this");
                            //Console.ReadLine();

                            // TODO esto es invalidar el bloque en todas, creo que no hace falta cacheOwnder 
                            Core cacheOwner = GetCoreWithBlock(dirBloque, dirBloqueCache, toFetchBlockDir);

                            /* si est'a compartido en otra(s) cache, invalidarlo */
                            //Computer.invalidateInOtherCaches( dirBloqueCache, dirBloque);
                            Computer.invalidateBlockInOtherCaches(cacheOwner.parent.id, cacheOwner._coreId, dirBloqueCache, dirBloque);
                            //OperatingSystem.log("Proc " + thisCore.parent.id + " dir STORE block " + dirBloque + " > invalidation \n" + toFetchBlockDir.toString());
                        }

                        /*guarda en mi cache el bloque desde memoria compartida*/
                        lock (thisCore.parent.shrmem)
                        {
                            thisCore.addTicksForAccessShMem(dirBloque);
                            //Marca el bloque como modificado en la cache
                            data[dirBloqueCache] = thisCore.parent.shrmem.getBloque(dirBloque);
                            data[dirBloqueCache].word[dirPalabra] = dato;
                            labelsOfWords[dirBloqueCache] = dirBloque;
                            statesOfWords[dirBloqueCache] = states.modified;
                        }

                        //Marca el bloque como modificado en en el dir
                        setMatrixState(thisCore, dirBloque, true);
                        toFetchBlockDir.setState(dirBloque, DirectoryProc.dirStates.M);
                        if (OperatingSystem.slowModeActivated)
                            OperatingSystem.log("Proc " + thisCore.parent.id + " dir STORE AFTER \n" + toFetchBlockDir.toString());
                        return true;
                    }
                } // EO miss Store

            } //EO class dataCache

        }
        //Methods
    }
}