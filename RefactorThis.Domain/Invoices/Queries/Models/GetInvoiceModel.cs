using RefactorThis.Domain.Common.Queries;
using RefactorThis.Persistence.Models;

namespace RefactorThis.Domain.Invoices.Queries.Models
{
    public class GetInvoiceModel : IQuery<Invoice>
    {
        public string Reference { get; set; }
    }
}