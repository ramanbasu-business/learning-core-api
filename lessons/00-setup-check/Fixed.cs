using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace SetupCheck;

/// <summary>The shape of the single row the raw SQL returns (column aliases must match the property names after the snake_case naming convention is applied).</summary>
public sealed class CountsRow
{
    public long Customers { get; set; }
    public long Products { get; set; }
    public long Orders { get; set; }
    public long OrderProducts { get; set; }
    public long OrderNotes { get; set; }
    public long Users { get; set; }
    public long Roles { get; set; }
    public long UserRoles { get; set; }
}

public static class Fixed
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDbContext();

        // One round trip for everything. Raw SQL does not apply the global query filter,
        // so the soft-delete condition on users is written out by hand.
        var row = await db.Database.SqlQueryRaw<CountsRow>("""
            SELECT
                (SELECT count(*) FROM customers)                  AS customers,
                (SELECT count(*) FROM products)                   AS products,
                (SELECT count(*) FROM orders)                     AS orders,
                (SELECT count(*) FROM order_products)             AS order_products,
                (SELECT count(*) FROM order_notes)                AS order_notes,
                (SELECT count(*) FROM users WHERE NOT is_deleted) AS users,
                (SELECT count(*) FROM roles)                      AS roles,
                (SELECT count(*) FROM user_roles)                 AS user_roles
            """).SingleAsync();

        return Naive.Describe(row.Customers, row.Products, row.Orders, row.OrderProducts,
                              row.OrderNotes, row.Users, row.Roles, row.UserRoles);
    }
}
