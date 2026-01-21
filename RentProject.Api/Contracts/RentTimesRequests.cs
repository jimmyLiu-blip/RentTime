namespace RentProject.Api.Contracts
{
    public sealed record UserRequest(string User);

    public sealed record ChangePeriodRequest(DateTime NewStart, DateTime NewEnd, string User);

    public sealed record CopyRequest(bool IsHandOver, string User);
}
