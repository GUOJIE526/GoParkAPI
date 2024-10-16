using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class ParkSpace
{
    public int SpaceId { get; set; }

    public string LotName { get; set; } = null!;

    public int? SpaceNum { get; set; }

    public bool IsRented { get; set; }

    public virtual ParkingLots LotNameNavigation { get; set; } = null!;
}
