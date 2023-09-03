using Microsoft.EntityFrameworkCore;
using VergilBot.Models;
using VergilBot.Services.Context;

namespace VergilBot.Repositories;

public class UserRepository : IUserRepository
{
    private readonly VergilDbContext _context;

    public UserRepository(VergilDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserById(string id)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.DiscordId == id);
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

public interface IUserRepository
{
    Task<User?> GetUserById(string id);
    Task<User?> Register(User user);
    Task Transact(User user, decimal balance);
}