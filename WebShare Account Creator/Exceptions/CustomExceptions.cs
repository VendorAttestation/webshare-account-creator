public class FailedCaptcha : Exception
{
    public FailedCaptcha() : base("Failed To Solver Recaptcha")
    {
    }
}