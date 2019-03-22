using Resto.Front.Api.V6;
using Resto.Front.Api.V6.Data.View;
using Resto.Front.Api.V6.Extensions;
using Resto.Front.Api.V6.UI;
using Resto.Front.Api.YagodaPlugCore;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace Resto.Front.Api.YagodaPlugin
{
    internal sealed class PluginCore : IDisposable
    {
        private readonly CompositeDisposable subscriptions;
         
        /// <summary>
        /// Экземляр логера.
        /// </summary>
        private ILog logger;

        public PluginCore(ILog logger)
        {
            this.logger = logger;
            subscriptions = new CompositeDisposable
            {
                PluginContext.Integration.AddButton("Yagoda", ShowListPopup),
            };
        }

        /// <summary>
        /// Реализация интерфейса IDisposable.
        /// </summary>
        public void Dispose()
        {
            subscriptions.Dispose();
        }

        /// <summary>
        /// Вывод меню плагина.
        /// </summary>
        /// <param name="viewManager"></param>
        /// <param name="receiptPrinter"></param>
        /// <param name="progressBar"></param>
        private void ShowListPopup(IViewManager viewManager, IReceiptPrinter receiptPrinter, IProgressBar progressBar)
        {
            var list = new List<string> { "Узнать баланс.", "Начислить бонусы.", "Списать бонусы" };

            var selectedItem = list[2];
            var inputResult = viewManager.ShowChooserPopup("Yagoda", list, i => i, selectedItem, ButtonWidth.Narrower);

            DisplayBonus(ShowKeyboardPopup(viewManager, receiptPrinter, progressBar));
        }

        /// <summary>
        /// Вывод экранной клавиатуры для ввода номера телефона.
        /// </summary>
        /// <param name="viewManager"></param>
        /// <param name="receiptPrinter"></param>
        /// <param name="progressBar"></param>
        private IPhoneInputResult ShowKeyboardPopup(IViewManager viewManager, IReceiptPrinter receiptPrinter, IProgressBar progressBar)
        {
            return (IPhoneInputResult)viewManager.ShowExtendedNumericInputPopup("Введите номер телефона:", "Номер телефона",
                new ExtendedInputSettings() { EnablePhone = true });
        }

        /// <summary>
        /// Вывод количества бонусов и ФИО через внутренние сообщения.
        /// </summary>
        /// <param name="inputResult"></param>
        public void DisplayBonus(IPhoneInputResult inputResult)
        {
            logger.Info("После клавиатуры.");
            Entity entity;
            CoreYagoda yagodaCore = null;
            try
            {
                yagodaCore = new CoreYagoda(logger);
            }
            catch (Exception exc)
            {
                logger.Error(exc.Message);
            }

            try
            {
                logger.Info("Открываем подключение к базе...");
                yagodaCore.Connect();
            }
            catch (Exception exp)
            {
                logger.Error("Ошибка подключения к базе." + exp.Message);
            }
            finally
            {
                logger.Info("Подключение к базе успешно.");
            }

            if (yagodaCore == null) { logger.Error("yagodaCore=Null"); }
            entity = yagodaCore.GetInfo(inputResult.PhoneNumber);

            logger.Info("Entity - " + entity);
            var notificationString = string.Format("Имя:{0}, баланс:{1}", entity.profile.name, entity.info.balance);
            PluginContext.Operations.AddNotificationMessage(notificationString, "Yagoda", TimeSpan.FromSeconds(15));
            yagodaCore.Dispose();
        }
    }
}