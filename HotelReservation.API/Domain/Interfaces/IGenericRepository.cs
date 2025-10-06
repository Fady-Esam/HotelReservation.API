namespace HotelReservation.API.Domain.Interfaces
{
    public interface IGenericRepository<TEntity> where TEntity : class
    {

        Task AddAsync(TEntity entity);

        Task Update(TEntity entity);

        Task Remove(TEntity entity);

        IQueryable<TEntity> GetAll();
    }
}
