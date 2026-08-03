namespace LogisticsSystem.Application.Authorization
{
    public static class Permissions
    {
        public static class Shipments
        {
            public const string Create = "Shipments.Create";
            public const string View = "Shipments.View";
            public const string ViewAll = "Shipments.ViewAll";
            public const string Update = "Shipments.Update";
            public const string Delete = "Shipments.Delete";
            public const string Cancel = "Shipments.Cancel";
        }

        public static class Drivers
        {
            public const string View = "Drivers.View";
            public const string ViewAll = "Drivers.ViewAll";
            public const string UpdateStatus = "Drivers.UpdateStatus";
            public const string Manage = "Drivers.Manage";
        }

        public static class Dispatch
        {
            public const string AssignDriver = "Dispatch.AssignDriver";
            public const string Manage = "Dispatch.Manage";
        }

        public static class Notifications
        {
            public const string View = "Notifications.View";
            public const string Send = "Notifications.Send";
        }

        public static class Users
        {
            public const string View = "Users.View";
            public const string Manage = "Users.Manage";
        }

        public static class Dashboard
        {
            public const string View = "Dashboard.View";
        }
    }
}
