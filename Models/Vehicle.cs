using System;
using System.Collections.Generic;

namespace VaultX_WebAPI.Models;

public partial class Vehicle
{
    public string VehicleId { get; set; } = null!;

    public string VehicleType { get; set; } = null!;

    public string VehicleModel { get; set; } = null!;

    public string VehicleName { get; set; } = null!;

    public string VehicleLicensePlateNumber { get; set; } = null!;

    public string VehicleRfidtagId { get; set; } = null!;

    public bool IsGuest { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid? Residentid { get; set; }

    public string VehicleColor { get; set; } = null!;

    public virtual Guest? Guest { get; set; }

    public virtual Residence? Resident { get; set; }
}
