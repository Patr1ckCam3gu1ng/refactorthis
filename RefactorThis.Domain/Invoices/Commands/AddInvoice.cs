using RefactorThis.Domain.Common.Command;
using RefactorThis.Domain.Invoices.Commands.Models;
using RefactorThis.Persistence.Models;
using RefactorThis.Persistence.Repositories;

namespace RefactorThis.Domain.Invoices.Commands
{
    public class AddInvoice : ICommandHandler<AddInvoiceModel>
    {
        private readonly IInvoiceRepository _invoiceRepository;

        public AddInvoice(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public void Handle(AddInvoiceModel model)
        {
            var invoice = new Invoice
            {
                TaxAmount = model.TaxAmount,
                AmountPaid = model.AmountPaid,
                Payments = model.Payments,
                Amount = model.Amount,
                Type = model.Type
            };

            _invoiceRepository.Add(invoice);
            _invoiceRepository.SaveInvoice(invoice);
        }
    }
}