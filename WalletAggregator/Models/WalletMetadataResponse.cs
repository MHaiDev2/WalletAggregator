namespace WalletAggregator.Models;

public class WalletMetadataResponse
{
    public string NetworkName { get; set; } = string.Empty;
    public string RpcUrl { get; set; } = string.Empty;
    public string ChainId { get; set; } = string.Empty;
    public ulong BlockHeight { get; set; }
}
