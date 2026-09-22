using System.Threading.Tasks;

namespace RimerApi.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}
