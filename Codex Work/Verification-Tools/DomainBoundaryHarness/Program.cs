using System.Text.Json;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;

if(args.Length==2 && args[1]=="cash-count")
{
    var records=new List<object>(); int failed=0;
    void Probe(string name,long denomination,long quantity,long expected,long observedTotal,long observedDifference)
    {
        var result=Falah.Domain.BusinessDay.BusinessDayRules.CalculateDenominationCount([(denomination,quantity)],expected);
        bool pass=result.TotalPiastres==observedTotal && result.DifferencePiastres==observedDifference;
        if(!pass)failed++;
        records.Add(new{name,denomination,quantity,expected,total=result.TotalPiastres,difference=result.DifferencePiastres,assertion_passed=pass});
    }
    Probe("ordinary count",20000,2,40000,40000,0);
    Probe("negative quantity intentionally clamped",100,-1,0,0,0);
    Probe("negative denomination accepted",-100,1,0,-100,-100);
    Probe("unsupported denomination accepted",1,1,0,1,1);
    Probe("positive quantity multiplication wraps",20000,long.MaxValue,0,-20000,-20000);
    Probe("difference subtraction wraps",25,1,long.MinValue,25,-9223372036854775783);
    var destination=Path.Combine(Path.GetFullPath(args[0]),"Codex Work","Local-Verification","cash-denomination-pure-results.json");
    var payload=new{at_utc=DateTimeOffset.UtcNow,assertions=records.Count,failures=failed,cases=records,
        framework=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        scope="CASH_COUNT_PURE_ONLY",limits="Separate net10 linked-baseline source executable. No server/API/user/session/database/count record or financial transaction. Assertions describe observed implementation, not desired valid-input policy."};
    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    var tmp=destination+"."+Guid.NewGuid().ToString("N")+".tmp";
    File.WriteAllText(tmp,JsonSerializer.Serialize(payload,new JsonSerializerOptions{WriteIndented=true}));
    using(JsonDocument.Parse(File.ReadAllText(tmp))){}
    File.Move(tmp,destination,true);
    Console.WriteLine(JsonSerializer.Serialize(new{assertions=records.Count,failures=failed,scope="CASH_COUNT_PURE_ONLY"}));
    Environment.ExitCode=failed==0?0:1;
    return;
}

if(args.Length!=1)throw new ArgumentException("Repository root required");
var root=Path.GetFullPath(args[0]);
var output=Path.Combine(root,"Codex Work","Local-Verification","negative-fee-pure-results.json");
var item=new OrderItem("synthetic-line","synthetic-product",null,"Synthetic non-business fixture",null,
    Money.FromPiastres(10000),Money.FromPiastres(10000),1,Money.FromPiastres(10000),false,null,null,null);
var cases=new List<object>();int failures=0;
void Check(string name,long fee,long paid,long? expected)
{
    try
    {
        var p=CheckoutRules.BuildPlan(OrderType.Takeaway,PaymentMethod.Cash,[item],Money.Zero,null,
            Money.FromPiastres(fee),Money.Zero,Money.FromPiastres(paid),null,null);
        var pass=expected.HasValue && p.PayableTotal.Piastres==expected;
        if(!pass)failures++;
        cases.Add(new{name,delivery_fee=fee,amount_paid=paid,actual_payable=p.PayableTotal.Piastres,
            p.IsPaid,p.DefersAllMoney,expected_payable=expected,assertion_passed=pass,exception_type=(string?)null});
    }
    catch(DomainException ex)
    {
        var pass=!expected.HasValue;if(!pass)failures++;
        cases.Add(new{name,delivery_fee=fee,amount_paid=paid,expected_payable=expected,
            assertion_passed=pass,exception_type=ex.GetType().Name});
    }
}
Check("positive sale control",0,10000,10000);
Check("positive fee control",500,10500,10500);
Check("insufficient payment control",0,0,null);
Check("negative fee reduces amount",-5000,5000,5000);
Check("negative fee cancels amount",-10000,0,0);
Check("negative fee below amount clamps",-10001,0,0);
Check("unchecked positive fee overflow clamps",long.MaxValue,0,0);
var result=new{at_utc=DateTimeOffset.UtcNow,tool="Isolated linked-baseline Domain source arithmetic harness",framework=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    cases,assertions=cases.Count,failures,
    limits=new[]{"Synthetic in-memory arithmetic only. No HTTP/server, SQL, user/session, inventory or financial ledger operation.",
        "Links approved Domain source into a separate net10 audit executable; not a net8 deployment test or full authorization bypass reproduction.",
        "Handler authorization/stock/persistence implications remain source traces, not runtime-verified application acceptance."}};
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
var temp=output+"."+Guid.NewGuid().ToString("N")+".tmp";
File.WriteAllText(temp,JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
using(JsonDocument.Parse(File.ReadAllText(temp))){}
File.Move(temp,output,true);
Console.WriteLine(JsonSerializer.Serialize(new{assertions=cases.Count,failures,scope="PURE_DOMAIN_ONLY"}));
Environment.ExitCode=failures==0?0:1;
