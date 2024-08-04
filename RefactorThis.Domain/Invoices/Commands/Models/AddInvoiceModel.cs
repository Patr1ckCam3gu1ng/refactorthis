using System.Collections.Generic;
using RefactorThis.Domain.Common.Command;
using RefactorThis.Persistence.Models;
using static RefactorThis.Persistence.Constants;

namespace RefactorThis.Domain.Invoices.Commands.Models
{
    public class AddInvoiceModel : ICommand
    {
        public decimal Amount { get; set; }

        public decimal AmountPaid { get; set; }

        public decimal TaxAmount { get; set; }

        public List<Payment> Payments { get; set; }

        public InvoiceType Type { get; set; }
    }
}