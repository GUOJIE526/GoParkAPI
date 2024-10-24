using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class MonthlyRental
{
    public int RenId { get; set; }

    public int CarId { get; set; }

    public int LotId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int Amount { get; set; }

    public bool PaymentStatus { get; set; }
    
    //// 新增 TransactionId 欄位
    public string? TransactionId { get; set; } // 支付平台的唯一交易 ID，可為 null

    public virtual Car Car { get; set; } = null!;

    public virtual ParkingLots Lot { get; set; } = null!;
}
