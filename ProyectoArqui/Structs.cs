namespace ProyectoArqui {

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
    }

}