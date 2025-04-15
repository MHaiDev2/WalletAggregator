using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Nethereum.Web3;
using WalletAggregator.Models;

namespace WalletAggregator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance([FromQuery] string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return BadRequest("Address is required.");

        try
        {
            var rpcUrl = "https://rpc-amoy.polygon.technology/";
            var web3 = new Web3(rpcUrl);

            var balanceWei = await web3.Eth.GetBalance.SendRequestAsync(address);
            var balanceEth = Web3.Convert.FromWei(balanceWei);

            return Ok(new
            {
                address,
                balance = balanceEth
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error fetching balance: {ex.Message}");
        }
    }

    [HttpPost("aggregate")]
    public async Task<IActionResult> AggregateBalances([FromBody] WalletAggregateRequest request)
    {
        if (request.Addresses == null || !request.Addresses.Any())
            return BadRequest("At least one address must be provided.");

        var ethAddressRegex = new Regex(@"^0x[a-fA-F0-9]{40}$");

        var invalidAddresses = request.Addresses
            .Where(addr => !ethAddressRegex.IsMatch(addr))
            .ToList();

        if (invalidAddresses.Any())
        {
            return BadRequest(new
            {
                error = "One or more addresses are invalid.",
                invalidAddresses
            });
        }

        try
        {
            var rpcUrl = "https://rpc-amoy.polygon.technology/";
            var web3 = new Web3(rpcUrl);

            decimal totalBalance = 0;
            var nonZeroAddresses = new List<string>();

            foreach (var address in request.Addresses)
            {
                var balanceWei = await web3.Eth.GetBalance.SendRequestAsync(address);
                var balanceEth = Web3.Convert.FromWei(balanceWei);

                if (balanceEth > 0)
                {
                    totalBalance += balanceEth;
                    nonZeroAddresses.Add(address);
                }
            }

            return Ok(new
            {
                addresses = nonZeroAddresses,
                totalBalance
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error aggregating balances: {ex.Message}");
        }
    }

    [HttpGet("metadata")]
    public async Task<IActionResult> GetMetadata()
    {
        try
        {
            var rpcUrl = "https://rpc-amoy.polygon.technology/";
            var web3 = new Web3(rpcUrl);

            var chainId = await web3.Net.Version.SendRequestAsync();
            var blockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();

            var metadata = new WalletMetadataResponse
            {
                NetworkName = "Polygon Amoy Testnet",
                RpcUrl = rpcUrl,
                ChainId = chainId,
                BlockHeight = (ulong)blockNumber.Value
            };

            return Ok(metadata);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Failed to get network metadata: {ex.Message}");
        }
    }



}
