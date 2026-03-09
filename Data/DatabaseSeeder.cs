using LeveLEO.Features.AttributeGroups.Models;
using LeveLEO.Features.Brands.Models;
using LeveLEO.Features.Categories.Models;
using LeveLEO.Features.Identity.Models;
using LeveLEO.Features.ProductAttributes.Models;
using LeveLEO.Features.ProductAttributeValues.Models;
using LeveLEO.Features.Products.Models;
using LeveLEO.Infrastructure.Media.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace LeveLEO.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        var context = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var mediaService = services.GetRequiredService<IMediaService>();

        // Видаляємо і створюємо БД заново (тільки в Development)
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        // Повне очищення бакету (S3/MinIO)
        Console.WriteLine("Очищення медіа-бакету...");
        try
        {
            await mediaService.ClearBucketAsync();  // метод, який я дав раніше
            Console.WriteLine("Бакет повністю очищено.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка очищення бакету: {ex.Message}");
        }
        // Створюємо ролі
        string[] roles = ["Admin", "Moderator", "User"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Дефолтний адмін
        var adminEmail = "admin@leveleo.com";
        var adminPassword = "Admin123!@#";  // Зміни на свій

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "LeveLEO",
                Language = "uk",
                IsActive = true,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine($"Дефолтний адмін створено: {adminEmail} / {adminPassword}");
            }
            else
            {
                Console.WriteLine("Помилка створення адміна: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            Console.WriteLine($"Адмін вже існує: {adminEmail}");
        }
        await context.SaveChangesAsync();
        // Бренди + переклади uk + en
        if (!context.Brands.Any())
        {
            var brands = new[]
         {
            new Brand { Name = "Korg", Slug = "korg", Description = "Japanese manufacturer of synthesizers since 1962", MetaTitle = "Korg Synthesizers - Leveleo", MetaDescription = "Buy Korg Minilogue, Volca and other models in Ukraine"},
            new Brand { Name = "Arturia", Slug = "arturia", Description = "French hybrid instruments and synthesizers", MetaTitle = "Arturia MiniFreak, MicroFreak - Leveleo", MetaDescription = "Innovative Arturia synthesizers with delivery"},
            new Brand { Name = "Sennheiser", Slug = "sennheiser", Description = "Professional headphones and microphones", MetaTitle = "Sennheiser HD 25, HD 280 - Leveleo", MetaDescription = "DJ and studio headphones Sennheiser"},
            new Brand { Name = "Moog", Slug = "moog", Description = "Legendary analog synthesizers", MetaTitle = "Moog Synthesizers - Leveleo", MetaDescription = "Moog Subsequent, Grandmother and other models"},
            new Brand { Name = "Roland", Slug = "roland", Description = "Iconic electronic music instruments", MetaTitle = "Roland JD-Xi, Boutique - Leveleo", MetaDescription = "Roland synthesizers and workstations" },
            new Brand { Name = "Kali Audio", Slug = "kali-audio", Description = "Affordable high-quality studio monitors", MetaTitle = "Kali Audio LP-6, IN-5 - Leveleo", MetaDescription = "Kali Audio studio monitors with delivery"},
            new Brand { Name = "Yamaha", Slug = "yamaha", Description = "Professional audio and musical instruments", MetaTitle = "Yamaha HS8 - Leveleo", MetaDescription = "Yamaha studio monitors and headphones" },
            new Brand { Name = "Behringer", Slug = "behringer", Description = "Affordable analog synths", MetaTitle = "Behringer DeepMind - Leveleo", MetaDescription = "Behringer synthesizers with delivery" },
            new Brand { Name = "Audio-Technica", Slug = "audio-technica", Description = "Professional studio headphones", MetaTitle = "Audio-Technica ATH-M50x - Leveleo", MetaDescription = "Audio-Technica headphones"}
            };

            context.Brands.AddRange(brands);
            await context.SaveChangesAsync();

            foreach (var b in brands)
            {
                context.BrandTranslations.AddRange(
                    new BrandTranslation { BrandId = b.Id, LanguageCode = "uk", Name = b.Name, Description = GetUkrainianBrandDescription(b.Name) },
                    new BrandTranslation { BrandId = b.Id, LanguageCode = "en", Name = b.Name, Description = b.Description }
                );
            }
            await context.SaveChangesAsync();
        }
        // Категорії + переклади uk + en + closure
        if (!context.Categories.Any())
        {
            var root = new Category { Name = "Synthesizers", Slug = "synthesizers", Description = "Electronic musical instruments", MetaTitle = "Buy Synthesizers in Ukraine", MetaDescription = "Analog and digital synthesizers with delivery", IsActive = true };
            context.Categories.Add(root);
            await context.SaveChangesAsync();

            var analog = new Category { Name = "Analog Synthesizers", Slug = "analog-synths", ParentId = root.Id, Description = "Pure analog sound", MetaTitle = "Analog Synthesizers", MetaDescription = "Moog, Korg, Arturia analog", IsActive = true };
            var digital = new Category { Name = "Digital/Hybrid Synthesizers", Slug = "digital-hybrid-synths", ParentId = root.Id, Description = "Digital and hybrid instruments", MetaTitle = "Digital Synthesizers", MetaDescription = "Roland, Korg digital synths", IsActive = true };
            var headphones = new Category { Name = "Headphones", Slug = "studio-headphones", Description = "For studio and DJ", MetaTitle = "Studio Headphones", MetaDescription = "Sennheiser, Audio-Technica", IsActive = true };
            var monitors = new Category { Name = "Studio Monitors", Slug = "studio-monitors", Description = "Nearfield studio speakers", MetaTitle = "Studio Monitors", MetaDescription = "Kali Audio, Yamaha HS", IsActive = true };
            var microphones = new Category { Name = "Microphones", Slug = "microphones", Description = "For vocals and recording", MetaTitle = "Microphones", MetaDescription = "Shure SM7B, Rode NT1", IsActive = true };
            context.Categories.AddRange(analog, digital, headphones, monitors, microphones);
            await context.SaveChangesAsync();

            var poly = new Category { Name = "Polyphonic", Slug = "polyphonic", ParentId = analog.Id, Description = "Multi-voice analog", MetaTitle = "Polyphonic Synthesizers", MetaDescription = "Korg Minilogue, Arturia", IsActive = true };
            var mono = new Category { Name = "Monophonic", Slug = "monophonic", ParentId = analog.Id, Description = "Single-voice analog", MetaTitle = "Monophonic Synthesizers", MetaDescription = "Moog Subsequent", IsActive = true };
            context.Categories.AddRange(poly, mono);
            await context.SaveChangesAsync();

            // CategoryClosure (твій код залишається без змін)
            var cats = await context.Categories.ToListAsync();
            context.CategoryClosures.RemoveRange(context.CategoryClosures);
            await context.SaveChangesAsync();

            foreach (var c in cats)
            {
                context.CategoryClosures.Add(new CategoryClosure { AncestorId = c.Id, DescendantId = c.Id, Depth = 0 });

                var curr = c;
                var depth = 1;
                while (curr.ParentId.HasValue)
                {
                    var p = cats.First(x => x.Id == curr.ParentId.Value);
                    context.CategoryClosures.Add(new CategoryClosure { AncestorId = p.Id, DescendantId = c.Id, Depth = depth });
                    curr = p;
                    depth++;
                }
            }
            await context.SaveChangesAsync();

            // Переклади категорій (якщо є)
            foreach (var c in cats)
            {
                context.CategoryTranslations.AddRange(
                    new CategoryTranslation { CategoryId = c.Id, LanguageCode = "uk", Name = c.Name, Description = c.Description },
                    new CategoryTranslation { CategoryId = c.Id, LanguageCode = "en", Name = c.Name, Description = c.Description }
                );
            }
            await context.SaveChangesAsync();
        }
        // Атрибути + групи + переклади
        if (!context.AttributeGroups.Any())
        {
            var group = new AttributeGroup { Name = "Technical Specifications", Slug = "technical-specs", Description = "Main device parameters" };
            context.AttributeGroups.Add(group);
            await context.SaveChangesAsync();

            var attrs = new[]
            {
                new ProductAttribute { Name = "Polyphony", Slug = "polyphony", AttributeGroupId = group.Id, Type = AttributeType.String, IsFilterable = true, IsComparable = true },
                new ProductAttribute { Name = "Impedance", Slug = "impedance", AttributeGroupId = group.Id, Type = AttributeType.String, Unit = "Ω", IsFilterable = true },
                new ProductAttribute { Name = "Woofer Size", Slug = "woofer-size", AttributeGroupId = group.Id, Type = AttributeType.String, Unit = "\"", IsFilterable = true } // <--- додай це
            };
            context.ProductAttributes.AddRange(attrs);
            await context.SaveChangesAsync();

            foreach (var a in attrs)
            {
                context.ProductAttributeTranslations.AddRange(
                    new ProductAttributeTranslation { ProductAttributeId = a.Id, LanguageCode = "uk", Name = GetUkrainianAttributeName(a.Name), Description = a.Description },
                    new ProductAttributeTranslation { ProductAttributeId = a.Id, LanguageCode = "en", Name = a.Name, Description = a.Description }
                );
            }
            await context.SaveChangesAsync();
        }
        //Продукти + переклади (uk + en) + атрибути
        if (!context.Products.Any())
        {
            var brandsDict = await context.Brands.ToDictionaryAsync(b => b.Slug, b => b.Id);
            var catsDict = await context.Categories.ToDictionaryAsync(c => c.Slug, c => c.Id);
            var attrsDict = await context.ProductAttributes.ToDictionaryAsync(a => a.Slug, a => a.Id);

            var products = new List<Product>
            {
                // Поліфонічні аналогові
                new() { Name = "Korg Minilogue XD", Slug = "korg-minilogue-xd", Description = "4-voice analog polyphonic synthesizer with multi-engine, effects, and built-in sequencer", Price = 749.00m, StockQuantity = 8, IsActive = true, BrandId = brandsDict["korg"], CategoryId = catsDict["polyphonic"] },
                new() { Name = "Arturia PolyBrute", Slug = "arturia-polybrute", Description = "6-voice fully analog polyphonic synthesizer with morphing, modulation matrix, and built-in effects", Price = 2499.00m, StockQuantity = 4, IsActive = true, BrandId = brandsDict["arturia"], CategoryId = catsDict["polyphonic"] },

                // Монофонічні аналогові
                new() { Name = "Moog Subsequent 37", Slug = "moog-subsequent-37", Description = "Paraphonic analog synthesizer with classic Moog ladder filter and multidrive", Price = 1799.00m, StockQuantity = 5, IsActive = true, BrandId = brandsDict["moog"], CategoryId = catsDict["monophonic"] },
                new() { Name = "Korg Minilogue XD (DeepMind clone vibe)", Slug = "korg-deepmind-vibe", Description = "12-voice analog polyphonic synthesizer with TC Electronic effects and DSP processor", Price = 699.00m, StockQuantity = 7, IsActive = true, BrandId = brandsDict["korg"], CategoryId = catsDict["monophonic"] },

                // Цифрові / Гібридні
                new() { Name = "Roland JD-Xi", Slug = "roland-jd-xi", Description = "Hybrid synthesizer with digital engines, vocoder, microphone input, and full sequencer", Price = 599.00m, StockQuantity = 10, IsActive = true, BrandId = brandsDict["roland"], CategoryId = catsDict["digital-hybrid-synths"] },
                new() { Name = "Korg Opsix", Slug = "korg-opsix", Description = "FM synthesizer with 6 operators and reimagined FM synthesis approach", Price = 799.00m, StockQuantity = 6, IsActive = true, BrandId = brandsDict["korg"], CategoryId = catsDict["digital-hybrid-synths"] },

                // Студійні навушники
                new() { Name = "Sennheiser HD 25", Slug = "sennheiser-hd-25", Description = "Closed-back DJ headphones with high sound isolation and replaceable parts", Price = 149.00m, StockQuantity = 30, IsActive = true, BrandId = brandsDict["sennheiser"], CategoryId = catsDict["studio-headphones"] },
                new() { Name = "Sennheiser HD 280 Pro", Slug = "sennheiser-hd-280-pro", Description = "Professional studio monitoring headphones with flat frequency response and detachable cable", Price = 169.00m, StockQuantity = 25, IsActive = true, BrandId = brandsDict["sennheiser"], CategoryId = catsDict["studio-headphones"] },

                // Студійні монітори
                new() { Name = "Kali Audio LP-6 v2", Slug = "kali-lp-6-v2", Description = "6.5-inch active nearfield studio monitors with accurate sound reproduction and boundary EQ", Price = 159.00m, StockQuantity = 20, IsActive = true, BrandId = brandsDict["kali-audio"], CategoryId = catsDict["studio-monitors"] },
                new() { Name = "Yamaha HS8", Slug = "yamaha-hs8", Description = "8-inch studio monitors with room control and high-precision sound reproduction", Price = 249.00m, StockQuantity = 12, IsActive = true, BrandId = brandsDict["yamaha"], CategoryId = catsDict["studio-monitors"] },

                // Мікрофони
                new() { Name = "Shure SM7B", Slug = "shure-sm7b", Description = "Dynamic microphone with legendary sound for vocals, podcasts, and broadcasting", Price = 399.00m, StockQuantity = 12, IsActive = true, BrandId = brandsDict["sennheiser"], CategoryId = catsDict["microphones"] },
                new() { Name = "Rode NT1 5th Gen", Slug = "rode-nt1-5th-gen", Description = "Large-diaphragm condenser microphone with ultra-low noise and USB/XLR outputs", Price = 249.00m, StockQuantity = 18, IsActive = true, BrandId = brandsDict["sennheiser"], CategoryId = catsDict["microphones"] }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            // Переклади продуктів (uk + en) — повноцінні тексти
            foreach (var p in products)
            {
                context.ProductTranslations.AddRange(
                    new ProductTranslation { ProductId = p.Id, LanguageCode = "uk", Name = GetUkrainianProductName(p.Name), Description = GetUkrainianProductDescription(p.Name) },
                    new ProductTranslation { ProductId = p.Id, LanguageCode = "en", Name = p.Name, Description = p.Description }
                );
            }
            await context.SaveChangesAsync();

            // Атрибути (як приклад)
            context.ProductAttributeValues.AddRange(
            new ProductAttributeValue { ProductId = products[0].Id, ProductAttributeId = attrsDict["polyphony"], StringValue = "4 voices" },
            new ProductAttributeValue { ProductId = products[1].Id, ProductAttributeId = attrsDict["polyphony"], StringValue = "6 voices" },
            new ProductAttributeValue { ProductId = products[4].Id, ProductAttributeId = attrsDict["impedance"], StringValue = "70 Ω" },
            new ProductAttributeValue { ProductId = products[7].Id, ProductAttributeId = attrsDict["impedance"], StringValue = "38 Ω" },
            new ProductAttributeValue { ProductId = products[8].Id, ProductAttributeId = attrsDict["woofer-size"], StringValue = "6.5\"" },
            new ProductAttributeValue { ProductId = products[9].Id, ProductAttributeId = attrsDict["woofer-size"], StringValue = "8\"" }
        );
            await context.SaveChangesAsync();
        }
        Console.WriteLine("База успішно засіяна!");
    }

    // Допоміжні функції для українських перекладів
    private static string GetUkrainianBrandName(string enName)
    {
        return enName; // назви брендів зазвичай не перекладають
    }

    private static string GetUkrainianBrandDescription(string enName)
    {
        return enName switch
        {
            "Korg" => "Японський виробник синтезаторів з 1962 року",
            "Arturia" => "Французькі гібридні інструменти та синтезатори",
            "Sennheiser" => "Професійні навушники та мікрофони",
            _ => "Професійне аудіообладнання"
        };
    }

    private static string GetUkrainianCategoryName(string enName)
    {
        return enName switch
        {
            "Synthesizers" => "Синтезатори",
            "Analog Synthesizers" => "Аналогові синтезатори",
            "Polyphonic" => "Поліфонічні",
            "Headphones" => "Навушники",
            _ => enName
        };
    }

    private static string GetUkrainianCategoryDescription(string enName)
    {
        return enName switch
        {
            "Synthesizers" => "Електронні музичні інструменти",
            "Analog Synthesizers" => "Чисто аналоговий звук",
            "Polyphonic" => "Багатоголосі аналогові",
            "Headphones" => "Для студії та DJ",
            _ => enName
        };
    }

    private static string GetUkrainianAttributeName(string enName)
    {
        return enName switch
        {
            "Polyphony" => "Поліфонія",
            "Impedance" => "Імпеданс",
            _ => enName
        };
    }

    private static string GetUkrainianProductName(string enName)
    {
        return enName switch
        {
            "Korg Minilogue XD" => "Korg Minilogue XD",
            "Arturia PolyBrute" => "Arturia PolyBrute",
            "Moog Subsequent 37" => "Moog Subsequent 37",
            "Behringer DeepMind 12" => "Behringer DeepMind 12",
            "Roland JD-Xi" => "Roland JD-Xi",
            "Korg Opsix" => "Korg Opsix",
            "Sennheiser HD 25" => "Sennheiser HD 25",
            "Audio-Technica ATH-M50x" => "Audio-Technica ATH-M50x",
            "Kali Audio LP-6 v2" => "Kali Audio LP-6 v2",
            "Yamaha HS8" => "Yamaha HS8",
            "Shure SM7B" => "Shure SM7B",
            "Rode NT1 5th Gen" => "Rode NT1 5th Gen",
            _ => enName
        };
    }

    private static string GetUkrainianProductDescription(string enName)
    {
        return enName switch
        {
            "Korg Minilogue XD" => "4-голосний аналоговий поліфонічний синтезатор з мультидвигуном, ефектами та вбудованим секвенсором",
            "Arturia PolyBrute" => "6-голосний повністю аналоговий поліфон з морфінгом, матрицею модуляції та вбудованими ефектами",
            "Moog Subsequent 37" => "Парафонічний аналоговый синтезатор з класичним Moog ladder-фільтром та мульті-драйвом",
            "Behringer DeepMind 12" => "12-голосний аналоговый поліфонічний синтезатор з ефектами TC Electronic та DSP-процесором",
            "Roland JD-Xi" => "Гібридний синтезатор з цифровими двигунами, вокодером, мікрофонним входом та повноцінним секвенсором",
            "Korg Opsix" => "FM-синтезатор з 6 операторами, переосмисленим FM-підходом та вбудованими ефектами",
            "Sennheiser HD 25" => "Закриті навушники для DJ та студії з високою звукоізоляцією та змінними компонентами",
            "Audio-Technica ATH-M50x" => "Професійні студійні моніторні навушники з плоскою частотною характеристикою та знімним кабелем",
            "Kali Audio LP-6 v2" => "6.5-дюймові активні nearfield монітори з точною передачею звуку та boundary EQ",
            "Yamaha HS8" => "8-дюймові студійні монітори з кімнатним контролем та високоточним відтворенням",
            "Shure SM7B" => "Динамічний мікрофон з легендарним звучанням для вокалу, подкастів та мовлення",
            "Rode NT1 5th Gen" => "Конденсаторний мікрофон з наднизьким рівнем шуму та USB/XLR виходами",
            _ => "Професійне музичне обладнання"
        };
    }
}