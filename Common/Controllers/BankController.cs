using Common.Entities;
using Common.Models;
using Common.Services;
using Microsoft.AspNetCore.Mvc;

namespace Common.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BankController : ControllerBase
{
    private readonly AccountService _accountService;
    private readonly TransferService _transferService;

    public BankController(TransferService transferService, AccountService accountService)
    {
        _transferService = transferService;
        _accountService = accountService;
    }

    [HttpGet("transfers")]
    public ActionResult<IList<Transfer>> GetAllTransfers()
    {
        return Ok(_transferService.GetAllTransfers());
    }

    [HttpPost("transfers/new")]
    public void SendTransfer(SendTransferRequest request)
    {
        _transferService.SendTransfer(request);
    }

    [HttpGet("accounts")]
    public ActionResult<IList<Transfer>> GetAllAccounts()
    {
        return Ok(_accountService.GetAllAccounts());
    }
}