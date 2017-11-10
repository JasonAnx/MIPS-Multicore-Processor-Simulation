using System;
using System.IO;

namespace ProyectoArqui {

    public class Processor {

        public class Core { }

        public class InstructionMemory {
        //Attributes
        Bloque[] mem;
        int lastBlock;
        int lastInstr;

        //Constructor
        public InstructionMemory(int sizeMem) {
            mem = new Bloque[sizeMem];
            lastBlock = 0;
            lastInstr = 0;
        }

        //Methods
        public void insertInstr(Instruction instr) {
            if (lastInstr == 0) {
                mem[lastBlock].word0.setValues(instr);
                lastInstr++;
            }
            if (lastInstr == 1) {
                mem[lastBlock].word1.setValues(instr);
                lastInstr++;
            }
            if (lastInstr == 2) {
                mem[lastBlock].word2.setValues(instr);
                lastInstr++;
            }
            if (lastInstr == 3) {
                mem[lastBlock].word3.setValues(instr);
                lastInstr = 0;
                if (lastBlock < mem.Length)
                    lastBlock++;
                else
                    Environment.Exit(0);
            }

        }

        public Bloque getBloque(int indexBloque) {
            return mem[indexBloque];
        }

    }
        
        public InstructionMemory isntrmem;
        public ShrdMem shrdMem;

        public static Core[] cores;

        public Processor(int n_cores, int isntrmem_size) {
            cores = new Core[n_cores];
            isntrmem = new InstructionMemory(isntrmem_size);
        }

        //Methods

        //
        
    }
}