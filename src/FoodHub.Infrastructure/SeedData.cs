using FoodHub.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodHub.Infrastructure;
public static class SeedData
{
    public static async Task Initialize(FoodDbContext db, IPasswordHasher<User> hasher)
    {
        if (await db.Users.AnyAsync())
            return;
        User Make(string name, string email, string role)
        {
            var u = new User
            {
                Name = name,
                Email = email,
                Role = role
            };
            u.PasswordHash = hasher.HashPassword(u, "Demo@12345");
            return u;
        }

        db.Users.AddRange(Make("Platform Admin", "admin@foodhub.local", Roles.Admin), Make("Atta Customer", "customer@foodhub.local", Roles.Customer));
        var a = new Restaurant
        {
            Latitude = 34.015,
            Longitude = 71.525,
            Name = "ABC Burgers",
            Cuisine = "Burgers • Fast food",
            Address = "University Road, Peshawar",
            CommissionPercent = 20,
            Owner = Make("ABC Owner", "abc@foodhub.local", Roles.Restaurant)
        };
        a.Menu = [new()
        {
            Name = "Classic Beef Burger",
            Description = "Grilled beef, lettuce, cheese and house sauce",
            PriceCents = 50000
        }, new()
        {
            Name = "Crispy Chicken Burger",
            Description = "Crispy chicken with fresh slaw",
            PriceCents = 45000
        }, new()
        {
            Name = "Loaded Fries",
            Description = "Fries with cheese and signature sauce",
            PriceCents = 30000
        }

        ];
        var p = new Restaurant
        {
            Latitude = 34.000,
            Longitude = 71.480,
            Name = "Peshawar Pizza",
            Cuisine = "Pizza • Italian",
            Address = "Hayatabad, Peshawar",
            CommissionPercent = 15,
            Owner = Make("Pizza Owner", "pizza@foodhub.local", Roles.Restaurant)
        };
        p.Menu = [new()
        {
            Name = "Chicken Tikka Pizza",
            Description = "Medium pizza with chicken tikka and peppers",
            PriceCents = 120000
        }, new()
        {
            Name = "Margherita Pizza",
            Description = "Tomato, mozzarella and basil",
            PriceCents = 100000
        }

        ];
        var k = new Restaurant
        {
            Latitude = 34.005,
            Longitude = 71.555,
            Name = "Khyber Kitchen",
            Cuisine = "Pakistani • Traditional",
            Address = "Saddar, Peshawar",
            CommissionPercent = 25,
            Owner = Make("Kitchen Owner", "khyber@foodhub.local", Roles.Restaurant)
        };
        k.Menu = [new()
        {
            Name = "Chicken Biryani",
            Description = "Aromatic rice, chicken and raita",
            PriceCents = 40000
        }, new()
        {
            Name = "Chapli Kebab Plate",
            Description = "Two kebabs with naan and chutney",
            PriceCents = 60000
        }

        ];
        db.Restaurants.AddRange(a, p, k);
        await db.SaveChangesAsync();
    }
}
