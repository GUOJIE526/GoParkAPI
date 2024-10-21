using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class MonApplyList
{
    public int ApplyId { get; set; }

    public int UserId { get; set; }

    public int LotId { get; set; }

    public DateTime ApplyDate { get; set; }

    public string ApplyStatus { get; set; } = null!;

    public bool NotificationStatus { get; set; }

    public bool IsCanceled { get; set; }

    public virtual ParkingLots Lot { get; set; } = null!;

    public virtual Customer User { get; set; } = null!;
}
