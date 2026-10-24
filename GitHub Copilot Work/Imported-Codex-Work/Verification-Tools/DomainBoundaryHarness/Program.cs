using System.Text.Json;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;

if(args.Length==2 && args[1]=="report-range")
{
    var rangeRecords=new List<object>(); int rangeFailures=0;
    var sampleInstant=new DateTimeOffset(2026,10,17,12,0,0,TimeSpan.Zero);
    void RangeProbeCheck(string label,DateOnly from,DateOnly to,int expectedDays,int expectedSeries,bool expectedCostRow)
    {
        var range=BusinessDateRange.Resolve(from,to,null)!.Value;
        var actual=range.ToUtcRange();
        bool normalizedSalesRow=sampleInstant>=actual.Start && sampleInstant<actual.End;
        // Explicit reproduction of ReportingQueryService.Range's two conversions only.
        // This is NOT an invocation of that EF service or its SQL query.
        var rawStart=BusinessClock.UtcRangeForBusinessDate(from).Start;
        var rawEnd=BusinessClock.UtcRangeForBusinessDate(to).End;
        bool simulatedCostRow=sampleInstant>=rawStart && sampleInstant<rawEnd;
        int simulatedSoldLines=simulatedCostRow?1:0;
        var componentProfit=Falah.Domain.Reports.ProfitabilityRules.ComputeGrossProfit(
            Money.FromPiastres(normalizedSalesRow?10000:0),Money.FromPiastres(simulatedCostRow?2500:0),simulatedSoldLines,simulatedSoldLines);
        var count=range.Days().Count();
        bool pass=normalizedSalesRow && simulatedCostRow==expectedCostRow && range.DayCount==expectedDays && count==expectedSeries;
        bool profitPass=componentProfit.IsComplete && componentProfit.GrossProfit?.Piastres==(expectedCostRow?7500:10000);
        if(!pass)rangeFailures++; if(!profitPass)rangeFailures++;
        rangeRecords.Add(new{label,from,to,range.DayCount,series_count=count,normalized_sales_contains_sample=normalizedSalesRow,
            simulated_raw_cost_contains_sample=simulatedCostRow,gross_profit=componentProfit.GrossProfit?.Piastres,
            componentProfit.IsComplete,range_assertion_passed=pass,profit_component_assertion_passed=profitPass});
    }
    RangeProbeCheck("ordered two days",new(2026,10,17),new(2026,10,18),2,2,true);
    RangeProbeCheck("same day",new(2026,10,17),new(2026,10,17),1,1,true);
    RangeProbeCheck("reversed adjacent",new(2026,10,18),new(2026,10,17),0,0,false);
    RangeProbeCheck("reversed separated",new(2026,10,19),new(2026,10,17),-1,0,false);
    var rangeProbeOutput=Path.Combine(Path.GetFullPath(args[0]),"Codex Work","Local-Verification","report-range-pure-results.json");
    var payload=new{at_utc=DateTimeOffset.UtcNow,assertions=8,failures=rangeFailures,cases=rangeRecords,
        framework=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        scope="PURE_DOMAIN_AND_EXPLICIT_RANGE_SIMULATION_ONLY",
        limits="Actual linked baseline BusinessDateRange/BusinessClock/ProfitabilityRules, plus explicit synthetic raw-date cost predicate. No ReportingQueryService, OwnerDashboard DTO, HTTP endpoint, database, financial record or native UI executed. Not an independent review or full end-to-end reproduction."};
    Directory.CreateDirectory(Path.GetDirectoryName(rangeProbeOutput)!);
    var rangeProbeTemp=rangeProbeOutput+"."+Guid.NewGuid().ToString("N")+".tmp";
    File.WriteAllText(rangeProbeTemp,JsonSerializer.Serialize(payload,new JsonSerializerOptions{WriteIndented=true}));
    using(JsonDocument.Parse(File.ReadAllText(rangeProbeTemp))){}
    File.Move(rangeProbeTemp,rangeProbeOutput,true);
    Console.WriteLine(JsonSerializer.Serialize(new{assertions=8,failures=rangeFailures,scope=payload.scope}));
    Environment.ExitCode=rangeFailures==0?0:1;return;
}

if(args.Length==2 && args[1]=="packaging-coverage")
{
    var records=new List<object>();int packagingFailures=0;
    void Probe(string name,long? estimated,bool includeRule,int knownLines,long foodCost,long? expectedProfit,bool expectedComplete,bool expectedKnown)
    {
        var ctx=new Falah.Domain.Inventory.CostingContext(
            [new Falah.Domain.Inventory.InventoryItem("synthetic-bag","Synthetic packaging",Falah.Domain.Inventory.CostSource.Purchase,estimated)],[],[],[]);
        Falah.Domain.Inventory.OrderPackagingRule[] rules=includeRule?[new("Delivery","synthetic-bag",1000,true)]:[];
        var packaging=Falah.Domain.Inventory.CostingRules.OrderPackagingCost("Delivery",rules,ctx);
        // Mirrors the documented report's accumulation boundary only; no query service/DTO/SQL invoked.
        var accumulated=foodCost+(packaging.Known?packaging.Cost.Piastres:0);
        var gross=Falah.Domain.Reports.ProfitabilityRules.ComputeGrossProfit(Money.FromPiastres(10000),Money.FromPiastres(accumulated),1,knownLines);
        var pass=packaging.Known==expectedKnown && gross.IsComplete==expectedComplete && gross.GrossProfit?.Piastres==expectedProfit;
        if(!pass)packagingFailures++;
        records.Add(new{name,packaging_known=packaging.Known,packaging_cost=packaging.Cost.Piastres,known_lines=knownLines,
            sold_lines=1,accumulated_cost=accumulated,gross_profit=gross.GrossProfit?.Piastres,is_complete=gross.IsComplete,assertion_passed=pass});
    }
    Probe("known dish, missing configured packaging",null,true,1,2000,8000,true,false);
    Probe("known dish and known packaging",100,true,1,2000,7900,true,true);
    Probe("no packaging rule intentionally optional",null,false,1,2000,8000,true,true);
    Probe("unknown dish correctly incomplete",100,true,0,0,null,false,true);
    var destination=Path.Combine(Path.GetFullPath(args[0]),"Codex Work","Local-Verification","packaging-coverage-pure-results.json");
    var payload=new{at_utc=DateTimeOffset.UtcNow,assertions=records.Count,packagingFailures,cases=records,
        framework=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        scope="PACKAGING_PURE_COMPONENTS_ONLY",limits="Actual linked baseline Domain component calls plus explicit synthetic accumulation matching source. ReportingQueryService/OwnerDashboard/DTO/UI/SQL not executed. Not independently reviewed and not full end-to-end financial reproduction."};
    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    var tmp=destination+"."+Guid.NewGuid().ToString("N")+".tmp";
    File.WriteAllText(tmp,JsonSerializer.Serialize(payload,new JsonSerializerOptions{WriteIndented=true}));
    using(JsonDocument.Parse(File.ReadAllText(tmp))){}
    File.Move(tmp,destination,true);
    Console.WriteLine(JsonSerializer.Serialize(new{assertions=records.Count,packagingFailures,scope="PACKAGING_PURE_COMPONENTS_ONLY"}));
    Environment.ExitCode=packagingFailures==0?0:1;return;
}

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
