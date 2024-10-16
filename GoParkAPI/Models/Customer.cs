using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class Customer
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string? Password { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateTime? RegisterDate { get; set; }

    public bool IsBlack { get; set; }

    public virtual ICollection<Car> Car { get; set; } = new List<Car>();

    public virtual ICollection<Coupon> Coupon { get; set; } = new List<Coupon>();

    public virtual ICollection<MonApplyList> MonApplyList { get; set; } = new List<MonApplyList>();

    public virtual ICollection<Survey> Survey { get; set; } = new List<Survey>();
}
