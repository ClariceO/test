using System.ComponentModel.DataAnnotations;

namespace H4G_Project.Models
{
    public class ValidateDOBAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            DateTime date = Convert.ToDateTime(value);
            if ((DateTime.Now.Year - date.Year )< 12)
            {
                return new ValidationResult("You must be 12 years old or older to register an account");
            }
            else
            {
                return ValidationResult.Success;
            }
        }
    }
}