using System;
using System.Collections.Generic;

namespace Resto.Front.Api.YagodaPlugCore
{
    /// <summary>
    /// Класс описывающий Json ответ сервера.
    /// </summary>
    ///
    [Serializable]
    public class Entity
    {
        /// <summary> 
        /// Профиль участника программы.
        /// </summary>
        public Profile profile { get; set; }

        /// <summary>
        /// Статус участиника программы.
        /// </summary>
        public string status { get; set; }

        /// <summary>
        /// Информация о балансе и покупках участника программы.
        /// </summary>
        public Info info { get; set; }

        /// <summary>
        /// Переопределенный метод ToString().
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return profile.ToString() + "\n\r" + info.ToString();
        }
    }

    public class Profile
    {
        /// <summary>
        /// Имя.
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// День рождения.
        /// </summary>
        public string birthDate { get; set; }

        /// <summary>
        /// Пол.
        /// </summary>
        public string sex { get; set; }

        /// <summary>
        /// Кто рекомендовал.
        /// </summary>
        public string fromSomeone { get; set; }

        /// <summary>
        /// Коментарий.
        /// </summary>
        public string comment { get; set; }

        public override string ToString()
        {
            return name + "\n\r" +
                birthDate + "\n\r" +
                sex + "\n\r" +
                fromSomeone + "\n\r" +
                comment;
        }
    }

    public class Info
    {
        /// <summary>
        /// Бонусный баланс.
        /// </summary>
        public double balance { get; set; }

        /// <summary>
        /// Средний чек.
        /// </summary>
        public int averagePurchaseAmount { get; set; }

        /// <summary>
        /// Общая сумма покупок.
        /// </summary>
        public int rubSum { get; set; }

        /// <summary>
        /// Статус.
        /// </summary>
        public string status { get; set; }

        /// <summary>
        /// Возвраст.
        /// </summary>
        public string age { get; set; }

        /// <summary>
        /// Количество покупок в месяц.
        /// </summary>
        public string monthCount { get; set; }

        /// <summary>
        /// Последняя покупка.
        /// </summary>
        public List<object> lastPurchase { get; set; }

        /// <summary>
        /// Сегмент покупок.
        /// </summary>
        public List<string> segments { get; set; }

        public override string ToString()
        {
            string retValue = balance + "\n\r" +
                averagePurchaseAmount + "\n\r" +
                rubSum + "\n\r" +
                status + "\n\r" +
                age + "\n\r" +
                monthCount + "\n\r";
            foreach (object obj in lastPurchase)
            {
                retValue += obj.ToString() + "\n\r";
            }

            foreach (string str in segments)
            {
                retValue += str + "\n\r";
            }

            return retValue;
        }
    }
}