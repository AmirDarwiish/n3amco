using System.Threading;
using System.Threading.Tasks;

namespace CourseCenter.Api.CustomerHistory
{
    public class CustomerHistoryLogger
    {
        private readonly ApplicationDbContext _context;

        public CustomerHistoryLogger(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task LogAsync(CustomerHistory entry, CancellationToken cancellationToken = default)
        {
            _context.CustomerHistories.Add(entry);
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
