﻿using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class Reservation
{
    public int ResId { get; set; }

    public string LicensePlate { get; set; } = null!;

    public string LotName { get; set; } = null!;

    public DateTime ResTime { get; set; }

    public DateTime ValidUntil { get; set; }

    public DateTime StartTime { get; set; }

    public int Amount { get; set; }

    public bool PaymentStatus { get; set; }

    public bool IsCanceled { get; set; }

    public bool IsOverdue { get; set; }

    public bool IsRefoundDeposit { get; set; }

    public bool NotificationStatus { get; set; }

    public bool IsFinish { get; set; }

    public virtual Car LicensePlateNavigation { get; set; } = null!;

    public virtual ParkingLots LotNameNavigation { get; set; } = null!;
}
