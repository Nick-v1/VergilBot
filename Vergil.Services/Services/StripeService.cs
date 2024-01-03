using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Stripe.Checkout;
using Vergil.Services.Enums;

namespace Vergil.Services.Services;

public interface IStripeService
{
    Task<string> StripeTransaction(PurchaseType purchaseType, string email, string? args);
}

public class StripeService : IStripeService
{
    private readonly IConfiguration _config;

    public StripeService(IConfiguration configuration)
    {
        _config = configuration;
    }
    public async Task<string> StripeTransaction(PurchaseType purchaseType, string email, string? args)
    {
        if (args is null)
        {
            if (purchaseType == PurchaseType.Subscription)
            {
                var options = FindCorrectPackage(null, email, PurchaseType.Subscription);

                var service = new SessionService();
                Session session = await service.CreateAsync(options);
                return session.Url;
            }
        }

        if (purchaseType == PurchaseType.Bloodstones)
        {
            //extracts the amount from the string
            var amount = int.Parse(Regex.Match(args, @"\d+").Value);
            var options = FindCorrectPackage(amount, email, PurchaseType.Bloodstones);

            var service = new SessionService();
            Session session = await service.CreateAsync(options);
            return session.Url;
        }

        if (purchaseType == PurchaseType.Tokens)
        {
            var amount = int.Parse(Regex.Match(args, @"\d+").Value);
            var options = FindCorrectPackage(amount, email, PurchaseType.Tokens);

            var service = new SessionService();
            Session session = await service.CreateAsync(options);
            return session.Url;
        }
        

        return "Invalid type";
    }

    private SessionCreateOptions? FindCorrectPackage(int? amount, string email, PurchaseType purchaseType)
    {
        if (purchaseType == PurchaseType.Bloodstones)
        {
            if (amount == 2000)
            {
                var discordAppFundsPackage1 = _config.GetSection("PurchaseOptions:DiscordAppFundsPackage1").Value;
                var options = new SessionCreateOptions
                {
                    CustomerEmail = email,
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            Price = discordAppFundsPackage1,
                            Quantity = 1
                        },
                    },
                    AllowPromotionCodes = true,
                    Mode = "payment",
                    SuccessUrl = "https://nick-v1.github.io/discord/SuccessPage.html"
                };
                return options;
            }

            if (amount == 5000)
            {
                var discordAppFundsPackage2 = _config.GetSection("PurchaseOptions:DiscordAppFundsPackage2").Value;
                var options = new SessionCreateOptions
                {
                    CustomerEmail = email,
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            Price = discordAppFundsPackage2,
                            Quantity = 1
                        },
                    },
                    AllowPromotionCodes = true,
                    Mode = "payment",
                    SuccessUrl = "https://nick-v1.github.io/discord/SuccessPage.html"
                };
                return options;
            }

            if (amount == 10000)
            {
                var discordAppFundsPackage3 = _config.GetSection("PurchaseOptions:DiscordAppFundsPackage3").Value;
                var options = new SessionCreateOptions
                {
                    CustomerEmail = email,
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            Price = discordAppFundsPackage3,
                            Quantity = 1
                        },
                    },
                    AllowPromotionCodes = true,
                    Mode = "payment",
                    SuccessUrl = "https://nick-v1.github.io/discord/SuccessPage.html"
                };
                return options;
            }

            if (amount == 20000)
            {
                var discordAppFundsPackage4 = _config.GetSection("PurchaseOptions:DiscordAppFundsPackage4").Value;
                var options = new SessionCreateOptions
                {
                    CustomerEmail = email,
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            Price = discordAppFundsPackage4,
                            Quantity = 1
                        },
                    },
                    AllowPromotionCodes = true,
                    Mode = "payment",
                    SuccessUrl = "https://nick-v1.github.io/discord/SuccessPage.html"
                };
                return options;
            }
        }
        else if (purchaseType == PurchaseType.Tokens)
        {
            if (amount == 10)
            {
                var discordTokensFundsPackage1 = _config.GetSection("PurchaseOptions:DiscordAppTokensPackage1").Value;
                var options = new SessionCreateOptions
                {
                    CustomerEmail = email,
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            Price = discordTokensFundsPackage1,
                            Quantity = 1
                        },
                    },
                    AllowPromotionCodes = true,
                    Mode = "payment",
                    SuccessUrl = "https://nick-v1.github.io/discord/SuccessPage.html"
                };
                return options;
            }

            if (amount == 100)
            {
                var discordTokensFundsPackage2 = _config.GetSection("PurchaseOptions:DiscordAppTokensPackage2").Value;
                var options = new SessionCreateOptions
                {
                    CustomerEmail = email,
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            Price = discordTokensFundsPackage2,
                            Quantity = 1
                        },
                    },
                    AllowPromotionCodes = true,
                    Mode = "payment",
                    SuccessUrl = "https://nick-v1.github.io/discord/SuccessPage.html"
                };
                return options;
            }

            if (amount == 200)
            {
                var discordTokensFundsPackage3 = _config.GetSection("PurchaseOptions:DiscordAppTokensPackage3").Value;
                var options = new SessionCreateOptions
                {
                    CustomerEmail = email,
                    LineItems = new List<SessionLineItemOptions>
                    {
                        new SessionLineItemOptions
                        {
                            Price = discordTokensFundsPackage3,
                            Quantity = 1
                        },
                    },
                    AllowPromotionCodes = true,
                    Mode = "payment",
                    SuccessUrl = "https://nick-v1.github.io/discord/SuccessPage.html"
                };
                return options;
            }
        }
        else if (purchaseType == PurchaseType.Subscription)
        {
            var discordPremiumMembership = _config.GetSection("PurchaseOptions:PremiumMembership").Value;
            var options = new SessionCreateOptions
            {
                CustomerEmail = email,
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = discordPremiumMembership,
                        Quantity = 1
                    },
                },
                AllowPromotionCodes = true,
                Mode = "subscription",
                SuccessUrl = "https://nick-v1.github.io/discord/SuccessPage.html"
            };
            return options;
        }
        
        return null;
    }
}