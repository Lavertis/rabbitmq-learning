using Common.Config;
using Microsoft.Extensions.Options;

namespace Common.Services;

public class AccountService
{
    private readonly List<string> _accounts = new();
    private readonly MyConfig _config;

    public AccountService(IOptions<MyConfig> config)
    {
        _config = config.Value;
        for (var i = 0; i < 3; i++)
        {
            _accounts.Add(_config.CurrentBank + '_' + i);
        }
    }

    public List<string> GetAllAccounts()
    {
        return _accounts;
    }

    public bool AccountExistsByNumber(string accountNumber)
    {
        return _accounts.Contains(accountNumber);
    }
}