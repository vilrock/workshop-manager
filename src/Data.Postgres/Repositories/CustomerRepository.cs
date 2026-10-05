using Dapper;
using Domain.Common;
using Domain.Models;
using Domain.Repositories;

namespace Data.Postgres.Repositories;

public sealed class CustomerRepository(PostgresSession session) : ICustomerRepository
{
    private const string SelectColumns = """
        SELECT c.customer_id, c.full_name, c.email, c.phone, c.address, c.is_active, c.created_at AS created_at_utc, c.updated_at AS updated_at_utc,
            (SELECT count(*) FROM vehicles v WHERE v.customer_id = c.customer_id)::int AS vehicle_count
        FROM customers c
        """;

    private const string SearchFilter = """
        (@Pattern::text IS NULL
            OR c.full_name ILIKE @Pattern ESCAPE '!'
            OR c.email ILIKE @Pattern ESCAPE '!'
            OR c.phone ILIKE @Pattern ESCAPE '!')
        """;

    public async Task<PagedResult<Customer>> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var parameters = new { Pattern = LikePattern.FromSearch(search), PageSize = pageSize, Offset = (page - 1) * pageSize };
        var sql = $"""
            SELECT count(*)::int FROM customers c WHERE {SearchFilter};
            {SelectColumns} WHERE {SearchFilter} ORDER BY c.full_name, c.customer_id LIMIT @PageSize OFFSET @Offset;
            """;

        using var grid = await session.QueryMultipleAsync(sql, parameters, cancellationToken);
        var totalCount = await grid.ReadSingleAsync<int>();
        var items = (await grid.ReadAsync<Customer>()).AsList();
        return new PagedResult<Customer>(items, page, pageSize, totalCount);
    }

    public Task<Customer?> GetAsync(Guid customerId, CancellationToken cancellationToken) =>
        session.QuerySingleOrDefaultAsync<Customer>($"{SelectColumns} WHERE c.customer_id = @CustomerId", new { CustomerId = customerId }, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, Guid? excludedCustomerId, CancellationToken cancellationToken) =>
        session.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM customers WHERE lower(email) = @Email AND (@ExcludedCustomerId::uuid IS NULL OR customer_id <> @ExcludedCustomerId))",
            new { Email = email, ExcludedCustomerId = excludedCustomerId },
            cancellationToken);

    public async Task CreateAsync(Customer customer, CancellationToken cancellationToken) =>
        await session.ExecuteAsync(
            """
            INSERT INTO customers (customer_id, full_name, email, phone, address, is_active, created_at, updated_at)
            VALUES (@CustomerId, @FullName, @Email, @Phone, @Address, @IsActive, @CreatedAtUtc, @UpdatedAtUtc)
            """,
            customer,
            cancellationToken);

    public async Task<bool> UpdateAsync(Customer customer, CancellationToken cancellationToken) =>
        await session.ExecuteAsync(
            """
            UPDATE customers
            SET full_name = @FullName, email = @Email, phone = @Phone, address = @Address, is_active = @IsActive, updated_at = @UpdatedAtUtc
            WHERE customer_id = @CustomerId
            """,
            customer,
            cancellationToken) == 1;
}
