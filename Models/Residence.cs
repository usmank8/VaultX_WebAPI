using System;
using System.Collections.Generic;

namespace VaultX_WebAPI.Models;

public partial class Residence
{
    public Guid Id { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string ResidenceType { get; set; } = null!;

    public string Residence1 { get; set; } = null!;

    public bool IsPrimary { get; set; }

    public bool IsApprovedBySociety { get; set; }

    public string? ApprovedBy { get; set; }

    public string? FlatNumber { get; set; }

    public string? Block { get; set; }

    public string? Userid { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Guest> Guests { get; set; } = new List<Guest>();

    public virtual User? User { get; set; }

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
