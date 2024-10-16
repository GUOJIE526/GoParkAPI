using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class MonthlyRental
{
    public int RenId { get; set; }

    public string LicensePlate { get; set; } = null!;

    public string LotName { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int Amount { get; set; }

    public bool PaymentStatus { get; set; }

    public virtual Car LicensePlateNavigation { get; set; } = null!;

    public virtual ParkingLots LotNameNavigation { get; set; } = null!;
}
