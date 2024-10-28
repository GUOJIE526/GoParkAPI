using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class ParkingLotImage
{
    public int ImageId { get; set; }

    public int? LotId { get; set; }

    public string? ImgTitle { get; set; }

    public string? ImgPath { get; set; }

    public virtual ParkingLot? Lot { get; set; }
}
