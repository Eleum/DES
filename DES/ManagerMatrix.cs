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
            for (int i = 0, start = 40; i < 8; i++, start += i % 2 == 0 ? 8 : 0) // rows
            {
                for (int step = 0, startInner = start; step < 2; step++, startInner -= 32 * (2 - step), i += 2 - step)
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
            for (int i = 0, start = 57, inner = 0; i < 4; start += koeff)
            {
                for (int j = 0; j < 8; j++, inner++)
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

            for (int i = 0, item = 0; i < 8; i++, item -= 2)
            {
                for (int j = 0; j < 6; j++, item++)
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
                case 3:
                    sbox = new int[4, 16] {
                        { 10, 0, 9, 14, 6, 3, 15, 5, 1, 13, 12, 7, 11, 4, 2, 8 },
                        { 13, 7, 0, 9, 3, 4, 6, 10, 2, 8, 5, 14, 12, 11, 15, 1 },
                        { 13, 6, 4, 9, 8, 15, 3, 0, 11, 1, 2, 12, 5, 10, 14, 7 },
                        { 1, 10, 13, 0, 6, 9, 8, 7, 4, 15, 14, 3, 11, 5, 2, 12 }
                    };
                    break;
                case 4:
                    sbox = new int[4, 16] {
                        { 7, 13, 14, 3, 0, 6, 9, 10, 1, 2, 8, 5, 11, 12, 4, 15 },
                        { 13, 8, 11, 5, 6, 15, 0, 3, 4, 7, 2, 12, 1, 10, 14, 9 },
                        { 10, 6, 9, 0, 12, 11, 7, 13, 15, 1, 3, 14, 5, 2, 8, 4 },
                        { 3, 15, 0, 6, 10, 1, 13, 8, 9, 4, 5, 11, 12, 7, 2, 14 },
                    };
                    break;
                case 5:
                    sbox = new int[4, 16] {
                        { 2, 12, 4, 1, 7, 10, 11, 6, 8, 5, 3, 15, 13, 0, 14, 9 },
                        { 14, 11, 2, 12, 4, 7, 13, 1, 5, 0, 15, 10, 3, 9, 8, 6 },
                        { 4, 2, 1, 11, 10, 13, 7, 8, 15, 9, 12, 5, 6, 3, 0, 14 },
                        { 11, 8, 12, 7, 1, 14, 2, 13, 6, 15, 0, 9, 10, 4, 5, 3 }
                    };
                    break;
                case 6:
                    sbox = new int[4, 16] {
                        { 12, 1, 10, 15, 9, 2, 6, 8, 0, 13, 3, 4, 14, 7, 5, 11 },
                        { 10, 15, 4, 2, 7, 12, 9, 5, 6, 1, 13, 14, 0, 11, 3, 8 },
                        { 9, 14, 15, 5, 2, 8, 12, 3, 7, 0, 4, 10, 1, 13, 11, 6 },
                        { 4, 3, 2, 12, 9, 5, 15, 10, 11, 14, 1, 7, 6, 0, 8, 13 }
                    };
                    break;
                case 7:
                    sbox = new int[4, 16] {
                        { 4, 11, 2, 14, 15, 0, 8, 13, 3, 12, 9, 7, 5, 10, 6, 1 },
                        { 13, 0, 11, 7, 4, 9, 1, 10, 14, 3, 5, 12, 2, 15, 8, 6 },
                        { 1, 4, 11, 13, 12, 3, 7, 14, 10, 15, 6, 8, 0, 5, 9, 2 },
                        { 6, 11, 13, 8, 1, 4, 10, 7, 9, 5, 0, 15, 14, 2, 3, 12 }
                    };
                    break;
                case 8:
                    sbox = new int[4, 16] {
                        { 13, 2, 8, 4, 6, 15, 11, 1, 10, 9, 3, 14, 5, 0, 12, 7 },
                        { 1, 15, 13, 8, 10, 3, 7, 4, 12, 5, 6, 11, 0, 14, 9, 2 },
                        { 7, 11, 4, 1, 9, 12, 14, 2, 0, 6, 10, 13, 15, 3, 5, 8 },
                        { 2, 1, 14, 7, 4, 10, 8, 13, 15, 12, 9, 0, 3, 5, 6, 11 }
                    };
                    break;
            }
            return sbox;
        }

        public static int[,] GetSBoxPermutationMatrix()
        {
            return new[,] {
                { 16, 7, 20, 21 },
                { 29, 12, 28, 17 },
                { 1, 15, 23, 26 },
                { 5, 18, 31, 10 },
                { 2, 8, 24, 14 },
                { 32, 27, 3, 9 },
                { 19, 13, 30, 6 },
                { 22, 11, 4, 25 }
            };
        }
    }
}
