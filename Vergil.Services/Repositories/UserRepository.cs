﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vergil.Data.Context;
using Vergil.Data.Models;

namespace Vergil.Services.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserById(string id);
    Task<User?> Register(User user);
    Task Transact(User user, decimal balance);
}

public class UserRepository : IUserRepository
{
    private readonly VergilDbContext _context;

    public UserRepository(VergilDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserById(string id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.DiscordId == id);

        Console.WriteLine("Fetching changes...");
        await _context.Entry(user).ReloadAsync();
        
        return user;
    }

    public async Task<User?> Register(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task Transact(User user, decimal balance)
    {
        user.Balance = balance;
        await _context.SaveChangesAsync();
    }
    
}