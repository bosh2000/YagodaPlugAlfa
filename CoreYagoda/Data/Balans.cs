using System;
namespace YagodaPluginCore.Data
{
    /// <summary>
    /// Класс описывающий ответ сервера при запросе баланса.
    /// </summary>
    ///
    [Serializable]
   
    public class Balance
    { 
        public Data data { get; set; }

        public string status { get; set; }
    }

    public class Data
    {
        /// <summary>
        /// Баланс бонусов.
        /// </summary>
        public string balance { get; set; }

        /// <summary>
        /// Процент возможной оплаты суммы покупки бонусами.
        /// </summary>
        public string payBonusesNoMorePercent { get; set; }

        /// <summary>
        /// Комментарий.
        /// </summary>
        public string comment { get; set; }
    }
}