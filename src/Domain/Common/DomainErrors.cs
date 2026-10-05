using Domain.Enums;

namespace Domain.Common;

public static class DomainErrors
{
    public static class Auth
    {
        public static readonly Error InvalidCredentials = Error.Unauthorized("auth.invalid_credentials", "The email or password is incorrect.");

        public static readonly Error InactiveUser = Error.Unauthorized("auth.inactive_user", "This account is disabled.");

        public static readonly Error UserNotFound = Error.Unauthorized("auth.user_not_found", "The authenticated user no longer exists.");

        public static readonly Error NotAuthenticated = Error.Unauthorized("auth.not_authenticated", "Authentication is required.");
    }

    public static class Customers
    {
        public static readonly Error NotFound = Error.NotFound("customers.not_found", "The customer was not found.");

        public static readonly Error EmailAlreadyExists = Error.Conflict("customers.email_exists", "Another customer already uses this email.");
    }

    public static class Vehicles
    {
        public static readonly Error NotFound = Error.NotFound("vehicles.not_found", "The vehicle was not found.");

        public static readonly Error LicensePlateAlreadyExists = Error.Conflict("vehicles.plate_exists", "Another vehicle already uses this license plate.");

        public static readonly Error DoesNotBelongToCustomer = Error.BusinessRule("vehicles.customer_mismatch", "The vehicle does not belong to the selected customer.");
    }

    public static class WorkOrders
    {
        public static readonly Error NotFound = Error.NotFound("work_orders.not_found", "The work order was not found.");

        public static readonly Error NotAssignedToUser = Error.Forbidden("work_orders.not_assigned", "You can only access work orders assigned to you.");

        public static readonly Error ConcurrentModification = Error.Conflict("work_orders.concurrent_modification", "The work order was changed by someone else. Reload it and try again.");

        public static readonly Error MechanicNotFound = Error.NotFound("work_orders.mechanic_not_found", "The selected mechanic was not found or is inactive.");

        public static readonly Error IdempotencyKeyReused = Error.BusinessRule("work_orders.idempotency_key_reused", "The Idempotency-Key was already used with a different request.");

        public static readonly Error IdempotencyKeyRequired = Error.Validation("work_orders.idempotency_key_required", "The Idempotency-Key header is required.");

        public static Error TransitionNotAllowed(WorkOrderStatus from, WorkOrderStatus to) =>
            Error.BusinessRule("work_orders.invalid_transition", $"A work order cannot move from {from} to {to}.");

        public static Error TransitionForbidden(string role, WorkOrderStatus from, WorkOrderStatus to) =>
            Error.Forbidden("work_orders.transition_forbidden", $"The {role} role cannot move a work order from {from} to {to}.");

        public static readonly Error MechanicRequired = Error.BusinessRule("work_orders.mechanic_required", "Assign a mechanic before moving this work order forward.");

        public static readonly Error ItemsRequired = Error.BusinessRule("work_orders.items_required", "Add at least one line item before approving this work order.");

        public static readonly Error ItemsLocked = Error.BusinessRule("work_orders.items_locked", "Line items can no longer be added to this work order.");

        public static readonly Error AssignmentLocked = Error.BusinessRule("work_orders.assignment_locked", "The mechanic can no longer be changed on this work order.");
    }
}
