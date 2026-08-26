using System;

namespace SchoolSystemAPI.Models
{
    public class SubscriptionPayment
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public bool IsNewStudent { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}
