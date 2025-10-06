using HotelReservation.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelReservation.API.DL.Repositories
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly ILogger<GenericRepository<TEntity>> _logger;

        public GenericRepository(ApplicationDbContext context, ILogger<GenericRepository<TEntity>> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IQueryable<TEntity> GetAll()
        {
            _logger.LogInformation("Getting all entities of type {EntityType}", typeof(TEntity).Name);
            return _context.Set<TEntity>().AsNoTracking();
        }

        public async Task AddAsync(TEntity entity)
        {
            _logger.LogInformation("Adding new entity of type {EntityType}", typeof(TEntity).Name);
            await _context.Set<TEntity>().AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task Update(TEntity entity)
        {
            _logger.LogInformation("Updating entity of type {EntityType}", typeof(TEntity).Name);
            _context.Set<TEntity>().Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task Remove(TEntity entity)
        {
            _logger.LogInformation("Removing entity of type {EntityType}", typeof(TEntity).Name);
            _context.Set<TEntity>().Remove(entity);
            await _context.SaveChangesAsync();
        }

    }

}
