using iDoctor.Domain.Constants;
using iDoctor.Domain.Entities;
using iDoctor.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace iDoctor.Api.Seed;

public class DataSeeder : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;

    public DataSeeder(IServiceProvider services, IConfiguration config)
    {
        _services = services;
        _config = config;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await db.Database.MigrateAsync(cancellationToken);
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        // Roles
        foreach (var role in new[] { Roles.Patient, Roles.Doctor, Roles.Admin })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new ApplicationRole { Id = Guid.NewGuid(), Name = role });
        }

        // Admin user
        var email = _config["Seed:AdminEmail"];
        var password = _config["Seed:AdminPassword"];
        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password)
            && await userManager.FindByEmailAsync(email) is null)
        {
            var admin = new User
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                PhoneNumber = _config["Seed:AdminPhone"] ?? "+992900000000",
                FullName = "Platform Admin"
            };
            var result = await userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
                throw new InvalidOperationException("Admin seed failed: " +
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }

        // Starter lookup data (so doctor registration has something to reference)
        if (!await db.Cities.AnyAsync(cancellationToken))
        {
            db.Cities.AddRange(
                new City { Name = "Dushanbe" }, new City { Name = "Khujand" },
                new City { Name = "Bokhtar" }, new City { Name = "Kulob" });
        }
        if (!await db.Specialties.AnyAsync(cancellationToken))
        {
            db.Specialties.AddRange(
                new Specialty { Name = "Therapist" }, new Specialty { Name = "Cardiologist" },
                new Specialty { Name = "Pediatrician" }, new Specialty { Name = "Neurologist" },
                new Specialty { Name = "Dermatologist" });
        }
        await db.SaveChangesAsync(cancellationToken);

                // Starter clinics (placeholder data, replace with real clinics later)
        if (!await db.Clinics.AnyAsync(cancellationToken))
        {
            var dushanbe = await db.Cities.FirstOrDefaultAsync(c => c.Name == "Dushanbe", cancellationToken);
            var khujand = await db.Cities.FirstOrDefaultAsync(c => c.Name == "Khujand", cancellationToken);

            db.Clinics.AddRange(
                new Clinic { Name = "City Medical Center", CityId = dushanbe?.Id, Address = "Rudaki Ave 10", Phone = "+992372000001" },
                new Clinic { Name = "Family Health Clinic", CityId = dushanbe?.Id, Address = "Somoni Ave 25", Phone = "+992372000002" },
                new Clinic { Name = "Sughd Diagnostic Center", CityId = khujand?.Id, Address = "Ismoili Somoni St 5", Phone = "+992342000003" });
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}