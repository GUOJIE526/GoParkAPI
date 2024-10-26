using System;
using System.Collections.Generic;

namespace GoParkAPI.Models;

public partial class Customer
{
    public int UserId { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? Salt { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public DateTime? RegisterDate { get; set; }

    public int BlackCount { get; set; }

    public bool IsBlack { get; set; }

    public string? Token { get; set; }

    public virtual ICollection<Car> Cars { get; set; } = new List<Car>();

    public virtual ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();

    public virtual ICollection<Survey> Surveys { get; set; } = new List<Survey>();
}
