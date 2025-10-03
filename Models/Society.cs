using System;
using System.Collections.Generic;

namespace VaultX_WebAPI.Models;

public partial class Society
{
    public string SocietyId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? City { get; set; }

    public string? State { get; set; }

    public string? PostalCode { get; set; }

    public string? UserId { get; set; }

    public virtual User? User { get; set; }
}
