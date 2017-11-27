using System;

namespace ProyectoArqui
{

    public struct Bloque<T>
    {
        public T[] word;

        public Bloque(int wordSize)
        {
            word = new T[wordSize];
        }

        public T GetInstruction(int numInstr)
        {
            return this.word[numInstr];
        }

        //public void generateErrorBloque()
        //{
        //    for (int i = 0; i < 4; i++)
        //    {
        //        word[i].operationCod = -1;
        //        word[i].argument1 = -1;
        //        word[i].argument2 = -1;
        //        word[i].argument3 = -1;
        //    }
        //}

        //public void setValue(Bloque bl)
        //{
        //    for (int i = 0; i < 4; i++)
        //    {
        //        this.word[i].setValue(bl.word[i]);
        //    }
        //}
        public void setValue(Bloque<T> bl) {
            for (int i = 0; i < Computer.block_size; i++)
            {
                word[i] = bl.word[i];
            }
        }

        public string toString()
        {
            string s = "|";
            for (int i = 0; i < Computer.block_size; i++)
            {
                s += word[i] + "|";
            }
            return s;
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

        public string toString()
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

        public int clockTicks;

        private int[] registerValues; //> Current thread register values 

        bool threadIsFinalized;
        public bool isFinalized { get { return threadIsFinalized; } set { threadIsFinalized = value; } }

        //
        public Context(int instr_ptr, string threadId, float threadExecutionTime, int[] registerValues, bool threadIsFinalized, int clockTicks)
        {
            this.ctxId = threadId;
            this.threadExecutionTime = threadExecutionTime;
            this.registerValues = registerValues;
            this.threadIsFinalized = threadIsFinalized;
            this.instruction_pointer = instr_ptr;
            this.clockTicks = clockTicks;
        }

        public Context(int instr_ptr, string threadId)
        {
            instruction_pointer = instr_ptr;
            this.ctxId = threadId;
            threadExecutionTime = 0;
            registerValues = new int[32];
            this.threadIsFinalized = false;
            clockTicks = 0;
        }

        public int[] getRegisterValues() { return registerValues; }

        // Print core register values (32 int array) from Core 

        public string registersToString()
        {
            string r = "\n  "+ ctxId + ": Register Values" + "\n";
            r += "\n\t";

            for (int i = 0; i < registerValues.Length; i++)
            {
                if (registerValues[i] != 0)
                {
                    //Console.Write("R" + i + ": " + registers[i] + " | ");
                    r += "R" + i + ": " + registerValues[i] + " | ";
                }
            }
            r += "\n";
            return r;
        }

        public string clockTicksToString()
        {
            string s = "Clock ticks of " + ctxId + ": ";
            s += clockTicks;
            s += "\n";
            return s;
        }

        public void addClockTicks(int ticks) {
            clockTicks += ticks;
        }
    }



}