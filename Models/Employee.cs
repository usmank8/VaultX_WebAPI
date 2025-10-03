using System;
using System.Collections.Generic;

namespace VaultX_WebAPI.Models;

public partial class Employee
{
    public Guid Id { get; set; }

    public string InternalRole { get; set; } = null!;

    public string? Department { get; set; }

    public string? Shift { get; set; }

    public DateOnly? JoiningDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? Userid { get; set; }

    public virtual User? User { get; set; }
}
