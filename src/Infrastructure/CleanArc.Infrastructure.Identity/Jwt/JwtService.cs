using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CleanArc.Application.Contracts;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Jwt;
using CleanArc.Domain.Entities.User;
using CleanArc.Infrastructure.Identity.Identity.Dtos;
using CleanArc.Infrastructure.Identity.Identity.Manager;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CleanArc.Infrastructure.Identity.Jwt;

public class JwtService : IJwtService
{
    private readonly IdentitySettings _siteSetting;
    private readonly AppUserManager _userManager;
    private IUserClaimsPrincipalFactory<User> _claimsPrincipal;

    private readonly IUnitOfWork _unitOfWork;

    public JwtService(IOptions<IdentitySettings> siteSetting, AppUserManager userManager, IUserClaimsPrincipalFactory<User> claimsPrincipal, IUnitOfWork unitOfWork)
    {
        _siteSetting = siteSetting.Value;
        _userManager = userManager;
        _claimsPrincipal = claimsPrincipal;
        _unitOfWork = unitOfWork;
    }

    public async Task<AccessToken> GenerateAsync(User user)
    {
        var secretKey = Encoding.UTF8.GetBytes(_siteSetting.SecretKey);
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(secretKey),
            SecurityAlgorithms.HmacSha256);

        var claims = await _getClaimsAsync(user);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _siteSetting.Issuer,
            Audience = _siteSetting.Audience,
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(_siteSetting.ExpirationMinutes),
            SigningCredentials = signingCredentials,
            Subject = new ClaimsIdentity(claims)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateJwtSecurityToken(descriptor);

        var refreshToken = await _unitOfWork.UserRefreshTokenRepository.CreateToken(user.Id);
        await _unitOfWork.CommitAsync();

        return new AccessToken(securityToken, refreshToken.ToString());
    }

    public Task<ClaimsPrincipal> GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_siteSetting.SecretKey)),
            ValidateLifetime = false,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        var jwtSecurityToken = securityToken as JwtSecurityToken;
        if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return Task.FromResult(principal);
    }

    public async Task<AccessToken> GenerateByPhoneNumberAsync(string phoneNumber)
    {
        var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        var result = await this.GenerateAsync(user);
        return result;
    }

    public async Task<AccessToken> RefreshToken(Guid refreshTokenId)
    {
        var refreshToken = await _unitOfWork.UserRefreshTokenRepository.GetTokenWithInvalidation(refreshTokenId);
            
        if (refreshToken is null)
            return null;

        refreshToken.IsValid = false;

        await _unitOfWork.CommitAsync();

        var user = await _unitOfWork.UserRefreshTokenRepository.GetUserByRefreshToken(refreshTokenId);

        if (user is null)
            return null;

        var result = await this.GenerateAsync(user);

        return result;
    }

    private async Task<IEnumerable<Claim>> _getClaimsAsync(User user)
    {
        var result = await _claimsPrincipal.CreateAsync(user);
        return result.Claims;
    }
}