using System;
using System.Collections.Generic;
using System.Text;

namespace ProyectoArqui
{
    public partial class Processor
    {
        private partial class Core
        {

            Processor parent;
            InstructionCache instructionsCache;
            DataCache dataCache;
            private int _coreId;

            Context currentContext;

            public int getId() { return _coreId; }
            public int getParentId() { return parent.id; }



            /// <summary>
            /// starts the execution of the threads
            /// controls the thread time/quantum and context change
            /// </summary>
            public void start()
            {
                Console.WriteLine("Started Core  " + (_coreId + 1) + "/" + parent.getCoreCount() + " on Processor" + parent.id);
                while (loadContext())
                {
                    int cycles = -1;
                    while (cycles++ < OperatingSystem.userQuantum && !currentContext.isFinalized)
                    {
                        ticks = 0;
                        Instruction nxtIst =
                            instructionsCache.fetchInstruction(
                                currentContext.instruction_pointer,
                                this
                                );
                        currentContext.instruction_pointer += 4;
                        execute_instruction(nxtIst);

                        // Print register values after each cycle

                        //Console.WriteLine(parent.contextQueue.Count);
                        // Print register values after each cycle


                        Computer.bsync.SignalAndWait();
                    }
                    if (!currentContext.isFinalized)
                    {
                        currentContext.addClockTicks(ticks);
                        saveCurrentContext();
                    }
                }
                {
                    Computer.bsync.RemoveParticipant();
                }
                //Computer.bsync.SignalAndWait();
            }

            public void log(string msg)
            {
                Console.WriteLine("\n[ Processor " + parent.id + " : Core " + (_coreId) + " ] > " + msg
                    );
            }

            public void stop()
            {
                Computer.bsync.SignalAndWait();
            }

            // Create a new Context struct, save current context and insert it in the contextQueue

            public void saveCurrentContext()
            {
                lock (parent.contextQueue)
                {

                    int[] registerValues = new int[32];
                    Array.Copy(registers, 0, registerValues, 0, 32);//Guarda los registros
                                                                    // Todavia esto no se mide, TODO
                    float currentThreadExecutionTime = 0;

                    Context currentContext = new Context(
                        this.currentContext.instruction_pointer,
                        this.currentContext.id,
                        currentThreadExecutionTime,
                        registerValues,
                        this.currentContext.isFinalized,
                        this.currentContext.clockTicks
                        );
                    parent.contextQueue.Enqueue(currentContext);

                }
            }

            public void execute_instruction(Instruction itr)
            {

                int opC = itr.operationCod;
                int src = itr.argument1;
                int sr2 = itr.argument2;
                int imm = itr.argument3;

                if (OperatingSystem.slowModeActivated || true) // TODO quitar esta mica

                    log("executing instruction " + itr.toString() + "\n" +
                    "\tRegister values" + currentContext.registersToString()
                    );

                switch (opC)
                {

                    case -1:
                        //suspendido = 16;
                        //cP--;
                        break;
                    /***** OPERACIONES ARITMETICAS BASICAS *****/
                    case 8:
                        registers[sr2] = registers[src] + imm;
                        break;
                    case 32:
                        registers[imm] = registers[src] + registers[sr2];
                        break;

                    case 34:
                        registers[imm] = registers[src] - registers[sr2];
                        break;

                    case 12:
                        registers[imm] = registers[src] * registers[sr2];
                        break;

                    case 14:
                        registers[imm] = registers[src] / registers[sr2];
                        break;
                    /**** LOADS Y STORES ******/

                    case 35: //LW


                        int memoryAddress = imm + registers[src];

                        int? datoLoad = dataCache.fetchData(memoryAddress, this);
                        /*   -->  
                         *   datoLoad =
                            traer de cacheDatos (  memoryAddress % 4, memoryAddress / 16 );
                        creo que hay que ver en el dir?? o si cae en mem compartida ??
                        */

                        //hay que hacer el caso if (dato == null) para que se proceda a intentar de nuevo
                        //con el fallo de caché ya resuelto                                        
                        if (datoLoad != null)
                        {
                            registers[sr2] = ((int)datoLoad);
                        }
                        else
                        {
                            // ?????
                        }

                        break;
                    case 43: //SW

                        int memoryAddressStore = imm + registers[(src)];
                        int word = registers[(sr2)];

                        bool datoStore = dataCache.storeData(memoryAddressStore, word, this);
                        //escribir en cache datos 
                        //(memoryAddressStore % 4, memoryAddressStore / 16, word);

                        //if (datoStore == false) { cP--; }

                        break;
                    case 50: //Load Linked
                             // no entra (creo lmao)                                       

                        break;

                    case 51: //Store conditional
                             // no entra (creo lmao)                                       

                        break;


                    /***BRANCHING Y DEMAS***/
                    case 2:  // JR
                        //cP = registers[src];
                        currentContext.instruction_pointer = registers[src];
                        break;

                    case 3:  // JAL

                        registers[31] = currentContext.instruction_pointer;
                        currentContext.instruction_pointer += imm;
                        break;

                    case 4:  // BEQZ
                        if (registers[src] == 0)
                        {
                            //cP += imm;
                            currentContext.instruction_pointer += imm * 4;
                        }
                        break;

                    case 5: // BNEZ 
                        if (registers[src] != 0)
                        {
                            //cP += imm;
                            currentContext.instruction_pointer += imm * 4;
                        }
                        break;

                    case 63:
                        currentContext.isFinalized = true;
                        log("Thread " + currentContext.id + " has finalized ");
                        parent.archiveContext(currentContext);

                        //Console.WriteLine("Press enter key to continue");
                        //Console.ReadLine();

                        // End of this thread
                        // print statistics
                        // set as finished in context
                        break;
                    default:
                        string reporter = "[Core  " + (_coreId + 1) + "/" + parent.getCoreCount() +
                            " on Processor" + parent.id + "]";
                        OperatingSystem.logError(reporter + " Error: Unknown Instruction opCode: " + opC, true);
                        Environment.Exit(0);
                        break;
                }

                // Incrementar PC
                //cP++;

            }
        }
    }
}
