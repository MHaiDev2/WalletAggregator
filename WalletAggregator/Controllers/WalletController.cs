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

        try
        {
            var rpcUrl = "https://rpc-amoy.polygon.technology/";
            var web3 = new Web3(rpcUrl);

            decimal totalBalance = 0;

            foreach (var address in request.Addresses)
            {
                var balanceWei = await web3.Eth.GetBalance.SendRequestAsync(address);
                totalBalance += Web3.Convert.FromWei(balanceWei);
            }

            return Ok(new
            {
                addresses = request.Addresses,
                totalBalance
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error aggregating balances: {ex.Message}");
        }
    }

}
