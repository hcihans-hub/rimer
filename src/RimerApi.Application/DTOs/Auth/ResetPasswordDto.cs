using System.ComponentModel.DataAnnotations;

namespace RimerApi.Application.DTOs.Auth;

public class ResetPasswordDto
{
    [Required(ErrorMessage = "Email gereklidir.")]
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Token gereklidir.")]
    public string Token { get; set; } = null!;

    [Required(ErrorMessage = "Yeni şifre gereklidir.")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    public string NewPassword { get; set; } = null!;
}
