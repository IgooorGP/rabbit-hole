using System.Linq;

namespace Rabbitcs.Infra
{
    /// <summary>
    /// Parameters used for pagination.
    /// </summary>
    public class PaginationParams
    {
        /// <summary>
        /// Constraints for pagination: max, min and default values
        /// </summary>
        private const int MAX_PAGE_SIZE = 50;
        private const int MIN_PAGE_SIZE = 1;
        private const int MIN_PAGE_NUMBER = 1;
        private const int DEFAULT_PAGE_SIZE = 10;
        private const int DEFAULT_PAGE_NUMBER = 1;

        private int _pageSize = DEFAULT_PAGE_SIZE;
        private int _pageNumber = DEFAULT_PAGE_NUMBER;

        /// <summary>
        /// Checks if the page size contains a valid number and sets it accordingly.
        /// </summary>        
        public int PageSize
        {
            get
            {
                return _pageSize;
            }
            set
            {
                if (value >= MAX_PAGE_SIZE)
                {
                    _pageSize = MAX_PAGE_SIZE;
                }
                else if (value < MIN_PAGE_SIZE)
                {
                    _pageSize = MIN_PAGE_SIZE;
                }
                else
                {
                    _pageSize = value;
                }
            }
        }
        /// <summary>
        /// Checks if the page number contains a valid number and sets it accordingly.
        /// </summary>
        public int PageNumber
        {
            get
            {
                return _pageNumber;
            }
            set
            {
                if (value < MIN_PAGE_NUMBER)
                {
                    _pageNumber = DEFAULT_PAGE_NUMBER;
                }
                else
                {
                    _pageNumber = value;
                }
            }
        }
        public int ItemsToSkip
        {
            get { return (PageNumber - 1) * PageSize; }
        }
    }
    /// <summary>
    /// Extension used to skip database items from the real database query in order to paginate results.
    /// </summary>    
    public static class PaginationExtension
    {
        public static IQueryable<T> GetPage<T>(this IOrderedQueryable<T> query, PaginationParams paginationParams)
        {
            return query.Skip(paginationParams.ItemsToSkip).Take(paginationParams.PageSize);
        }
    }
}