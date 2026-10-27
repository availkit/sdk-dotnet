
using AvailKit;
using System.Text.Json;
try
{
    // docs:configuration:start
    if(args.Length!=1)throw new ArgumentException();
    using var document=JsonDocument.Parse(PrivateFiles.Read(args[0],16384));var config=document.RootElement;
    string Get(string key)=>config.GetProperty(key).GetString()!;
    if(Get("environment")!="test")throw new ArgumentException();
    string credential=PrivateFiles.Credential(Get("credentialFile"));string intents=Get("intentDirectory");
    var options=new ClientOptions(config.TryGetProperty("baseUrl",out var origin)?origin.GetString()!:"https://api.availkit.com");
    // docs:configuration:end
    using var client=new WorkspaceClient(Get("workspaceId"),AvailKit.Environment.Test,credential,options);
    // docs:lifecycle:start
    var times=(await client.AvailabilityAsync(Get("offeringId"),Get("from"),Get("to"))).Slots;
    if(times.Count<2){Console.WriteLine("Fewer than two available times; no booking sent.");return;}
    var create=client.PrepareBook(new BookingInput(Get("offeringId"),times[0].Start),Secrets.NewIdempotencyKey());
    PrivateFiles.WriteNew(Path.Combine(intents,"create.json"),create.ExportPrivate());
    var booked=await client.ExecuteAsync(create);
    var current=await client.GetBookingAsync(booked.Id);
    if(times.Count>1)
    {
        var move=client.PrepareRescheduleBooking(current.Id,new RescheduleInput(current.Start,times[1].Start),Secrets.NewIdempotencyKey());
        PrivateFiles.WriteNew(Path.Combine(intents,"move.json"),move.ExportPrivate());
        current=await client.ExecuteAsync(move);
    }
    var cancel=client.PrepareCancel(current.Id,Secrets.NewIdempotencyKey());
    PrivateFiles.WriteNew(Path.Combine(intents,"cancel.json"),cancel.ExportPrivate());
    Console.WriteLine("Owned test booking lifecycle: "+(await client.ExecuteAsync(cancel)).Status);
    // docs:lifecycle:end
}
catch(AvailKitException error){Console.Error.WriteLine(error);System.Environment.ExitCode=1;}
catch{Console.Error.WriteLine("Example stopped; retain private intents and reconcile before another mutation.");System.Environment.ExitCode=1;}
