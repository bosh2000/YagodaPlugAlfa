using YagodaPluginCore.Data;
using Newtonsoft.Json;
using Resto.Front.Api.V6;
using System;
using System.IO;
using System.Net;
using System.Text;
using Resto.Front.Api.V6.Exceptions;
using Resto.Front.Api.V6.UI;
using System.Globalization;
 
namespace Resto.Front.Api.YagodaPlugCore
{
    public class CoreYagoda : System.IDisposable 
    {
        private WebClient webClient;
        private ILog logger;

        ///// <summary>
        ///// Параметры подключения к серверу Ягоды.
        ///// </summary>
        private SettingYagodaCore setting;

        public CoreYagoda(ILog logger)
        {
            this.logger = logger;
            logger.Info("Инициализация InitYagodaCore");
            var init = new InitYagodaCore(logger);
            if (init == null) { logger.Error("CoreYagoda.constructor;init=null"); } else { logger.Info("CoreYagoda.constructor;Init!=null"); }
            setting = init.GetSetting();
            if (setting == null) { logger.Info("CoreYagoda.constructor;setting==null"); }
        }

        /// <summary>
        /// Подготовка и инициализация WebClient
        /// </summary>
        /// <returns>False при неудачной попытки создать объект.</returns>
        public bool Connect()
        {
            logger.Info("Init WebClient");
            try
            {
                webClient = new WebClient()
                {
                    Encoding = Encoding.UTF8
                };
            }
            catch (WebException exp)
            {
                logger.Error($"CoreYagoda.Connect ; Error {exp.Message}, StackTrace {exp.StackTrace}");
            }

            return webClient == null ? false : true;
        }

        /// <summary>
        /// Получение информации и статуса по номеру телефона.
        /// </summary>
        /// <param name="NumberTel">Номер телефона.</param>
        /// <returns>Класс Entity, десерелизованный Json ответ </returns>
        public Entity GetInfo(string NumberTel)
        {
            string urlRequest = string.Format("{0}:{1}{2}/{3}/getJsonInfo/{4}",
                setting.Url,
                setting.Port,
                setting.PrefixDataBase,
                setting.IdSale,
                NumberTel);

            string responceJson = string.Empty;

            try
            {
                NetworkCredential networkCredential = new NetworkCredential(setting.Login, setting.Password);
                webClient.Credentials = networkCredential;
                responceJson = webClient.DownloadString(new Uri(urlRequest));
            }
            catch (WebException exp)
            {
                var errorString = "Ошибка получения информации по номеру телефона -" + NumberTel;
                errorString += ";" + exp.Message;
                logger.Error(errorString);
            }
#if DEBUG
            logger.Info($"ResponceJson - {responceJson}");
#endif
            Entity result=null;
            try
            {
                result = JsonConvert.DeserializeObject<Entity>(responceJson);
            }catch (JsonException exp)
            {
                logger.Error($"Exp - {exp.Message}");
            }

#if DEBUG
            logger.Info($"Entity - {result.ToString()}");
#endif

            return result;
        }

        /// <summary>
        /// Отправка  на сервер покупки.
        /// </summary>
        /// <param name="purchase"></param>
        /// <returns></returns>
        public bool WritePurchase(Purchase purchase)
        {
            // Заполняем поля данными о магазине
            purchase.shopCode = setting.ShopCode;
            purchase.shopName = setting.ShopName;

            // заполняем неиспользуеммые пока поля
            purchase.discountCard = string.Empty;

            if (purchase == null) { logger.Error("CoreYagoda.WritePurchase;Purchase = null"); return false; } else { logger.Info("CoreYagoda.WritePurchase"); }

            var json = string.Empty;

            try
            {
                json = JsonConvert.SerializeObject(purchase);
            }
            catch (Exception exp)
            {
                logger.Error($"Ошибка серилиазации объекта purchase, {exp.Message},{exp.StackTrace}");
                throw new PaymentActionCancelledException();
            }
            var response = string.Empty;

            try
            {
                NetworkCredential networkCredential = new NetworkCredential(setting.Login, setting.Password);
                var httpRequest = (HttpWebRequest)WebRequest.Create(new Uri(setting.Url + ":" + setting.Port + setting.PrefixDataBase + "/" + setting.IdSale + "/postdata"));
                httpRequest.Method = "POST";
                httpRequest.Credentials = networkCredential;
                httpRequest.ContentType = "application/json";

                using (var requestStream = httpRequest.GetRequestStream())
                using (var writer = new StreamWriter(requestStream))
                {
                    writer.Write(json);
                }
                using (var httpResponse = httpRequest.GetResponse())
                using (var responseStream = httpResponse.GetResponseStream())
                using (var reader = new StreamReader(responseStream))
                {
                    response = reader.ReadToEnd();
                }
            }
            catch (WebException exp)
            {
                var errorString = "Ошибка записи бонусов для клиента - " + purchase.buyerTel;
                errorString += ";" + exp.Message;
                logger.Error(errorString);
            }
            logger.Info("Ответ сервера при записи покупки - " + response);
            return true;
        }

        /// <summary>
        /// Списание бонусов при оплате бонусами.
        /// </summary>
        /// <param name="purchase">Покупка</param>
        /// <returns>Если списание прошло успешно то true/</returns>
        public bool WriteOffPurchase(Purchase purchase, IViewManager viewManager)
        {
            // Заполняем поля данными о магазине
            purchase.shopCode = setting.ShopCode;
            purchase.shopName = setting.ShopName;
            // Заполняем поле оплаты бонусами.
            //purchase.payByBonus = purchase.checkAmount;
            // Получение баланса по номеру телефона.
            Balance balance = GetBalans(purchase.buyerTel);
#if DEBUG
            logger.Info($"Balance.data.balance - {Double.Parse(balance.data.balance, CultureInfo.InvariantCulture)}");
            logger.Info($"purchase.checkAmount - {Double.Parse(purchase.checkAmount, CultureInfo.InvariantCulture)}");
#endif
            // Если баланс по балам меньше суммы чека, то возврат с ошибкой.
            if (Double.Parse(balance.data.balance, CultureInfo.InvariantCulture) <= Double.Parse(purchase.checkAmount))
            {
                var notificationString = string.Format("Недостаточно балов для списания: Телефон :{0}, баланс:{1}", purchase.buyerTel, balance.data.balance);
                PluginContext.Operations.AddNotificationMessage(notificationString, "Yagoda", TimeSpan.FromSeconds(15));
                throw new PaymentActionCancelledException();
            }

            string pinCode = GeneratePinCode();
#if DEBUG
            logger.Info($"PinCode - {pinCode}");
#endif
            SendSms(purchase.buyerTel, pinCode);
            string pinCodeValue = viewManager.ShowKeyboard("Введите пин-код:");

            if (!pinCodeValue.Equals(pinCode))
            {
                logger.Info($"Введенный пин кода {pinCodeValue} не совпал с высланным {pinCode} на телефон {purchase.buyerTel}");
                throw new PaymentActionCancelledException();
            }
            // заполняем неиспользуеммые пока поля
            purchase.discountCard = string.Empty;

            if (purchase == null) { logger.Error("CoreYagoda.WriteOffPurchase;Purchase = null"); return false; } else { logger.Info("CoreYagoda.WriteOffPurchase"); }

            var json = string.Empty;

            try
            {
                json = JsonConvert.SerializeObject(purchase);
            }
            catch (Exception exp)
            {
                logger.Error($"Ошибка серилиазации объекта purchase, {exp.Message},{exp.StackTrace}");
                throw new PaymentActionCancelledException();
            }
            var response = string.Empty;

            try
            {
                NetworkCredential networkCredential = new NetworkCredential(setting.Login, setting.Password);
                var httpRequest = (HttpWebRequest)WebRequest.Create(new Uri(setting.Url + ":" + setting.Port + setting.PrefixDataBase + "/" + setting.IdSale + "/postdata"));
                httpRequest.Method = "POST";
                httpRequest.Credentials = networkCredential;
                httpRequest.ContentType = "application/json";

                using (var requestStream = httpRequest.GetRequestStream())
                using (var writer = new StreamWriter(requestStream))
                {
                    writer.Write(json);
                }
                using (var httpResponse = httpRequest.GetResponse())
                using (var responseStream = httpResponse.GetResponseStream())
                using (var reader = new StreamReader(responseStream))
                {
                    response = reader.ReadToEnd();
                }
            }
            catch (WebException exp)
            {
                var errorString = "Ошибка записи бонусов для клиента - " + purchase.buyerTel;
                errorString += ";" + exp.Message;
                logger.Error(errorString);
                throw new PaymentActionCancelledException();
            }
            logger.Info("Ответ сервера при записи покупки - " + response);
            return true;
        }

        /// <summary>
        /// Получение по номеру телефона баланса.
        /// </summary>
        /// <param name="numberTel"></param>
        /// <returns>Класс Balans </returns>
        public Balance GetBalans(string numberTel)
        {
            
            string urlRequest = string.Format("{0}:{1}{2}/{3}/getBalance/{4}",
                 setting.Url,
                 setting.Port,
                 setting.PrefixDataBase,
                 setting.IdSale,
                 numberTel.Trim(new char[] { '+' }));

            string responceJson = string.Empty;

            try
            {
                NetworkCredential networkCredential = new NetworkCredential(setting.Login, setting.Password);
                webClient.Credentials = networkCredential;
                responceJson = webClient.DownloadString(new Uri(urlRequest));
            }
            catch (WebException exp)
            {
                var errorString = "Ошибка получения информации по номеру телефона -" + numberTel;
                errorString += ";" + exp.Message;
                logger.Error(errorString);
            }
#if DEBUG
            logger.Info($"responceJson - {responceJson}");
#endif
            Balance balance = null;
            try
            {
                balance = JsonConvert.DeserializeObject<Balance>(responceJson);
            }catch(Exception exp)
            {
                logger.Error($"Exp - {exp.Message}");
            }
            return balance;
        }

        /// <summary>
        /// Отсылка смс с пинкодом для проверки оплаты балами.
        /// </summary>
        /// <param name="numberTel"></param>
        /// <param name="pinCode"></param>
        public void SendSms(string numberTel, string pinCode)
        {

            var json = string.Empty;
            PinCodeSms pinCodeSms = new PinCodeSms();
            pinCodeSms.buyerTel = numberTel.Trim(new char[] { '+' });
            pinCodeSms.text = pinCode;
            try
            {
                json = JsonConvert.SerializeObject(pinCodeSms);
            }
            catch (Exception exp)
            {
                logger.Error($"Ошибка серилиазации объекта PinCodeSms, {exp.Message},{exp.StackTrace}");
                throw new PaymentActionCancelledException();
            }
            var response = string.Empty;

            try
            {
                NetworkCredential networkCredential = new NetworkCredential(setting.Login, setting.Password);
                var httpRequest = (HttpWebRequest)WebRequest.Create(new Uri(setting.Url + ":" + setting.Port + setting.PrefixDataBase + "/" + setting.IdSale + "/send-sms"));
                httpRequest.Method = "POST";
                httpRequest.Credentials = networkCredential;
                httpRequest.ContentType = "application/json";

                using (var requestStream = httpRequest.GetRequestStream())
                using (var writer = new StreamWriter(requestStream))
                {
                    writer.Write(json);
                }
                using (var httpResponse = httpRequest.GetResponse())
                using (var responseStream = httpResponse.GetResponseStream())
                using (var reader = new StreamReader(responseStream))
                {
                    response = reader.ReadToEnd();
                }
            }
            catch (WebException exp)
            {
                var errorString = "Ошибка отправки смс для клиента - " + numberTel;
                errorString += ";" + exp.Message;
                logger.Error(errorString);
                throw new PaymentActionCancelledException();
            }
            logger.Info("Ответ сервера при отправки смс - " + response);
        }

        public string GeneratePinCode()
        {
            string pinCode = string.Empty;
            Random rnd = new Random();
            int rndPinCode = rnd.Next(9999);
            pinCode = rndPinCode.ToString("D4");
#if DEBUG
            logger.Info($"Generate pincode - {pinCode}");
#endif
            return pinCode;
        }
        /// <summary>
        /// Реализация интерфейся IDisposable, для закрытия рессурсов.
        /// </summary>
        public void Dispose()
        {
            logger.Info("Закрытие рессурсов webClient");
            webClient.Dispose();
        }
    }
}