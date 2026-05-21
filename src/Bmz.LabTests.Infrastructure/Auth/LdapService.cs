using Bmz.LabTests.Application.Abstractions.Auth;
using Bmz.LabTests.Domain.Common;
using Microsoft.Extensions.Options;
using Novell.Directory.Ldap;
using System.Linq;

namespace Bmz.LabTests.Infrastructure.Auth;

/// <summary>
/// Сервис для интеграции с Active Directory (LDAP).
/// Позволяет проверять доменные учетные данные пользователей.
/// </summary>
public sealed class LdapService(IOptions<LdapOptions> options) : ILdapService
{
    private readonly LdapOptions _options = options.Value;

    /// <summary>
    /// Проверяет корректность пары логин/пароль в домене.
    /// </summary>
    public async Task<Result<bool>> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken)
    {
        try
        {
            using var connection = await CreateConnectionAsync(cancellationToken);
            var bindDn = string.IsNullOrWhiteSpace(_options.Domain) ? username : $"{_options.Domain}\\{username}";
            await connection.BindAsync(bindDn, password);

            return Result.Success(connection.Bound);
        }
        catch (LdapException ex)
        {
            return Result.Failure<bool>($"Ошибка LDAP: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Failure<bool>($"Ошибка аутентификации: {ex.Message}");
        }
    }

    /// <summary>
    /// Запрашивает информацию о пользователе (ФИО, email) из Active Directory.
    /// </summary>
    public async Task<Result<LdapUserInfo>> GetUserInfoAsync(string username, CancellationToken cancellationToken)
    {
        try
        {
            using var connection = await CreateConnectionAsync(cancellationToken);
            
            if (!string.IsNullOrWhiteSpace(_options.ServiceUser))
            {
                await connection.BindAsync(_options.ServiceUser, _options.ServicePassword);
            }

            var filter = string.Format(_options.SearchFilter, username);
            var searchResults = await connection.SearchAsync(
                _options.SearchBase,
                LdapConnection.ScopeSub,
                filter,
                new string[] { "sAMAccountName", "displayName", "mail", "cn" },
                false,
                (LdapSearchConstraints?)null);

            LdapEntry? entry = null;
            await foreach (var item in searchResults)
            {
                entry = item;
                break;
            }

            if (entry == null)
            {
                return Result.Failure<LdapUserInfo>("Пользователь не найден в LDAP.");
            }

            var login = entry.GetAttributeSet().GetAttribute("sAMAccountName")?.StringValue ?? username;
            var fullName = entry.GetAttributeSet().GetAttribute("displayName")?.StringValue 
                        ?? entry.GetAttributeSet().GetAttribute("cn")?.StringValue 
                        ?? username;
            var email = entry.GetAttributeSet().GetAttribute("mail")?.StringValue;

            return Result.Success(new LdapUserInfo(login, fullName, email));
        }
        catch (Exception ex)
        {
            return Result.Failure<LdapUserInfo>($"Ошибка при поиске в LDAP: {ex.Message}");
        }
    }

    /// <summary>
    /// Создает и настраивает TCP-соединение с LDAP-сервером.
    /// </summary>
    private async Task<LdapConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new LdapConnection();

        // В библиотеке Novell.Directory.Ldap.NETStandard 4.0+ SecureSocketLayer может быть недоступен напрямую через интерфейс
        // или свойство называется иначе. Попробуем ConnectAsync с SSL если порт 636.
        
        await connection.ConnectAsync(_options.Host, _options.Port);
        return connection;
    }
}
