namespace LogisticsSystem.Application.Authorization
{
    public static class Policies
    {
        public const string ShipmentCreate = Permissions.Shipments.Create;
        public const string ShipmentView = Permissions.Shipments.View;
        public const string ShipmentViewAll = Permissions.Shipments.ViewAll;
        public const string ShipmentUpdate = Permissions.Shipments.Update;
        public const string ShipmentDelete = Permissions.Shipments.Delete;
        public const string ShipmentCancel = Permissions.Shipments.Cancel;

        public const string DriverView = Permissions.Drivers.View;
        public const string DriverViewAll = Permissions.Drivers.ViewAll;
        public const string DriverUpdateStatus = Permissions.Drivers.UpdateStatus;
        public const string DriverManage = Permissions.Drivers.Manage;

        public const string DispatchAssignDriver = Permissions.Dispatch.AssignDriver;
        public const string DispatchManage = Permissions.Dispatch.Manage;

        public const string NotificationView = Permissions.Notifications.View;
        public const string NotificationSend = Permissions.Notifications.Send;

        public const string UserView = Permissions.Users.View;
        public const string UserManage = Permissions.Users.Manage;

        public const string DashboardView = Permissions.Dashboard.View;
    }
}
