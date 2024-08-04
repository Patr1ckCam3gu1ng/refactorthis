namespace RefactorThis.Domain.Common
{
    public abstract class Constants
    {
        public abstract class ResponseMessage
        {
            public static string InvoiceIsNowFullPaid { get; set; } = "invoice is now fully paid";
            public static string InvoiceIsNowPartialPaid { get; set; } = "invoice is now partially paid";
            public static string AnotherPartialPaymentReceived { get; } = "another partial payment received, still not fully paid";
            public static string FinalPartialPaymentReceived { get; set; } = "final partial payment received, invoice is now fully paid";
        }
    }
}