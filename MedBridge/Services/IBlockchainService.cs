using MedBridge.Models;

namespace MedBridge.Services;

public interface IBlockchainService
{
    Task<BlockchainBlock> AddBlockAsync(string action, string userId, string userName, string role, object? data = null);
    Task<bool> ValidateChainAsync();
    Task<IEnumerable<BlockchainBlock>> GetChainAsync(int page = 1, int pageSize = 50);
    string ComputeHash(BlockchainBlock block);
    bool IsBlockValid(BlockchainBlock block, BlockchainBlock? previousBlock);
}
