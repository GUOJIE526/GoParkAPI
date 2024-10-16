using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class MonApplyList
{
    public int ApplyId { get; set; }

    public string Username { get; set; } = null!;

    public string LotName { get; set; } = null!;

    public DateTime ApplyDate { get; set; }

    public string ApplyStatus { get; set; } = null!;

    public bool NotificationStatus { get; set; }

    public bool IsCanceled { get; set; }

    public virtual ParkingLots LotNameNavigation { get; set; } = null!;

    public virtual Customer UsernameNavigation { get; set; } = null!;
}
