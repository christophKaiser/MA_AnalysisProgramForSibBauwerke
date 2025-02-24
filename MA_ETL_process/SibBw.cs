using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MA_ETL_process
{
    internal class SibBw
    {
        public Dictionary<string, string> stringValues = new Dictionary<string, string>();
        public Dictionary<string, double> numberValues = new Dictionary<string, double>();
    }

    internal class SibBW_GES_BW : SibBw
    {
        public List<SibBW_TEIL_BW> teilbauwerke = new List<SibBW_TEIL_BW>();
    }

    internal class SibBW_TEIL_BW : SibBw
    {

    }
}
