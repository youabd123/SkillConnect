using System.ComponentModel.DataAnnotations;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using SkillConnect.Infrastructure.Identity;
using SkillConnect.Infrastructure.Persistence;

namespace SkillConnect.Api.Accounts;

public static class AccountEndpoints
{
    public static void AddSkillConnectAccounts(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddIdentity<SkillConnectUser, IdentityRole<Guid>>(options =>
        {
            options.SignIn.RequireConfirmedEmail = true;
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = string.Empty;
            options.Password.RequiredLength = 12;
        })
        .AddEntityFrameworkStores<SkillConnectDbContext>()
        .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services.AddAuthorizationBuilder()
            .AddPolicy(AccountAccess.CustomerPolicy, policy => policy.RequireAccountRole(AccountRole.Customer))
            .AddPolicy(AccountAccess.ProviderPolicy, policy => policy.RequireAccountRole(AccountRole.Provider));
        services.AddScoped<IConfirmationEmailSender, ConfirmationEmailSender>();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("account-attempts", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                }));
        });
    }

    public static void MapSkillConnectAccounts(this WebApplication app)
    {
        var accounts = app.MapGroup("/api/accounts");
        accounts.MapPost("/register", RegisterAsync).RequireRateLimiting("account-attempts");
        accounts.MapPost("/resend-confirmation", ResendConfirmationAsync).RequireRateLimiting("account-attempts");
        accounts.MapPost("/confirm-email", ConfirmEmailAsync).RequireRateLimiting("account-attempts");
        accounts.MapPost("/login", LoginAsync).RequireRateLimiting("account-attempts");
        accounts.MapGet("/me", GetAccountAsync).RequireAuthorization();
        app.MapPut("/api/provider/profile", UpdateProviderProfileAsync).RequireAuthorization(AccountAccess.ProviderPolicy);
        accounts.MapPost("/logout", LogoutAsync).RequireAuthorization();
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request, UserManager<SkillConnectUser> users, IConfirmationEmailSender sender)
    {
        var email = request.Email?.Trim();
        if (!IsValidEmail(email) || request.Password is null || request.Password.Length > 128)
        {
            return Results.BadRequest(new { message = "Enter a valid email and password." });
        }

        var role = request.Role switch
        {
            "customer" => AccountRole.Customer,
            "provider" => AccountRole.Provider,
            _ => (AccountRole?)null,
        };
        if (role is null)
        {
            return Results.BadRequest(new { message = "Choose customer or provider." });
        }

        var user = new SkillConnectUser { Id = Guid.NewGuid(), Email = email, UserName = email, AccountRole = role.Value };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            if (result.Errors.All(error => error.Code is "DuplicateEmail" or "DuplicateUserName"))
            {
                return Results.Accepted();
            }

            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["account"] = result.Errors.Select(error => error.Description).ToArray(),
            });
        }

        await SendConfirmationAsync(user, users, sender);
        return Results.Accepted();
    }

    private static async Task<IResult> ResendConfirmationAsync(
        EmailRequest request, UserManager<SkillConnectUser> users, IConfirmationEmailSender sender)
    {
        var email = request.Email?.Trim();
        if (IsValidEmail(email))
        {
            var user = await users.FindByEmailAsync(email!);
            if (user is { EmailConfirmed: false })
            {
                await SendConfirmationAsync(user, users, sender);
            }
        }

        return Results.Accepted();
    }

    private static async Task SendConfirmationAsync(
        SkillConnectUser user, UserManager<SkillConnectUser> users, IConfirmationEmailSender sender)
    {
        var code = await users.GenerateEmailConfirmationTokenAsync(user);
        await sender.SendAsync(user.Email!, user.Id, code);
    }

    private static async Task<IResult> ConfirmEmailAsync(ConfirmEmailRequest request, UserManager<SkillConnectUser> users)
    {
        if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest();
        }

        var user = await users.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return Results.BadRequest();
        }

        var result = await users.ConfirmEmailAsync(user, request.Code);
        return result.Succeeded ? Results.NoContent() : Results.BadRequest();
    }

    private static async Task<IResult> LoginAsync(LoginRequest request, SignInManager<SkillConnectUser> signIn)
    {
        var email = request.Email?.Trim();
        if (!IsValidEmail(email) || string.IsNullOrEmpty(request.Password) || request.Password.Length > 128)
        {
            return Results.Unauthorized();
        }

        var result = await signIn.PasswordSignInAsync(email!, request.Password, isPersistent: false, lockoutOnFailure: true);
        return result.Succeeded ? Results.NoContent() : Results.Unauthorized();
    }

    private static async Task<IResult> GetAccountAsync(HttpContext context, UserManager<SkillConnectUser> users)
    {
        var user = await users.GetUserAsync(context.User);
        return user is null ? Results.Unauthorized() : Results.Ok(new
        {
            email = user.Email,
            role = user.AccountRole == AccountRole.Provider ? "provider" : "customer",
            businessName = user.AccountRole == AccountRole.Provider ? user.BusinessName : null,
            businessDescription = user.AccountRole == AccountRole.Provider ? user.BusinessDescription : null,
        });
    }

    private static async Task<IResult> UpdateProviderProfileAsync(
        ProviderProfileRequest request, HttpContext context, UserManager<SkillConnectUser> users)
    {
        var businessName = request.BusinessName?.Trim();
        var businessDescription = request.BusinessDescription?.Trim();
        if (businessName is not { Length: > 0 and <= 120 })
        {
            return Results.BadRequest(new { message = "Enter a business name of up to 120 characters." });
        }
        if (businessDescription is { Length: > 1000 })
        {
            return Results.BadRequest(new { message = "Enter a business description of up to 1000 characters." });
        }

        var user = await users.GetUserAsync(context.User);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        user.BusinessName = businessName;
        if (businessDescription is not null)
        {
            user.BusinessDescription = businessDescription.Length == 0 ? null : businessDescription;
        }
        var result = await users.UpdateAsync(user);
        return result.Succeeded
            ? Results.Ok(new { businessName, businessDescription = user.BusinessDescription })
            : Results.Conflict(new { message = "The provider profile could not be saved. Try again." });
    }

    private static async Task<IResult> LogoutAsync(HttpRequest request, SignInManager<SkillConnectUser> signIn)
    {
        if (!request.HasJsonContentType())
        {
            return Results.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }

        await signIn.SignOutAsync();
        return Results.NoContent();
    }

    private static bool IsValidEmail(string? email) =>
        email is { Length: > 0 and <= 254 } && new EmailAddressAttribute().IsValid(email);

    private sealed record RegisterRequest(string? Email, string? Password, string? Role);
    private sealed record EmailRequest(string? Email);
    private sealed record ConfirmEmailRequest(Guid UserId, string? Code);
    private sealed record LoginRequest(string? Email, string? Password);
    private sealed record ProviderProfileRequest(string? BusinessName, string? BusinessDescription);
}
