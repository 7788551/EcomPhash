
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// 1. Register MVC
builder.Services.AddControllersWithViews();

// 2. Register Session storage
builder.Services.AddDistributedMemoryCache();

// 3. Configure existing Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(
        builder.Configuration.GetValue<int>(
            "SessionSettings:TimeoutMinutes"
        )
    );

    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 4. Configure JWT Authentication
builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme
    )
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                // Validate token issuer
                ValidateIssuer = true,
                ValidIssuer =
                    builder.Configuration["Jwt:Issuer"],

                // Validate token audience
                ValidateAudience = true,
                ValidAudience =
                    builder.Configuration["Jwt:Audience"],

                // Reject expired tokens
                ValidateLifetime = true,

                // Validate the digital signature
                ValidateIssuerSigningKey = true,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Convert.FromBase64String(
                            builder.Configuration["Jwt:Key"]
                            ?? throw new InvalidOperationException(
                                "JWT Key is missing from configuration."
                            )
                        )
                    )
            };

        // Read JWT from our cookie
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token =
                    context.Request.Cookies["jwt"];

                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

// 5. Configure HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// 6. Keep existing Session
app.UseSession();

// 7. Authenticate before authorization
app.UseAuthentication();
app.UseAuthorization();

// 8. Default MVC route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);

app.Run();
