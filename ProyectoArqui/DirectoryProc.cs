using System;
using System.Collections.Generic;
using System.Text;

namespace ProyectoArqui
{
    public class DirectoryProc
    {
        public enum dirStates { U, S, M }
        dirStates[] block_states;
        Boolean[,] caches_matrix;
        int n_caches;
        Processor parent;

        // Construye las dos matrices seg�n la cantidad de bloques y caches ingresados
        // Lleva dos matrices:
        // - Una es de dimensiones 2 x cantBloques, lleva en cada fila la etiqueta del bloque y su estado
        // - Otra es de dimensiones cantidadCaches x cantBloques, lleva en cada fila 
        public DirectoryProc(int n_blocks, int n_caches, Processor prnt)
        {
            parent = prnt;
            this.n_caches = n_caches;
            block_states = new dirStates[n_blocks];
            caches_matrix = new Boolean[n_caches, n_blocks];
        }


        public dirStates[] getStates()
        {
            return block_states;
        }

        public dirStates getStateOfBlock(int dirBlock)
        {
            if (dirBlock < Computer.p0_sharedmem_size)
            {
                return block_states[dirBlock];
            }
            else
            {
                return block_states[dirBlock - Computer.p0_sharedmem_size];
            }

        }

        public string toString()
        {
            string s = "\nblock\tstatus\t ";
            for (int j = 0; j < n_caches; j++)
            {
                s +=j+"\t";
            }
            s += "\n";


            for (int i = 0; i < block_states.Length; i++)
            {
                s += i + "\t" + block_states[i] + "\t";
                for (int j = 0; j < n_caches; j++)
                {
                    s += caches_matrix[j, i]? caches_matrix[j, i] +"\t": " - \t";
                }
                s += "\n";
            }
            return s;
        }

        public Boolean[,] getCacheMatrix()
        {
            return caches_matrix;
        }

        // Set state to U
        public void setUState(int dirBloque)
        {
            block_states[dirBloque] = dirStates.U;
        }

        public Processor getParent()
        {
            return parent;
        }

        public int getIdOfCacheWithBlock(int dirBloque)
        {
            int i = 0;
            while (i < n_caches)
            {
                if (caches_matrix[dirBloque, i] == true)
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        public void setState(int bloque, dirStates state)
        {
            //Console.WriteLine(bloque + " " + state);
            block_states[bloque % block_states.Length] = state;
        }

    }

}
