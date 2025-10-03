using System;
using System.Collections.Generic;

namespace VaultX_WebAPI.Models;

public partial class User
{
    public string Userid { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? Firstname { get; set; }

    public string? Lastname { get; set; }

    public string? Cnic { get; set; }

    public bool? IsVerified { get; set; }

    public bool? IsBlocked { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string Role { get; set; } = null!;

    public string? Phone { get; set; }

    public bool IsEmailVerified { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual Otp? Otp { get; set; }

    public virtual ICollection<Residence> Residences { get; set; } = new List<Residence>();

    public virtual Society? Society { get; set; }
}
