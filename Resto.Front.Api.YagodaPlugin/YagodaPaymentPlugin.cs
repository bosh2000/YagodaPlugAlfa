using Resto.Front.Api.V6;
using Resto.Front.Api.V6.Attributes.JetBrains;
using Resto.Front.Api.V6.Data.Cheques;
using Resto.Front.Api.V6.Data.Orders;
using Resto.Front.Api.V6.Data.Organization;
using Resto.Front.Api.V6.Data.Payments;
using Resto.Front.Api.V6.Data.Security;
using Resto.Front.Api.V6.Data.View;
using Resto.Front.Api.V6.Exceptions;
using Resto.Front.Api.V6.Extensions;
using Resto.Front.Api.V6.UI;
using Resto.Front.Api.YagodaPlugCore;
using Resto.Front.Api.YagodaPluginCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Xml.Linq;

namespace Resto.Front.Api.YagodaPluginCore
{
    public class YagodaPaymentPlugin : MarshalByRefObject, IExternalPaymentProcessor, IDisposable
    { 
        public string PaymentSystemKey { get { return "Yagoda"; } set { } }
        public string PaymentSystemName { get { return "Yagoda Payment"; } set { } }
        private ILog logger;
        private int selectResult;

        private Purchase purchase;

        private CompositeDisposable subscriptions;

        public YagodaPaymentPlugin()
        {
            logger = PluginContext.Log;
            logger.Info("Start YagodaPaymentPlugin constructor");
            try
            {
                subscriptions = new CompositeDisposable
            {
                PluginContext.Notifications.SubscribeOnCafeSessionClosing(CafeSessionClosing),
                PluginContext.Notifications.SubscribeOnCafeSessionOpening(CafeSessionOpening)
            };
            }
            catch (Exception exp)
            {
                logger.Error(GetAllFootprints(exp));
            }
        }

        public string GetAllFootprints(Exception x)
        {
            var st = new StackTrace(x, true);
            var frames = st.GetFrames();
            var traceString = new StringBuilder();

            foreach (var frame in frames)
            {
                if (frame.GetFileLineNumber() < 1)
                    continue;

                traceString.Append("File: " + frame.GetFileName());
                traceString.Append(", Method:" + frame.GetMethod().Name);
                traceString.Append(", LineNumber: " + frame.GetFileLineNumber());
                traceString.Append("  -->  ");
            }

            return traceString.ToString();
        }

        public void CollectData(Guid orderId, Guid paymentTypeId, IUser cashier, IReceiptPrinter printer, IViewManager viewManager, IPaymentDataContext context, IProgressBar progressBar)
        {
            PluginContext.Log.InfoFormat("Collect data for order ({0})", orderId);
            var input = (IPhoneInputResult)viewManager.ShowExtendedNumericInputPopup("Введите номер телефона:", "Номер телефона",
                new ExtendedInputSettings() { EnablePhone = true });

            IList<string> chooseList = new List<string>
            {
                "Зачисление бонусов.",
                "Оплата бонусами."
            };
            selectResult = viewManager.ShowChooserPopup("Выберете действие", chooseList);

            //var orderTest = PluginContext.Operations.GetOrders().Last();
            //var credential = PluginContext.Operations.AuthenticateByPin("0858");
            //PluginContext.Operations.PrintOrderItems(credential, orderTest,
            //    new List<IOrderRootItem> { orderTest.Items.Last() });

            var orderTest = GetOrderSafe(orderId);
            var orderItems = orderTest.Items;
            logger.Info($"Items count - {orderItems.Count}");
            foreach (var orderItem in orderItems)
            {
                if (!(orderItem is IOrderProductItem)) continue;
                var product = orderItem as IOrderProductItem;
                logger.Info($"Item - {product.Product.Name}");
                logger.Info($"  ItemGroup - {PluginContext.Operations.TryGetParentByProduct(product.Product).Name}");
            }

            if (selectResult == 0)
            {
                // Зачисление бонусов.
                if (input == null)
                    throw new PaymentActionCancelledException();

                progressBar.ChangeMessage("Обработка данных Yagoda");
                var order = GetOrderSafe(orderId);
                var productByOrder = PluginContext.Operations.GetOrderItemProductGroups(order);
                foreach(IReadOnlyList<IReadOnlyList<IOrderCookingItem>> var1 in productByOrder)
                {
                    foreach(IOrderCookingItem var3 in var1)
                    {

                    }
                }
                PluginContext.Log.InfoFormat("Number   {0}, Order id  {1} , Order sum {2}", input.PhoneNumber, orderId, order.ResultSum);
                purchase = new Purchase
                {
                    buyerTel = input.PhoneNumber,
                    uuid = orderId.ToString(),
                    seller = order?.Cashier?.Name,
                    checkAmount = order.ResultSum.ToString()
                };
                purchase.goods = new List<Good>();
                foreach (var orderItem in orderItems)
                {
                    if (!(orderItem is IOrderProductItem)) continue;
                    var product = orderItem as IOrderProductItem;
                    logger.Info($"Item - {product.Product.Name}");
                    logger.Info($"  ItemGroup - {PluginContext.Operations.TryGetParentByProduct(product.Product).Name}");
                    Good good = new Good();
                    string goodGroupName= PluginContext.Operations.TryGetParentByProduct(product.Product).Name;
                    if (!string.IsNullOrEmpty(goodGroupName))
                    {
                        GoodsGroup goodsGroup = new GoodsGroup();
                        goodsGroup.groupName = goodGroupName;
                        goodsGroup.groupCode = PluginContext.Operations.TryGetParentByProduct(product.Product).Id.ToString();
                        good.goodsGroup = goodsGroup;
                    }
                    good.goodsName = product.Product.Name;
                    good.price = product.Product.Price.ToString();
                    good.applyBonus = true;
                    purchase.goods.Add(good);
                }
            }
            if (selectResult == 1)
            {
                // Оплата бонусами.
                
                if (input == null)
                    throw new PaymentActionCancelledException();

                progressBar.ChangeMessage("Обработка данных Yagoda");
                var order = GetOrderSafe(orderId);

                foreach (IPaymentItem payment in order.Payments)
                {
                    logger.Info($"payment - {payment.Type.Name}");
                }


                var productByOrder = PluginContext.Operations.GetOrderItemProductGroups(order);
                PluginContext.Log.InfoFormat("Number   {0}, Order id  {1} , Order sum {2}", input.PhoneNumber, orderId, order.ResultSum);
                purchase = new Purchase
                {
                    buyerTel = input.PhoneNumber,
                    uuid = orderId.ToString(),
                    seller = order?.Cashier?.Name,
                    checkAmount = order.ResultSum.ToString()
                };
                purchase.goods = new List<Good>();
            }
        }


        [CanBeNull]
        private IOrder GetOrderSafe(Guid? orderId)
        {
            return orderId.HasValue ? PluginContext.Operations.TryGetOrderById(orderId.Value) : null;
        }

        public void EmergencyCancelPayment(decimal sum, Guid? orderId, Guid paymentTypeId, Guid transactionId, IPointOfSale pointOfSale, IUser cashier, IReceiptPrinter printer, IViewManager viewManager, IPaymentDataContext context, IProgressBar progressBar)
        {
            logger.Info("EmergencyCancelPayment");
            //throw new NotImplementedException();
        }

        public void OnPaymentAdded(IOrder order, IPaymentItem paymentItem, IUser cashier, IOperationService operationService, IReceiptPrinter printer, IViewManager viewManager, IPaymentDataContext context, IProgressBar progressBar)
        {
            logger.Info("OnPaymentAdded");
            logger.Info($"paymentItem {paymentItem.Type.Name}, sum - {paymentItem.Sum}");
            //throw new NotImplementedException();
        }

        public bool OnPreliminaryPaymentEditing(IOrder order, IPaymentItem paymentItem, IUser cashier, IOperationService operationService, IReceiptPrinter printer, IViewManager viewManager, IPaymentDataContext context, IProgressBar progressBar)
        {
            logger.Info("OnPreliminaryPaymentEditing");
            return true;
            //throw new NotImplementedException();
        }

        public void Pay(decimal sum, Guid? orderId, Guid paymentTypeId, Guid transactionId, IPointOfSale pointOfSale, IUser cashier, IReceiptPrinter printer, IViewManager viewManager, IPaymentDataContext context, IProgressBar progressBar)
        {
            logger.Info("Pay");

            if (purchase != null)
            {
                using (CoreYagoda yagoda = new CoreYagoda(logger))
                {
                    yagoda.Connect();
                    var order = GetOrderSafe(orderId);
                    //purchase.checkAmount = order.ResultSum.ToString();
                    foreach (IPaymentItem payment in order.Payments)
                    {
                        logger.Info($"payment - {payment.Type.Name} , sum - {payment.Sum}");
                        if (payment.Type.Name.Equals("Yagoda"))
                        {
                            purchase.payByBonus = payment.Sum.ToString();
                        }
                    }

                    string dt = DateTime.Now.ToString("yyyy-MM-dd");
                    dt += "T" + DateTime.Now.ToString("HH:mm");
                    purchase.checkDateTime = dt;
#if DEBUG
                    logger.Info($"try Write purchase");
                    logger.Info($"selectresult={selectResult}");
#endif
                    switch (selectResult)
                    {
                        case 0:
#if DEBUG
                            logger.Info("WritePurchase");
#endif
                            yagoda.WritePurchase(purchase);
                            break;

                        case 1:
#if DEBUG
                            logger.Info("WriteOffPurchase");
#endif
                            yagoda.WriteOffPurchase(purchase,viewManager);
                            break;
                    }
#if DEBUG
                    logger.Info("Try success write purchase");
#endif                
                }
            }
            else
            {
#if DEBUG
                logger.Error("Purchase ==null");
#endif
            }
        }

        public void ReturnPayment(decimal sum, Guid? orderId, Guid paymentTypeId, Guid transactionId, IPointOfSale pointOfSale, IUser cashier, IReceiptPrinter printer, IViewManager viewManager, IPaymentDataContext context, IProgressBar progressBar)
        {
            logger.Info("ReturnPayment");
            //throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (subscriptions != null)
                subscriptions.Dispose();
        }

        private void CafeSessionClosing([NotNull] IReceiptPrinter printer, [NotNull] IProgressBar progressBar)
        {
            PluginContext.Log.Info("Cafe Session Closing.");
            var slip = new ReceiptSlip
            {
                Doc = new XElement(Tags.Doc,
                    new XElement(Tags.Center, PaymentSystemKey),
                    new XElement(Tags.Center, "Cafe session closed."))
            };
            printer.Print(slip);
        }

        private void CafeSessionOpening([NotNull] IReceiptPrinter printer, [NotNull] IProgressBar progressBar)
        {
            PluginContext.Log.Info("Cafe Session Opening.");
            //var message =
            //    "SamplePaymentPlugin: 'I can not connect to my server to open operation session. But I'll not stop openning iikoFront cafe session.'";
            //PluginContext.Operations.AddNotificationMessage(message, "SamplePaymentPlugin");
        }

        [Serializable]
        public class CollectedDataDemoClass
        {
            public bool IsCard;
            public string Data;
        }
    }
}