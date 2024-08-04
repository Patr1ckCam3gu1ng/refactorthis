using RefactorThis.Domain.Common.Queries;
using RefactorThis.Domain.Invoices.Queries.Models;
using RefactorThis.Persistence.Models;
using RefactorThis.Persistence.Repositories;

namespace RefactorThis.Domain.Invoices.Queries
{
    public class GetInvoiceQueryHandler : IQueryHandler<GetInvoiceModel, Invoice>
    {
        private readonly IInvoiceRepository _invoiceRepository;

        public GetInvoiceQueryHandler(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public Invoice Handle(GetInvoiceModel query)
        {
            return _invoiceRepository.GetInvoice(query.Reference);
        }
    }
}