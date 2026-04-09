using LeveLEO.Features.AttributeGroups.Models;
using LeveLEO.Features.Brands.Models;
using LeveLEO.Features.Categories.Models;
using LeveLEO.Features.Identity.Models;
using LeveLEO.Features.ProductAttributes.Models;
using LeveLEO.Features.ProductAttributeValues.Models;
using LeveLEO.Features.Products.Models;
using LeveLEO.Features.Promotions.Models;
using LeveLEO.Features.Promotions.Models.LevelConditions;
using LeveLEO.Infrastructure.Media.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        Console.WriteLine("Очищення медіа-бакету...");
        try
        {
            await mediaService.ClearBucketAsync();

            Console.WriteLine("Бакет повністю очищено.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка очищення бакету: {ex.Message}");
        }

        string[] roles = ["Admin", "Moderator", "User"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminEmail = "admin@leveleo.com";

        var adminPassword = "Admin123!@#";

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

        // БРЕНДИ
        if (!context.Brands.Any())
        {
            var brands = new[]
            {
                new Brand { Name = "Korg", Slug = "korg", Description = "Japanese manufacturer of synthesizers since 1962" },
                new Brand { Name = "Arturia", Slug = "arturia", Description = "French hybrid instruments and synthesizers" },
                new Brand { Name = "Moog", Slug = "moog", Description = "Legendary analog synthesizers" },
                new Brand { Name = "Roland", Slug = "roland", Description = "Iconic electronic music instruments" },
                new Brand { Name = "Behringer", Slug = "behringer", Description = "Affordable analog synths" },
                new Brand { Name = "Sennheiser", Slug = "sennheiser", Description = "Professional headphones and microphones" },
                new Brand { Name = "Audio-Technica", Slug = "audio-technica", Description = "Professional studio headphones" },
                new Brand { Name = "Shure", Slug = "shure", Description = "Professional microphones and audio equipment" },
                new Brand { Name = "Rode", Slug = "rode", Description = "Studio microphones and audio equipment" },
                new Brand { Name = "Kali Audio", Slug = "kali-audio", Description = "Affordable high-quality studio monitors" },
                new Brand { Name = "Yamaha", Slug = "yamaha", Description = "Professional audio and musical instruments" },
                new Brand { Name = "JBL", Slug = "jbl", Description = "Professional sound systems and monitors" },
                new Brand { Name = "QSC", Slug = "qsc", Description = "Professional power amplifiers" },
                new Brand { Name = "Soundcraft", Slug = "soundcraft", Description = "Professional mixing consoles" },
                new Brand { Name = "Allen & Heath", Slug = "allen-heath", Description = "Professional mixing consoles" },
                new Brand { Name = "Fender", Slug = "fender", Description = "Legendary electric guitars and basses" },
                new Brand { Name = "Ibanez", Slug = "ibanez", Description = "High-performance guitars and basses" },
                new Brand { Name = "Gibson", Slug = "gibson", Description = "Iconic American guitars" },
                new Brand { Name = "PRS", Slug = "prs", Description = "Premium electric guitars" },
                new Brand { Name = "ESP", Slug = "esp", Description = "Metal and rock guitars" },
                new Brand { Name = "Pearl", Slug = "pearl", Description = "Professional drum kits" },
                new Brand { Name = "Tama", Slug = "tama", Description = "High-quality drum kits" },
                new Brand { Name = "Zildjian", Slug = "zildjian", Description = "Premium cymbals" },
                new Brand { Name = "Casio", Slug = "casio", Description = "Digital pianos and keyboards" },
                new Brand { Name = "Nord", Slug = "nord", Description = "Professional stage keyboards" },
                new Brand { Name = "Yamaha Electric", Slug = "yamaha-electric", Description = "Electric violins and strings" },
                new Brand { Name = "NS Design", Slug = "ns-design", Description = "Electric string instruments" },
                new Brand { Name = "Pioneer DJ", Slug = "pioneer-dj", Description = "Professional DJ equipment" },
                new Brand { Name = "Numark", Slug = "numark", Description = "DJ equipment and controllers" },
                new Brand { Name = "Denon DJ", Slug = "denon-dj", Description = "Professional DJ equipment" },
                new Brand { Name = "Native Instruments", Slug = "native-instruments", Description = "DJ software and hardware" },
                new Brand { Name = "Yamaha Winds", Slug = "yamaha-winds", Description = "Professional wind instruments" },
                new Brand { Name = "Conn-Selmer", Slug = "conn-selmer", Description = "Wind instruments" }
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

        // КАТЕГОРІЇ
        if (!context.Categories.Any())
        {
            var soundEquipment = new Category { Name = "Sound Equipment", Slug = "sound-equipment", Description = "Professional audio equipment", MetaTitle = "Sound Equipment - LeveLEO", MetaDescription = "Professional sound equipment", IsActive = true };
            var musicalInstruments = new Category { Name = "Musical Instruments", Slug = "musical-instruments", Description = "All types of musical instruments", MetaTitle = "Musical Instruments - LeveLEO", MetaDescription = "Guitars, keyboards, drums and more", IsActive = true };
            var djEquipment = new Category { Name = "DJ Equipment", Slug = "dj-equipment", Description = "Professional DJ gear", MetaTitle = "DJ Equipment - LeveLEO", MetaDescription = "DJ controllers, mixers, turntables", IsActive = true };

            context.Categories.AddRange(soundEquipment, musicalInstruments, djEquipment);
            await context.SaveChangesAsync();

            var speakers = new Category { Name = "Speaker Systems", Slug = "speaker-systems", ParentId = soundEquipment.Id, Description = "Active and passive speakers", MetaTitle = "Speaker Systems", MetaDescription = "Professional speakers", IsActive = true };
            var mixers = new Category { Name = "Mixing Consoles", Slug = "mixing-consoles", ParentId = soundEquipment.Id, Description = "Audio mixing consoles", MetaTitle = "Mixing Consoles", MetaDescription = "Professional mixers", IsActive = true };
            var amplifiers = new Category { Name = "Power Amplifiers", Slug = "power-amplifiers", ParentId = soundEquipment.Id, Description = "Audio power amplifiers", MetaTitle = "Power Amplifiers", MetaDescription = "Professional amplifiers", IsActive = true };
            var microphones = new Category { Name = "Microphones", Slug = "microphones", ParentId = soundEquipment.Id, Description = "Studio and stage microphones", MetaTitle = "Microphones", MetaDescription = "Professional microphones", IsActive = true };
            var headphones = new Category { Name = "Headphones", Slug = "headphones", ParentId = soundEquipment.Id, Description = "Studio and DJ headphones", MetaTitle = "Headphones", MetaDescription = "Professional headphones", IsActive = true };
            var audioProcessing = new Category { Name = "Audio Processing", Slug = "audio-processing", ParentId = soundEquipment.Id, Description = "Effects and processors", MetaTitle = "Audio Processing", MetaDescription = "Audio effects", IsActive = true };
            var studioMonitors = new Category { Name = "Studio Monitors", Slug = "studio-monitors", ParentId = soundEquipment.Id, Description = "Nearfield studio monitors", MetaTitle = "Studio Monitors", MetaDescription = "Professional monitors", IsActive = true };

            var guitars = new Category { Name = "Guitars", Slug = "guitars", ParentId = musicalInstruments.Id, Description = "Electric and acoustic guitars", MetaTitle = "Guitars", MetaDescription = "Electric guitars", IsActive = true };
            var keyboards = new Category { Name = "Keyboards", Slug = "keyboards", ParentId = musicalInstruments.Id, Description = "Synthesizers, pianos, organs", MetaTitle = "Keyboards", MetaDescription = "Keyboards and synths", IsActive = true };
            var drums = new Category { Name = "Drums", Slug = "drums", ParentId = musicalInstruments.Id, Description = "Drum kits and percussion", MetaTitle = "Drums", MetaDescription = "Drum kits", IsActive = true };
            var winds = new Category { Name = "Wind Instruments", Slug = "wind-instruments", ParentId = musicalInstruments.Id, Description = "Saxophones, trumpets, flutes", MetaTitle = "Wind Instruments", MetaDescription = "Wind instruments", IsActive = true };
            var strings = new Category { Name = "String Instruments", Slug = "string-instruments", ParentId = musicalInstruments.Id, Description = "Violins, cellos, basses", MetaTitle = "String Instruments", MetaDescription = "String instruments", IsActive = true };

            var djControllers = new Category { Name = "DJ Controllers", Slug = "dj-controllers", ParentId = djEquipment.Id, Description = "DJ controllers for software", MetaTitle = "DJ Controllers", MetaDescription = "DJ controllers", IsActive = true };
            var djMixers = new Category { Name = "DJ Mixers", Slug = "dj-mixers", ParentId = djEquipment.Id, Description = "Standalone DJ mixers", MetaTitle = "DJ Mixers", MetaDescription = "DJ mixers", IsActive = true };
            var turntables = new Category { Name = "Turntables", Slug = "turntables", ParentId = djEquipment.Id, Description = "Vinyl turntables", MetaTitle = "Turntables", MetaDescription = "Vinyl players", IsActive = true };
            var djKits = new Category { Name = "DJ Kits", Slug = "dj-kits", ParentId = djEquipment.Id, Description = "Complete DJ setups", MetaTitle = "DJ Kits", MetaDescription = "DJ complete kits", IsActive = true };
            var recordingKits = new Category { Name = "Recording Kits", Slug = "recording-kits", ParentId = djEquipment.Id, Description = "Home recording setups", MetaTitle = "Recording Kits", MetaDescription = "Recording kits", IsActive = true };

            context.Categories.AddRange(
                speakers, mixers, amplifiers, microphones, headphones, audioProcessing, studioMonitors,
                guitars, keyboards, drums, winds, strings,
                djControllers, djMixers, turntables, djKits, recordingKits
            );
            await context.SaveChangesAsync();

            var electricGuitars = new Category { Name = "Electric Guitars", Slug = "electric-guitars", ParentId = guitars.Id, Description = "Electric guitars", IsActive = true };
            var bassGuitars = new Category { Name = "Bass Guitars", Slug = "bass-guitars", ParentId = guitars.Id, Description = "Bass guitars", IsActive = true };
            var acousticGuitars = new Category { Name = "Acoustic Guitars", Slug = "acoustic-guitars", ParentId = guitars.Id, Description = "Acoustic guitars", IsActive = true };

            var synthesizers = new Category { Name = "Synthesizers", Slug = "synthesizers", ParentId = keyboards.Id, Description = "Analog and digital synthesizers", IsActive = true };
            var digitalPianos = new Category { Name = "Digital Pianos", Slug = "digital-pianos", ParentId = keyboards.Id, Description = "Digital pianos", IsActive = true };

            var analogSynths = new Category { Name = "Analog Synthesizers", Slug = "analog-synths", ParentId = synthesizers.Id, Description = "Pure analog sound", IsActive = true };
            var digitalSynths = new Category { Name = "Digital Synthesizers", Slug = "digital-synths", ParentId = synthesizers.Id, Description = "Digital synths", IsActive = true };

            var polyphonic = new Category { Name = "Polyphonic", Slug = "polyphonic", ParentId = analogSynths.Id, Description = "Multi-voice analog", IsActive = true };
            var monophonic = new Category { Name = "Monophonic", Slug = "monophonic", ParentId = analogSynths.Id, Description = "Single-voice analog", IsActive = true };

            var acousticDrums = new Category { Name = "Acoustic Drums", Slug = "acoustic-drums", ParentId = drums.Id, Description = "Acoustic drum kits", IsActive = true };
            var electronicDrums = new Category { Name = "Electronic Drums", Slug = "electronic-drums", ParentId = drums.Id, Description = "Electronic drum kits", IsActive = true };
            var cymbals = new Category { Name = "Cymbals", Slug = "cymbals", ParentId = drums.Id, Description = "Drum cymbals", IsActive = true };

            var violins = new Category { Name = "Violins", Slug = "violins", ParentId = strings.Id, Description = "Acoustic and electric violins", IsActive = true };
            var electricViolins = new Category { Name = "Electric Violins", Slug = "electric-violins", ParentId = violins.Id, Description = "Electric violins", IsActive = true };

            context.Categories.AddRange(
                electricGuitars, bassGuitars, acousticGuitars,
                synthesizers, digitalPianos,
                analogSynths, digitalSynths,
                polyphonic, monophonic,
                acousticDrums, electronicDrums, cymbals,
                violins, electricViolins
            );
            await context.SaveChangesAsync();

            var allCategories = await context.Categories.ToListAsync();

            foreach (var c in allCategories)
            {
                context.CategoryTranslations.AddRange(
                    new CategoryTranslation { CategoryId = c.Id, LanguageCode = "uk", Name = GetUkrainianCategoryName(c.Name), Description = GetUkrainianCategoryDescription(c.Name) },
                    new CategoryTranslation { CategoryId = c.Id, LanguageCode = "en", Name = c.Name, Description = c.Description ?? c.Name }
                );
            }
            await context.SaveChangesAsync();

            context.CategoryClosures.RemoveRange(context.CategoryClosures);
            await context.SaveChangesAsync();

            foreach (var c in allCategories)
            {
                context.CategoryClosures.Add(new CategoryClosure { AncestorId = c.Id, DescendantId = c.Id, Depth = 0 });
                var curr = c;
                var depth = 1;
                while (curr.ParentId.HasValue)
                {
                    var p = allCategories.First(x => x.Id == curr.ParentId.Value);
                    context.CategoryClosures.Add(new CategoryClosure { AncestorId = p.Id, DescendantId = c.Id, Depth = depth });
                    curr = p;
                    depth++;
                }
            }
            await context.SaveChangesAsync();
        }

        // АТРИБУТИ
        if (!context.AttributeGroups.Any())
        {
            var technicalGroup = new AttributeGroup
            {
                Name = "Technical Specifications",
                Slug = "technical-specs",
                Description = "Core technical parameters and measurable characteristics of the product"
            };

            var featuresGroup = new AttributeGroup
            {
                Name = "Key Features",
                Slug = "key-features",
                Description = "Important functional capabilities, construction details and notable characteristics"
            };

            context.AttributeGroups.AddRange(technicalGroup, featuresGroup);
            await context.SaveChangesAsync();

            context.AttributeGroupTranslations.AddRange(
                new AttributeGroupTranslation { AttributeGroupId = technicalGroup.Id, LanguageCode = "uk", Name = "Технічні характеристики", Description = "Основні технічні параметри та вимірювані характеристики товару" },
                new AttributeGroupTranslation { AttributeGroupId = technicalGroup.Id, LanguageCode = "en", Name = "Technical Specifications", Description = "Core technical parameters and measurable characteristics of the product" },

                new AttributeGroupTranslation { AttributeGroupId = featuresGroup.Id, LanguageCode = "uk", Name = "Основні особливості", Description = "Важливі функціональні можливості, деталі конструкції та характерні риси" },
                new AttributeGroupTranslation { AttributeGroupId = featuresGroup.Id, LanguageCode = "en", Name = "Key Features", Description = "Important functional capabilities, construction details and notable characteristics" }
            );
            await context.SaveChangesAsync();

            var attributes = new[]
            {
                new ProductAttribute { Name = "Polyphony", Slug = "polyphony", AttributeGroupId = technicalGroup.Id, Type = AttributeType.String, IsFilterable = true, IsComparable = true, Description = "Maximum number of simultaneously sounding voices / notes" },
                new ProductAttribute { Name = "Impedance", Slug = "impedance", AttributeGroupId = technicalGroup.Id, Type = AttributeType.String, Unit = "Ω", IsFilterable = true, Description = "Electrical impedance of headphones or speakers" },
                new ProductAttribute { Name = "Woofer Size", Slug = "woofer-size", AttributeGroupId = technicalGroup.Id, Type = AttributeType.String, Unit = "\"", IsFilterable = true, Description = "Diameter of the low-frequency driver" },
                new ProductAttribute { Name = "Power Output", Slug = "power-output", AttributeGroupId = technicalGroup.Id, Type = AttributeType.String, Unit = "W", IsFilterable = true, Description = "Rated / peak power output (per channel or total)" },
                new ProductAttribute { Name = "Number of Channels", Slug = "channels", AttributeGroupId = technicalGroup.Id, Type = AttributeType.String, IsFilterable = true, Description = "Number of input / output / mixing channels" },
                new ProductAttribute { Name = "Number of Strings", Slug = "strings-count", AttributeGroupId = technicalGroup.Id, Type = AttributeType.String, IsFilterable = true, Description = "Total number of strings on the instrument" },

                new ProductAttribute { Name = "Keyboard Type", Slug = "keyboard-type", AttributeGroupId = featuresGroup.Id, Type = AttributeType.String, IsFilterable = true, Description = "Type of keyboard mechanism (synth-action, semi-weighted, hammer-action etc.)" },
                new ProductAttribute { Name = "Body Material", Slug = "body-material", AttributeGroupId = featuresGroup.Id, Type = AttributeType.String, IsFilterable = true, Description = "Primary material of the instrument body" },
                new ProductAttribute { Name = "USB Connectivity", Slug = "usb-connectivity", AttributeGroupId = featuresGroup.Id, Type = AttributeType.Boolean, IsFilterable = true, Description = "Presence of USB interface for connection to computer or MIDI" },
                new ProductAttribute { Name = "Bluetooth", Slug = "bluetooth", AttributeGroupId = featuresGroup.Id, Type = AttributeType.Boolean, IsFilterable = true, Description = "Support for wireless Bluetooth audio / MIDI connection" },
                new ProductAttribute { Name = "Weight", Slug = "weight", AttributeGroupId = featuresGroup.Id, Type = AttributeType.String, Unit = "kg", IsFilterable = true, Description = "Net weight of the product (without packaging)" }
            };

            context.ProductAttributes.AddRange(attributes);
            await context.SaveChangesAsync();

            var attrsDict = attributes.ToDictionary(a => a.Slug, a => a.Id);

            foreach (var attr in attributes)
            {
                var ukName = GetUkrainianAttributeName(attr.Name);
                var ukDesc = GetUkrainianAttributeDescription(attr.Name);

                context.ProductAttributeTranslations.AddRange(
                    new ProductAttributeTranslation { ProductAttributeId = attr.Id, LanguageCode = "uk", Name = ukName, Description = ukDesc },
                    new ProductAttributeTranslation { ProductAttributeId = attr.Id, LanguageCode = "en", Name = attr.Name, Description = attr.Description }
                );
            }
            await context.SaveChangesAsync();
        }

        // ПРОДУКТИ — ПОВНИЙ СПИСОК З ПОЧАТКОВОГО ФАЙЛУ
        if (!context.Products.Any())
        {
            var brandsDict = await context.Brands.ToDictionaryAsync(b => b.Slug, b => b.Id);
            var catsDict = await context.Categories.ToDictionaryAsync(c => c.Slug, c => c.Id);
            var attrsDict = await context.ProductAttributes.ToDictionaryAsync(a => a.Slug, a => a.Id);

            var products = new List<Product>
            {
                // ====== СИНТЕЗАТОРИ ======
                new() { Name = "Korg Minilogue XD", Slug = "korg-minilogue-xd", Description = "4-voice analog polyphonic synthesizer with multi-engine, effects, and built-in sequencer", Price = 33990.00m, StockQuantity = 8, IsActive = true, BrandId = brandsDict["korg"], CategoryId = catsDict["polyphonic"] },
                new() { Name = "Arturia PolyBrute", Slug = "arturia-polybrute", Description = "6-voice fully analog polyphonic synthesizer with morphing, modulation matrix, and built-in effects", Price = 109839.00m, StockQuantity = 4, IsActive = true, BrandId = brandsDict["arturia"], CategoryId = catsDict["polyphonic"] },
                new() { Name = "Korg Prologue 16", Slug = "korg-prologue-16", Description = "16-voice analog polyphonic synthesizer with digital multi-engine and extensive modulation", Price = 84990.00m, StockQuantity = 6, IsActive = true, BrandId = brandsDict["korg"], CategoryId = catsDict["polyphonic"] },
                new() { Name = "Arturia MicroFreak", Slug = "arturia-microfreak", Description = "Hybrid polyphonic synthesizer with digital oscillators and analog filter", Price = 15990.00m, StockQuantity = 12, IsActive = true, BrandId = brandsDict["arturia"], CategoryId = catsDict["polyphonic"] },
                new() { Name = "Behringer Deepmind 12", Slug = "behringer-deepmind-12", Description = "12-voice analog polyphonic synthesizer with TC Electronic effects", Price = 28929.00m, StockQuantity = 7, IsActive = true, BrandId = brandsDict["behringer"], CategoryId = catsDict["polyphonic"] },

                new() { Name = "Moog Subsequent 37", Slug = "moog-subsequent-37", Description = "Paraphonic analog synthesizer with classic Moog ladder filter and multidrive", Price = 101200.00m, StockQuantity = 5, IsActive = true, BrandId = brandsDict["moog"], CategoryId = catsDict["monophonic"] },
                new() { Name = "Moog Grandmother", Slug = "moog-grandmother", Description = "Semi-modular analog synthesizer with spring reverb and arpeggiator", Price = 52990.00m, StockQuantity = 6, IsActive = true, BrandId = brandsDict["moog"], CategoryId = catsDict["monophonic"] },
                new() { Name = "Behringer Model D", Slug = "behringer-model-d", Description = "Analog synthesizer clone of classic Moog sound", Price = 12990.00m, StockQuantity = 15, IsActive = true, BrandId = brandsDict["behringer"], CategoryId = catsDict["monophonic"] },
                new() { Name = "Arturia MiniBrute 2", Slug = "arturia-minibrute-2", Description = "Semi-modular monophonic analog synthesizer with sequencer", Price = 24990.00m, StockQuantity = 8, IsActive = true, BrandId = brandsDict["arturia"], CategoryId = catsDict["monophonic"] },
                new() { Name = "Korg MS-20 Mini", Slug = "korg-ms-20-mini", Description = "Compact monophonic analog synthesizer with patch bay", Price = 29990.00m, StockQuantity = 7, IsActive = true, BrandId = brandsDict["korg"], CategoryId = catsDict["monophonic"] },

                new() { Name = "Roland JD-Xi", Slug = "roland-jd-xi", Description = "Hybrid synthesizer with digital engines, vocoder, microphone input, and full sequencer", Price = 30429.00m, StockQuantity = 10, IsActive = true, BrandId = brandsDict["roland"], CategoryId = catsDict["digital-synths"] },
                new() { Name = "Korg Opsix", Slug = "korg-opsix", Description = "FM synthesizer with 6 operators and reimagined FM synthesis approach", Price = 23000.00m, StockQuantity = 6, IsActive = true, BrandId = brandsDict["korg"], CategoryId = catsDict["digital-synths"] },
                new() { Name = "Roland Fantom-0 6", Slug = "roland-fantom-0-6", Description = "61-key synthesizer workstation with ZEN-Core sound engine", Price = 67990.00m, StockQuantity = 5, IsActive = true, BrandId = brandsDict["roland"], CategoryId = catsDict["digital-synths"] },
                new() { Name = "Yamaha MODX7", Slug = "yamaha-modx7", Description = "76-key synthesizer with FM-X and AWM2 engines", Price = 54990.00m, StockQuantity = 6, IsActive = true, BrandId = brandsDict["yamaha"], CategoryId = catsDict["digital-synths"] },
                new() { Name = "Nord Stage 4 Compact", Slug = "nord-stage-4-compact", Description = "73-key stage keyboard with piano, organ and synth sections", Price = 159990.00m, StockQuantity = 3, IsActive = true, BrandId = brandsDict["nord"], CategoryId = catsDict["digital-synths"] },

                new() { Name = "Yamaha P-125", Slug = "yamaha-p-125", Description = "88-key weighted digital piano with pure CF sound engine", Price = 32990.00m, StockQuantity = 10, IsActive = true, BrandId = brandsDict["yamaha"], CategoryId = catsDict["digital-pianos"] },
                new() { Name = "Casio Privia PX-S3100", Slug = "casio-privia-px-s3100", Description = "Slim 88-key digital piano with smart scaled hammer action", Price = 27990.00m, StockQuantity = 12, IsActive = true, BrandId = brandsDict["casio"], CategoryId = catsDict["digital-pianos"] },
                new() { Name = "Roland FP-30X", Slug = "roland-fp-30x", Description = "Portable 88-key digital piano with SuperNATURAL sound", Price = 31490.00m, StockQuantity = 8, IsActive = true, BrandId = brandsDict["roland"], CategoryId = catsDict["digital-pianos"] },
                new() { Name = "Yamaha Clavinova CLP-745", Slug = "yamaha-clavinova-clp-745", Description = "Digital piano with GrandTouch-S keyboard and CFX/Bösendorfer samples", Price = 89990.00m, StockQuantity = 4, IsActive = true, BrandId = brandsDict["yamaha"], CategoryId = catsDict["digital-pianos"] },
                new() { Name = "Casio CDP-S110", Slug = "casio-cdp-s110", Description = "Compact 88-key digital piano with scaled hammer action", Price = 19990.00m, StockQuantity = 15, IsActive = true, BrandId = brandsDict["casio"], CategoryId = catsDict["digital-pianos"] },

                new() { Name = "Fender Player Stratocaster HSS", Slug = "fender-player-stratocaster-hss", Description = "Classic Stratocaster with HSS pickup configuration, maple neck, and vintage-style tremolo system", Price = 23249.00m, StockQuantity = 15, IsActive = true, BrandId = brandsDict["fender"], CategoryId = catsDict["electric-guitars"] },
                new() { Name = "Ibanez RG550 Genesis", Slug = "ibanez-rg550-genesis", Description = "High-performance superstrat with wizard neck, Edge tremolo, and powerful pickups", Price = 16720.00m, StockQuantity = 10, IsActive = true, BrandId = brandsDict["ibanez"], CategoryId = catsDict["electric-guitars"] },
                new() { Name = "Gibson Les Paul Standard '50s", Slug = "gibson-les-paul-standard-50s", Description = "Classic Les Paul with vintage '50s neck profile and Burstbucker pickups", Price = 115990.00m, StockQuantity = 4, IsActive = true, BrandId = brandsDict["gibson"], CategoryId = catsDict["electric-guitars"] },
                new() { Name = "PRS SE Custom 24", Slug = "prs-se-custom-24", Description = "Versatile guitar with 24 frets, wide thin neck, and 85/15 S pickups", Price = 34990.00m, StockQuantity = 8, IsActive = true, BrandId = brandsDict["prs"], CategoryId = catsDict["electric-guitars"] },
                new() { Name = "ESP LTD EC-1000", Slug = "esp-ltd-ec-1000", Description = "Single-cut guitar with EMG active pickups and mahogany body", Price = 42990.00m, StockQuantity = 7, IsActive = true, BrandId = brandsDict["esp"], CategoryId = catsDict["electric-guitars"] },
                new() { Name = "Fender Telecaster American Professional II", Slug = "fender-telecaster-american-pro-ii", Description = "Professional Telecaster with V-Mod II pickups and deep C neck", Price = 67990.00m, StockQuantity = 6, IsActive = true, BrandId = brandsDict["fender"], CategoryId = catsDict["electric-guitars"] },

                new() { Name = "Fender Player Precision Bass", Slug = "fender-player-precision-bass", Description = "Classic P-Bass with split single-coil pickup and maple neck", Price = 29990.00m, StockQuantity = 10, IsActive = true, BrandId = brandsDict["fender"], CategoryId = catsDict["bass-guitars"] },
                new() { Name = "Ibanez SR300E", Slug = "ibanez-sr300e", Description = "Active bass guitar with Accu-cast B120 bridge and Dynamix P/J pickups", Price = 18990.00m, StockQuantity = 12, IsActive = true, BrandId = brandsDict["ibanez"], CategoryId = catsDict["bass-guitars"] },
                new() { Name = "Fender Jazz Bass American Pro II", Slug = "fender-jazz-bass-american-pro-ii", Description = "Professional Jazz Bass with V-Mod II pickups", Price = 72990.00m, StockQuantity = 5, IsActive = true, BrandId = brandsDict["fender"], CategoryId = catsDict["bass-guitars"] },
                new() { Name = "Ibanez BTB845SC", Slug = "ibanez-btb845sc", Description = "5-string bass with Nordstrand Big Single pickups", Price = 54990.00m, StockQuantity = 4, IsActive = true, BrandId = brandsDict["ibanez"], CategoryId = catsDict["bass-guitars"] },
                new() { Name = "ESP LTD B-206SM", Slug = "esp-ltd-b-206sm", Description = "6-string bass guitar with active ESP pickups", Price = 32990.00m, StockQuantity = 6, IsActive = true, BrandId = brandsDict["esp"], CategoryId = catsDict["bass-guitars"] },

                new() { Name = "Pearl Export EXX725", Slug = "pearl-export-exx725", Description = "5-piece drum kit with poplar/mahogany shells", Price = 34990.00m, StockQuantity = 5, IsActive = true, BrandId = brandsDict["pearl"], CategoryId = catsDict["acoustic-drums"] },
                new() { Name = "Tama Imperialstar IE52KH6", Slug = "tama-imperialstar-ie52kh6", Description = "5-piece drum kit with hardware and cymbals", Price = 29990.00m, StockQuantity = 6, IsActive = true, BrandId = brandsDict["tama"], CategoryId = catsDict["acoustic-drums"] },
                new() { Name = "Pearl Decade Maple DMP925", Slug = "pearl-decade-maple-dmp925", Description = "5-piece maple drum kit with professional sound", Price = 52990.00m, StockQuantity = 4, IsActive = true, BrandId = brandsDict["pearl"], CategoryId = catsDict["acoustic-drums"] },
                new() { Name = "Tama Starclassic Maple MR52TZUS", Slug = "tama-starclassic-maple-mr52tzus", Description = "Professional 5-piece maple drum kit", Price = 119990.00m, StockQuantity = 2, IsActive = true, BrandId = brandsDict["tama"], CategoryId = catsDict["acoustic-drums"] },
                new() { Name = "Pearl Vision VML925", Slug = "pearl-vision-vml925", Description = "5-piece birch drum kit with SST tom suspension", Price = 42990.00m, StockQuantity = 5, IsActive = true, BrandId = brandsDict["pearl"], CategoryId = catsDict["acoustic-drums"] },

                new() { Name = "Roland TD-17KVX", Slug = "roland-td-17kvx", Description = "Electronic drum kit with mesh heads and TD-17 module", Price = 52990.00m, StockQuantity = 6, IsActive = true, BrandId = brandsDict["roland"], CategoryId = catsDict["electronic-drums"] },
                new() { Name = "Yamaha DTX6K-X", Slug = "yamaha-dtx6k-x", Description = "Electronic drum kit with real wood shells and DTX-PRO module", Price = 89990.00m, StockQuantity = 4, IsActive = true, BrandId = brandsDict["yamaha"], CategoryId = catsDict["electronic-drums"] },
                new() { Name = "Roland TD-27KV", Slug = "roland-td-27kv", Description = "Professional electronic drum kit with TD-27 sound module", Price = 119990.00m, StockQuantity = 3, IsActive = true, BrandId = brandsDict["roland"], CategoryId = catsDict["electronic-drums"] },
                new() { Name = "Yamaha DTX452K", Slug = "yamaha-dtx452k", Description = "Compact electronic drum kit for beginners", Price = 24990.00m, StockQuantity = 8, IsActive = true, BrandId = brandsDict["yamaha"], CategoryId = catsDict["electronic-drums"] },
                new() { Name = "Roland TD-1DMK", Slug = "roland-td-1dmk", Description = "Entry-level electronic drum kit with mesh snare", Price = 19990.00m, StockQuantity = 10, IsActive = true, BrandId = brandsDict["roland"], CategoryId = catsDict["electronic-drums"] },

                new() { Name = "Zildjian A Custom Cymbal Set", Slug = "zildjian-a-custom-set", Description = "Professional cymbal set with brilliant finish", Price = 42990.00m, StockQuantity = 5, IsActive = true, BrandId = brandsDict["zildjian"], CategoryId = catsDict["cymbals"] },
                new() { Name = "Zildjian K Custom Dark Crash 18", Slug = "zildjian-k-custom-dark-crash-18", Description = "18-inch dark crash cymbal with dry sound", Price = 14990.00m, StockQuantity = 8, IsActive = true, BrandId = brandsDict["zildjian"], CategoryId = catsDict["cymbals"] },
                new() { Name = "Zildjian A Series Medium Ride 20", Slug = "zildjian-a-series-medium-ride-20", Description = "20-inch medium ride cymbal with balanced sound", Price = 12990.00m, StockQuantity = 10, IsActive = true, BrandId = brandsDict["zildjian"], CategoryId = catsDict["cymbals"] },
                new() { Name = "Zildjian ZBT Cymbal Set", Slug = "zildjian-zbt-set", Description = "Entry-level cymbal set with bright sound", Price = 8990.00m, StockQuantity = 12, IsActive = true, BrandId = brandsDict["zildjian"], CategoryId = catsDict["cymbals"] },
                new() { Name = "Zildjian K Constantinople Ride 22", Slug = "zildjian-k-constantinople-ride-22", Description = "22-inch hand-hammered ride cymbal", Price = 28990.00m, StockQuantity = 3, IsActive = true, BrandId = brandsDict["zildjian"], CategoryId = catsDict["cymbals"] },

                new() { Name = "Yamaha YEV-104 BL", Slug = "yamaha-yev-104-bl", Description = "4-string electric violin with solid spruce body and piezo pickup", Price = 28086.00m, StockQuantity = 8, IsActive = true, BrandId = brandsDict["yamaha-electric"], CategoryId = catsDict["electric-violins"] },
                new() { Name = "NS Design WAV-5 Electric Violin", Slug = "ns-design-wav-5", Description = "5-string electric violin with active polar pickup and carbon fiber", Price = 58440.00m, StockQuantity = 5, IsActive = true, BrandId = brandsDict["ns-design"], CategoryId = catsDict["electric-violins"] },
                new() { Name = "Yamaha SV250", Slug = "yamaha-sv250", Description = "Silent electric violin with maple body", Price = 39990.00m, StockQuantity = 7, IsActive = true, BrandId = brandsDict["yamaha-electric"], CategoryId = catsDict["electric-violins"] },
                new() { Name = "NS Design NXT5a", Slug = "ns-design-nxt5a", Description = "5-string active electric violin with low impedance", Price = 46990.00m, StockQuantity = 6, IsActive = true, BrandId = brandsDict["ns-design"], CategoryId = catsDict["electric-violins"] },
                new() { Name = "Yamaha YSV104", Slug = "yamaha-ysv104", Description = "Silent violin with reverb and auxiliary input", Price = 32990.00m, StockQuantity = 8, IsActive = true, BrandId = brandsDict["yamaha-electric"], CategoryId = catsDict["electric-violins"] },

                new() { Name = "Sennheiser HD 25", Slug = "sennheiser-hd-25", Description = "Closed-back DJ headphones with high sound isolation", Price = 5999.00m, StockQuantity = 30, IsActive = true, BrandId = brandsDict["sennheiser"], CategoryId = catsDict["headphones"] },
                new() { Name = "Sennheiser HD 280 Pro", Slug = "sennheiser-hd-280-pro", Description = "Professional studio monitoring headphones with flat frequency", Price = 4049.00m, StockQuantity = 25, IsActive = true, BrandId = brandsDict["sennheiser"], CategoryId = catsDict["headphones"] },
                new() { Name = "Audio-Technica ATH-M50x", Slug = "audio-technica-ath-m50x", Description = "Professional monitor headphones with exceptional clarity", Price = 7490.00m, StockQuantity = 20, IsActive = true, BrandId = brandsDict["audio-technica"], CategoryId = catsDict["headphones"] },
                new() { Name = "Beyerdynamic DT 770 Pro 80 Ohm", Slug = "beyerdynamic-dt-770-pro", Description = "Closed studio headphones with bass reflex technology", Price = 8990.00m, StockQuantity = 15, IsActive = true, BrandId = brandsDict["sennheiser"], CategoryId = catsDict["headphones"] },
                new() { Name = "Sony MDR-7506", Slug = "sony-mdr-7506", Description = "Professional studio monitor headphones", Price = 5490.00m, StockQuantity = 25, IsActive = true, BrandId = brandsDict["sennheiser"], CategoryId = catsDict["headphones"] },
                new() { Name = "Audio-Technica ATH-M40x", Slug = "audio-technica-ath-m40x", Description = "Professional studio monitor headphones", Price = 4990.00m, StockQuantity = 18, IsActive = true, BrandId = brandsDict["audio-technica"], CategoryId = catsDict["headphones"] },
                new() { Name = "Shure SRH840A", Slug = "shure-srh840a", Description = "Professional closed-back studio headphones with precise, detailed sound", Price = 6490.00m, StockQuantity = 14, IsActive = true, BrandId = brandsDict["shure"], CategoryId = catsDict["headphones"] },
                new() { Name = "Sennheiser HD 560S", Slug = "sennheiser-hd-560s", Description = "Open-back reference headphones for analytical listening and mixing", Price = 8290.00m, StockQuantity = 12, IsActive = true, BrandId = brandsDict["sennheiser"], CategoryId = catsDict["headphones"] },
                new() { Name = "JBL Tune 760NC", Slug = "jbl-tune-760nc", Description = "Wireless over-ear headphones with active noise cancellation", Price = 3990.00m, StockQuantity = 22, IsActive = true, BrandId = brandsDict["jbl"], CategoryId = catsDict["headphones"] },
                new() { Name = "Audio-Technica ATH-R70x", Slug = "audio-technica-ath-r70x", Description = "Open-back professional reference headphones for mixing and mastering", Price = 12490.00m, StockQuantity = 8, IsActive = true, BrandId = brandsDict["audio-technica"], CategoryId = catsDict["headphones"] },

                new() { Name = "Kali Audio LP-6 v2", Slug = "kali-lp-6-v2", Description = "6.5-inch active nearfield studio monitors with boundary EQ", Price = 9919.00m, StockQuantity = 20, IsActive = true, BrandId = brandsDict["kali-audio"], CategoryId = catsDict["studio-monitors"] },
                new() { Name = "Yamaha HS8", Slug = "yamaha-hs8", Description = "8-inch studio monitors with room control", Price = 13429.00m, StockQuantity = 12, IsActive = true, BrandId = brandsDict["yamaha"], CategoryId = catsDict["studio-monitors"] },
                new() { Name = "JBL 305P MkII", Slug = "jbl-305p-mkii", Description = "5-inch powered studio monitor with image control waveguide", Price = 6990.00m, StockQuantity = 18, IsActive = true, BrandId = brandsDict["jbl"], CategoryId = catsDict["studio-monitors"] },
                new() { Name = "Yamaha HS5", Slug = "yamaha-hs5", Description = "5-inch powered studio monitor", Price = 8990.00m, StockQuantity = 20, IsActive = true, BrandId = brandsDict["yamaha"], CategoryId = catsDict["studio-monitors"] },
                new() { Name = "Kali Audio IN-5", Slug = "kali-audio-in-5", Description = "5-inch 3-way studio monitor", Price = 12490.00m, StockQuantity = 10, IsActive = true, BrandId = brandsDict["kali-audio"], CategoryId = catsDict["studio-monitors"] },
                new() { Name = "JBL LSR308P MkII", Slug = "jbl-lsr308p-mkii", Description = "8-inch powered studio monitor", Price = 11990.00m, StockQuantity = 12, IsActive = true, BrandId = brandsDict["jbl"], CategoryId = catsDict["studio-monitors"] },

                new() { Name = "Shure SM7B", Slug = "shure-sm7b", Description = "Dynamic microphone for vocals, podcasts, and broadcasting", Price = 18809.00m, StockQuantity = 12, IsActive = true, BrandId = brandsDict["shure"], CategoryId = catsDict["microphones"] },
                new() { Name = "Rode NT1 5th Gen", Slug = "rode-nt1-5th-gen", Description = "Large-diaphragm condenser microphone with USB/XLR", Price = 10349.00m, StockQuantity = 18, IsActive = true, BrandId = brandsDict["rode"], CategoryId = catsDict["microphones"] },
                new() { Name = "Shure SM58", Slug = "shure-sm58", Description = "Legendary vocal dynamic microphone", Price = 5490.00m, StockQuantity = 25, IsActive = true, BrandId = brandsDict["shure"], CategoryId = catsDict["microphones"] },
                new() { Name = "Rode NT-USB", Slug = "rode-nt-usb", Description = "USB condenser microphone for podcasting", Price = 7990.00m, StockQuantity = 15, IsActive = true, BrandId = brandsDict["rode"], CategoryId = catsDict["microphones"] },
                new() { Name = "Shure Beta 87A", Slug = "shure-beta-87a", Description = "Condenser vocal microphone with supercardioid pattern", Price = 12990.00m, StockQuantity = 10, IsActive = true, BrandId = brandsDict["shure"], CategoryId = catsDict["microphones"] },
                new() { Name = "Rode Procaster", Slug = "rode-procaster", Description = "Broadcast quality dynamic microphone", Price = 9990.00m, StockQuantity = 12, IsActive = true, BrandId = brandsDict["rode"], CategoryId = catsDict["microphones"] },

                new() { Name = "JBL EON710", Slug = "jbl-eon710", Description = "10-inch powered PA speaker with Bluetooth", Price = 17990.00m, StockQuantity = 10, IsActive = true, BrandId = brandsDict["jbl"], CategoryId = catsDict["speaker-systems"] },
                new() { Name = "JBL EON712", Slug = "jbl-eon712", Description = "12-inch powered PA speaker with 1300W peak power", Price = 21990.00m, StockQuantity = 8, IsActive = true, BrandId = brandsDict["jbl"], CategoryId = catsDict["speaker-systems"] },
                new() { Name = "Yamaha DXR12 mkII", Slug = "yamaha-dxr12-mkii", Description = "12-inch powered speaker with 1100W power", Price = 24990.00m, StockQuantity = 7, IsActive = true, BrandId = brandsDict["yamaha"], CategoryId = catsDict["speaker-systems"] },
                new() { Name = "JBL PRX815W", Slug = "jbl-prx815w", Description = "15-inch powered speaker with Wi-Fi control", Price = 34990.00m, StockQuantity = 5, IsActive = true, BrandId = brandsDict["jbl"], CategoryId = catsDict["speaker-systems"] },
                new() { Name = "Yamaha DBR15", Slug = "yamaha-dbr15", Description = "15-inch powered speaker for live sound", Price = 19990.00m, StockQuantity = 10, IsActive = true, BrandId = brandsDict["yamaha"], CategoryId = catsDict["speaker-systems"] },

                new() { Name = "Soundcraft Signature 12MTK", Slug = "soundcraft-signature-12mtk", Description = "12-channel mixer with USB audio interface", Price = 19990.00m, StockQuantity = 6, IsActive = true, BrandId = brandsDict["soundcraft"], CategoryId = catsDict["mixing-consoles"] },
                new() { Name = "Allen & Heath ZEDi-10FX", Slug = "allen-heath-zedi-10fx", Description = "10-channel mixer with USB and FX", Price = 17990.00m, StockQuantity = 7, IsActive = true, BrandId = brandsDict["allen-heath"], CategoryId = catsDict["mixing-consoles"] },
                new() { Name = "Yamaha MG12XU", Slug = "yamaha-mg12xu", Description = "12-channel mixer with built-in FX and USB", Price = 16990.00m, StockQuantity = 8, IsActive = true, BrandId = brandsDict["yamaha"], CategoryId = catsDict["mixing-consoles"] },
                new() { Name = "Soundcraft Signature 22MTK", Slug = "soundcraft-signature-22mtk", Description = "22-channel mixer with multitrack USB", Price = 32990.00m, StockQuantity = 4, IsActive = true, BrandId = brandsDict["soundcraft"], CategoryId = catsDict["mixing-consoles"] },
                new() { Name = "Allen & Heath SQ-5", Slug = "allen-heath-sq-5", Description = "Digital mixer with 48 channels", Price = 119990.00m, StockQuantity = 2, IsActive = true, BrandId = brandsDict["allen-heath"], CategoryId = catsDict["mixing-consoles"] },

                new() { Name = "QSC GX5", Slug = "qsc-gx5", Description = "Power amplifier with 700W per channel at 8 ohms", Price = 14990.00m, StockQuantity = 8, IsActive = true, BrandId = brandsDict["qsc"], CategoryId = catsDict["power-amplifiers"] },
                new() { Name = "QSC GX7", Slug = "qsc-gx7", Description = "Power amplifier with 1000W per channel at 8 ohms", Price = 19990.00m, StockQuantity = 6, IsActive = true, BrandId = brandsDict["qsc"], CategoryId = catsDict["power-amplifiers"] },
                new() { Name = "QSC RMX1450", Slug = "qsc-rmx1450", Description = "Power amplifier with 450W per channel at 8 ohms", Price = 12990.00m, StockQuantity = 10, IsActive = true, BrandId = brandsDict["qsc"], CategoryId = catsDict["power-amplifiers"] },
                new() { Name = "Yamaha P7000S", Slug = "yamaha-p7000s", Description = "Professional power amplifier 2x700W", Price = 42990.00m, StockQuantity = 4, IsActive = true, BrandId = brandsDict["yamaha"], CategoryId = catsDict["power-amplifiers"] },
                new() { Name = "QSC PLX1804", Slug = "qsc-plx1804", Description = "Lightweight power amplifier with PowerLight technology", Price = 34990.00m, StockQuantity = 5, IsActive = true, BrandId = brandsDict["qsc"], CategoryId = catsDict["power-amplifiers"] },

                new() { Name = "Pioneer DDJ-FLX4", Slug = "pioneer-ddj-flx4", Description = "2-channel DJ controller compatible with rekordbox, Serato DJ Lite, and djay", Price = 12990.00m, StockQuantity = 20, IsActive = true, BrandId = brandsDict["pioneer-dj"], CategoryId = catsDict["dj-controllers"] },
                new() { Name = "Pioneer DDJ-400", Slug = "pioneer-ddj-400", Description = "2-channel controller for rekordbox dj", Price = 14990.00m, StockQuantity = 18, IsActive = true, BrandId = brandsDict["pioneer-dj"], CategoryId = catsDict["dj-controllers"] },
                new() { Name = "Numark Mixtrack Pro FX", Slug = "numark-mixtrack-pro-fx", Description = "2-channel DJ controller with FX paddles", Price = 9990.00m, StockQuantity = 15, IsActive = true, BrandId = brandsDict["numark"], CategoryId = catsDict["dj-controllers"] },
                new() { Name = "Pioneer DDJ-REV7", Slug = "pioneer-ddj-rev7", Description = "2-channel controller for Serato DJ Pro with motorized jog wheels", Price = 89990.00m, StockQuantity = 5, IsActive = true, BrandId = brandsDict["pioneer-dj"], CategoryId = catsDict["dj-controllers"] },
                new() { Name = "Denon DJ MC4000", Slug = "denon-dj-mc4000", Description = "2-channel DJ controller with dual audio interface", Price = 11990.00m, StockQuantity = 12, IsActive = true, BrandId = brandsDict["denon-dj"], CategoryId = catsDict["dj-controllers"] },
                new() { Name = "Native Instruments Traktor Kontrol S2 MK3", Slug = "ni-traktor-s2-mk3", Description = "2-channel DJ controller for Traktor Pro 3", Price = 18990.00m, StockQuantity = 10, IsActive = true, BrandId = brandsDict["native-instruments"], CategoryId = catsDict["dj-controllers"] },

                new() { Name = "Pioneer DJM-450", Slug = "pioneer-djm-450", Description = "2-channel DJ mixer with Sound Color FX", Price = 24990.00m, StockQuantity = 8, IsActive = true, BrandId = brandsDict["pioneer-dj"], CategoryId = catsDict["dj-mixers"] },
                new() { Name = "Allen & Heath XONE:23", Slug = "allen-heath-xone-23", Description = "2+2 channel DJ mixer with VCF filter", Price = 19990.00m, StockQuantity = 6, IsActive = true, BrandId = brandsDict["allen-heath"], CategoryId = catsDict["dj-mixers"] },
                new() { Name = "Pioneer DJM-750MK2", Slug = "pioneer-djm-750mk2", Description = "4-channel digital DJ mixer", Price = 54990.00m, StockQuantity = 4, IsActive = true, BrandId = brandsDict["pioneer-dj"], CategoryId = catsDict["dj-mixers"] },
                new() { Name = "Numark M6 USB", Slug = "numark-m6-usb", Description = "4-channel DJ mixer with USB audio interface", Price = 7990.00m, StockQuantity = 12, IsActive = true, BrandId = brandsDict["numark"], CategoryId = catsDict["dj-mixers"] },
                new() { Name = "Denon DJ X1850 Prime", Slug = "denon-dj-x1850-prime", Description = "4-channel club mixer with dual USB inputs", Price = 64990.00m, StockQuantity = 3, IsActive = true, BrandId = brandsDict["denon-dj"], CategoryId = catsDict["dj-mixers"] },

                new() { Name = "Pioneer PLX-500", Slug = "pioneer-plx-500", Description = "Direct drive DJ turntable with USB output", Price = 17990.00m, StockQuantity = 8, IsActive = true, BrandId = brandsDict["pioneer-dj"], CategoryId = catsDict["turntables"] },
                new() { Name = "Pioneer PLX-1000", Slug = "pioneer-plx-1000", Description = "Professional direct drive DJ turntable", Price = 34990.00m, StockQuantity = 5, IsActive = true, BrandId = brandsDict["pioneer-dj"], CategoryId = catsDict["turntables"] },
                new() { Name = "Denon VL12 Prime", Slug = "denon-vl12-prime", Description = "Direct drive DJ turntable with motor control", Price = 39990.00m, StockQuantity = 4, IsActive = true, BrandId = brandsDict["denon-dj"], CategoryId = catsDict["turntables"] },
                new() { Name = "Numark TT250USB", Slug = "numark-tt250usb", Description = "Professional DJ turntable with USB", Price = 12990.00m, StockQuantity = 10, IsActive = true, BrandId = brandsDict["numark"], CategoryId = catsDict["turntables"] },
                new() { Name = "Audio-Technica AT-LP120XUSB", Slug = "audio-technica-at-lp120xusb", Description = "Direct drive turntable with USB output", Price = 14990.00m, StockQuantity = 12, IsActive = true, BrandId = brandsDict["audio-technica"], CategoryId = catsDict["turntables"] },

                new() { Name = "Pioneer DJ Starter Pack", Slug = "pioneer-dj-starter-pack", Description = "Complete DJ setup with DDJ-400 and headphones", Price = 19990.00m, StockQuantity = 6, IsActive = true, BrandId = brandsDict["pioneer-dj"], CategoryId = catsDict["dj-kits"] },
                new() { Name = "Numark Party Mix II Bundle", Slug = "numark-party-mix-ii-bundle", Description = "DJ controller with speakers and headphones", Price = 8990.00m, StockQuantity = 10, IsActive = true, BrandId = brandsDict["numark"], CategoryId = catsDict["dj-kits"] },
                new() { Name = "Pioneer DJ Performance Pack", Slug = "pioneer-dj-performance-pack", Description = "Professional DJ setup with XDJ-RR and monitors", Price = 89990.00m, StockQuantity = 2, IsActive = true, BrandId = brandsDict["pioneer-dj"], CategoryId = catsDict["dj-kits"] },
                new() { Name = "Denon DJ Prime Go Bundle", Slug = "denon-dj-prime-go-bundle", Description = "Standalone DJ system with case and headphones", Price = 54990.00m, StockQuantity = 4, IsActive = true, BrandId = brandsDict["denon-dj"], CategoryId = catsDict["dj-kits"] },
                new() { Name = "Native Instruments Traktor Complete", Slug = "ni-traktor-complete", Description = "Complete Traktor DJ setup with controller and software", Price = 24990.00m, StockQuantity = 8, IsActive = true, BrandId = brandsDict["native-instruments"], CategoryId = catsDict["dj-kits"] },

                new() { Name = "Focusrite Scarlett 2i2 Studio Pack", Slug = "focusrite-scarlett-2i2-studio", Description = "Recording bundle with interface, mic, and headphones", Price = 12990.00m, StockQuantity = 15, IsActive = true, BrandId = brandsDict["rode"], CategoryId = catsDict["recording-kits"] },
                new() { Name = "Rode AI-1 Complete Studio Kit", Slug = "rode-ai-1-complete", Description = "Complete recording setup with NT1 microphone", Price = 15990.00m, StockQuantity = 10, IsActive = true, BrandId = brandsDict["rode"], CategoryId = catsDict["recording-kits"] },
                new() { Name = "Audio-Technica AT2035 Studio Pack", Slug = "audio-technica-at2035-studio", Description = "Recording bundle with condenser mic and accessories", Price = 10990.00m, StockQuantity = 12, IsActive = true, BrandId = brandsDict["audio-technica"], CategoryId = catsDict["recording-kits"] },
                new() { Name = "Shure MV7 Podcast Kit", Slug = "shure-mv7-podcast-kit", Description = "Complete podcasting setup with MV7 microphone", Price = 18990.00m, StockQuantity = 8, IsActive = true, BrandId = brandsDict["shure"], CategoryId = catsDict["recording-kits"] },
                new() { Name = "Yamaha Steinberg UR22C Recording Pack", Slug = "yamaha-ur22c-recording-pack", Description = "Recording bundle with audio interface and software", Price = 11990.00m, StockQuantity = 10, IsActive = true, BrandId = brandsDict["yamaha"], CategoryId = catsDict["recording-kits"] },

                new() { Name = "Yamaha YAS-280 Alto Saxophone", Slug = "yamaha-yas-280", Description = "Student alto saxophone with case and mouthpiece", Price = 42990.00m, StockQuantity = 5, IsActive = true, BrandId = brandsDict["yamaha-winds"], CategoryId = catsDict["wind-instruments"] },
                new() { Name = "Yamaha YTR-2330 Trumpet", Slug = "yamaha-ytr-2330", Description = "Student trumpet with case and accessories", Price = 24990.00m, StockQuantity = 8, IsActive = true, BrandId = brandsDict["yamaha-winds"], CategoryId = catsDict["wind-instruments"] },
                new() { Name = "Yamaha YCL-255 Clarinet", Slug = "yamaha-ycl-255", Description = "Student clarinet with case", Price = 19990.00m, StockQuantity = 10, IsActive = true, BrandId = brandsDict["yamaha-winds"], CategoryId = catsDict["wind-instruments"] },
                new() { Name = "Conn-Selmer AS710 Alto Sax", Slug = "conn-selmer-as710", Description = "Student alto saxophone", Price = 34990.00m, StockQuantity = 6, IsActive = true, BrandId = brandsDict["conn-selmer"], CategoryId = catsDict["wind-instruments"] },
                new() { Name = "Yamaha YFL-222 Flute", Slug = "yamaha-yfl-222", Description = "Student flute with case", Price = 16990.00m, StockQuantity = 12, IsActive = true, BrandId = brandsDict["yamaha-winds"], CategoryId = catsDict["wind-instruments"] }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            // Дата додання для нових навушників — «сьогодні» на момент сидування (UTC)
            var newHeadphoneSlugs = new[] { "shure-srh840a", "sennheiser-hd-560s", "jbl-tune-760nc", "audio-technica-ath-r70x" };
            var addedAt = DateTimeOffset.UtcNow;
            foreach (var slug in newHeadphoneSlugs)
            {
                var p = products.First(x => x.Slug == slug);
                p.CreatedAt = addedAt;
                p.UpdatedAt = addedAt;
            }

            await context.SaveChangesAsync();

            foreach (var p in products)
            {
                context.ProductTranslations.AddRange(
                    new ProductTranslation { ProductId = p.Id, LanguageCode = "uk", Name = GetUkrainianProductName(p.Name), Description = GetUkrainianProductDescription(p.Name) },
                    new ProductTranslation { ProductId = p.Id, LanguageCode = "en", Name = p.Name, Description = p.Description }
                );
            }
            await context.SaveChangesAsync();

            var attributeValues = new List<ProductAttributeValue>();

            // Поліфонія для синтезаторів
            attributeValues.Add(new ProductAttributeValue { ProductId = products.First(p => p.Slug == "korg-minilogue-xd").Id, ProductAttributeId = attrsDict["polyphony"], StringValue = "4 voices" });
            attributeValues.Add(new ProductAttributeValue { ProductId = products.First(p => p.Slug == "arturia-polybrute").Id, ProductAttributeId = attrsDict["polyphony"], StringValue = "6 voices" });
            attributeValues.Add(new ProductAttributeValue { ProductId = products.First(p => p.Slug == "korg-prologue-16").Id, ProductAttributeId = attrsDict["polyphony"], StringValue = "16 voices" });

            // Імпеданс для навушників
            attributeValues.Add(new ProductAttributeValue { ProductId = products.First(p => p.Slug == "sennheiser-hd-25").Id, ProductAttributeId = attrsDict["impedance"], StringValue = "70 Ω" });
            attributeValues.Add(new ProductAttributeValue { ProductId = products.First(p => p.Slug == "sennheiser-hd-280-pro").Id, ProductAttributeId = attrsDict["impedance"], StringValue = "64 Ω" });
            attributeValues.Add(new ProductAttributeValue { ProductId = products.First(p => p.Slug == "audio-technica-ath-m50x").Id, ProductAttributeId = attrsDict["impedance"], StringValue = "38 Ω" });
            attributeValues.Add(new ProductAttributeValue { ProductId = products.First(p => p.Slug == "beyerdynamic-dt-770-pro").Id, ProductAttributeId = attrsDict["impedance"], StringValue = "80 Ω" });
            attributeValues.Add(new ProductAttributeValue { ProductId = products.First(p => p.Slug == "sony-mdr-7506").Id, ProductAttributeId = attrsDict["impedance"], StringValue = "63 Ω" });
            attributeValues.Add(new ProductAttributeValue { ProductId = products.First(p => p.Slug == "audio-technica-ath-m40x").Id, ProductAttributeId = attrsDict["impedance"], StringValue = "35 Ω" });
            attributeValues.Add(new ProductAttributeValue { ProductId = products.First(p => p.Slug == "shure-srh840a").Id, ProductAttributeId = attrsDict["impedance"], StringValue = "44 Ω" });
            attributeValues.Add(new ProductAttributeValue { ProductId = products.First(p => p.Slug == "sennheiser-hd-560s").Id, ProductAttributeId = attrsDict["impedance"], StringValue = "120 Ω" });
            attributeValues.Add(new ProductAttributeValue { ProductId = products.First(p => p.Slug == "jbl-tune-760nc").Id, ProductAttributeId = attrsDict["impedance"], StringValue = "32 Ω" });
            attributeValues.Add(new ProductAttributeValue { ProductId = products.First(p => p.Slug == "audio-technica-ath-r70x").Id, ProductAttributeId = attrsDict["impedance"], StringValue = "99 Ω" });

            // Вага та Bluetooth для навушників
            void AddHeadphoneExtras(string slug, string weightKg, bool bluetooth)
            {
                var pid = products.First(p => p.Slug == slug).Id;
                attributeValues.Add(new ProductAttributeValue { ProductId = pid, ProductAttributeId = attrsDict["weight"], StringValue = weightKg });
                attributeValues.Add(new ProductAttributeValue { ProductId = pid, ProductAttributeId = attrsDict["bluetooth"], BoolValue = bluetooth });
            }

            AddHeadphoneExtras("sennheiser-hd-25", "0.14", false);
            AddHeadphoneExtras("sennheiser-hd-280-pro", "0.29", false);
            AddHeadphoneExtras("audio-technica-ath-m50x", "0.29", false);
            AddHeadphoneExtras("beyerdynamic-dt-770-pro", "0.27", false);
            AddHeadphoneExtras("sony-mdr-7506", "0.23", false);
            AddHeadphoneExtras("audio-technica-ath-m40x", "0.24", false);
            AddHeadphoneExtras("shure-srh840a", "0.31", false);
            AddHeadphoneExtras("sennheiser-hd-560s", "0.24", false);
            AddHeadphoneExtras("jbl-tune-760nc", "0.22", true);
            AddHeadphoneExtras("audio-technica-ath-r70x", "0.22", false);

            // Розмір вуфера для моніторів
            attributeValues.Add(new ProductAttributeValue { ProductId = products.First(p => p.Slug == "kali-lp-6-v2").Id, ProductAttributeId = attrsDict["woofer-size"], StringValue = "6.5\"" });
            attributeValues.Add(new ProductAttributeValue { ProductId = products.First(p => p.Slug == "yamaha-hs8").Id, ProductAttributeId = attrsDict["woofer-size"], StringValue = "8\"" });
            attributeValues.Add(new ProductAttributeValue { ProductId = products.First(p => p.Slug == "jbl-305p-mkii").Id, ProductAttributeId = attrsDict["woofer-size"], StringValue = "5\"" });

            // Канали для мікшерів
            attributeValues.Add(new ProductAttributeValue { ProductId = products.First(p => p.Slug == "soundcraft-signature-12mtk").Id, ProductAttributeId = attrsDict["channels"], StringValue = "12 channels" });
            attributeValues.Add(new ProductAttributeValue { ProductId = products.First(p => p.Slug == "pioneer-djm-450").Id, ProductAttributeId = attrsDict["channels"], StringValue = "2 channels" });

            // Струни для гітар
            attributeValues.Add(new ProductAttributeValue { ProductId = products.First(p => p.Slug == "fender-player-stratocaster-hss").Id, ProductAttributeId = attrsDict["strings-count"], StringValue = "6 strings" });
            attributeValues.Add(new ProductAttributeValue { ProductId = products.First(p => p.Slug == "fender-player-precision-bass").Id, ProductAttributeId = attrsDict["strings-count"], StringValue = "4 strings" });

            context.ProductAttributeValues.AddRange(attributeValues);
            await context.SaveChangesAsync();
        }

        // ПРОМОАКЦІЯ
        if (!context.Promotions.Any())
        {
            var djCategory = await context.Categories.FirstAsync(c => c.Slug == "dj-equipment");

            var promotion = new Promotion
            {
                Name = "Весняний DJ Сезон",
                Slug = "spring-dj-season-2025",
                Description = "Знижка 10% на все DJ обладнання до 30 червня!",
                Level = PromotionLevel.Cart,
                DiscountType = DiscountType.Percentage,
                DiscountValue = 10,
                StartDate = new DateTimeOffset(2025, 3, 14, 0, 0, 0, TimeSpan.Zero),
                EndDate = new DateTimeOffset(2025, 6, 30, 23, 59, 59, TimeSpan.Zero),
                IsCoupon = false,
                IsPersonal = false,
                CartConditions = new CartLevelCondition
                {
                    CategoryIds = new List<Guid> { djCategory.Id }
                }
            };

            context.Promotions.Add(promotion);
            await context.SaveChangesAsync();

            context.PromotionTranslations.AddRange(
                new PromotionTranslation { PromotionId = promotion.Id, LanguageCode = "uk", Name = "Весняний DJ Сезон", Description = "Знижка 10% на все DJ обладнання до 30 червня! Контролери, мікшери, програвачі та комплекти за вигідною ціною." },
                new PromotionTranslation { PromotionId = promotion.Id, LanguageCode = "en", Name = "Spring DJ Season", Description = "10% off all DJ equipment until June 30! Controllers, mixers, turntables and kits at great prices." }
            );
            await context.SaveChangesAsync();
        }

        Console.WriteLine("База успішно засіяна!");
    }

    private static string GetUkrainianBrandName(string enName)
    {
        return enName;
    }

    private static string GetUkrainianBrandDescription(string name) => name switch
    {
        "Korg" => "Японський виробник синтезаторів з 1962 року",
        "Arturia" => "Французькі гібридні інструменти та синтезатори",
        "Moog" => "Легендарні аналогові синтезатори",
        "Roland" => "Іконічні електронні музичні інструменти",
        "Behringer" => "Доступні аналогові синтезатори",
        "Sennheiser" => "Професійні навушники та мікрофони",
        "Audio-Technica" => "Професійні студійні навушники",
        "Shure" => "Професійні мікрофони та аудіообладнання",
        "Rode" => "Студійні мікрофони та аудіообладнання",
        "Kali Audio" => "Доступні високоякісні студійні монітори",
        "Yamaha" => "Професійна аудіотехніка та музичні інструменти",
        "JBL" => "Професійні звукові системи та монітори",
        "QSC" => "Професійні підсилювачі потужності",
        "Soundcraft" => "Професійні мікшерні пульти",
        "Allen & Heath" => "Професійні мікшерні пульти",
        "Fender" => "Легендарні електрогітари та бас-гітари",
        "Ibanez" => "Високопродуктивні гітари та бас-гітари",
        "Gibson" => "Іконічні американські гітари",
        "PRS" => "Преміальні електрогітари",
        "ESP" => "Гітари для металу та року",
        "Pearl" => "Професійні барабанні установки",
        "Tama" => "Високоякісні барабанні установки",
        "Zildjian" => "Преміальні тарілки",
        "Casio" => "Цифрові піаніно та клавішні",
        "Nord" => "Професійні сценічні клавішні",
        "Yamaha Electric" => "Електричні скрипки та смичкові",
        "NS Design" => "Електричні смичкові інструменти",
        "Pioneer DJ" => "Професійне DJ обладнання",
        "Numark" => "DJ обладнання та контролери",
        "Denon DJ" => "Професійне DJ обладнання",
        "Native Instruments" => "DJ програмне забезпечення та апаратура",
        "Yamaha Winds" => "Професійні духові інструменти",
        "Conn-Selmer" => "Духові інструменти",
        _ => name
    };

    private static string GetUkrainianCategoryName(string en) => en switch
    {
        "Sound Equipment" => "Звукове обладнання",
        "Musical Instruments" => "Музичні інструменти",
        "DJ Equipment" => "DJ обладнання",
        "Speaker Systems" => "Акустичні системи",
        "Mixing Consoles" => "Мікшерні пульти",
        "Power Amplifiers" => "Підсилювачі потужності",
        "Microphones" => "Мікрофони",
        "Headphones" => "Навушники",
        "Audio Processing" => "Обробка звуку",
        "Studio Monitors" => "Студійні монітори",
        "Guitars" => "Гітари",
        "Keyboards" => "Клавішні інструменти",
        "Drums" => "Ударні інструменти",
        "Wind Instruments" => "Духові інструменти",
        "String Instruments" => "Смичкові інструменти",
        "DJ Controllers" => "DJ контролери",
        "DJ Mixers" => "DJ мікшери",
        "Turntables" => "Вінілові програвачі",
        "DJ Kits" => "DJ комплекти",
        "Recording Kits" => "Комплекти звукозапису",
        "Electric Guitars" => "Електрогітари",
        "Bass Guitars" => "Бас-гітари",
        "Acoustic Guitars" => "Акустичні гітари",
        "Synthesizers" => "Синтезатори",
        "Digital Pianos" => "Цифрові піаніно",
        "Analog Synthesizers" => "Аналогові синтезатори",
        "Digital Synthesizers" => "Цифрові синтезатори",
        "Polyphonic" => "Поліфонічні",
        "Monophonic" => "Монофонічні",
        "Acoustic Drums" => "Акустичні барабани",
        "Electronic Drums" => "Електронні барабани",
        "Cymbals" => "Тарілки",
        "Violins" => "Скрипки",
        "Electric Violins" => "Електричні скрипки",
        _ => en
    };

    private static string GetUkrainianCategoryDescription(string en) => en switch
    {
        "Sound Equipment" => "Професійне аудіообладнання",
        "Musical Instruments" => "Всі види музичних інструментів",
        "DJ Equipment" => "Професійне DJ обладнання",
        "Speaker Systems" => "Активні та пасивні акустичні системи",
        "Mixing Consoles" => "Аудіо мікшерні пульти",
        "Power Amplifiers" => "Аудіо підсилювачі потужності",
        "Microphones" => "Студійні та сценічні мікрофони",
        "Headphones" => "Студійні та DJ навушники",
        "Audio Processing" => "Ефекти та процесори",
        "Studio Monitors" => "Nearfield студійні монітори",
        "Guitars" => "Електричні та акустичні гітари",
        "Keyboards" => "Синтезатори, піаніно, органи",
        "Drums" => "Барабанні установки та перкусія",
        "Wind Instruments" => "Саксофони, труби, флейти",
        "String Instruments" => "Скрипки, віолончелі, контрабаси",
        "DJ Controllers" => "DJ контролери для програм",
        "DJ Mixers" => "Автономні DJ мікшери",
        "Turntables" => "Вінілові програвачі",
        "DJ Kits" => "Повні DJ комплекти",
        "Recording Kits" => "Комплекти домашнього запису",
        "Electric Guitars" => "Електричні гітари",
        "Bass Guitars" => "Бас-гітари",
        "Acoustic Guitars" => "Акустичні гітари",
        "Synthesizers" => "Аналогові та цифрові синтезатори",
        "Digital Pianos" => "Цифрові піаніно",
        "Analog Synthesizers" => "Чисто аналоговий звук",
        "Digital Synthesizers" => "Цифрові синтезатори",
        "Polyphonic" => "Багатоголосі аналогові",
        "Monophonic" => "Одноголосі аналогові",
        "Acoustic Drums" => "Акустичні барабанні установки",
        "Electronic Drums" => "Електронні барабанні установки",
        "Cymbals" => "Барабанні тарілки",
        "Violins" => "Акустичні та електричні скрипки",
        "Electric Violins" => "Електричні скрипки",
        _ => en
    };

    private static string GetUkrainianAttributeName(string en) => en switch
    {
        "Polyphony" => "Поліфонія",
        "Impedance" => "Імпеданс",
        "Woofer Size" => "Розмір НЧ-динаміка",
        "Power Output" => "Потужність",
        "Number of Channels" => "Кількість каналів",
        "Number of Strings" => "Кількість струн",
        "Keyboard Type" => "Тип клавіатури",
        "Body Material" => "Матеріал корпусу",
        "USB Connectivity" => "Підтримка USB",
        "Bluetooth" => "Bluetooth",
        "Weight" => "Вага",
        _ => en
    };

    private static string GetUkrainianAttributeDescription(string en) => en switch
    {
        "Polyphony" => "Максимальна кількість одночасно звучних голосів / нот",
        "Impedance" => "Електричний імпеданс навушників або колонок",
        "Woofer Size" => "Діаметр низькочастотного динаміка",
        "Power Output" => "Номінальна / пікова потужність (на канал або загальна)",
        "Number of Channels" => "Кількість вхідних / вихідних / мікшерних каналів",
        "Number of Strings" => "Загальна кількість струн на інструменті",
        "Keyboard Type" => "Тип механізму клавіатури (synth-action, напівзважена, молоточкова тощо)",
        "Body Material" => "Основний матеріал корпусу інструменту",
        "USB Connectivity" => "Наявність USB-інтерфейсу для підключення до комп'ютера або MIDI",
        "Bluetooth" => "Підтримка бездротового підключення Bluetooth (аудіо / MIDI)",
        "Weight" => "Вага виробу нетто (без упаковки)",
        _ => ""
    };

    private static string GetUkrainianProductName(string name) => name;

    private static string GetUkrainianProductDescription(string name) => name switch
    {
        "Korg Minilogue XD" => "4-голосний аналоговий поліфонічний синтезатор з мультидвигуном, ефектами та вбудованим секвенсором",
        "Arturia PolyBrute" => "6-голосний повністю аналоговий поліфонічний синтезатор з морфінгом, матрицею модуляції та вбудованими ефектами",
        "Korg Prologue 16" => "16-голосний аналоговий поліфонічний синтезатор з цифровим мультидвигуном та розширеною модуляцією",
        "Arturia MicroFreak" => "Гібридний поліфонічний синтезатор з цифровими осциляторами та аналоговим фільтром",
        "Behringer Deepmind 12" => "12-голосний аналоговий поліфонічний синтезатор з ефектами TC Electronic",
        "Moog Subsequent 37" => "Парафонічний аналоговий синтезатор з класичним Moog ladder-фільтром та мульті-драйвом",
        "Moog Grandmother" => "Напівмодульний аналоговий синтезатор з пружинним ревербератором та арпеджіатором",
        "Behringer Model D" => "Аналоговий синтезатор - клон класичного Moog звучання",
        "Arturia MiniBrute 2" => "Напівмодульний монофонічний аналоговий синтезатор з секвенсором",
        "Korg MS-20 Mini" => "Компактний монофонічний аналоговий синтезатор з патч-панеллю",
        "Roland JD-Xi" => "Гібридний синтезатор з цифровими двигунами, вокодером, мікрофонним входом та повноцінним секвенсором",
        "Korg Opsix" => "FM-синтезатор з 6 операторами та переосмисленим підходом до FM-синтезу",
        "Roland Fantom-0 6" => "61-клавішна синтезаторна робоча станція зі звуковим двигуном ZEN-Core",
        "Yamaha MODX7" => "76-клавішний синтезатор з двигунами FM-X та AWM2",
        "Nord Stage 4 Compact" => "73-клавішна сценічна клавіатура з секціями піаніно, органу та синтезатора",
        "Yamaha P-125" => "88-клавішне зважене цифрове піаніно зі звуковим двигуном Pure CF",
        "Casio Privia PX-S3100" => "Тонке 88-клавішне цифрове піаніно з розумною зваженою клавіатурою",
        "Roland FP-30X" => "Портативне 88-клавішне цифрове піаніно зі звуком SuperNATURAL",
        "Yamaha Clavinova CLP-745" => "Цифрове піаніно з клавіатурою GrandTouch-S та семплами CFX/Bösendorfer",
        "Casio CDP-S110" => "Компактне 88-клавішне цифрове піаніно зі зваженою механікою",
        "Fender Player Stratocaster HSS" => "Класичний Stratocaster з конфігурацією звукознімачів HSS, кленовим грифом та вінтажною тремоло-системою",
        "Ibanez RG550 Genesis" => "Високопродуктивний суперстрат з грифом wizard, тремоло Edge та потужними звукознімачами",
        "Gibson Les Paul Standard '50s" => "Класичний Les Paul з вінтажним профілем грифа 50-х років та звукознімачами Burstbucker",
        "PRS SE Custom 24" => "Універсальна гітара з 24 ладами, тонким широким грифом та звукознімачами 85/15 S",
        "ESP LTD EC-1000" => "Гітара з одним вирізом, активними звукознімачами EMG та корпусом з червоного дерева",
        "Fender Telecaster American Professional II" => "Професійний Telecaster зі звукознімачами V-Mod II та грифом Deep C",
        "Fender Player Precision Bass" => "Класичний P-Bass з роздільним сингл-койлом та кленовим грифом",
        "Ibanez SR300E" => "Активна бас-гітара з бриджем Accu-cast B120 та звукознімачами Dynamix P/J",
        "Fender Jazz Bass American Pro II" => "Професійний Jazz Bass зі звукознімачами V-Mod II",
        "Ibanez BTB845SC" => "5-струнний бас зі звукознімачами Nordstrand Big Single",
        "ESP LTD B-206SM" => "6-струнна бас-гітара з активними звукознімачами ESP",
        "Pearl Export EXX725" => "5-барабанна установка з корпусами з тополі/червоного дерева",
        "Tama Imperialstar IE52KH6" => "5-барабанна установка з апаратурою та тарілками",
        "Pearl Decade Maple DMP925" => "5-барабанна кленова установка з професійним звучанням",
        "Tama Starclassic Maple MR52TZUS" => "Професійна 5-барабанна кленова установка",
        "Pearl Vision VML925" => "5-барабанна березова установка з підвіскою томів SST",
        "Roland TD-17KVX" => "Електронна барабанна установка з сітчастими пластиками та модулем TD-17",
        "Yamaha DTX6K-X" => "Електронна барабанна установка з корпусами з натурального дерева та модулем DTX-PRO",
        "Roland TD-27KV" => "Професійна електронна барабанна установка зі звуковим модулем TD-27",
        "Yamaha DTX452K" => "Компактна електронна барабанна установка для початківців",
        "Roland TD-1DMK" => "Початкова електронна барабанна установка з сітчастим малим барабаном",
        "Zildjian A Custom Cymbal Set" => "Професійний набір тарілок з блискучою обробкою",
        "Zildjian K Custom Dark Crash 18" => "18-дюймова темна креш-тарілка з сухим звучанням",
        "Zildjian A Series Medium Ride 20" => "20-дюймова райд-тарілка середньої жорсткості зі збалансованим звучанням",
        "Zildjian ZBT Cymbal Set" => "Початковий набір тарілок з яскравим звучанням",
        "Zildjian K Constantinople Ride 22" => "22-дюймова кована вручну райд-тарілка",
        "Yamaha YEV-104 BL" => "4-струнна електрична скрипка з корпусом із цільної ялини, грифом з чорного дерева та вбудованим п'єзозвукознімачем",
        "NS Design WAV-5 Electric Violin" => "5-струнна електрична скрипка з активною системою полярних звукознімачів, конструкцією з вуглецевого волокна",
        "Yamaha SV250" => "Безшумна електрична скрипка з кленовим корпусом",
        "NS Design NXT5a" => "5-струнна активна електрична скрипка з низьким імпедансом",
        "Yamaha YSV104" => "Безшумна скрипка з ревербератором та допоміжним входом",
        "Sennheiser HD 25" => "Закриті DJ-навушники з високою звукоізоляцією та замінними компонентами",
        "Sennheiser HD 280 Pro" => "Професійні студійні моніторні навушники з плоскою частотною характеристикою та знімним кабелем",
        "Audio-Technica ATH-M50x" => "Професійні моніторні навушники з виключною чіткістю звуку",
        "Beyerdynamic DT 770 Pro 80 Ohm" => "Закриті студійні навушники з технологією Bass Reflex",
        "Sony MDR-7506" => "Професійні студійні моніторні навушники",
        "Audio-Technica ATH-M40x" => "Професійні студійні моніторні навушники",
        "Shure SRH840A" => "Професійні закриті студійні навушники з точним детальним звучанням",
        "Sennheiser HD 560S" => "Відкриті референсні навушники для аналітичного прослуховування та міксу",
        "JBL Tune 760NC" => "Бездротові навколо-вушні навушники з активним шумозаглушенням",
        "Audio-Technica ATH-R70x" => "Відкриті професійні референсні навушники для міксу та мастерингу",
        "Kali Audio LP-6 v2" => "6.5-дюймові активні nearfield-монітори з точною передачею звуку та boundary EQ",
        "Yamaha HS8" => "8-дюймові студійні монітори з кімнатним контролем та високоточним відтворенням звуку",
        "JBL 305P MkII" => "5-дюймовий активний студійний монітор з Image Control Waveguide",
        "Yamaha HS5" => "5-дюймовий активний студійний монітор",
        "Kali Audio IN-5" => "5-дюймовий 3-смуговий студійний монітор",
        "JBL LSR308P MkII" => "8-дюймовий активний студійний монітор",
        "Shure SM7B" => "Динамічний мікрофон з легендарним звучанням для вокалу, подкастів та мовлення",
        "Rode NT1 5th Gen" => "Конденсаторний мікрофон з великою діафрагмою, наднизьким рівнем шуму та виходами USB/XLR",
        "Shure SM58" => "Легендарний вокальний динамічний мікрофон",
        "Rode NT-USB" => "USB конденсаторний мікрофон для подкастингу",
        "Shure Beta 87A" => "Конденсаторний вокальний мікрофон з суперкардіоїдною діаграмою спрямованості",
        "Rode Procaster" => "Динамічний мікрофон мовної якості",
        "JBL EON710" => "10-дюймова активна PA-колонка з Bluetooth",
        "JBL EON712" => "12-дюймова активна PA-колонка з піковою потужністю 1300 Вт",
        "Yamaha DXR12 mkII" => "12-дюймова активна колонка з потужністю 1100 Вт",
        "JBL PRX815W" => "15-дюймова активна колонка з Wi-Fi управлінням",
        "Yamaha DBR15" => "15-дюймова активна колонка для концертного звуку",
        "Soundcraft Signature 12MTK" => "12-канальний мікшер з USB аудіоінтерфейсом",
        "Allen & Heath ZEDi-10FX" => "10-канальний мікшер з USB та ефектами",
        "Yamaha MG12XU" => "12-канальний мікшер з вбудованими ефектами та USB",
        "Soundcraft Signature 22MTK" => "22-канальний мікшер з багатоканальним USB",
        "Allen & Heath SQ-5" => "Цифровий мікшер з 48 каналами",
        "QSC GX5" => "Підсилювач потужності 700 Вт на канал при 8 Ом",
        "QSC GX7" => "Підсилювач потужності 1000 Вт на канал при 8 Ом",
        "QSC RMX1450" => "Підсилювач потужності 450 Вт на канал при 8 Ом",
        "Yamaha P7000S" => "Професійний підсилювач потужності 2x700 Вт",
        "QSC PLX1804" => "Легкий підсилювач потужності з технологією PowerLight",
        "Pioneer DDJ-FLX4" => "2-канальний DJ-контролер сумісний з rekordbox, Serato DJ Lite та djay, з функціями Smart CFX та Smart Fader",
        "Pioneer DDJ-400" => "2-канальний контролер для rekordbox dj",
        "Numark Mixtrack Pro FX" => "2-канальний DJ-контролер з FX-педалями",
        "Pioneer DDJ-REV7" => "2-канальний контролер для Serato DJ Pro з моторизованими jog-колесами",
        "Denon DJ MC4000" => "2-канальний DJ-контролер з подвійним аудіоінтерфейсом",
        "Native Instruments Traktor Kontrol S2 MK3" => "2-канальний DJ-контролер для Traktor Pro 3",
        "Pioneer DJM-450" => "2-канальний DJ-мікшер з ефектами Sound Color FX",
        "Allen & Heath XONE:23" => "2+2-канальний DJ-мікшер з VCF-фільтром",
        "Pioneer DJM-750MK2" => "4-канальний цифровий DJ-мікшер",
        "Numark M6 USB" => "4-канальний DJ-мікшер з USB аудіоінтерфейсом",
        "Denon DJ X1850 Prime" => "4-канальний клубний мікшер з подвійними USB-входами",
        "Pioneer PLX-500" => "DJ-програвач з прямим приводом та USB-виходом",
        "Pioneer PLX-1000" => "Професійний DJ-програвач з прямим приводом",
        "Denon VL12 Prime" => "DJ-програвач з прямим приводом та контролем двигуна",
        "Numark TT250USB" => "Професійний DJ-програвач з USB",
        "Audio-Technica AT-LP120XUSB" => "Програвач з прямим приводом та USB-виходом",
        "Pioneer DJ Starter Pack" => "Повний DJ-комплект з DDJ-400 та навушниками",
        "Numark Party Mix II Bundle" => "DJ-контролер з колонками та навушниками",
        "Pioneer DJ Performance Pack" => "Професійний DJ-комплект з XDJ-RR та моніторами",
        "Denon DJ Prime Go Bundle" => "Автономна DJ-система з кейсом та навушниками",
        "Native Instruments Traktor Complete" => "Повний Traktor DJ-комплект з контролером та програмним забезпеченням",
        "Focusrite Scarlett 2i2 Studio Pack" => "Комплект для звукозапису з інтерфейсом, мікрофоном та навушниками",
        "Rode AI-1 Complete Studio Kit" => "Повний комплект для звукозапису з мікрофоном NT1",
        "Audio-Technica AT2035 Studio Pack" => "Комплект для звукозапису з конденсаторним мікрофоном та аксесуарами",
        "Shure MV7 Podcast Kit" => "Повний комплект для подкастингу з мікрофоном MV7",
        "Yamaha Steinberg UR22C Recording Pack" => "Комплект для звукозапису з аудіоінтерфейсом та програмним забезпеченням",
        "Yamaha YAS-280 Alto Saxophone" => "Альт-саксофон для студентів з кейсом та мундштуком",
        "Yamaha YTR-2330 Trumpet" => "Труба для студентів з кейсом та аксесуарами",
        "Yamaha YCL-255 Clarinet" => "Кларнет для студентів з кейсом",
        "Conn-Selmer AS710 Alto Sax" => "Альт-саксофон для студентів",
        "Yamaha YFL-222 Flute" => "Флейта для студентів з кейсом",
        _ => name
    };
}