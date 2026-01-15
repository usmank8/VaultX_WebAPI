using System;
using System.Collections.Generic;

namespace VaultX_WebAPI.Models;

public partial class Guest
{
    public string GuestId { get; set; } = null!;
    public string GuestName { get; set; } = null!;
    
    public string GuestPhoneNumber { get; set; } = null!;
    
    public string Gender { get; set; } = string.Empty;  // ✅ NEW

    // ===== QR CODE VALIDITY WINDOW =====
    public DateTime Eta { get; set; }  // QR valid FROM
    
    public DateTime CheckoutTime { get; set; }  // ✅ NEW: QR valid UNTIL

    // ===== ACTUAL ARRIVAL (SET BY IOT) =====
    public DateTime? ActualArrivalTime { get; set; }  // ✅ NEW: When IoT scanned

    // ===== STATUS TRACKING =====
    public string Status { get; set; } = "pending";  // ✅ NEW: pending/active/completed/cancelled
    
    public bool VisitCompleted { get; set; }
    
    public bool IsVerified { get; set; }

    // ===== QR CODE =====
    public byte[] QrCode { get; set; } = null!;

    // ===== RELATIONSHIPS (FOREIGN KEYS) =====
    public string? Userid { get; set; }  
    
    public Guid? ResidenceId { get; set; }
    
    public string? VehicleId { get; set; }

    // ===== AUDIT TIMESTAMPS =====
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }

    // ===== NAVIGATION PROPERTIES =====
    public virtual User User { get; set; } = null!;  // ✅ NEW
    
    public virtual Residence? Residence { get; set; }
    
    public virtual Vehicle? Vehicle { get; set; }
}

