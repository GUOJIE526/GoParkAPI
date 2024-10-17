using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class ParkingLotImages
{
    public int ImageId { get; set; }

    public string? LotName { get; set; }

    public string? ImageName { get; set; }

    public virtual ParkingLots? LotNameNavigation { get; set; }
}
