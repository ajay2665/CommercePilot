using Commerce.Domain;
using Commerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Infrastructure.Persistence;

/// <summary>
/// Deterministic local-first demo dataset (decision 10): 5 brands, products,
/// customers, 120 days of orders with weekly seasonality, inventory, routing
/// rules, and KB articles — so every pod has data without any external system.
/// </summary>
public static class DemoSeeder
{
    public static readonly string[] Brands =
        ["Aurora Audio", "Peak Outdoors", "Luma Beauty", "Nordic Home", "VoltEdge"];

    public static async Task SeedAsync(CommerceDbContext db, CancellationToken ct = default)
    {
        var rnd = new Random(42);

        // ── Categories + products (6 per brand) ────────────────────────────
        var categoryNames = new Dictionary<string, string>
        {
            ["Aurora Audio"] = "Audio",
            ["Peak Outdoors"] = "Outdoor Gear",
            ["Luma Beauty"] = "Beauty",
            ["Nordic Home"] = "Home & Living",
            ["VoltEdge"] = "Power Tools",
        };
        var categories = categoryNames.Values
            .Select(name => new Category { Name = name })
            .ToList();

        string[][] productNames =
        [
            ["Wireless Earbuds Pro", "Studio Headphones X2", "Soundbar S450", "Portable Speaker Go", "Vinyl Turntable V1", "USB Microphone M3"],
            ["Trail Backpack 45L", "2-Person Tent Ridge", "Insulated Bottle 1L", "Trekking Poles Carbon", "Headlamp 400lm", "Sleeping Bag -5C"],
            ["Vitamin C Serum", "Hydrating Face Cream", "Sunscreen SPF50", "Retinol Night Oil", "Cleansing Balm", "Lip Repair Kit"],
            ["Oak Coffee Table", "Linen Throw Blanket", "Ceramic Vase Set", "Wall Shelf Duo", "Scented Candle Trio", "Wool Area Rug"],
            ["Cordless Drill 18V", "Angle Grinder 750W", "Circular Saw C7", "Impact Driver Kit", "Rotary Sander RS2", "Laser Level L360"],
        ];

        var products = new List<Product>();
        for (int bi = 0; bi < Brands.Length; bi++)
        {
            string brand = Brands[bi];
            var category = categories[bi];
            for (int pi = 0; pi < productNames[bi].Length; pi++)
            {
                products.Add(new Product
                {
                    Brand = brand,
                    Sku = $"{brand.Split(' ')[0].ToUpperInvariant()[..3]}-{100 + pi}",
                    Name = productNames[bi][pi],
                    Description = $"{productNames[bi][pi]} by {brand}.",
                    CategoryId = category.Id,
                    Price = Math.Round((decimal)(rnd.Next(15, 400) + rnd.NextDouble()), 2),
                });
            }
        }

        // ── Customers ──────────────────────────────────────────────────────
        var customers = Enumerable.Range(1, 30)
            .Select(i => new Customer { Name = $"Customer {i:D2}", Email = $"customer{i:D2}@example.com" })
            .ToList();

        // ── Orders: 120 days of history, weekend bump, per-product popularity ──
        // Weighted sampling (not max-draw) so the long tail sells too — otherwise
        // a handful of bestsellers absorb every order and the rest look dead.
        var popularity = products.ToDictionary(p => p.Id, _ => rnd.NextDouble() + 0.2);
        double totalPopularity = popularity.Values.Sum();
        Product PickWeighted()
        {
            double r = rnd.NextDouble() * totalPopularity;
            foreach (var p in products)
            {
                r -= popularity[p.Id];
                if (r <= 0) return p;
            }
            return products[^1];
        }
        var orders = new List<Order>();
        DateTimeOffset today = DateTimeOffset.UtcNow.Date;
        for (int day = 120; day >= 1; day--)
        {
            DateTimeOffset date = today.AddDays(-day);
            bool weekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            int orderCount = rnd.Next(4, 9) + (weekend ? 3 : 0);

            for (int i = 0; i < orderCount; i++)
            {
                var order = new Order
                {
                    CustomerId = customers[rnd.Next(customers.Count)].Id,
                    CreatedAt = date.AddHours(rnd.Next(8, 22)),
                    Status = OrderStatus.Completed,
                };
                int itemCount = rnd.Next(1, 4);
                for (int j = 0; j < itemCount; j++)
                {
                    var product = PickWeighted();
                    order.Items.Add(new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = product.Id,
                        Quantity = rnd.Next(1, 3),
                        UnitPrice = product.Price,
                    });
                }
                order.Total = order.Items.Sum(x => x.Quantity * x.UnitPrice);
                orders.Add(order);
            }
        }

        // ── Warehouses, suppliers, inventory ───────────────────────────────
        var warehouses = new List<Warehouse>
        {
            new() { Name = "Central DC", Location = "Rotterdam" },
            new() { Name = "North Hub", Location = "Hamburg" },
        };
        var suppliers = new List<Supplier>
        {
            new() { Name = "Globex Supply", Email = "orders@globex.example", AvgLeadTimeDays = 12 },
            new() { Name = "Meridian Trade", Email = "sales@meridian.example", AvgLeadTimeDays = 8 },
            new() { Name = "Kito Logistics", Email = "b2b@kito.example", AvgLeadTimeDays = 18 },
        };
        var inventory = products.Select(p => new InventoryLevel
        {
            ProductId = p.Id,
            WarehouseId = warehouses[rnd.Next(warehouses.Count)].Id,
            CurrentStock = rnd.Next(15, 90),
            SafetyStock = 10,
            ReorderPoint = 25,
            LeadTimeDays = rnd.Next(7, 21),
        }).ToList();

        // ── Routing rules: wildcard per category + one brand override ──────
        var rules = new List<RoutingRule>
        {
            new() { Brand = "*", Category = TicketCategory.Refund, TargetTeam = "Billing Team" },
            new() { Brand = "*", Category = TicketCategory.Payment, TargetTeam = "Billing Team" },
            new() { Brand = "*", Category = TicketCategory.Shipping, TargetTeam = "Logistics Team" },
            new() { Brand = "*", Category = TicketCategory.Warranty, TargetTeam = "Warranty Desk" },
            new() { Brand = "*", Category = TicketCategory.Technical, TargetTeam = "Tech Support" },
            new() { Brand = "*", Category = TicketCategory.Complaint, TargetTeam = "Customer Experience" },
            new() { Brand = "*", Category = TicketCategory.Other, TargetTeam = "General Support" },
            // Brand-specific override demonstrating precedence over the wildcard:
            new() { Brand = "VoltEdge", Category = TicketCategory.Technical, TargetTeam = "VoltEdge Tech Desk" },
        };

        // ── KB articles (embedded in Phase 1b) ─────────────────────────────
        var articles = new List<KnowledgeArticle>();
        foreach (string brand in Brands)
        {
            articles.Add(new KnowledgeArticle
            {
                Brand = brand,
                SourceType = KnowledgeSourceType.Policy,
                Title = $"{brand} — Return & Refund Policy",
                Body = $"{brand} accepts returns within 30 days of delivery in original packaging. " +
                       "Refunds are issued to the original payment method within 5 business days of receiving the return. " +
                       "Opened consumables and personalised items are excluded.",
            });
            articles.Add(new KnowledgeArticle
            {
                Brand = brand,
                SourceType = KnowledgeSourceType.Policy,
                Title = $"{brand} — Shipping Policy",
                Body = $"{brand} ships within 2 business days. Standard delivery takes 3–5 business days, express 1–2. " +
                       "Tracking numbers are emailed on dispatch. Lost parcels are reshipped free after a 10-day carrier investigation.",
            });
            articles.Add(new KnowledgeArticle
            {
                Brand = brand,
                SourceType = KnowledgeSourceType.Policy,
                Title = $"{brand} — Warranty Terms",
                Body = $"All {brand} products carry a 24-month manufacturer warranty covering defects in materials and workmanship. " +
                       "Accidental damage and normal wear are not covered. Warranty claims require the order number.",
            });
        }
        articles.Add(new KnowledgeArticle
        {
            SourceType = KnowledgeSourceType.Faq,
            Title = "How do I change my account email?",
            Body = "Go to Account → Settings → Email, enter the new address and confirm via the verification link. " +
                   "Order history and subscriptions carry over automatically.",
        });
        articles.Add(new KnowledgeArticle
        {
            SourceType = KnowledgeSourceType.Faq,
            Title = "Which payment methods are accepted?",
            Body = "We accept major credit/debit cards, PayPal, iDEAL and Klarna. " +
                   "Payment is captured when the order ships, except pre-orders which are captured at checkout.",
        });

        db.Categories.AddRange(categories);
        db.Products.AddRange(products);
        db.Customers.AddRange(customers);
        db.Orders.AddRange(orders);
        db.Warehouses.AddRange(warehouses);
        db.Suppliers.AddRange(suppliers);
        db.InventoryLevels.AddRange(inventory);
        db.RoutingRules.AddRange(rules);
        db.KnowledgeArticles.AddRange(articles);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Phase 2/3 backfill — runs whenever StockMovements is empty (fresh DB or
    /// upgrade of a Phase-1 DB). Derives stock movements and customer events
    /// from the seeded orders, and forces a few demo-worthy inventory states
    /// (guaranteed dead stock, overstock, low stock) so dashboards have content.
    /// </summary>
    public static async Task SeedPodDataAsync(CommerceDbContext db, CancellationToken ct = default)
    {
        var rnd = new Random(7);
        var products = await db.Products.ToListAsync(ct);
        var inventory = await db.InventoryLevels.ToListAsync(ct);
        var orders = await db.Orders.Include(o => o.Items).OrderBy(o => o.CreatedAt).ToListAsync(ct);

        // ── Guaranteed dead stock: a product with inventory but zero sales ──
        if (!await db.Products.AnyAsync(p => p.Sku == "NOR-199", ct))
        {
            var legacy = new Product
            {
                Brand = "Nordic Home",
                Sku = "NOR-199",
                Name = "Cable Organizer Box (discontinued)",
                Description = "Legacy cable organizer box by Nordic Home. No longer promoted.",
                CategoryId = products.FirstOrDefault(p => p.Brand == "Nordic Home")?.CategoryId,
                Price = 19.95m,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-120),
            };
            db.Products.Add(legacy);
            db.InventoryLevels.Add(new InventoryLevel
            {
                ProductId = legacy.Id,
                CurrentStock = 64,
                SafetyStock = 10,
                ReorderPoint = 25,
                LeadTimeDays = 14,
            });
            products.Add(legacy);
        }

        // ── Forced demo states: two low-stock, one overstock ────────────────
        var byRate = orders.SelectMany(o => o.Items)
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
        var sellers = inventory
            .Where(i => byRate.ContainsKey(i.ProductId))
            .OrderByDescending(i => byRate[i.ProductId])
            .ToList();

        void ForceStock(InventoryLevel level, int stock, string why)
        {
            if (level.CurrentStock == stock) return;
            db.StockMovements.Add(new StockMovement
            {
                ProductId = level.ProductId,
                Delta = stock - level.CurrentStock,
                Reason = "correction",
                Source = "seed",
                Timestamp = DateTimeOffset.UtcNow.AddHours(-2),
            });
            level.CurrentStock = stock;
        }

        if (sellers.Count >= 4)
        {
            ForceStock(sellers[0], 8, "low stock demo");   // best seller nearly out
            ForceStock(sellers[2], 14, "low stock demo");
            ForceStock(sellers[^1], 480, "overstock demo"); // slow seller drowning in stock
        }

        // ── Stock movements derived from order history + monthly restocks ───
        foreach (var order in orders)
        {
            foreach (var item in order.Items)
            {
                db.StockMovements.Add(new StockMovement
                {
                    ProductId = item.ProductId,
                    Delta = -item.Quantity,
                    Reason = "sale",
                    Source = "order",
                    Timestamp = order.CreatedAt,
                });
            }
        }
        foreach (var product in products)
        {
            for (int daysAgo = 110; daysAgo > 0; daysAgo -= 30)
            {
                db.StockMovements.Add(new StockMovement
                {
                    ProductId = product.Id,
                    Delta = rnd.Next(30, 80),
                    Reason = "restock",
                    Source = "seed",
                    Timestamp = DateTimeOffset.UtcNow.AddDays(-daysAgo),
                });
            }
        }

        // ── Customer events: purchases from orders + synthetic views/carts ──
        foreach (var order in orders)
        {
            foreach (var item in order.Items)
            {
                db.CustomerEvents.Add(new CustomerEvent
                {
                    CustomerId = order.CustomerId,
                    ProductId = item.ProductId,
                    EventType = ShopEventType.Purchase,
                    CreatedAt = order.CreatedAt,
                });
                int views = rnd.Next(2, 6);
                for (int v = 0; v < views; v++)
                {
                    db.CustomerEvents.Add(new CustomerEvent
                    {
                        CustomerId = order.CustomerId,
                        ProductId = item.ProductId,
                        EventType = ShopEventType.View,
                        CreatedAt = order.CreatedAt.AddHours(-rnd.Next(1, 72)),
                    });
                }
                if (rnd.NextDouble() < 0.5)
                {
                    db.CustomerEvents.Add(new CustomerEvent
                    {
                        CustomerId = order.CustomerId,
                        ProductId = item.ProductId,
                        EventType = ShopEventType.Cart,
                        CreatedAt = order.CreatedAt.AddMinutes(-rnd.Next(10, 300)),
                    });
                }
            }
        }

        // ── Abandoned carts: recent cart activity, no purchase, ripe for recovery ──
        var customers = await db.Customers.Take(10).ToListAsync(ct);
        for (int i = 0; i < 3 && i < customers.Count; i++)
        {
            var cartProducts = products.OrderBy(_ => rnd.Next()).Take(rnd.Next(1, 3)).ToList();
            DateTimeOffset lastActive = DateTimeOffset.UtcNow.AddHours(-rnd.Next(6, 30));
            foreach (var p in cartProducts)
            {
                db.CustomerEvents.Add(new CustomerEvent
                {
                    CustomerId = customers[i].Id,
                    ProductId = p.Id,
                    EventType = ShopEventType.Cart,
                    CreatedAt = lastActive,
                });
            }
            db.AbandonedCarts.Add(new AbandonedCart
            {
                CustomerId = customers[i].Id,
                ProductIdsJson = System.Text.Json.JsonSerializer.Serialize(cartProducts.Select(p => p.Id)),
                LastActiveAt = lastActive,
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
