using EfCoreLab.Common.Running;
using Microsoft.EntityFrameworkCore;

namespace SetupCheck;

public static class Naive
{
    public static async Task<LessonOutcome> RunAsync(LessonContext ctx)
    {
        await using var db = ctx.NewDb();

        // One round trip per table: 8 queries.
        var customers = await db.Customers.CountAsync();
        var products = await db.Products.CountAsync();
        var orders = await db.Orders.CountAsync();
        var orderProducts = await db.OrderProducts.CountAsync();
        var orderNotes = await db.OrderNotes.CountAsync();
        var users = await db.Users.CountAsync();          // the global query filter hides soft-deleted users
        var roles = await db.Roles.CountAsync();
        var userRoles = await db.UserRoles.CountAsync();

        return Describe(customers, products, orders, orderProducts, orderNotes, users, roles, userRoles);
    }

    internal static LessonOutcome Describe(
        long customers, long products, long orders, long orderProducts,
        long orderNotes, long users, long roles, long userRoles) =>
        new(
            Rows: (int)(customers + products + orders + orderProducts + orderNotes + users + roles + userRoles),
            Fingerprint: $"{customers},{products},{orders},{orderProducts},{orderNotes},{users},{roles},{userRoles}",
            Note: $"customers={customers:N0} products={products:N0} orders={orders:N0} order_products={orderProducts:N0} " +
                  $"order_notes={orderNotes:N0} users={users:N0} roles={roles:N0} user_roles={userRoles:N0}");
}
