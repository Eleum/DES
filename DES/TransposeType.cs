using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DES
{
    enum TransposeType
    {
        InitialPermutation,
        FinalPermutation,
        CompressedKey, 
        RoundKey,
        ExpandedBlock,
        SBoxPermutation
    }
}
