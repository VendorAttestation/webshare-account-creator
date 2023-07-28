public class FailedCaptcha : Exception
{
    public FailedCaptcha() : base("Failed To Solve Recaptcha")
    {
    }
}