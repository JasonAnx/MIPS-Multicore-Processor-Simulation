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

        //Retorna el arreglo de estados del directorio
        public dirStates[] getStates()
        {
            return block_states;
        }

        //Retorna el estado de un bloque especifico en el directorio
        public dirStates getStateOfBlock(int dirBlock)
        {
            //Directorio de proc 0
            if (dirBlock < Computer.p0_sharedmem_size)
            {
                return block_states[dirBlock];
            }
            //Directorio de proc 1
            else
            {
                return block_states[dirBlock - Computer.p0_sharedmem_size];
            }

        }

        //Imprime el directorio
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

        //Retorna los estados de los bloques en el directorio
        public Boolean[,] getCacheMatrix()
        {
            return caches_matrix;
        }

        //Marca un bloque en especifico con estado U
        public void setUState(int dirBloque)
        {
            block_states[dirBloque] = dirStates.U;
        }

        public Processor getParent()
        {
            return parent;
        }

        //Retorna el indice de la cache que tiene al bloque
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

        //Marca un bloque con el estado que recibe como parametro
        public void setState(int bloque, dirStates state)
        {
            //Console.WriteLine(bloque + " " + state);
            block_states[bloque % block_states.Length] = state;
        }

    }

}
