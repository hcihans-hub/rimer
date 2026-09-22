using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using RimerApi.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using RimerApi.Application.Common;
using RimerApi.Application.DTOs.Auth;
using RimerApi.Application.Interfaces;
using RimerApi.Infrastructure.Identity;

namespace RimerApi.Infrastructure.Services;

/// <summary>
/// IAuthService implementation using ASP.NET Identity + JWT + Refresh Tokens.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly IValidator<RegisterDto> _registerValidator;
    private readonly IIdentityService _identityService;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailService _emailService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        IValidator<LoginDto> loginValidator,
        IValidator<RegisterDto> registerValidator,
        IIdentityService identityService,
        ILogger<AuthService> logger,
        IEmailService emailService)
    {
        _userManager = userManager;
        _configuration = configuration;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
        _identityService = identityService;
        _logger = logger;
        _emailService = emailService;
    }

    // ── Register ───────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ServiceResult<AuthResponseDto>> RegisterAsync(RegisterDto dto)
    {
        var validation = await _registerValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ServiceResult<AuthResponseDto>.Failure(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser is not null)
            return ServiceResult<AuthResponseDto>.Failure("Bu e-mail adresine sahip bir kullanıcı zaten mevcut.");

        // ── Güvenlik: Genel kayıt HER ZAMAN Öğrenci'dir ──────────
        // Personel/Yönetici rolleri, yetkili bir uç nokta aracılığıyla bir Yönetici tarafından atanmalı.
        // İstemci tarafından gönderilen herhangi bir rol değerini yok say.
        const UserRole assignedRole = UserRole.Student;
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = new ApplicationUser
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            UserName = email,
            Email = email,
            Role = assignedRole,
            DepartmentId = dto.DepartmentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Registration failed for {Email}: {Errors}", email, errors);
            return ServiceResult<AuthResponseDto>.Failure(errors);
        }

        // Rol ataması — başarısız olursa kullanıcıyı geri al (rollback)
        var roleResult = await _userManager.AddToRoleAsync(user, assignedRole.ToString());
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user); // rollback: önce oluşturuldu, şimdi siliniyor
            var errors = string.Join(" ", roleResult.Errors.Select(e => e.Description));
            _logger.LogError("Role assignment failed for {Email}, user deleted: {Errors}", email, errors);
            return ServiceResult<AuthResponseDto>.Failure(errors);
        }

        _logger.LogInformation("User registered: {Email} with role {Role}.", email, assignedRole);

        var response = await GenerateAuthResponseAsync(user);
        return ServiceResult<AuthResponseDto>.Success(response);
    }

    // ── Login ──────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDto dto)
    {
        var validation = await _loginValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return ServiceResult<AuthResponseDto>.Failure(
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return ServiceResult<AuthResponseDto>.Failure("Invalid email or password.", 401);

        var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!passwordValid)
        {
            _logger.LogWarning("Failed login attempt for {Email}.", dto.Email);
            return ServiceResult<AuthResponseDto>.Failure("Invalid email or password.", 401);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Locked out user attempted login: {Email}.", dto.Email);
            return ServiceResult<AuthResponseDto>.Failure(
                "Account is locked. Please try again later.", 403);
        }

        _logger.LogInformation("User logged in: {Email}.", dto.Email);

        // ── Login sync: update LastLoginAt + identity fields ─────────────
        try
        {
            user.LastLoginAt = DateTime.UtcNow;
            var syncUpdated = true; // always update at minimum LastLoginAt

            var identityInfo = await _identityService.GetByEmailAsync(user.Email!);
            if (identityInfo != null)
            {
                if (user.ExternalId != identityInfo.ExternalId)
                {
                    user.ExternalId = identityInfo.ExternalId;
                }

                if (user.PersonType != identityInfo.PersonType)
                {
                    user.PersonType = identityInfo.PersonType;
                }

                user.LastSyncedAt = DateTime.UtcNow;
                _logger.LogInformation("Identity synced for {Email}: {PersonType}", user.Email, user.PersonType);
            }

            await _userManager.UpdateAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync identity for {Email} during login.", user.Email);
        }

        var response = await GenerateAuthResponseAsync(user);
        return ServiceResult<AuthResponseDto>.Success(response);
    }

    // ── Refresh Token ──────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ServiceResult<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto dto)
    {
        // Extract user id from the expired access token (without validating lifetime)
        var principal = GetPrincipalFromExpiredToken(dto.Token);
        if (principal is null)
            return ServiceResult<AuthResponseDto>.Failure("Invalid access token.", 401);

        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null)
            return ServiceResult<AuthResponseDto>.Failure("Invalid token claims.", 401);

        var user = await _userManager.FindByIdAsync(userIdClaim);
        if (user is null)
            return ServiceResult<AuthResponseDto>.Failure("User not found.", 401);

        // Validate refresh token
        if (user.RefreshToken != dto.RefreshToken)
        {
            _logger.LogWarning("Invalid refresh token for user {Email}.", user.Email);
            return ServiceResult<AuthResponseDto>.Failure("Invalid refresh token.", 401);
        }

        if (user.RefreshTokenExpiry < DateTime.UtcNow)
        {
            _logger.LogWarning("Expired refresh token for user {Email}.", user.Email);
            return ServiceResult<AuthResponseDto>.Failure(
                "Refresh token has expired. Please login again.", 401);
        }

        _logger.LogInformation("Token refreshed for user {Email}.", user.Email);

        // Generate new token pair
        var response = await GenerateAuthResponseAsync(user);
        return ServiceResult<AuthResponseDto>.Success(response);
    }

    // ── Logout ─────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> LogoutAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return ServiceResult<bool>.NotFound("User not found.");

        // Revoke refresh token
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User logged out: {Email}. Refresh token revoked.", user.Email);

        return ServiceResult<bool>.Success(true);
    }

    // ── Password Reset ─────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            return ServiceResult<bool>.Failure("Bu e-posta adresine kayıtlı bir hesap bulunamadı.", 404);
        }

        if (!user.IsActive)
        {
            return ServiceResult<bool>.Failure("Bu hesap aktif değil. Yönetici ile iletişime geçin.", 403);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(token));

        var frontendUrl = _configuration["EmailSettings:FrontendBaseUrl"] ?? "http://localhost:5173";
        var resetLink = $"{frontendUrl}/#/reset-password?email={Uri.EscapeDataString(dto.Email)}&token={Uri.EscapeDataString(encodedToken)}";

        var body = $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden;'>
            <div style='background-color: #f8fafc; padding: 20px; text-align: center; border-bottom: 1px solid #e2e8f0;'>
                <h2 style='color: #0f172a; margin: 0;'>RİMER Destek Sistemi</h2>
            </div>
            <div style='padding: 30px; background-color: #ffffff;'>
                <h3 style='color: #1e293b; margin-top: 0;'>Şifre Sıfırlama Talebi</h3>
                <p style='color: #475569; line-height: 1.6;'>Merhaba {user.FullName},</p>
                <p style='color: #475569; line-height: 1.6;'>Hesabınız için bir şifre sıfırlama talebinde bulunuldu. Şifrenizi sıfırlamak için aşağıdaki butona tıklayın:</p>
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{resetLink}' style='background-color: #4f46e5; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;'>Şifremi Sıfırla</a>
                </div>
                <p style='color: #64748b; font-size: 13px; line-height: 1.5;'>Eğer bu talebi siz yapmadıysanız, bu e-postayı dikkate almayın. Hesabınız güvendedir.</p>
            </div>
            <div style='background-color: #f8fafc; padding: 15px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0;'>
                &copy; {DateTime.Now.Year} RİMER. Tüm hakları saklıdır.
            </div>
        </div>";

        await _emailService.SendEmailAsync(user.Email, "RİMER - Şifre Sıfırlama", body);

        _logger.LogInformation("Password reset link sent to {Email}", dto.Email);
        return ServiceResult<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            return ServiceResult<bool>.Failure("Kullanıcı bulunamadı.");
        }

        string decodedToken;
        try
        {
            decodedToken = Encoding.UTF8.GetString(Convert.FromBase64String(dto.Token));
        }
        catch
        {
            return ServiceResult<bool>.Failure("Geçersiz token formatı.");
        }

        var result = await _userManager.ResetPasswordAsync(user, decodedToken, dto.NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            return ServiceResult<bool>.Failure($"Şifre sıfırlama başarısız: {errors}");
        }

        _logger.LogInformation("Password has been reset for {Email}", dto.Email);
        return ServiceResult<bool>.Success(true);
    }

    // ── Private: Generate Auth Response ────────────────────────────

    /// <summary>
    /// Generates JWT access token + refresh token, saves refresh token to DB.
    /// </summary>
    private async Task<AuthResponseDto> GenerateAuthResponseAsync(ApplicationUser user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("role", user.Role.ToString()),
            new("firstName", user.FirstName),
            new("lastName", user.LastName),
            new("fullName", user.FullName),
        };

        // Embed department id so service layer can enforce dept-scoped access without DB roundtrip
        if (user.DepartmentId.HasValue)
            claims.Add(new Claim("departmentId", user.DepartmentId.Value.ToString()));


        var expirationMinutes = int.Parse(jwtSettings["ExpirationInMinutes"] ?? "60");
        var accessTokenExpiration = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var accessToken = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: accessTokenExpiration,
            signingCredentials: credentials);

        // Generate refresh token
        var refreshToken = GenerateRefreshToken();
        var refreshTokenDays = int.Parse(jwtSettings["RefreshTokenExpirationInDays"] ?? "7");
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(refreshTokenDays);

        // Save refresh token to user record
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = refreshTokenExpiry;
        await _userManager.UpdateAsync(user);

        return new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(accessToken),
            Expiration = accessTokenExpiration,
            RefreshToken = refreshToken,
            RefreshTokenExpiration = refreshTokenExpiry,
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = user.Role.ToString()
        };
    }

    // ── Private: Generate Refresh Token ────────────────────────────

    /// <summary>
    /// Generates a cryptographically secure random refresh token.
    /// </summary>
    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    // ── Private: Extract Principal from Expired Token ──────────────

    /// <summary>
    /// Validates and extracts claims from an expired JWT (skips lifetime validation).
    /// </summary>
    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]!;

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false, // Allow expired tokens for refresh
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };

        try
        {
            var principal = new JwtSecurityTokenHandler()
                .ValidateToken(token, validationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
