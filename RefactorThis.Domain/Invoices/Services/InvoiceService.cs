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

            if (invoice.Amount == 0)
            {
                if (invoice.Payments == null || !invoice.Payments.Any())
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
                if (invoice.Payments != null && invoice.Payments.Any())
                {
                    responseMessage = ProcessFirstPayment(payment, invoice);
                }
                else
                {
                    responseMessage = ProcessSubsequentPayment(payment, invoice);
                }
            }

            AddInvoice(invoice);

            return responseMessage;
        }

        private void AddInvoice(Invoice invoice)
        {
            if (invoice.Amount == 0 && invoice.Payments.Count == 0)
            {
                return;
            }
            
            _addInvoice.Handle(new AddInvoiceModel
            {
                Amount = invoice.Amount,
                Payments = invoice.Payments,
                AmountPaid = invoice.AmountPaid,
                TaxAmount = invoice.TaxAmount,
                Type = invoice.Type
            });
        }

        private static string ProcessSubsequentPayment(Payment payment, Invoice invoice)
        {
            string responseMessage;
            if (payment.Amount > invoice.Amount)
            {
                responseMessage = "the payment is greater than the invoice amount";
            }
            else if (invoice.Amount == payment.Amount)
            {
                switch (invoice.Type)
                {
                    case InvoiceType.Standard:
                        CalculateAmountPaid(payment, invoice);
                        responseMessage = InvoiceIsNowFullPaid;
                        break;
                    case InvoiceType.Commercial:
                        CalculateAmountPaid(payment, invoice);
                        responseMessage = InvoiceIsNowFullPaid;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                switch (invoice.Type)
                {
                    case InvoiceType.Standard:
                        CalculateAmountPaid(payment, invoice);
                        responseMessage = InvoiceIsNowPartialPaid;
                        break;
                    case InvoiceType.Commercial:
                        CalculateAmountPaid(payment, invoice);
                        responseMessage = InvoiceIsNowPartialPaid;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return responseMessage;
        }

        private static void CalculateAmountPaid(Payment payment, Invoice inv)
        {
            inv.AmountPaid = payment.Amount;
            inv.TaxAmount = payment.Amount * 0.14m;
            inv.Payments.Add(payment);
        }

        private string ProcessFirstPayment(Payment payment, Invoice invoice)
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
                    switch (invoice.Type)
                    {
                        case InvoiceType.Standard:
                            CalculateInvoiceTypeStandard(payment, invoice);
                            responseMessage = FinalPartialPaymentReceived;
                            break;
                        case InvoiceType.Commercial:
                            CalculateInvoiceTypeCommercial(payment, invoice);
                            responseMessage = FinalPartialPaymentReceived;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    switch (invoice.Type)
                    {
                        case InvoiceType.Standard:
                            CalculateInvoiceTypeStandard(payment, invoice);
                            responseMessage = AnotherPartialPaymentReceived;
                            break;
                        case InvoiceType.Commercial:
                            CalculateInvoiceTypeCommercial(payment, invoice);
                            responseMessage = AnotherPartialPaymentReceived;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            return responseMessage;
        }

        private static void CalculateInvoiceTypeCommercial(Payment payment, Invoice invoice)
        {
            invoice.AmountPaid += payment.Amount;
            invoice.TaxAmount += payment.Amount * 0.14m;
            invoice.Payments.Add(payment);
        }

        private static void CalculateInvoiceTypeStandard(Payment payment, Invoice invoice)
        {
            invoice.AmountPaid += payment.Amount;
            invoice.Payments.Add(payment);
        }

        private Invoice GetInvoiceByReference(Payment payment)
        {
            var query = new GetInvoiceModel { Reference = payment.Reference };
            return _getInvoice.Handle(query);
        }
    }
}