using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class DealRecord
{
    public int DealId { get; set; }

    public string LicensePlate { get; set; } = null!;

    public int Amount { get; set; }

    public DateTime PaymentTime { get; set; }

    public string ParkType { get; set; } = null!;

    public virtual Car LicensePlateNavigation { get; set; } = null!;
}
