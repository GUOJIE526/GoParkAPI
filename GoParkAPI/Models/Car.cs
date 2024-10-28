using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class Car
{
    public int CarId { get; set; }

    public int UserId { get; set; }

    public string LicensePlate { get; set; } = null!;

    public DateTime? RegisterDate { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<DealRecord> DealRecords { get; set; } = new List<DealRecord>();

    public virtual ICollection<EntryExitManagement> EntryExitManagements { get; set; } = new List<EntryExitManagement>();

    public virtual ICollection<MonApplyList> MonApplyLists { get; set; } = new List<MonApplyList>();

    public virtual ICollection<MonthlyRental> MonthlyRentals { get; set; } = new List<MonthlyRental>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public virtual Customer User { get; set; } = null!;
}
