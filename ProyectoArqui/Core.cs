using System;
using System.Collections.Generic;
using System.Text;

namespace ProyectoArqui
{
    public partial class Processor
    {

        public partial class Core
        {

            private int quantum = 5;
            int pc = 0; // memory position of the next instruction to be fetched
            /// <summary>
            /// starts the execution of the threads
            /// controls the thread time/quantum and context change
            /// </summary>
            public void start()
            {
                Console.WriteLine("Hello! from Core  " + (_coreId + 1) + "/" + parent.getCoreCount() + " on Processor" + parent.id);
                while (  /*ctxQueue.loadThread()*/ true)
                {

                    while (0 < quantum)
                    {
                        Instruction nxtIst = dataCache.fetchInstruction(0,this);
                        execute_instruction(nxtIst);
                        Computer.bsync.SignalAndWait();
                    }


                }
            }

            public void stop()
            {
                Computer.bsync.SignalAndWait();
            }
            public void execute_instruction(Instruction _instruction)
            {

                int copOp = _instruction.operationCod;
                int arg1 = _instruction.argument1;
                int arg2 = _instruction.argument2;
                int arg3 = _instruction.argument3;

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
                        }
                        break;

                    case 5:
                        if (registers[arg1] != 0)
                        {
                            //cP += arg3;
                        }
                        break;

                    case 3:
                        //registers[32] = cP;
                        //cP += arg3 / 4;
                        break;

                    case 2:
                        //cP = registers[arg1];
                        break;
                    case 63:
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
