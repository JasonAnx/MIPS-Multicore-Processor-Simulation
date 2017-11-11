using System;
using System.IO;

namespace ProyectoArqui {

    public class Processor {

        public class Core { 
            public struct InstructionCache {
            }
            public struct DataCache {                
            } 
        }

        public class SharedMemory { }

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
                mem[lastBlock].word[0].setValue(instr);
                lastInstr++;
            }
            if (lastInstr == 1) {
                mem[lastBlock].word[1].setValue(instr);
                lastInstr++;
            }
            if (lastInstr == 2) {
                mem[lastBlock].word[2].setValue(instr);
                lastInstr++;
            }
            if (lastInstr == 3) {
                mem[lastBlock].word[3].setValue(instr);
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
        public SharedMemory sharedMemory;
        private int id; // processor id
        public static Core[] cores;

        public Processor(int _id, int n_cores, int isntrmem_size) {
            id = _id;
            cores = new Core[n_cores];
            isntrmem = new InstructionMemory(isntrmem_size);
        }

        public void start () {
            Console.WriteLine("hola desde proc "+id);
        }

        //Methods

        //
        
    }
}