using System;

namespace QRTracker.Services;

public class AuthStateChangedEventArgs : EventArgs
{
    public AuthStateChangedEventArgs(bool isAuthenticated, string? accessToken, string? accountUpn)
    {
        IsAuthenticated = isAuthenticated;
        AccessToken = accessToken;
        AccountUpn = accountUpn;
    }

    public bool IsAuthenticated { get; }
    public string? AccessToken { get; }
    public string? AccountUpn { get; }
}
