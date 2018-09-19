using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DES
{
    public class ManagerMatrix
    {
        public static int[,] GetInitialPermutationMatrix()
        {
            var matrix = new int[8, 8];
            for (int i = 0, start = 58; i < 8; i++, start += i != 4 ? 2 : -7)
            {
                for (int j = 0; j < 8; j++)
                {
                    matrix[i, j] = start - j * 8;
                }
            }

            return matrix;
        }

        public static int[,] GetFinalTranspositionMatrix()
        {
            var matrix = new int[8, 8];
            for(int i = 0, start = 40; i < 8; i++, start += i % 2 == 0 ? 8 : 0) // rows
            {
                for(int step = 0, startInner = start; step < 2; step++, startInner -= 32 * (2-step), i += 2-step)
                {
                    for (int j = 0; j < 8; j++) //cols
                    {
                        matrix[i, j] = startInner - j;
                    }
                }
            }

            return matrix;
        }

        public static int[,] GetCompressedKeyMatrix()
        {
            var matrix = new int[4, 14];
            var koeff = 1; // handles changing of start variable
            for(int i = 0, start = 57, inner = 0; i < 4; start += koeff)
            {
                for(int j = 0; j < 8; j++, inner++)
                {
                    if (inner > 13)
                    {
                        inner = 0;
                        i++;

                        if (i == 2)
                        {
                            koeff = -1;
                            start = 64; // start second key block with 63
                        }
                        
                        if (i % 2 == 0) break; // end loops
                    }
                    matrix[i, inner] = start - j * 8;
                }
                if (start == 61) start = 29; // for some reason start of last 4 symbols is 28
            }

            return matrix;
        }

        /// <summary>
        /// Use this to get the round key from 56 bits key
        /// </summary>
        /// <returns></returns>
        public static int[,] GetPBoxCompressMatrix()
        {
            return new[,] 
            {{ 14, 17, 11, 24, 1, 5, 3, 28 },
             { 15, 6, 21, 10, 23, 19, 12, 4},
             { 26, 8, 16, 7, 27, 20, 13, 2},
             { 41, 52, 31, 37, 47, 55, 30, 40},
             { 51, 45, 33, 48, 44, 49, 39, 56},
             { 34, 53, 46, 42, 50, 36, 29, 32}};
        }

        public static int[,] GetExpandedBlockMatrix()
        {
            var matrix = new int[8, 6];
            matrix[0, 0] = 32;

            for(int i = 0, item = 0; i < 8; i++, item -= 2)
            {
                for(int j = 0; j < 6; j++, item++)
                {
                    if (i == 0 && j == 0) continue;
                    matrix[i, j] = item;
                }
            }

            matrix[7, 5] = 1;

            return matrix;
        }

        public static int[,] GetSBoxMatrix(int idx)
        {
            int[,] sbox = null;

            switch (idx)
            {
                case 1:
                    sbox = new int[4, 16] {
                        { 14, 4, 13, 1, 2, 15, 11, 8, 3, 10, 6, 12, 5, 9, 0, 7 },
                        { 0, 15, 7, 4, 14, 2, 13, 1, 10, 6, 12, 11, 9, 5, 3, 8 },
                        { 4, 1, 14, 8, 13, 6, 2, 11, 15, 12, 9, 7, 3, 10, 5, 0 },
                        { 15, 12, 8, 2, 4, 9, 1, 7, 5, 11, 3, 14, 10, 0, 6, 13 }
                    };
                    break;
                case 2:
                    sbox = new int[4, 16] {
                        { 15, 1, 8, 14, 6, 11, 3, 4, 9, 7, 2, 13, 12, 0, 5, 10 },
                        { 3, 13, 4, 7, 15, 2, 8, 14, 12, 0, 1, 10, 6, 9, 11, 5 },
                        { 0, 14, 7, 11, 10, 4, 13, 1, 5, 8, 12, 6, 9, 3, 2, 15 },
                        { 13, 8, 10, 1, 3, 15, 4, 2, 11, 6, 7, 12, 0, 5, 14, 9 }
                    };
                    break;
            }

            return sbox;
        }
    }
}
