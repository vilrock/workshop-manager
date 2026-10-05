using Domain.Abstractions;
using Domain.Common;
using Domain.Models;
using Facade.Contracts;
using Service.Common;
using Service.Queries.GetDashboardStats;
using Service.Queries.Login;

namespace Facade.Mapping;

public static class ContractMapper
{
    public static UserResponse ToResponse(this User user) => new(user.UserId, user.FullName, user.Email, user.Role);

    public static MechanicResponse ToMechanicResponse(this User user) => new(user.UserId, user.FullName);

    public static LoginResponse ToResponse(this LoginResult result) =>
        new(result.Token.Value, "Bearer", result.Token.ExpiresAtUtc, result.User.ToResponse());

    public static CustomerResponse ToResponse(this Customer customer) =>
        new(customer.CustomerId, customer.FullName, customer.Email, customer.Phone, customer.Address, customer.IsActive, customer.VehicleCount, customer.CreatedAtUtc);

    public static VehicleResponse ToResponse(this Vehicle vehicle) =>
        new(vehicle.VehicleId, vehicle.CustomerId, vehicle.CustomerName, vehicle.LicensePlate, vehicle.Make, vehicle.Model, vehicle.Year, vehicle.Vin, vehicle.Color, vehicle.MileageKm);

    public static PagedResponse<TResponse> ToResponse<TItem, TResponse>(this PagedResult<TItem> page, Func<TItem, TResponse> map) =>
        new(page.Items.Select(map).ToList(), page.Page, page.PageSize, page.TotalCount, page.TotalPages);

    public static WorkOrderSummaryResponse ToResponse(this WorkOrderSummaryView view)
    {
        var summary = view.Summary;
        return new WorkOrderSummaryResponse(
            summary.WorkOrderId,
            FormatOrderNumber(summary.OrderNumber),
            summary.Status,
            summary.CustomerId,
            summary.CustomerName,
            summary.VehicleId,
            summary.VehicleDescription,
            summary.Description,
            summary.AssignedMechanicId,
            summary.AssignedMechanicName,
            summary.TotalAmount,
            summary.CreatedAtUtc,
            summary.UpdatedAtUtc,
            view.AllowedTransitions);
    }

    public static WorkOrderDetailResponse ToResponse(this WorkOrderDetailView view)
    {
        var detail = view.Detail;
        return new WorkOrderDetailResponse(
            detail.WorkOrderId,
            FormatOrderNumber(detail.OrderNumber),
            detail.Status,
            detail.CustomerId,
            detail.CustomerName,
            detail.VehicleId,
            detail.VehicleDescription,
            detail.Description,
            detail.AssignedMechanicId,
            detail.AssignedMechanicName,
            detail.TotalAmount,
            detail.CreatedAtUtc,
            detail.UpdatedAtUtc,
            view.AllowedTransitions,
            detail.MileageKm,
            detail.CreatedByName,
            detail.CompletedAtUtc,
            detail.Items.Select(ToResponse).ToList(),
            detail.History.Select(ToResponse).ToList(),
            view.CanAssignMechanic,
            view.CanAddItems);
    }

    public static DashboardStatsResponse ToResponse(this DashboardView view) =>
        new(
            view.Stats.OrdersByStatus.Select(entry => new StatusCountResponse(entry.Status, entry.Count)).ToList(),
            view.Stats.OpenOrders,
            view.Stats.CompletedThisMonth,
            view.Stats.MonthlyRevenue,
            view.Stats.AverageHoursToComplete,
            view.Stats.AverageHoursToDiagnose,
            view.Stats.RevenueByMonth.Select(entry => new MonthRevenueResponse(entry.Month, entry.Revenue)).ToList(),
            view.RecentOrders.Select(order => order.ToResponse()).ToList());

    private static LineItemResponse ToResponse(WorkOrderLineItem item) =>
        new(item.LineItemId, item.ItemType, item.Description, item.Quantity, item.UnitPrice, item.LineTotal);

    private static HistoryEntryResponse ToResponse(WorkOrderHistoryEntry entry) =>
        new(entry.HistoryId, entry.FromStatus, entry.ToStatus, entry.ChangedByUserId, entry.ChangedByName, entry.ChangedAtUtc, entry.CorrelationId, entry.Note);

    private static string FormatOrderNumber(int orderNumber) => $"WO-{orderNumber}";
}
