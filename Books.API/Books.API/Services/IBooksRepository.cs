﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Books.API.Services
{
    public interface IBooksRepository
    {
        IEnumerable<Entities.Book> GetBooks();

        //Entities.Book GetBook(Guid id);

        Task<IEnumerable<Entities.Book>> GetBooksAsync();

        Task<Entities.Book> GetBookAsync(Guid id);
        Task<IEnumerable<Entities.Book>> GetBooksAsync(IEnumerable<Guid> bookIds);

        void AddBook(Entities.Book bookToAdd);

        Task<bool> SaveChangesAsync();
    }
}
