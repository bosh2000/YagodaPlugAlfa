namespace Resto.Front.Api.YagodaPlugCore
{
    /// <summary>
    /// Класс с описанием настроек.
    /// </summary> 
    public class SettingYagodaCore
    {
        /// <summary>
        /// Параметры подключения к серверу Ягоды.
        /// </summary>

        /// URL или IP адрес сервера Yagoda.
        public string Url { get; set; }

        /// <summary>
        /// Логин доступа к серверу Yagoda.
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Пароль доступа к серверу Yagoda.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Порт для подключения к серверу Yagoda.
        /// </summary>
        public string Port { get; set; }

        /// <summary>
        /// Идентификатор участника\продавца в системе Yagoda.
        /// </summary>
        public string IdSale { get; set; }

        /// <summary>
        /// Префикс используеммой базы данных.
        /// </summary>
        public string PrefixDataBase { get; set; }

        /// <summary>
        /// ИД магазина
        /// </summary>
        public string ShopCode { get; set; }

        /// <summary>
        /// Название, или адрес магазина
        /// </summary>
        public string ShopName { get; set; }
    }
}