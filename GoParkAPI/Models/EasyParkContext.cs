using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace GoParkAPI.Models;

public partial class EasyParkContext : DbContext
{
    public EasyParkContext()
    {
    }

    public EasyParkContext(DbContextOptions<EasyParkContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Car> Car { get; set; }

    public virtual DbSet<Coupon> Coupon { get; set; }

    public virtual DbSet<Customer> Customer { get; set; }

    public virtual DbSet<DealRecord> DealRecord { get; set; }

    public virtual DbSet<EntryExitManagement> EntryExitManagement { get; set; }

    public virtual DbSet<MonApplyList> MonApplyList { get; set; }

    public virtual DbSet<MonthlyRental> MonthlyRental { get; set; }

    public virtual DbSet<Orders> Orders { get; set; }

    public virtual DbSet<ParkSpace> ParkSpace { get; set; }

    public virtual DbSet<ParkingLotImages> ParkingLotImages { get; set; }

    public virtual DbSet<ParkingLots> ParkingLots { get; set; }

    public virtual DbSet<Reservation> Reservation { get; set; }

    public virtual DbSet<Revenue> Revenue { get; set; }

    public virtual DbSet<Survey> Survey { get; set; }

    public virtual DbSet<Transactions> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Car>(entity =>
        {
            entity.HasKey(e => e.CarId).HasName("PK__Car__4C9A0DB36167FBD8");

            entity.HasIndex(e => e.LicensePlate, "UK_license_plate").IsUnique();

            entity.Property(e => e.CarId).HasColumnName("car_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LicensePlate)
                .HasMaxLength(30)
                .HasColumnName("license_plate");
            entity.Property(e => e.RegisterDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("register_date");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Car)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Car_User");
        });

        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(e => e.CouponId).HasName("PK__Coupon__58CF6389A1A7E084");

            entity.HasIndex(e => e.CouponCode, "UQ__Coupon__ADE5CBB76649B54B").IsUnique();

            entity.Property(e => e.CouponId).HasColumnName("coupon_id");
            entity.Property(e => e.CouponCode)
                .HasMaxLength(50)
                .HasColumnName("coupon_code");
            entity.Property(e => e.DiscountAmount).HasColumnName("discount_amount");
            entity.Property(e => e.IsUsed).HasColumnName("is_used");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ValidFrom)
                .HasColumnType("datetime")
                .HasColumnName("valid_from");
            entity.Property(e => e.ValidUntil)
                .HasColumnType("datetime")
                .HasColumnName("valid_until");

            entity.HasOne(d => d.User).WithMany(p => p.Coupon)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Coupon_User");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Customer__B9BE370F1062AB11");

            entity.HasIndex(e => e.Email, "UQ__Customer__AB6E616460A15AFE").IsUnique();

            entity.HasIndex(e => e.Username, "UQ__Customer__F3DBC572583BB2A6").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.IsBlack).HasColumnName("is_black");
            entity.Property(e => e.Password).HasColumnName("password");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.RegisterDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("register_date");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .HasColumnName("username");
        });

        modelBuilder.Entity<DealRecord>(entity =>
        {
            entity.HasKey(e => e.DealId).HasName("PK__DealReco__C012A76CEB8CFF36");

            entity.Property(e => e.DealId).HasColumnName("deal_id");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.LicensePlate)
                .HasMaxLength(30)
                .HasColumnName("license_plate");
            entity.Property(e => e.ParkType)
                .HasMaxLength(50)
                .HasColumnName("parkType");
            entity.Property(e => e.PaymentTime)
                .HasColumnType("datetime")
                .HasColumnName("payment_time");

            entity.HasOne(d => d.LicensePlateNavigation).WithMany(p => p.DealRecord)
                .HasPrincipalKey(p => p.LicensePlate)
                .HasForeignKey(d => d.LicensePlate)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DealRecord");
        });

        modelBuilder.Entity<EntryExitManagement>(entity =>
        {
            entity.HasKey(e => e.EntryexitId).HasName("PK__EntryExi__FD3EA5F6ECB3AD79");

            entity.Property(e => e.EntryexitId).HasColumnName("entryexit_id");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.EntryTime)
                .HasColumnType("datetime")
                .HasColumnName("entry_time");
            entity.Property(e => e.ExitTime)
                .HasColumnType("datetime")
                .HasColumnName("exit_time");
            entity.Property(e => e.IsFinish).HasColumnName("is_finish");
            entity.Property(e => e.LicensePlate)
                .HasMaxLength(30)
                .HasColumnName("license_plate");
            entity.Property(e => e.LicensePlateKeyinTime)
                .HasColumnType("datetime")
                .HasColumnName("license_plate_keyin_time");
            entity.Property(e => e.LicensePlatePhoto).HasColumnName("license_plate_photo");
            entity.Property(e => e.LotName)
                .HasMaxLength(255)
                .HasColumnName("lot_name");
            entity.Property(e => e.Parktype)
                .HasMaxLength(50)
                .HasColumnName("parktype");
            entity.Property(e => e.PaymentStatus).HasColumnName("payment_status");
            entity.Property(e => e.PaymentTime)
                .HasColumnType("datetime")
                .HasColumnName("payment_time");
            entity.Property(e => e.ValidTime)
                .HasColumnType("datetime")
                .HasColumnName("valid_time");

            entity.HasOne(d => d.LicensePlateNavigation).WithMany(p => p.EntryExitManagement)
                .HasPrincipalKey(p => p.LicensePlate)
                .HasForeignKey(d => d.LicensePlate)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EntryExit_Car");

            entity.HasOne(d => d.LotNameNavigation).WithMany(p => p.EntryExitManagement)
                .HasPrincipalKey(p => p.LotName)
                .HasForeignKey(d => d.LotName)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EntryExit_Lot");
        });

        modelBuilder.Entity<MonApplyList>(entity =>
        {
            entity.HasKey(e => e.ApplyId).HasName("PK__MonApply__8260CA824D0CFE41");

            entity.Property(e => e.ApplyId).HasColumnName("apply_id");
            entity.Property(e => e.ApplyDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("apply_date");
            entity.Property(e => e.ApplyStatus)
                .HasMaxLength(50)
                .HasDefaultValue("pending")
                .HasColumnName("apply_status");
            entity.Property(e => e.IsCanceled).HasColumnName("is_canceled");
            entity.Property(e => e.LotName)
                .HasMaxLength(255)
                .HasColumnName("lot_name");
            entity.Property(e => e.NotificationStatus).HasColumnName("notification_status");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .HasColumnName("username");

            entity.HasOne(d => d.LotNameNavigation).WithMany(p => p.MonApplyList)
                .HasPrincipalKey(p => p.LotName)
                .HasForeignKey(d => d.LotName)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MonApplyList_Lot");

            entity.HasOne(d => d.UsernameNavigation).WithMany(p => p.MonApplyList)
                .HasPrincipalKey(p => p.Username)
                .HasForeignKey(d => d.Username)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MonApplyList_User");
        });

        modelBuilder.Entity<MonthlyRental>(entity =>
        {
            entity.HasKey(e => e.RenId).HasName("PK__MonthlyR__5833C152CA74A319");

            entity.Property(e => e.RenId).HasColumnName("ren_id");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.EndDate)
                .HasColumnType("datetime")
                .HasColumnName("end_date");
            entity.Property(e => e.LicensePlate)
                .HasMaxLength(30)
                .HasColumnName("license_plate");
            entity.Property(e => e.LotName)
                .HasMaxLength(255)
                .HasColumnName("lot_name");
            entity.Property(e => e.PaymentStatus).HasColumnName("payment_status");
            entity.Property(e => e.StartDate)
                .HasColumnType("datetime")
                .HasColumnName("start_date");

            entity.HasOne(d => d.LicensePlateNavigation).WithMany(p => p.MonthlyRental)
                .HasPrincipalKey(p => p.LicensePlate)
                .HasForeignKey(d => d.LicensePlate)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MonthlyRental_Car");

            entity.HasOne(d => d.LotNameNavigation).WithMany(p => p.MonthlyRental)
                .HasPrincipalKey(p => p.LotName)
                .HasForeignKey(d => d.LotName)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MonthlyRental_Lot");
        });

        modelBuilder.Entity<Orders>(entity =>
        {
            entity.HasKey(e => e.OrdId).HasName("PK__Orders__DC39D7DF9F2C1921");

            entity.Property(e => e.OrdId).HasColumnName("ord_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.CreatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_time");
            entity.Property(e => e.OrdType)
                .HasMaxLength(50)
                .HasColumnName("ord_type");
            entity.Property(e => e.PaymentStatus).HasColumnName("payment_status");
            entity.Property(e => e.PaymentTime)
                .HasColumnType("datetime")
                .HasColumnName("payment_time");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id");
        });

        modelBuilder.Entity<ParkSpace>(entity =>
        {
            entity.HasKey(e => e.SpaceId).HasName("PK__ParkSpac__793ECA55E80D4F73");

            entity.Property(e => e.SpaceId).HasColumnName("space_id");
            entity.Property(e => e.IsRented).HasColumnName("is_Rented");
            entity.Property(e => e.LotName)
                .HasMaxLength(255)
                .HasColumnName("lot_name");
            entity.Property(e => e.SpaceNum).HasColumnName("space_num");

            entity.HasOne(d => d.LotNameNavigation).WithMany(p => p.ParkSpace)
                .HasPrincipalKey(p => p.LotName)
                .HasForeignKey(d => d.LotName)
                .HasConstraintName("FK_lotName");
        });

        modelBuilder.Entity<ParkingLotImages>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__ParkingL__DC9AC955212ABB16");

            entity.Property(e => e.ImageId).HasColumnName("image_id");
            entity.Property(e => e.ImageName)
                .HasMaxLength(100)
                .HasColumnName("image_name");
            entity.Property(e => e.LotName)
                .HasMaxLength(255)
                .HasColumnName("lot_name");

            entity.HasOne(d => d.LotNameNavigation).WithMany(p => p.ParkingLotImages)
                .HasPrincipalKey(p => p.LotName)
                .HasForeignKey(d => d.LotName)
                .HasConstraintName("FK_ParkingLotImages_Lot");
        });

        modelBuilder.Entity<ParkingLots>(entity =>
        {
            entity.HasKey(e => e.LotId).HasName("PK__ParkingL__38CAA92B675ACF7F");

            entity.HasIndex(e => e.LotName, "UQ__ParkingL__3D9F16FE425CD987").IsUnique();

            entity.Property(e => e.LotId).HasColumnName("lot_id");
            entity.Property(e => e.AvailableSpaces).HasColumnName("available_spaces");
            entity.Property(e => e.ContactPhone)
                .HasMaxLength(50)
                .HasColumnName("contact_phone");
            entity.Property(e => e.DailyMaxRate)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("daily_max_rate");
            entity.Property(e => e.District)
                .HasMaxLength(100)
                .HasColumnName("district");
            entity.Property(e => e.EvSpaces)
                .HasDefaultValue(0)
                .HasColumnName("ev_spaces");
            entity.Property(e => e.HandicapSpaces)
                .HasDefaultValue(0)
                .HasColumnName("handicap_spaces");
            entity.Property(e => e.HourlyRate)
                .HasMaxLength(255)
                .HasColumnName("hourly_rate");
            entity.Property(e => e.Latitude)
                .HasColumnType("decimal(18, 9)")
                .HasColumnName("latitude");
            entity.Property(e => e.Location).HasColumnName("location");
            entity.Property(e => e.Longitude)
                .HasColumnType("decimal(18, 15)")
                .HasColumnName("longitude");
            entity.Property(e => e.LotName)
                .HasMaxLength(255)
                .HasColumnName("lot_name");
            entity.Property(e => e.MonthlyRate)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("monthly_rate");
            entity.Property(e => e.MonthlyRentAllowed)
                .HasDefaultValue(false)
                .HasColumnName("monthly_rent_allowed");
            entity.Property(e => e.NightRate)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("night_rate");
            entity.Property(e => e.ReservationAllowed)
                .HasDefaultValue(false)
                .HasColumnName("reservation_allowed");
            entity.Property(e => e.TotalSpaces).HasColumnName("total_spaces");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.ResId).HasName("PK__Reservat__2090B50DB492BD45");

            entity.Property(e => e.ResId).HasColumnName("res_id");
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.IsCanceled).HasColumnName("is_canceled");
            entity.Property(e => e.IsFinish).HasColumnName("is_finish");
            entity.Property(e => e.IsOverdue).HasColumnName("is_overdue");
            entity.Property(e => e.IsRefoundDeposit).HasColumnName("is_refound_deposit");
            entity.Property(e => e.LicensePlate)
                .HasMaxLength(30)
                .HasColumnName("license_plate");
            entity.Property(e => e.LotName)
                .HasMaxLength(255)
                .HasColumnName("lot_name");
            entity.Property(e => e.NotificationStatus).HasColumnName("notification_status");
            entity.Property(e => e.PaymentStatus).HasColumnName("payment_status");
            entity.Property(e => e.ResTime)
                .HasColumnType("datetime")
                .HasColumnName("res_time");
            entity.Property(e => e.StartTime)
                .HasColumnType("datetime")
                .HasColumnName("start_time");
            entity.Property(e => e.ValidUntil)
                .HasColumnType("datetime")
                .HasColumnName("valid_until");

            entity.HasOne(d => d.LicensePlateNavigation).WithMany(p => p.Reservation)
                .HasPrincipalKey(p => p.LicensePlate)
                .HasForeignKey(d => d.LicensePlate)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reservation_Car");

            entity.HasOne(d => d.LotNameNavigation).WithMany(p => p.Reservation)
                .HasPrincipalKey(p => p.LotName)
                .HasForeignKey(d => d.LotName)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reservation_Lot");
        });

        modelBuilder.Entity<Revenue>(entity =>
        {
            entity.HasKey(e => e.RevenueId).HasName("PK__Revenue__3DF902E947B6B487");

            entity.Property(e => e.RevenueId).HasColumnName("revenue_id");
            entity.Property(e => e.CreatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_time");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.RentalIncome).HasColumnName("rental_income");
            entity.Property(e => e.ReservationIncome).HasColumnName("reservation_income");
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount");
        });

        modelBuilder.Entity<Survey>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Survey__3213E83F16D5E939");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsReplied).HasColumnName("is_replied");
            entity.Property(e => e.Question).HasColumnName("question");
            entity.Property(e => e.RepliedAt)
                .HasColumnType("datetime")
                .HasColumnName("replied_at");
            entity.Property(e => e.ReplyMessage).HasColumnName("reply_message");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("未回覆")
                .HasColumnName("status");
            entity.Property(e => e.SubmittedAt)
                .HasColumnType("datetime")
                .HasColumnName("submitted_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Survey)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Survey_Customer");
        });

        modelBuilder.Entity<Transactions>(entity =>
        {
            entity.HasKey(e => e.TranId).HasName("PK__Transact__A67F8A2021F2F074");

            entity.Property(e => e.TranId).HasColumnName("tran_id");
            entity.Property(e => e.CouponId).HasColumnName("coupon_id");
            entity.Property(e => e.CreatedTime)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_time");
            entity.Property(e => e.DiscountAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("discount_amount");
            entity.Property(e => e.OrdId).HasColumnName("ord_id");
            entity.Property(e => e.PaymentAmount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("payment_amount");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentStatus).HasColumnName("payment_status");
            entity.Property(e => e.PaymentTime)
                .HasColumnType("datetime")
                .HasColumnName("payment_time");

            entity.HasOne(d => d.Coupon).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.CouponId)
                .HasConstraintName("FK_Transaction_Coupon");

            entity.HasOne(d => d.Ord).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.OrdId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transaction_Order");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
