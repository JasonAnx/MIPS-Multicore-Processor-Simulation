using System;
using System.IO;


namespace ProyectoArqui
{
    class Program
    {
        public struct Instruction
        {
            public int operation;
            public int result;
            public int operator1;
            public int operator2;

            public Instruction(int subI1, int subI2, int subI3, int subI4)
            {
                operation = subI1;
                result = subI2;
                operator1 = subI3;
                operator2 = subI4;
            }

            public void setValues(Instruction instr)
            {
                operation = instr.operation;
                result = instr.result;
                operator1 = instr.operator1;
                operator2 = instr.operator2;
            }
        }

        public struct Bloque
        {
            public Instruction word0;
            public Instruction word1;
            public Instruction word2;
            public Instruction word3;

            public Bloque(Instruction w0, Instruction w1,
                         Instruction w2, Instruction w3)
            {
                word0 = w0;
                word1 = w1;
                word2 = w2;
                word3 = w3;
            }
        }

        public class LocalMem
        {
            //Attributes
            Bloque[] mem;
            int lastBlock;
            int lastInstr;

            //Constructor
            public LocalMem(int sizeMem)
            {
                mem = new Bloque[sizeMem];
                lastBlock = 0;
                lastInstr = 0;
            }

            //Methods
            public void insertInstr(Instruction instr)
            {
                if (lastInstr == 0)
                {
                    mem[lastBlock].word0.setValues(instr);
                    lastInstr++;
                }
                if (lastInstr == 1)
                {
                    mem[lastBlock].word1.setValues(instr);
                    lastInstr++;
                }
                if (lastInstr == 2)
                {
                    mem[lastBlock].word2.setValues(instr);
                    lastInstr++;
                }
                if (lastInstr == 3)
                {
                    mem[lastBlock].word3.setValues(instr);
                    lastInstr = 0;
                    if (lastBlock < mem.Length)
                        lastBlock++;
                    else
                        Environment.Exit(0);
                }

            }

            public Bloque getBloque(int indexBloque)
            {
                return mem[indexBloque];
            }

        }

        public class Processor
        {
            //Attributes
            //LocalMem localMemory;


            //Methods
        }

        LocalMem memoria = new LocalMem(24);
        static void Main(string[] args)
        {
            allocateInstInMem();
        }

        public void allocateInstInMem()
        {
            string filePath = @"C:\Users\Esteban\Downloads\0.txt";
            string[] lines = File.ReadAllLines(filePath);
            for (int line = 0; line < lines.Length; line++)
            {
                string[] instructionParts = lines[line].Split(' ');
                Instruction inst = new Instruction(int.Parse(instructionParts[0]),
                                                    int.Parse(instructionParts[1]),
                                                    int.Parse(instructionParts[2]),
                                                    int.Parse(instructionParts[3]));
                memoria.insertInstr(inst);
            }
            Console.WriteLine(memoria.getBloque(5).word0.operation);
        }
    }
}
