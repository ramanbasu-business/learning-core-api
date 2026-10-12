using Bogus;
using EfCoreLab.Common.Data;
using EfCoreLab.Common.Domain;

namespace EfCoreLab.Common.Seeding;

/// <summary>
/// Fills an empty database with fake data using plain EF Core: build the objects, AddRange, SaveChanges.
/// The random generators use fixed seeds, so every machine gets the same row counts.
/// Takes a minute or two for the large dataset - it is a one-off, the result is saved as the baseline.
/// </summary>
public static class Seeder
{
    private static readonly DateTime Now = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);   // "today" inside the dataset

    // 20 entries => 55% Delivered, 15% Shipped, 10% Paid, 10% Pending, 10% Cancelled
    private static readonly string[] Statuses =
    {
        "Delivered", "Delivered", "Delivered", "Delivered", "Delivered", "Delivered",
        "Delivered", "Delivered", "Delivered", "Delivered", "Delivered",
        "Shipped", "Shipped", "Shipped", "Paid", "Paid", "Pending", "Pending", "Cancelled", "Cancelled"
    };

    private static readonly string[] NoteTexts =
    {
        "Customer called to confirm the delivery address.", "Payment received, releasing to warehouse.",
        "Item backordered, customer notified by email.", "Gift wrap requested.",
        "Address corrected after courier feedback.", "Customer asked for an invoice copy.",
        "Escalated to the account manager.", "Partial shipment sent, remainder follows.",
        "Fraud check passed.", "Customer requested a delivery slot change.",
        "Replacement sent for damaged item.", "Refund issued to original payment method.",
        "Loyalty discount applied manually.", "Awaiting customer reply on size.",
        "Courier reports recipient not at home.", "Order merged with a duplicate."
    };

    public static async Task SeedAsync(AppDbContext db, bool small)
    {
        var customerCount = small ? 2_000 : 10_000;
        var productCount = small ? 300 : 1_000;
        var orderCount = small ? 20_000 : 200_000;
        const int userCount = 50;
        const int salesRepCount = 20;     // the first 20 users are sales reps; orders are assigned to them

        Randomizer.Seed = new Random(2026);   // makes Bogus (the fake-data library) repeatable
        var random = new Random(42);
        var faker = new Faker();

        // We add complete object graphs ourselves, so EF doesn't need to re-scan every entity on each save.
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        // ---- 1. Roles and users ------------------------------------------------------
        var roles = new[] { "Admin", "SalesRep", "Support", "Viewer" }
            .Select(name => new Role { Id = Guid.CreateVersion7(), Name = name })
            .ToList();
        db.Roles.AddRange(roles);

        var users = new List<User>();
        for (var i = 0; i < userCount; i++)
        {
            var createdAt = Now.AddDays(-900 + i);
            var user = new User
            {
                Id = Guid.CreateVersion7(),
                Username = $"user{i:D3}",
                Email = $"user{i:D3}@example.com",
                PasswordHash = "seed-data-not-a-real-hash",
                Name = faker.Name.FullName(),
                IsActive = true,
                IsDeleted = i % 25 == 24,          // 2 soft-deleted users
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

            var role = i == 0 ? roles[0]                       // user 0 is the admin
                     : i < salesRepCount ? roles[1]            // users 1-19 are sales reps
                     : roles[2 + i % 2];                       // the rest: support or viewer
            user.UserRoles.Add(new UserRole { Id = Guid.CreateVersion7(), RoleId = role.Id });

            users.Add(user);
        }
        db.Users.AddRange(users);
        await SaveAsync(db);
        Console.WriteLine($"  roles: {roles.Count}, users: {users.Count}");

        var salesReps = users.Take(salesRepCount).ToList();

        // ---- 2. Customers ------------------------------------------------------------
        var customers = new List<Customer>();
        for (var i = 0; i < customerCount; i++)
        {
            var first = faker.Name.FirstName();
            var last = faker.Name.LastName();
            customers.Add(new Customer
            {
                Id = Guid.CreateVersion7(),
                Name = $"{first} {last}",
                Email = $"{first}.{last}.{i}@example.com".ToLowerInvariant(),   // {i} keeps emails unique
                Country = faker.Address.Country(),
                City = faker.Address.City(),
                IsDeleted = i % 100 == 99,                                       // about 1% soft-deleted
                CreatedAt = Now.AddDays(-1000 + random.Next(0, 900))
            });
        }
        db.Customers.AddRange(customers);
        await SaveAsync(db);
        Console.WriteLine($"  customers: {customers.Count:N0}");

        // ---- 3. Products -------------------------------------------------------------
        var products = new List<Product>();
        for (var i = 0; i < productCount; i++)
        {
            products.Add(new Product
            {
                Id = Guid.CreateVersion7(),
                Sku = $"SKU-{i:D5}",
                Name = faker.Commerce.ProductName(),
                Category = faker.Commerce.Department(),
                UnitPrice = random.Next(500, 50_000) / 100m,    // 5.00 to 500.00
                IsActive = i % 20 != 19,                         // 5% inactive
                CreatedAt = Now.AddDays(-1000)
            });
        }
        db.Products.AddRange(products);
        await SaveAsync(db);
        Console.WriteLine($"  products: {products.Count:N0}");

        // ---- 4. Orders, each with its lines and notes ---------------------------------
        // Saved in batches of 5,000 orders so memory stays small and each SaveChanges stays fast.
        const int batchSize = 5_000;
        var lineTotal = 0;
        var noteTotal = 0;

        for (var start = 0; start < orderCount; start += batchSize)
        {
            var end = Math.Min(start + batchSize, orderCount);
            var batch = new List<Order>();

            for (var i = start; i < end; i++)
            {
                var order = CreateOrder(i, random, customers, products, salesReps, users);
                lineTotal += order.OrderProducts.Count;
                noteTotal += order.OrderNotes.Count;
                batch.Add(order);
            }

            db.Orders.AddRange(batch);      // also adds each order's lines and notes
            await SaveAsync(db);

            if (end % 50_000 == 0 || end == orderCount)
                Console.WriteLine($"  orders: {end:N0} / {orderCount:N0}");
        }

        Console.WriteLine($"  order lines: {lineTotal:N0}, order notes: {noteTotal:N0}");
    }

    private static Order CreateOrder(
        int index, Random random, List<Customer> customers, List<Product> products,
        List<User> salesReps, List<User> users)
    {
        var orderDate = Now.AddMinutes(-random.Next(0, 3 * 365 * 24 * 60));    // somewhere in the last 3 years

        var order = new Order
        {
            Id = Guid.CreateVersion7(),
            CustomerId = customers[random.Next(customers.Count)].Id,
            SalesRepId = index % 20 == 0 ? null : salesReps[random.Next(salesReps.Count)].Id,   // 5% unassigned
            OrderNumber = $"ORD-{index + 1:D7}",
            Status = Statuses[random.Next(Statuses.Length)],
            OrderDate = orderDate,
            CreatedAt = orderDate
        };

        // 1 to 5 order lines, each for a different product.
        var lineCount = random.Next(1, 6);
        var chosen = new HashSet<int>();
        while (chosen.Count < lineCount)
            chosen.Add(random.Next(products.Count));

        foreach (var productIndex in chosen)
        {
            var product = products[productIndex];
            var quantity = random.Next(1, 6);
            order.OrderProducts.Add(new OrderProduct
            {
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = product.UnitPrice
            });
            order.TotalAmount += quantity * product.UnitPrice;
        }

        // 0 to 4 notes written by random users.
        var noteCount = random.Next(0, 5);
        for (var n = 0; n < noteCount; n++)
        {
            order.OrderNotes.Add(new OrderNote
            {
                Id = Guid.CreateVersion7(),
                AuthorId = users[random.Next(users.Count)].Id,
                Text = NoteTexts[random.Next(NoteTexts.Length)],
                CreatedAt = orderDate.AddMinutes(random.Next(5, 60 * 24 * 10))
            });
        }

        return order;
    }

    /// <summary>Save, then forget everything EF was tracking so memory doesn't grow batch after batch.</summary>
    private static async Task SaveAsync(AppDbContext db)
    {
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }
}
