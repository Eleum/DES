using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DES
{
    enum TransposeType
    {
        Message,
        CompressedKey, 
        RoundKey,
        ExpandedBlock,
        SBoxPermutation
    }
}
