namespace ProyectoArqui {

    public struct Bloque {
        public Instruction word0;
        public Instruction word1;
        public Instruction word2;
        public Instruction word3;

        public Bloque(Instruction w0, Instruction w1,
                     Instruction w2, Instruction w3) {
            word0 = w0;
            word1 = w1;
            word2 = w2;
            word3 = w3;
        }
    }

    public struct Instruction {
        public int operation;
        public int result;
        public int operator1;
        public int operator2;

        public Instruction(int subI1, int subI2, int subI3, int subI4) {
            operation = subI1;
            result = subI2;
            operator1 = subI3;
            operator2 = subI4;
        }

        public void setValues(Instruction instr) {
            operation = instr.operation;
            result = instr.result;
            operator1 = instr.operator1;
            operator2 = instr.operator2;
        }
    }

}