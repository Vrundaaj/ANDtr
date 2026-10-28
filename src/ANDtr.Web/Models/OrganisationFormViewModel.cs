using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ANDtr.Web.Models;

// Create / edit organisation form (A2).
public class OrganisationFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Enter the organisation name.")]
    [StringLength(100)]
    [Display(Name = "Organisation name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter a contact email.")]
    [StringLength(256)]
    [RegularExpression(ValidationPatterns.Email, ErrorMessage = ValidationPatterns.EmailMessage)]
    [Display(Name = "Contact email")]
    public string ContactEmail { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Notes (optional)")]
    public string? Notes { get; set; }

    // Customer users already in this organisation, shown on Edit.
    [ValidateNever]
    public List<UserListItem> Customers { get; set; } = [];

    public bool IsEdit => Id is not null;
}
