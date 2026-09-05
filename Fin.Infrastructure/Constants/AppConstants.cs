namespace Fin.Infrastructure.Constants;

public static class AppConstants
{
    public const string AppName = "Fin App";
    public const string FrontUrlConfigKey = "ApiSettings:FrontendConfigs:Url";
    public const string VersionConfigKey = "ApiSettings:Version";

    /// Portfolio/demo mode: disables Google login, real email sending and password reset,
    /// and enables the recurring cleanup of non-admin tenants.
    public const string DemoModeConfigKey = "ApiSettings:DemoMode";
}