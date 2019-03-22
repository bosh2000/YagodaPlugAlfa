using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YagodaPluginCore.Data
{
    /// <summary>
    /// Класс запроса Пин кода 
    /// </summary>
    [Serializable]
    class PinCodeSms 
    {
            public string buyerTel { get; set; }
            public string text { get; set; }

    }
}
