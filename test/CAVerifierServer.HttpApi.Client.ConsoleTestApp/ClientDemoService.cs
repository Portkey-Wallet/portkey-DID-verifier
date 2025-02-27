using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Volo.Abp.Account;
using Volo.Abp.DependencyInjection;

namespace CAVerifierServer.HttpApi.Client.ConsoleTestApp;

public class ClientDemoService : ITransientDependency
{
    private readonly IProfileAppService _profileAppService;

    public ClientDemoService(IProfileAppService profileAppService)
    {
        _profileAppService = profileAppService;
    }

    public async Task RunAsync()
    {
        var output = await _profileAppService.GetAsync();
        Console.WriteLine($"UserName : {output.UserName}");
        Console.WriteLine($"Email    : {output.Email}");
        Console.WriteLine($"Name     : {output.Name}");
        Console.WriteLine($"Surname  : {output.Surname}");
    }

    public async Task RunHubClientAsync()
    {
        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5588/ca")
            .Build();
        /*connection.On<HubResponse<string>>("Ping", s => { Console.WriteLine($"Receive ping, requestId={s.RequestId} body={s.Body}"); });
        connection.On<HubResponse<GenerateSignatureOutput>>("Sin", s =>
        {
            {
                Console.WriteLine($"Receive Sin, requestId={s.RequestId} body={s.Body.Signature}");
                connection.InvokeAsync("Ack", "client_6464", s.RequestId);
            }
        });*/
        await connection.StartAsync().ConfigureAwait(false);

        await connection.InvokeAsync("Connect", "client_6464");
    }
}