using System;
using System.Collections.Generic;
using System.Text;

namespace ProyectoArqui
{
    public partial class Processor
    {
        public partial class Core
        {

            Processor parent;
            InstructionCache instructionsCache;
            DataCache dataCache;
            private int _coreId;

            Context currentContext;

            public int getId() { return _coreId; }

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
                    while (cycles++ < OperatingSystem.userQuantum && !currentContext.isFinalized )
                    {
                        Instruction nxtIst =
                            instructionsCache.fetchInstruction(
                                currentContext.instruction_pointer,
                                this
                                );
                        currentContext.instruction_pointer += 4;
                        execute_instruction(nxtIst);

                        // Print register values after each cycle
                        currentContext.printRegisterValues(registers);
                        // Print register values after each cycle


                        Computer.bsync.SignalAndWait();
                    }
                    saveCurrentContext();
                    if (currentContext.isFinalized)
                    {
                        Computer.bsync.RemoveParticipant();
                        break;
                    }
                }
                //Computer.bsync.SignalAndWait();
            }

            public void log(string msg)
            {
                Console.WriteLine("[Core " + (_coreId + 1) +
                    " on Processor " + parent.id + "] > " + msg
                    );
            }

            public void stop()
            {
                Computer.bsync.SignalAndWait();
            }

            // Create a new Context struct, save current context and insert it in the contextQueue

            public void saveCurrentContext()
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
                    this.currentContext.isFinalized
                    );
                parent.contextQueue.Enqueue(currentContext);
            }

            public void execute_instruction(Instruction _instruction)
            {

                int copOp = _instruction.operationCod;
                int arg1 = _instruction.argument1;
                int arg2 = _instruction.argument2;
                int arg3 = _instruction.argument3;


                log("executing instruction " + _instruction.printValue());

                switch (copOp)
                {

                    case -1:
                        //suspendido = 16;
                        //cP--;
                        break;
                    /***** OPERACIONES ARITMETICAS BASICAS *****/
                    case 8:
                        registers[arg2] = registers[arg1] + arg2;
                        break;
                    case 32:
                        registers[arg3] = registers[arg1] + registers[arg2];
                        break;

                    case 34:
                        registers[arg3] = registers[arg1] - registers[arg2];
                        break;

                    case 12:
                        registers[arg3] = registers[arg1] * registers[arg2];
                        break;

                    case 14:
                        registers[arg3] = registers[arg1] / registers[arg2];
                        break;
                    /**** LOADS Y STORES ******/

                    case 35: //LW

                        int memoryAddress = arg3 + registers[arg1];

                        int? datoLoad = 0;
                        /*   -->  
                         *   datoLoad =
                            traer de cacheDatos (  memoryAddress % 4, memoryAddress / 16 );
                        creo que hay que ver en el dir?? o si cae en mem compartida ??
                        */

                        //hay que hacer el caso if (dato == null) para que se proceda a intentar de nuevo
                        //con el fallo de caché ya resuelto                                        
                        if (datoLoad != null)
                        {
                            registers[arg2] = ((int)datoLoad);
                        }
                        else
                        {
                            // ?????
                        }

                        break;
                    case 43: //SW

                        int posicionMemoriaStore = arg3 + registers[(arg1)];
                        int datoEscribir = registers[(arg2)];

                        bool datoStore = false;
                        //  datoStore = false;
                        //escribir en cache datos 
                        //(posicionMemoriaStore % 4, posicionMemoriaStore / 16, datoEscribir);

                        //if (datoStore == false) { cP--; }

                        break;
                    case 50: //Load Linked
                             // no entra (creo lmao)                                       

                        break;

                    case 51: //Store conditional
                             // no entra (creo lmao)                                       

                        break;


                    /***BRANCHING Y DEMAS***/
                    case 4:
                        if (registers[arg1] == 0)
                        {
                            //cP += arg3;
                            currentContext.instruction_pointer += arg3;
                        }
                        break;

                    case 5:
                        if (registers[arg1] != 0)
                        {
                            //cP += arg3;
                            currentContext.instruction_pointer += arg3;
                        }
                        break;

                    case 3:
                        //registers[32] = cP;
                        registers[31] = currentContext.instruction_pointer;
                        //cP += arg3 / 4;
                        currentContext.instruction_pointer += arg3 / 4;
                        break;

                    case 2:
                        //cP = registers[arg1];
                        currentContext.instruction_pointer = registers[arg1];
                        break;
                    case 63:
                        currentContext.isFinalized = true;
                        // End of this thread
                        // print statistics
                        // set as finished in context
                        break;
                    default:
                        string reporter = "[Core  " + (_coreId + 1) + "/" + parent.getCoreCount() + " on Processor" + parent.id + "]";
                        OperatingSystem.logError(reporter + " Error: Unknown Instruction opCode: " + copOp, true);
                        Environment.Exit(0);
                        break;
                }

                // Incrementar PC
                //cP++;

            }
        }
    }
}
