using Resto.Front.Api.V6;
using Resto.Front.Api.V6.Attributes;
using Resto.Front.Api.V6.Attributes.JetBrains;
using Resto.Front.Api.V6.Exceptions;
using Resto.Front.Api.YagodaPlugin;
using Resto.Front.Api.YagodaPluginCore;
using System.Reactive.Disposables;

namespace Resto.Front.Api.YagodaPlug
{
    [UsedImplicitly]
    [PluginLicenseModuleId(21005108)]
    public sealed class YagodaPlug : IFrontPlugin
    {
        //private readonly Stack<IDisposable> subscriptions = new Stack<IDisposable>();
        private readonly CompositeDisposable subscriptions;

        private static ILog logger;
         
        public YagodaPlug()
        {
            logger = PluginContext.Log;
            logger.Info("Initializing YagodaPlugin");
            subscriptions = new CompositeDisposable();

            var paymentYagoda = new YagodaPaymentPlugin();

            subscriptions.Add(paymentYagoda);
            try
            {
                subscriptions.Add(PluginContext.Operations.RegisterPaymentSystem(paymentYagoda));
            }
            catch (LicenseRestrictionException ex)
            {
                PluginContext.Log.Warn(ex.Message);
                return;
            }
            catch (PaymentSystemRegistrationException ex)
            {
                PluginContext.Log.WarnFormat("Payment system '{0}': '{1}' wasn't registered. Reason: {2}", paymentYagoda.PaymentSystemKey, paymentYagoda.PaymentSystemName, ex.Message);
                return;
            }

            PluginContext.Log.InfoFormat("Payment system '{0}': '{1}' was successfully registered on server.", paymentYagoda.PaymentSystemKey, paymentYagoda.PaymentSystemName);

            subscriptions.Add(new PluginCore(logger));

            logger.Info("YagodaPlugin");
        }

        public void Dispose()
        {
            if (subscriptions != null)
                subscriptions.Dispose();
            //while (subscriptions.Any())
            //{
            //    var subscription = subscriptions.Pop();
            //    try
            //    {
            //        subscription.Dispose();
            //    }
            //    catch (RemotingException)
            //    {
            //        logger.Info("Ошибка освобождения ресурсов");
            //    }
            //}

            //logger.Info("YagodaPlugin stopped");
        }
    }
}