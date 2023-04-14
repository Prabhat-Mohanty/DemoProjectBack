using EmailVerrificationService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailVerrificationService.Services
{
    public interface IEmailService
    {
        void SendEmail(Message message);
    }
}
