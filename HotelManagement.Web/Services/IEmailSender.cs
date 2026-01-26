using System.Threading.Tasks;

namespace HotelManagement.Web.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
