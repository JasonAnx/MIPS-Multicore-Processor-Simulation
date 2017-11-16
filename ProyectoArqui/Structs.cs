using System;

namespace ProyectoArqui
{

    public struct Bloque
    {
        public Instruction[] word;

        public Bloque(int wordSize)
        {
            word = new Instruction[wordSize];
        }

        public Instruction GetInstruction(int numInstr)
        {
            return this.word[numInstr];
        }

        public void generateErrorBloque()
        {
            for (int i = 0; i < 4; i++)
            {
                word[i].operationCod = -1;
                word[i].argument1 = -1;
                word[i].argument2 = -1;
                word[i].argument3 = -1;
            }
        }

        public void setValue(Bloque bl)
        {
            for (int i = 0; i < 4; i++)
            {
                this.word[i].setValue(bl.word[i]);
            }
        }

    }

    public struct Instruction
    {
        public int operationCod;
        public int argument1;
        public int argument2;
        public int argument3;

        public Instruction(int subI1, int subI2, int subI3, int subI4)
        {
            operationCod = subI1;
            argument1 = subI2;
            argument2 = subI3;
            argument3 = subI4;
        }

        public void setValue(Instruction instr)
        {
            operationCod = instr.operationCod;
            argument1 = instr.argument1;
            argument2 = instr.argument2;
            argument3 = instr.argument3;
        }

        public string printValue()
        {
            string values = operationCod.ToString() + " ";
            values += argument1.ToString() + " ";
            values += argument2.ToString() + " ";
            values += argument3.ToString();
            return values;
        }
    }


    public struct Context
    {
        // attrs
        string ctxId;
        public string id { get { return ctxId; } }

        public int instruction_pointer;

        float threadExecutionTime;

        private int[] registerValues; //> Current thread register values 

        bool threadIsFinalized;
        public bool isFinalized { get { return threadIsFinalized; } set { threadIsFinalized = value; } }

        //
        public Context(int instr_ptr, string threadId, float threadExecutionTime, int[] registerValues, bool threadIsFinalized)
        {
            this.ctxId = threadId;
            this.threadExecutionTime = threadExecutionTime;
            this.registerValues = registerValues;
            this.threadIsFinalized = threadIsFinalized;
            this.instruction_pointer = instr_ptr;
        }

        public Context(int instr_ptr, string threadId)
        {
            instruction_pointer = instr_ptr;
            this.ctxId = threadId;
            threadExecutionTime = 0;
            registerValues = new int[32];
            this.threadIsFinalized = false;
        }
        public int[] getRegisterValues() { return registerValues; }

        // Print core register values (32 int array) from Core 

        public void printRegisterValues(int[] registers, int Cid, int id)
        {
            string r = "Register values from core " + (Cid+1) + ", Proc " + id +" : "+ ctxId + ": " + "\n";
            for (int i = 0; i < registers.Length; i++)
            {
                if (registers[i] != 0)
                {
                    //Console.Write("R" + i + ": " + registers[i] + " | ");
                    r += "R" + i + ": " + registers[i] + " | ";
                }
            }
            r += "\n" + "\n" + "-------------------------------------" + "\n";
            Console.WriteLine(r);
        }
    }



}