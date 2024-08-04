using RefactorThis.Persistence.Models;

namespace RefactorThis.Persistence.Repositories
{
    public interface IInvoiceRepository
    {
        Invoice GetInvoice(string reference);

        void SaveInvoice(Invoice invoice);

        void Add(Invoice invoice);
    }
}