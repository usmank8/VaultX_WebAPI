using System;

namespace VaultX_WebAPI.Models;

public class VehicleAccessLog
{
    public Guid Id { get; set; }
    
    public string VehicleId { get; set; } = null!;
    
    public string AccessType { get; set; } = null!; // "Entry" or "Exit"
    
    public DateTime Timestamp { get; set; }
    
    public string? GateName { get; set; }
    
    public string? RecordedBy { get; set; } // UserId of employee/admin who recorded
    
    public virtual Vehicle Vehicle { get; set; } = null!;
}

