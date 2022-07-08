using Books.API.Contexts;
using Books.API.Entities;
using Books.API.ExternalModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Books.API.Services
{
    public class BooksRepository : IBooksRepository, IDisposable
    {
        private BooksContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private CancellationTokenSource _cancellationTokenSource;

        public BooksRepository(BooksContext context,
            IHttpClientFactory httpClientFactory)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _httpClientFactory = httpClientFactory ??
                throw new ArgumentNullException(nameof(httpClientFactory));
        }


        public async Task<Book> GetBookAsync(Guid id)
        {
            await _context.Database.ExecuteSqlRawAsync("WAITFOR DELAY '00:00:02';");
            return await _context.Books
                .Include(b => b.Author).FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<IEnumerable<Book>> GetBooksAsync()
        {
            return await _context.Books.Include(b => b.Author).ToListAsync();
        }

        public void AddBook(Book bookToAdd)
        {
            if (bookToAdd == null)
            {
                throw new ArgumentNullException(nameof(bookToAdd));
            }

            _context.Add(bookToAdd);
        }

        public async Task<bool> SaveChangesAsync()
        {
            // return true if 1 or more entities were changed
            return (await _context.SaveChangesAsync() > 0);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_context != null)
                {
                    _context.Dispose();
                    _context = null;
                }

            }
        }

        public async Task<BookCover> GetBookCoverAsync(string coverId)
        {
            var httpClient = _httpClientFactory.CreateClient();
            // pass through a dummy name
            var response = await httpClient
                   .GetAsync($"http://localhost:52644/api/bookcovers/{coverId}");
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<BookCover>(
                    await response.Content.ReadAsStringAsync(),
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    });
            }

            return null;
        }

        private async Task<BookCover> DownloadBookCoverAsync(
            HttpClient httpClient, string bookCoverUrl,
            CancellationToken cancellationToken)
            {
                throw new Exception("Cannot download book cover, " +
                    "writer isn't finishing book fast enough.");

                var response = await httpClient
                           .GetAsync(bookCoverUrl, cancellationToken);

                if (response.IsSuccessStatusCode)
                    {
                        var bookCover = JsonSerializer.Deserialize<BookCover>(
                        await response.Content.ReadAsStringAsync(),
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                        });
                       return bookCover;
                    }

                _cancellationTokenSource.Cancel();
                return null;
                }

        public IEnumerable<Book> GetBooks()
        {
            _context.Database.ExecuteSqlRaw("WAITFOR DELAY '00:00:02';");
            return _context.Books.Include(b => b.Author).ToList();
        }

        public async Task<IEnumerable<Book>> GetBooksAsync(IEnumerable<Guid> bookIds)
        {
            return await _context.Books.Where(b => bookIds.Contains(b.Id))
                .Include(b => b.Author).ToListAsync();
        }

        public async Task<IEnumerable<BookCover>> GetBookCoversAsync(Guid bookId)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var bookCovers = new List<BookCover>();

            // create a list of fake bookcovers
            var bookCoverUrls = new[]
            {
                $"http://localhost:52644/api/bookcovers/{bookId}-dummycover1",
                $"http://localhost:52644/api/bookcovers/{bookId}-dummycover2?returnFault=true",
                $"http://localhost:52644/api/bookcovers/{bookId}-dummycover3",
                $"http://localhost:52644/api/bookcovers/{bookId}-dummycover4",
                $"http://localhost:52644/api/bookcovers/{bookId}-dummycover5"
            };

            // create the tasks
            var downloadBookCoverTasksQuery =
                 from bookCoverUrl
                 in bookCoverUrls
                 select DownloadBookCoverAsync(httpClient, bookCoverUrl,
                 _cancellationTokenSource.Token);

            // start the tasks
            var downloadBookCoverTasks = downloadBookCoverTasksQuery.ToList();

            return await Task.WhenAll(downloadBookCoverTasks);

            //foreach (var bookCoverUrl in bookCoverUrls)
            //{
            //    var response = await httpClient
            //       .GetAsync(bookCoverUrl);

            //    if (response.IsSuccessStatusCode)
            //    {
            //        bookCovers.Add(JsonSerializer.Deserialize<BookCover>(
            //            await response.Content.ReadAsStringAsync(),
            //            new JsonSerializerOptions
            //            {
            //                PropertyNameCaseInsensitive = true,
            //            }));
            //    }
            //}

            //return bookCovers;
        }
    }
}
