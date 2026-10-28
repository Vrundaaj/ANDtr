using System.ComponentModel.DataAnnotations;
using ANDtr.Web.Data;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ANDtr.Web.Models;

// Create / edit user form (A1, A2, A3).
public class UserFormViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Enter the user's full name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter an email address.")]
    [StringLength(256)]
    [RegularExpression(ValidationPatterns.Email, ErrorMessage = ValidationPatterns.EmailMessage)]
    public string Email { get; set; } = string.Empty;

    // Required when creating; on edit, blank keeps the current password.
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string? ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Pick a role.")]
    public string Role { get; set; } = Roles.Employee;

    [Display(Name = "Organisation")]
    public int? OrganisationId { get; set; }

    [Display(Name = "Account active")]
    public bool IsActive { get; set; } = true;

    [ValidateNever]
    public List<SelectListItem> OrganisationOptions { get; set; } = [];

    public bool IsEdit => Id is not null;
}
