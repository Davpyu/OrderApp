/*
    ! Important: The permission constant should be sync with "/SeedersData/Permission.json"
*/
namespace DotNetService.Constants.Permission
{
    public static class PermissionConstant
    {
        public const string ALL = "all";
        public const string DEPRECATED = "deprecated";

        /* ----------------------------- User Management ---------------------------- */
        public const string USER_VIEW = "user-view";
        public const string USER_CREATE = "user-create";
        public const string USER_UPDATE = "user-update";
        public const string USER_DELETE = "user-delete";

        /* ----------------------------- Role Management ---------------------------- */
        public const string ROLE_VIEW = "role-view";
        public const string ROLE_CREATE = "role-create";
        public const string ROLE_UPDATE = "role-update";
        public const string ROLE_DELETE = "role-delete";

        /* -------------------------- Permission Management ------------------------- */
        public const string PERMISSION_VIEW = "permission-view";
        public const string PERMISSION_CREATE = "permission-create";
        public const string PERMISSION_UPDATE = "permission-update";
        public const string PERMISSION_DELETE = "permission-delete";

        /* -------------------------- Order Management ------------------------- */
        public const string ORDER_VIEW = "order-view";
        public const string ORDER_CREATE = "order-create";
        public const string ORDER_UPDATE = "order-update";
        public const string ORDER_DELETE = "order-delete";
    }
}