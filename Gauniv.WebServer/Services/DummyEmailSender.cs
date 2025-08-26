using Microsoft.AspNetCore.Identity;

namespace Gauniv.WebServer.Services
{
    public class DummyEmailSender<TUser> : IEmailSender<TUser> where TUser : class
    {
        public Task SendConfirmationLinkAsync(TUser user, string email, string confirmationLink)
        {
            return Task.CompletedTask;
        }

        public Task SendEmailAsync(TUser user, string subject, string htmlMessage)
        {
            return Task.CompletedTask;
        }

        public Task SendPasswordResetCodeAsync(TUser user, string email, string resetCode)
        {
            return Task.CompletedTask;
        }

        public Task SendPasswordResetLinkAsync(TUser user, string email, string resetLink)
        {
            return Task.CompletedTask;
        }
    }
}