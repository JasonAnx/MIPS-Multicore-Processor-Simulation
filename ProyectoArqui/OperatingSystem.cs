using System;
using System.IO;

namespace ProyectoArqui
{

    class OperatingSystem
    {
        public static uint userQuantum;
        public static bool slowModeActivated;
        public static bool valueShMem;


        //Lee los folders con los hillios 
        public static void allocateInstInMem()
        {
            string programPath = chooseProgramFolder();

            if (programPath == null)
            {
                logError("Wrong program path specified");
                Environment.Exit(66);
            }

            for (int numProcessor = 0; numProcessor < Computer.processors.Length; numProcessor++)
            {

                string folderPath = programPath + "p" + numProcessor;

                string[] files = System.IO.Directory.GetFiles(folderPath);
                int instr_ptr = 0;
                foreach (string filePath in files)
                {
                    try
                    {
                        //Console.Write(filePath + "> ");
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
                                logError("Unable to insert instruction " + inst.toString() +
                                          " on instruction memory of processor " + numProcessor);
                            }


                        }
                        /* Path converts   
                         *              ./programs\litle test - lw+sw/p0\1.txt
                         *  to
                         *              1.txt
                         */
                        Computer.processors[numProcessor].createContext(instr_ptr * 4, Path.GetFileName(filePath));
                        instr_ptr += lines.Length;
                        //Console.WriteLine(memoria.getBloque(5).word0.operation);
                    }
                    catch (FileNotFoundException e)
                    {
                        logError("File not found: " + e.FileName);
                        logError("Could not load program");
                        Environment.Exit(11);
                        //Console.WriteLine("An error occurred: '{0}'", e);
                    }
                }
            }
        }

        private static string chooseProgramFolder()
        {
            // TODO add try catch
            string[] dirs = System.IO.Directory.GetDirectories("./programs");

            string menu = "Programs found in this Computer> \n";
            for (int i = 0; i < dirs.Length; i++)
            {
                menu += "\t[" + i + "] " + dirs[i].Replace("./programs", "") + "\n";
            }

            log(menu);
            log("Enter the number of the program you want to run> ");

            uint programIndex;

            if (!UInt32.TryParse(Console.ReadLine(), out programIndex))
                return null;

            if (programIndex < dirs.Length)
            {
                return dirs[(int)programIndex] + "/";
            }
            else return null;
        }
        
        //Metodos de impresion de mensajes

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
                Console.WriteLine("\tProgram Halted. Press any key to continue");
                Environment.Exit(1);
            }
            else Console.ResetColor();
        }
    }
}
