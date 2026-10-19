using System.ComponentModel.DataAnnotations;

namespace FamilyTree.Data;

public class Relative
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome"), MaxLength(200)]
    public string Name { get; set; } = "";

    // Previous names / spelling variations found in certificates
    [MaxLength(300)]
    public string? OtherNames { get; set; }

    [MaxLength(80)]
    public string? Nickname { get; set; }

    // "M" or "F"
    [Required, MaxLength(1)]
    public string Sex { get; set; } = "M";

    // Relationship to the root person (e.g. "Avó paterna (mãe do pai)")
    [MaxLength(200)]
    public string? Relationship { get; set; }

    public DateOnly? BirthDate { get; set; }
    [MaxLength(200)] public string? BirthPlace { get; set; }
    [MaxLength(80)] public string? BirthCountry { get; set; }

    // null = unknown
    public bool? IsDeceased { get; set; }
    public DateOnly? DeathDate { get; set; }
    [MaxLength(200)] public string? DeathPlace { get; set; }
    [MaxLength(200)] public string? BurialPlace { get; set; }

    public int? FatherId { get; set; }
    public Relative? Father { get; set; }
    public int? MotherId { get; set; }
    public Relative? Mother { get; set; }

    // Registered spouse (couples are also inferred from children in common)
    public int? SpouseId { get; set; }
    public Relative? Spouse { get; set; }

    // Spouse not registered as a relative
    [MaxLength(200)] public string? SpouseName { get; set; }

    [MaxLength(120)] public string? Occupation { get; set; }

    // Brazilian individual taxpayer ID (CPF)
    [MaxLength(14)] public string? TaxId { get; set; }
    [MaxLength(500)] public string? PhotoUrl { get; set; }

    // Confirmado / Provável / Hipótese
    [MaxLength(20)] public string? Confidence { get; set; }

    public string? Biography { get; set; }
    public string? Notes { get; set; }
    public string? Sources { get; set; }

    // Central person of the tree
    public bool IsRoot { get; set; }

    public string Years
    {
        get
        {
            var birth = BirthDate?.Year.ToString();
            var death = DeathDate?.Year.ToString();
            if (birth is null && death is null) return IsDeceased == true ? "†" : "";
            if (death is not null) return $"{birth ?? "?"} – {death}";
            return IsDeceased == true ? $"{birth} – †" : birth!;
        }
    }

    public int? Age
    {
        get
        {
            if (BirthDate is not { } birth) return null;
            if (IsDeceased == true && DeathDate is null) return null;
            var end = DeathDate ?? DateOnly.FromDateTime(DateTime.Today);
            var age = end.Year - birth.Year;
            if (end < birth.AddYears(age)) age--;
            return age;
        }
    }
}
