using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class ParkingLots
{
    public int LotId { get; set; }

    public string LotName { get; set; } = null!;

    public string Location { get; set; } = null!;

    public string District { get; set; } = null!;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public int TotalSpaces { get; set; }

    public int AvailableSpaces { get; set; }

    public int? EvSpaces { get; set; }

    public int? HandicapSpaces { get; set; }

    public decimal HourlyRate { get; set; }

    public decimal? DailyMaxRate { get; set; }

    public decimal? NightRate { get; set; }

    public decimal? MonthlyRate { get; set; }

    public bool? ReservationAllowed { get; set; }

    public bool? MonthlyRentAllowed { get; set; }

    public string? ContactPhone { get; set; }

    public virtual ICollection<EntryExitManagement> EntryExitManagement { get; set; } = new List<EntryExitManagement>();

    public virtual ICollection<MonApplyList> MonApplyList { get; set; } = new List<MonApplyList>();

    public virtual ICollection<MonthlyRental> MonthlyRental { get; set; } = new List<MonthlyRental>();

    public virtual ICollection<ParkSpace> ParkSpace { get; set; } = new List<ParkSpace>();

    public virtual ICollection<ParkingLotImages> ParkingLotImages { get; set; } = new List<ParkingLotImages>();

    public virtual ICollection<Reservation> Reservation { get; set; } = new List<Reservation>();
}
