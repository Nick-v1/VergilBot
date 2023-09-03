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
        return user;
        
    }

   
}

public interface IUserRepository
{
    Task<User?> GetUserById(string id);
    Task<User?> Register(User user);
}