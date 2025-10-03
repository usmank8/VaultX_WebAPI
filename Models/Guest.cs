using System;
using System.Collections.Generic;

namespace VaultX_WebAPI.Models;

public partial class Guest
{
    public string GuestId { get; set; } = null!;

    public string GuestName { get; set; } = null!;

    public string GuestPhoneNumber { get; set; } = null!;

    public DateTime Eta { get; set; }

    public bool VisitCompleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid? ResidenceId { get; set; }

    public string? VehicleId { get; set; }

    public bool IsVerified { get; set; }

    public string QrCode { get; set; } = null!;

    public virtual Residence? Residence { get; set; }

    public virtual Vehicle? Vehicle { get; set; }
}
