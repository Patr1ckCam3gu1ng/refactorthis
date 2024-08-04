using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using RefactorThis.Domain.Common.Queries;
using RefactorThis.Domain.Invoices.Commands;
using RefactorThis.Domain.Invoices.Queries.Models;
using RefactorThis.Domain.Invoices.Services;
using RefactorThis.Persistence.Models;
using RefactorThis.Persistence.Repositories;

namespace RefactorThis.Domain.Tests
{
    [TestFixture]
    public class InvoicePaymentProcessorTests
    {
        private readonly Mock<IQueryHandler<GetInvoiceModel, Invoice>> _mockGetInvoice;
        private readonly Mock<IInvoiceRepository> _mockInvoiceRepo;
        private readonly Mock<AddInvoice> _mockAddInvoice;
        private readonly InvoiceService _invoiceService;
        private readonly Mock<InvoiceRepository> _mockRepo;

        public InvoicePaymentProcessorTests()
        {
            _mockInvoiceRepo = new Mock<IInvoiceRepository>();
            _mockGetInvoice = new Mock<IQueryHandler<GetInvoiceModel, Invoice>>();
            _mockAddInvoice = new Mock<AddInvoice>(_mockInvoiceRepo.Object);
            _invoiceService = new InvoiceService(_mockGetInvoice.Object, _mockAddInvoice.Object);
        }

        [Test]
        public void ProcessPayment_Should_ThrowException_When_NoInvoiceFoundForPaymentReference()
        {
            Invoice invoice = null;

            var payment = new Payment();
            var failureMessage = "";

            _mockGetInvoice
                .Setup(x => x.Handle(It.Is<GetInvoiceModel>(m => m.Reference == payment.Reference)))
                .Returns(invoice);

            try
            {
                var result = _invoiceService.ProcessPayment(payment);
            }
            catch (InvalidOperationException e)
            {
                failureMessage = e.Message;
            }

            Assert.AreEqual("There is no invoice matching this payment", failureMessage);
        }

        [Test]
        public void ProcessPayment_Should_ReturnFailureMessage_When_NoPaymentNeeded()
        {
            var invoice = new Invoice
            {
                Amount = 0,
                AmountPaid = 0,
                Payments = null
            };

            var payment = new Payment();

            _mockGetInvoice
                .Setup(x => x.Handle(It.Is<GetInvoiceModel>(m => m.Reference == payment.Reference)))
                .Returns(invoice);

            var result = _invoiceService.ProcessPayment(payment);

            Assert.AreEqual("no payment needed", result);
        }

        [Test]
        public void ProcessPayment_Should_ReturnFailureMessage_When_InvoiceAlreadyFullyPaid()
        {
            var invoice = new Invoice()
            {
                Amount = 10,
                AmountPaid = 10,
                Payments = new List<Payment>
                {
                    new Payment
                    {
                        Amount = 10
                    }
                }
            };

            var payment = new Payment();

            _mockGetInvoice
                .Setup(x => x.Handle(It.Is<GetInvoiceModel>(m => m.Reference == payment.Reference)))
                .Returns(invoice);

            var result = _invoiceService.ProcessPayment(payment);

            Assert.AreEqual("invoice was already fully paid", result);
        }

        [Test]
        public void ProcessPayment_Should_ReturnFailureMessage_When_PartialPaymentExistsAndAmountPaidExceedsAmountDue()
        {
            var invoice = new Invoice
            {
                Amount = 10,
                AmountPaid = 5,
                Payments = new List<Payment>
                {
                    new Payment
                    {
                        Amount = 5
                    }
                }
            };

            var payment = new Payment
            {
                Amount = 6
            };

            _mockGetInvoice
                .Setup(x => x.Handle(It.Is<GetInvoiceModel>(m => m.Reference == payment.Reference)))
                .Returns(invoice);

            var result = _invoiceService.ProcessPayment(payment);

            Assert.AreEqual("the payment is greater than the partial amount remaining", result);
        }

        [Test]
        public void ProcessPayment_Should_ReturnFailureMessage_When_NoPartialPaymentExistsAndAmountPaidExceedsInvoiceAmount()
        {
            var invoice = new Invoice
            {
                Amount = 5,
                AmountPaid = 0,
                Payments = new List<Payment>()
            };

            var payment = new Payment
            {
                Amount = 6
            };

            _mockGetInvoice
                .Setup(x => x.Handle(It.Is<GetInvoiceModel>(m => m.Reference == payment.Reference)))
                .Returns(invoice);

            var result = _invoiceService.ProcessPayment(payment);

            Assert.AreEqual("the payment is greater than the invoice amount", result);
        }

        [Test]
        public void ProcessPayment_Should_ReturnFullyPaidMessage_When_PartialPaymentExistsAndAmountPaidEqualsAmountDue()
        {
            var invoice = new Invoice
            {
                Amount = 10,
                AmountPaid = 5,
                Payments = new List<Payment>
                {
                    new Payment
                    {
                        Amount = 5
                    }
                }
            };

            var payment = new Payment
            {
                Amount = 5
            };

            _mockGetInvoice
                .Setup(x => x.Handle(It.Is<GetInvoiceModel>(m => m.Reference == payment.Reference)))
                .Returns(invoice);

            var result = _invoiceService.ProcessPayment(payment);

            Assert.AreEqual("final partial payment received, invoice is now fully paid", result);
        }

        [Test]
        public void ProcessPayment_Should_ReturnFullyPaidMessage_When_NoPartialPaymentExistsAndAmountPaidEqualsInvoiceAmount()
        {
            var invoice = new Invoice
            {
                Amount = 10,
                AmountPaid = 0,
                Payments = new List<Payment>
                {
                    new Payment
                    {
                        Amount = 10
                    }
                }
            };

            var payment = new Payment
            {
                Amount = 10
            };

            _mockGetInvoice
                .Setup(x => x.Handle(It.Is<GetInvoiceModel>(m => m.Reference == payment.Reference)))
                .Returns(invoice);

            var result = _invoiceService.ProcessPayment(payment);

            Assert.AreEqual("invoice was already fully paid", result);
        }

        [Test]
        public void ProcessPayment_Should_ReturnPartiallyPaidMessage_When_PartialPaymentExistsAndAmountPaidIsLessThanAmountDue()
        {
            var invoice = new Invoice
            {
                Amount = 10,
                AmountPaid = 5,
                Payments = new List<Payment>
                {
                    new Payment
                    {
                        Amount = 5
                    }
                }
            };

            var payment = new Payment
            {
                Amount = 1
            };

            _mockGetInvoice
                .Setup(x => x.Handle(It.Is<GetInvoiceModel>(m => m.Reference == payment.Reference)))
                .Returns(invoice);

            var result = _invoiceService.ProcessPayment(payment);

            Assert.AreEqual("another partial payment received, still not fully paid", result);
        }

        [Test]
        public void ProcessPayment_Should_ReturnPartiallyPaidMessage_When_NoPartialPaymentExistsAndAmountPaidIsLessThanInvoiceAmount()
        {
            var invoice = new Invoice
            {
                Amount = 10,
                AmountPaid = 0,
                Payments = new List<Payment>()
            };

            var payment = new Payment
            {
                Amount = 1
            };

            _mockGetInvoice
                .Setup(x => x.Handle(It.Is<GetInvoiceModel>(m => m.Reference == payment.Reference)))
                .Returns(invoice);

            var result = _invoiceService.ProcessPayment(payment);

            Assert.AreEqual("invoice is now partially paid", result);
        }
    }
}