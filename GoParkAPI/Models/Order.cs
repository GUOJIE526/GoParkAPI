using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class Order
{
    public int OrdId { get; set; }

    public string? OrdType { get; set; }

    public int ReferenceId { get; set; }

    public decimal Amount { get; set; }

    public bool PaymentStatus { get; set; }

    public DateTime? PaymentTime { get; set; }

    public DateTime CreatedTime { get; set; }

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
