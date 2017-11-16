using System;
using System.IO;

namespace ProyectoArqui
{
  
    class OperatingSystem
    {
        public int userQuantum;
        public bool slowModeActivated;
        public void allocateInstInMem()
        {
            for (int numProcessor = 0; numProcessor < Computer.processors.Length; numProcessor++)
            {
                string folderPath = "p" + numProcessor;
                string[] files = System.IO.Directory.GetFiles(folderPath);
                int instr_ptr = 0;
                int idx = 0;
                foreach (string filePath in files)
                {
                    try
                    {
                        Console.Write(filePath+"> ");
                        string[] lines = File.ReadAllLines(filePath);
                        for (int line = 0; line < lines.Length; line++)
                        {
                            string[] instructionParts = lines[line].Split(' ');
                            Instruction inst = new Instruction(int.Parse(instructionParts[0]),
                                                                int.Parse(instructionParts[1]),
                                                                int.Parse(instructionParts[2]),
                                                                int.Parse(instructionParts[3]));
                            try
                            {
                                Computer.processors[numProcessor].isntrmem.insertInstr(inst);
                            }
                            catch
                            {
                                logError("Unable to insert instruction " + inst.printValue() +
                                          " on instruction memory of processor " + numProcessor);
                            }


                        }
                        Computer.processors[numProcessor].createContext(instr_ptr, idx);
                        idx++;
                        instr_ptr += lines.Length;
                        //Console.WriteLine(memoria.getBloque(5).word0.operation);
                    }
                    catch (FileNotFoundException e)
                    {
                        logError("File not found: " + filePath);
                        logError("Could not load program");
                        Environment.Exit(11);
                        //Console.WriteLine("An error occurred: '{0}'", e);
                    }
                }
                //Console.WriteLine(memoria.getBloque(5).word0.operation);
            }
        }

        public static void log(string s)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[OS Message]: " + s);
            Console.ResetColor();
        }

        public static void logError(string s, bool halt = false)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[OS Message]: " + s);
            if (halt)
            {
                Console.WriteLine("\tProgram Halted. Press any key to exit");
                Console.ReadLine();
            }
            else Console.ResetColor();
        }
    }
}
