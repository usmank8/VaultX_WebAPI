using System;
using System.Collections.Generic;

namespace VaultX_WebAPI.Models;

public partial class Otp
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsUsed { get; set; }

    public string? UserUserid { get; set; }

    public virtual User? UserUser { get; set; }
}
