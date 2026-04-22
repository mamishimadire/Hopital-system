using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MedBridge.Data;
using MedBridge.Models;
using Microsoft.EntityFrameworkCore;

namespace MedBridge.Services;

public static class BlockchainCategory
{
    public const string System      = "System";
    public const string Medication  = "Medication";
    public const string Prescription = "Prescription";
    public const string Procurement = "Procurement";
    public const string UserMgmt    = "UserManagement";
    public const string HelpDesk    = "HelpDesk";
}

public class BlockchainService : IBlockchainService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    // Proof-of-work difficulty: hash must start with "000"
    private const string Difficulty = "000";

    public BlockchainService(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<BlockchainBlock> AddBlockAsync(
        string action, string userId, string userName, string role,
        object? data = null, string category = BlockchainCategory.System)
    {
        await _lock.WaitAsync();
        try
        {
            var lastBlock = await _db.BlockchainBlocks
                .OrderByDescending(b => b.BlockIndex)
                .FirstOrDefaultAsync();

            var newBlock = new BlockchainBlock
            {
                BlockIndex    = lastBlock == null ? 0 : lastBlock.BlockIndex + 1,
                PreviousHash  = lastBlock == null ? "0" : lastBlock.Hash,
                Timestamp     = DateTime.UtcNow,
                Data          = data != null ? JsonSerializer.Serialize(data) : "{}",
                UserId        = userId,
                UserName      = userName,
                Role          = role,
                Action        = action,
                Category      = category,
                Nonce         = 0
            };

            // Mine the block — find nonce so hash starts with difficulty prefix
            do
            {
                newBlock.Nonce++;
                newBlock.Hash = ComputeHash(newBlock);
            } while (!newBlock.Hash.StartsWith(Difficulty));

            // Sign the final hash with HMAC-SHA256
            newBlock.Signature = ComputeSignature(newBlock.Hash);

            _db.BlockchainBlocks.Add(newBlock);
            await _db.SaveChangesAsync();

            _db.AuditLogs.Add(new AuditLog
            {
                Timestamp             = newBlock.Timestamp,
                UserId                = userId,
                UserName              = userName,
                Role                  = role,
                Action                = action,
                BlockchainBlockIndex  = newBlock.BlockIndex,
                BlockchainHash        = newBlock.Hash
            });
            await _db.SaveChangesAsync();

            return newBlock;
        }
        finally
        {
            _lock.Release();
        }
    }

    public string ComputeHash(BlockchainBlock block)
    {
        var raw = $"{block.BlockIndex}{block.PreviousHash}{block.Timestamp:O}{block.Data}{block.UserId}{block.Action}{block.Nonce}";
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLower();
    }

    public string ComputeSignature(string hash)
    {
        var secret = _config["Blockchain:HmacSecret"] ?? "MedBridge-HMAC-Secret-2026";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var sig = hmac.ComputeHash(Encoding.UTF8.GetBytes(hash));
        return Convert.ToHexString(sig).ToLower()[..16]; // 16-char prefix for display
    }

    public async Task<bool> ValidateChainAsync()
    {
        var chain = await _db.BlockchainBlocks.OrderBy(b => b.BlockIndex).ToListAsync();
        for (int i = 0; i < chain.Count; i++)
        {
            var current  = chain[i];
            var previous = i > 0 ? chain[i - 1] : null;
            if (!IsBlockValid(current, previous)) return false;
        }
        return true;
    }

    public bool IsBlockValid(BlockchainBlock block, BlockchainBlock? previousBlock)
    {
        if (ComputeHash(block) != block.Hash) return false;
        if (previousBlock != null && block.PreviousHash != previousBlock.Hash) return false;
        return true;
    }

    public async Task<IEnumerable<BlockchainBlock>> GetChainAsync(int page = 1, int pageSize = 50)
    {
        return await _db.BlockchainBlocks
            .OrderByDescending(b => b.BlockIndex)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
