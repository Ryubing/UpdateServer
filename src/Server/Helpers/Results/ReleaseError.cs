using Gommon;

namespace Ryujinx.Systems.Update.Server.Helpers.Results;

public readonly struct ReleaseError(ReleaseError.ReleaseErrorType error) : IErrorState
{
    public ReleaseErrorType Error { get; } = error;
    
    public override string ToString() => Error.ToString();
    
    public enum ReleaseErrorType
    {
        NotInitialized,
        RateLimited,
    }
}