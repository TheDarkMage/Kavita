﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using API.Data;
using API.DTOs.Email;
using API.Entities.Enums;
using Flurl;
using Flurl.Http;
using Kavita.Common;
using Kavita.Common.EnvironmentInfo;
using Kavita.Common.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace API.Services;

public interface IEmailService
{
    Task SendConfirmationEmail(ConfirmationEmailDto data);
    Task<bool> CheckIfAccessible(string host);
    Task<bool> SendMigrationEmail(EmailMigrationDto data);
    Task<bool> SendPasswordResetEmail(PasswordResetEmailDto data);
    Task<bool> SendFilesToEmail(SendToDto data);
    Task<EmailTestResultDto> TestConnectivity(string emailUrl, string adminEmail, bool sendEmail);
    Task<bool> IsDefaultEmailService();
    Task SendEmailChangeEmail(ConfirmationEmailDto data);
    Task<string?> GetVersion(string emailUrl);
    bool IsValidEmail(string email);
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDownloadService _downloadService;

    /// <summary>
    /// This is used to initially set or reset the ServerSettingKey. Do not access from the code, access via UnitOfWork
    /// </summary>
    public const string DefaultApiUrl = "https://email.kavitareader.com";

    public EmailService(ILogger<EmailService> logger, IUnitOfWork unitOfWork, IDownloadService downloadService)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _downloadService = downloadService;


        FlurlHttp.ConfigureClient(DefaultApiUrl, cli =>
            cli.Settings.HttpClientFactory = new UntrustedCertClientFactory());
    }

    /// <summary>
    /// Test if this instance is accessible outside the network
    /// </summary>
    /// <remarks>This will do some basic filtering to auto return false if the emailUrl is a LAN ip</remarks>
    /// <param name="emailUrl"></param>
    /// <param name="sendEmail">Should an email be sent if connectivity is successful</param>
    /// <returns></returns>
    public async Task<EmailTestResultDto> TestConnectivity(string emailUrl, string adminEmail, bool sendEmail)
    {
        var result = new EmailTestResultDto();
        try
        {
            if (IsLocalIpAddress(emailUrl))
            {
                result.Successful = false;
                result.ErrorMessage = "This is a local IP address";
            }
            result.Successful = await SendEmailWithGet($"{emailUrl}/api/test?adminEmail={Url.Encode(adminEmail)}&sendEmail={sendEmail}");
        }
        catch (KavitaException ex)
        {
            result.Successful = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<bool> IsDefaultEmailService()
    {
        return (await _unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.EmailServiceUrl))!.Value!
            .Equals(DefaultApiUrl);
    }

    public async Task SendEmailChangeEmail(ConfirmationEmailDto data)
    {
        var emailLink = (await _unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.EmailServiceUrl))!.Value;
        var success = await SendEmailWithPost(emailLink + "/api/account/email-change", data);
        if (!success)
        {
            _logger.LogError("There was a critical error sending Confirmation email");
        }
    }

    public async Task<string> GetVersion(string emailUrl)
    {
        try
        {
            var settings = await _unitOfWork.SettingsRepository.GetSettingsDtoAsync();
            var response = await $"{emailUrl}/api/about/version"
                .WithHeader("Accept", "application/json")
                .WithHeader("User-Agent", "Kavita")
                .WithHeader("x-api-key", "MsnvA2DfQqxSK5jh")
                .WithHeader("x-kavita-version", BuildInfo.Version)
                .WithHeader("x-kavita-installId", settings.InstallId)
                .WithHeader("Content-Type", "application/json")
                .WithTimeout(TimeSpan.FromSeconds(10))
                .GetStringAsync();

            if (!string.IsNullOrEmpty(response))
            {
                return response.Replace("\"", string.Empty);
            }
        }
        catch (Exception)
        {
            return null;
        }

        return null;
    }

    public bool IsValidEmail(string email)
    {
        return new EmailAddressAttribute().IsValid(email);
    }

    public async Task SendConfirmationEmail(ConfirmationEmailDto data)
    {
        var emailLink = (await _unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.EmailServiceUrl)).Value;
        var success = await SendEmailWithPost(emailLink + "/api/invite/confirm", data);
        if (!success)
        {
            _logger.LogError("There was a critical error sending Confirmation email");
        }
    }

    public async Task<bool> CheckIfAccessible(string host)
    {
        // This is the only exception for using the default because we need an external service to check if the server is accessible for emails
        try
        {
            if (IsLocalIpAddress(host))
            {
                _logger.LogDebug("[EmailService] Server is not accessible, using local ip");
                return false;
            }

            var url = DefaultApiUrl + "/api/reachable?host=" + host;
            _logger.LogDebug("[EmailService] Checking if this server is accessible for sending an email to: {Url}", url);
            return await SendEmailWithGet(url);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> SendMigrationEmail(EmailMigrationDto data)
    {
        var emailLink = (await _unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.EmailServiceUrl)).Value;
        return await SendEmailWithPost(emailLink + "/api/invite/email-migration", data);
    }

    public async Task<bool> SendPasswordResetEmail(PasswordResetEmailDto data)
    {
        var emailLink = (await _unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.EmailServiceUrl)).Value;
        return await SendEmailWithPost(emailLink + "/api/invite/email-password-reset", data);
    }

    public async Task<bool> SendFilesToEmail(SendToDto data)
    {
        if (await IsDefaultEmailService()) return false;
        var emailLink = (await _unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.EmailServiceUrl)).Value;
        return await SendEmailWithFiles(emailLink + "/api/sendto", data.FilePaths, data.DestinationEmail);
    }

    private async Task<bool> SendEmailWithGet(string url, int timeoutSecs = 30)
    {
        try
        {
            var settings = await _unitOfWork.SettingsRepository.GetSettingsDtoAsync();
            var response = await (url)
                .WithHeader("Accept", "application/json")
                .WithHeader("User-Agent", "Kavita")
                .WithHeader("x-api-key", "MsnvA2DfQqxSK5jh")
                .WithHeader("x-kavita-version", BuildInfo.Version)
                .WithHeader("x-kavita-installId", settings.InstallId)
                .WithHeader("Content-Type", "application/json")
                .WithTimeout(TimeSpan.FromSeconds(timeoutSecs))
                .GetStringAsync();

            if (!string.IsNullOrEmpty(response) && bool.Parse(response))
            {
                return true;
            }
        }
        catch (Exception ex)
        {
            throw new KavitaException(ex.Message);
        }
        return false;
    }


    private async Task<bool> SendEmailWithPost(string url, object data, int timeoutSecs = 30)
    {
        try
        {
            var settings = await _unitOfWork.SettingsRepository.GetSettingsDtoAsync();
            var response = await (url)
                .WithHeader("Accept", "application/json")
                .WithHeader("User-Agent", "Kavita")
                .WithHeader("x-api-key", "MsnvA2DfQqxSK5jh")
                .WithHeader("x-kavita-version", BuildInfo.Version)
                .WithHeader("x-kavita-installId", settings.InstallId)
                .WithHeader("Content-Type", "application/json")
                .WithTimeout(TimeSpan.FromSeconds(timeoutSecs))
                .PostJsonAsync(data);

            if (response.StatusCode != StatusCodes.Status200OK)
            {
                var errorMessage = await response.GetStringAsync();
                throw new KavitaException(errorMessage);
            }
        }
        catch (FlurlHttpException ex)
        {
            _logger.LogError(ex, "There was an exception when interacting with Email Service");
            return false;
        }
        return true;
    }


    private async Task<bool> SendEmailWithFiles(string url, IEnumerable<string> filePaths, string destEmail, int timeoutSecs = 300)
    {
        try
        {
            var settings = await _unitOfWork.SettingsRepository.GetSettingsDtoAsync();
            var response = await (url)
                .WithHeader("User-Agent", "Kavita")
                .WithHeader("x-api-key", "MsnvA2DfQqxSK5jh")
                .WithHeader("x-kavita-version", BuildInfo.Version)
                .WithHeader("x-kavita-installId", settings.InstallId)
                .WithTimeout(timeoutSecs)
                .AllowHttpStatus("4xx")
                .PostMultipartAsync(mp =>
                {
                    mp.AddString("email", destEmail);
                    var index = 1;
                    foreach (var filepath in filePaths)
                    {
                        mp.AddFile("file" + index, filepath, _downloadService.GetContentTypeFromFile(filepath));
                        index++;
                    }
                }
                );

            if (response.StatusCode != StatusCodes.Status200OK)
            {
                var errorMessage = await response.GetStringAsync();
                throw new KavitaException(errorMessage);
            }
        }
        catch (FlurlHttpException ex)
        {
            _logger.LogError(ex, "There was an exception when sending Email for SendTo");
            return false;
        }
        return true;
    }

    private static bool IsLocalIpAddress(string url)
    {
        var host = url.Split(':')[0];
        try
        {
            // get host IP addresses
            var hostIPs = Dns.GetHostAddresses(host);
            // get local IP addresses
            var localIPs = Dns.GetHostAddresses(Dns.GetHostName());

            // test if any host IP equals to any local IP or to localhost
            foreach (var hostIp in hostIPs)
            {
                // is localhost
                if (IPAddress.IsLoopback(hostIp)) return true;
                // is local address
                if (localIPs.Contains(hostIp))
                {
                    return true;
                }
            }
        }
        catch
        {
            // ignored
        }

        return false;
    }

}
