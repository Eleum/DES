using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DES
{
    public class RoundInfo
    {
        public int RoundNo { get; set; }
        public string LeftPart { get; set; }
        public string RightPart { get; set; }
        public string RoundKey { get; set; }
    }
}
