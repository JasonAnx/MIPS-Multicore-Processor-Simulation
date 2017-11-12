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

        public void generateErrorBloque() {
            for (int i = 0; i < 4; i++) {
                word[i].operationCod = -1;
                word[i].argument1 = -1;
                word[i].argument2 = -1;
                word[i].argument3 = -1;
            }
        }

        public void setValue(Bloque bl) {
            for (int i = 0; i < 4; i++) {
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
    }

    /*
    public struct Context{
        int threadId;
        float threadExecutionTime;
        // Un vector con los valores actuales de los registros del hilo
        int[] currentRegisterValues;

        public Context(){
            
        }
    }
    */


}