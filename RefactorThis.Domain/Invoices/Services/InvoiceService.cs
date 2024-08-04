using System;
using System.Linq;
using RefactorThis.Domain.Common.Queries;
using RefactorThis.Domain.Invoices.Commands;
using RefactorThis.Domain.Invoices.Commands.Models;
using RefactorThis.Domain.Invoices.Queries.Models;
using RefactorThis.Persistence.Models;
using static RefactorThis.Domain.Common.Constants.ResponseMessage;
using static RefactorThis.Persistence.Constants;

namespace RefactorThis.Domain.Invoices.Services
{
    public class InvoiceService
    {
        private readonly IQueryHandler<GetInvoiceModel, Invoice> _getInvoice;
        private readonly AddInvoice _addInvoice;

        public InvoiceService(IQueryHandler<GetInvoiceModel, Invoice> getInvoice,
            AddInvoice addInvoice)
        {
            _getInvoice = getInvoice;
            _addInvoice = addInvoice;
        }

        public string ProcessPayment(Payment payment)
        {
            var invoice = GetInvoiceByReference(payment);

            string responseMessage;

            if (invoice == null)
            {
                throw new InvalidOperationException("There is no invoice matching this payment");
            }

            var invoiceModel = new AddInvoiceModel
            {
                Amount = invoice.Amount,
                Payments = invoice.Payments,
                AmountPaid = invoice.AmountPaid,
                TaxAmount = invoice.TaxAmount,
                Type = invoice.Type
            };

            if (invoiceModel.Amount == 0)
            {
                if (invoiceModel.Payments == null || !invoiceModel.Payments.Any())
                {
                    responseMessage = "no payment needed";
                }
                else
                {
                    throw new InvalidOperationException("The invoice is in an invalid state, it has an amount of 0 and it has payments.");
                }
            }
            else
            {
                if (invoiceModel.Payments != null && invoiceModel.Payments.Any())
                {
                    responseMessage = ProcessFirstPayment(payment, invoiceModel);
                }
                else
                {
                    responseMessage = ProcessSubsequentPayment(payment, invoiceModel);
                }
            }

            AddInvoice(invoiceModel);

            return responseMessage;
        }

        #region First Payment

        private string ProcessFirstPayment(Payment payment, AddInvoiceModel invoice)
        {
            string responseMessage;
            if (invoice.Payments.Sum(x => x.Amount) != 0 && invoice.Amount == invoice.Payments.Sum(x => x.Amount))
            {
                responseMessage = "invoice was already fully paid";
            }
            else if (invoice.Payments.Sum(x => x.Amount) != 0 && payment.Amount > (invoice.Amount - invoice.AmountPaid))
            {
                responseMessage = "the payment is greater than the partial amount remaining";
            }
            else
            {
                if ((invoice.Amount - invoice.AmountPaid) == payment.Amount)
                {
                    responseMessage = CalculateFirstPayment(payment, invoice, FinalPartialPaymentReceived);
                }
                else
                {
                    responseMessage = CalculateFirstPayment(payment, invoice, AnotherPartialPaymentReceived);
                }
            }

            return responseMessage;
        }

        private string CalculateFirstPayment(Payment payment, AddInvoiceModel invoice, string responseMessageToReturn)
        {
            switch (invoice.Type)
            {
                case InvoiceType.Standard:
                    CalculateInvoiceTypeStandard(payment, invoice);
                    return responseMessageToReturn;
                case InvoiceType.Commercial:
                    CalculateInvoiceTypeCommercial(payment, invoice);
                    return responseMessageToReturn;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void CalculateInvoiceTypeCommercial(Payment payment, AddInvoiceModel invoice)
        {
            invoice.AmountPaid += payment.Amount;
            invoice.TaxAmount += payment.Amount * 0.14m;
            invoice.Payments.Add(payment);
        }

        private static void CalculateInvoiceTypeStandard(Payment payment, AddInvoiceModel invoice)
        {
            invoice.AmountPaid += payment.Amount;
            invoice.Payments.Add(payment);
        }

        #endregion

        #region Subsequent Payment

        private string ProcessSubsequentPayment(Payment payment, AddInvoiceModel invoice)
        {
            string responseMessage;
            if (payment.Amount > invoice.Amount)
            {
                responseMessage = "the payment is greater than the invoice amount";
            }
            else if (invoice.Amount == payment.Amount)
            {
                responseMessage = CalculateSubsequentPayment(payment, invoice, InvoiceIsNowFullPaid);
            }
            else
            {
                responseMessage = CalculateSubsequentPayment(payment, invoice, InvoiceIsNowPartialPaid);
            }

            return responseMessage;
        }

        private string CalculateSubsequentPayment(Payment payment, AddInvoiceModel invoice, string responseMessageToReturn)
        {
            switch (invoice.Type)
            {
                case InvoiceType.Standard:
                    CalculateAmountPaid(payment, invoice);
                    return responseMessageToReturn;
                case InvoiceType.Commercial:
                    CalculateAmountPaid(payment, invoice);
                    return responseMessageToReturn;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void CalculateAmountPaid(Payment payment, AddInvoiceModel inv)
        {
            inv.AmountPaid = payment.Amount;
            inv.TaxAmount = payment.Amount * 0.14m;
            inv.Payments.Add(payment);
        }

        #endregion

        #region CRUD

        private void AddInvoice(AddInvoiceModel invoice)
        {
            if (invoice.Amount == 0 && invoice.Payments.Count == 0)
            {
                return;
            }

            _addInvoice.Handle(invoice);
        }

        private Invoice GetInvoiceByReference(Payment payment)
        {
            var query = new GetInvoiceModel { Reference = payment.Reference };
            return _getInvoice.Handle(query);
        }

        #endregion
    }
}