using DnsClient;
using DnsClient.Protocol;
using Maximus.Library.ManagedModuleBase;

using Microsoft.EnterpriseManagement.HealthService;
using Microsoft.EnterpriseManagement.Mom.Modules.DataItems;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Maximus.Connectivity.DNS.Modules
{
  [MonitoringModule(ModuleType.ReadAction)]
  [ModuleOutput(true)]
  public class DNSDigPA : ModuleBaseSimpleAction<PropertyBagDataItem>
  {
    private int TargetIndex;
    private string FullyQualifiedDomainName, TestDisplayName, HostOrDomainName;
    private Guid ResultEvaluation;
    private bool Recursion, UseTcpOnly;
    private int RetriesCount, TimeoutSeconds, Port;
    private QueryType queryType;
    private List<string> NameList = new List<string>();
    private List<IPAddress> IPList = new List<IPAddress>();
    private string RegExExpression = "";

    public DNSDigPA(ModuleHost<PropertyBagDataItem> moduleHost, XmlReader configuration, byte[] previousState) : base(moduleHost, configuration, previousState)
    {
    }

    protected override void PreInitialize(ModuleHost<PropertyBagDataItem> moduleHost, XmlReader configuration, byte[] previousState)
    {
      ModInit.Logger.AddLoggingSource(GetType(), ModInit.evtId_DNSDigPA);
      base.PreInitialize(moduleHost, configuration, previousState);
    }

    protected override PropertyBagDataItem[] GetOutputData(DataItemBase[] inputDataItems)
    {
      try
      {
        if (!IPAddress.TryParse(FullyQualifiedDomainName, out IPAddress dnsServerAddress))
          dnsServerAddress = Dns.GetHostAddresses(FullyQualifiedDomainName).FirstOrDefault();
        if (dnsServerAddress == null)
          throw new Exception($"DNS server address cannot be parsed of resolved. Unknown IP for '{FullyQualifiedDomainName}'");

        IPEndPoint dnsServerEndpoint = new IPEndPoint(dnsServerAddress, Port);
        LookupClientOptions opt = new LookupClientOptions(dnsServerEndpoint)
        {
          CacheFailedResults = false,
          Recursion = Recursion,
          Retries = RetriesCount,
          ThrowDnsErrors = false,
          Timeout = TimeSpan.FromSeconds(TimeoutSeconds),
          UseCache = false,
          UseTcpOnly = UseTcpOnly
        };
        LookupClient dnsServer = new LookupClient(opt);
        DnsQuestion dnsQuestion = new DnsQuestion(HostOrDomainName, queryType);
        IDnsQueryResponse response;
        DNSDigResult result = new DNSDigResult(); // also set start time
        try
        {
          response = dnsServer.Query(dnsQuestion);
        }
        catch (DnsResponseException dnse)
        {
          result.SetHealth(Microsoft.EnterpriseManagement.Configuration.HealthState.Error);
          result.Message = $"DNS query failed: {dnse.Message}";
          return new PropertyBagDataItem[] { result.GetPropertyBagDataItem() };
        }

        // System error is always a critical (error) state regardless of the result evaluation mode.
        if (response.HasError)
        {
          result.SetHealth(Microsoft.EnterpriseManagement.Configuration.HealthState.Error);
          result.Message = $"DNS query failed: {response.ErrorMessage}";
          return new PropertyBagDataItem[] { result.GetPropertyBagDataItem() };
        }
        // add the answers to the resultant data item
        foreach (DnsResourceRecord answer in response.Answers)
          result.Answer += answer.ToString() + "\r\n";

        // now validate response as per the selected mode
        bool validatedOK = true;
        string message = "";

        if (ResultEvaluation == Ids.DNSDigResultEnumMembers.OperationSuccessfulEnumId)
        {
          message = "Operation is successful";
        }
        #region Regular expression
        else if (ResultEvaluation == Ids.DNSDigResultEnumMembers.RegexMatchEnumId)
        {
          validatedOK = Regex.IsMatch(result.Answer, RegExExpression, RegexOptions.IgnoreCase | RegexOptions.Multiline);
          if (validatedOK)
            message = "RegEx match found";
          else
            message = $"RegEx pattern '{RegExExpression}' not found";
        }
        #endregion
        #region IP List
        else if (ResultEvaluation == Ids.DNSDigResultEnumMembers.SpecificIPListEnumId)
        {
          
          switch (queryType)
          {
            case QueryType.A:
              IEnumerable<ARecord> returnedIP = GetAnswersOf<ARecord>(response.Answers);
              
              foreach (IPAddress addr in IPList)
                if (!returnedIP.Any(r => r.Address.Equals(addr)))
                {
                  validatedOK = false;
                  message += $"required address '{addr}' not returned; ";
                }
              break;
            case QueryType.AAAA:
              IEnumerable<AaaaRecord> returnedIPv6 = GetAnswersOf<AaaaRecord>(response.Answers);

              foreach (IPAddress addr in IPList)
                if (!returnedIPv6.Any(r => r.Address.Equals(addr)))
                {
                  validatedOK = false;
                  message += $"required address '{addr}' not returned; ";
                }
              break;
            default:
              ModInit.Logger.WriteWarning($"The '{queryType}' query type is not supported for IP List evaluation mode. Failed over to the Operation Successful mode.", this);
              break;
          }
        }
        #endregion
        #region Name List
        else if (ResultEvaluation == Ids.DNSDigResultEnumMembers.SpecificNameListEnumId)
        {
          switch (queryType)
          {
            case QueryType.MX:
              var returnedMX = GetAnswersOf<MxRecord>(response.Answers);
              foreach(string strName in NameList)
                if (!returnedMX.Any(m=>m.Exchange.Value.ToUpperInvariant() == strName.ToUpperInvariant()))
                {
                  validatedOK = false;
                  message += $"required mail exchange '{strName}' not returned; ";
                }
              break;
            default:
              ModInit.Logger.WriteWarning($"The '{queryType}' query type is not supported for Name List evaluation mode. Failed over to the Operation Successful mode.", this);
              break;
          }
        }
        #endregion
        else 
          ModInit.Logger.WriteError("Unexpected result evaluation mode. Failed over to the Operation Successful mode.", this);

        result.Message = message;
        if (validatedOK)
        {
          result.SetHealth(Microsoft.EnterpriseManagement.Configuration.HealthState.Success);
          return new PropertyBagDataItem[] { result.GetPropertyBagDataItem() };
        }
        else
        {
          result.SetHealth(Microsoft.EnterpriseManagement.Configuration.HealthState.Warning);
          return new PropertyBagDataItem[] { result.GetPropertyBagDataItem() };
        }
      }
      catch (Exception e)
      {
        ModuleErrorSignalReceiver(ModuleErrorSeverity.DataLoss, ModuleErrorCriticality.ThrowAndContinue, e, $"Failed to perform DNS lookup (dig) test for the {TestDisplayName} test object related to the {FullyQualifiedDomainName}:{TargetIndex} destination object.");
        return null;
      }
    }

    private static IEnumerable<T> GetAnswersOf<T>(IReadOnlyList<DnsResourceRecord> list) where T : DnsResourceRecord => list.Where(a => a is T).Cast<T>();

    protected override void ModuleErrorSignalReceiver(ModuleErrorSeverity severity, ModuleErrorCriticality criticality, Exception e, string message, params object[] extaInfo)
    {
      ModInit.ModuleErrorSignalReceiver(severity, criticality, e, message, this);
    }

    protected override void LoadConfiguration(XmlDocument cfgDoc)
    {
      try
      {
        // base class properties
        LoadConfigurationElement(cfgDoc, "TestDisplayName", out TestDisplayName, "<no test name provided>", false); // for logging purposes only
        // parent class property
        LoadConfigurationElement(cfgDoc, "FullyQualifiedDomainName", out FullyQualifiedDomainName);
        LoadConfigurationElement(cfgDoc, "TargetIndex", out TargetIndex);
        // specific class properties
        LoadConfigurationElement(cfgDoc, "HostOrDomainName", out HostOrDomainName);
        LoadConfigurationElement(cfgDoc, "QueryType", out string strQueryType, defaultValue: "A", Compulsory: false);
        LoadConfigurationElement(cfgDoc, "Recursion", out Recursion, defaultValue: true, Compulsory: false);
        LoadConfigurationElement(cfgDoc, "RetriesCount", out RetriesCount, defaultValue: 2, Compulsory: false);
        LoadConfigurationElement(cfgDoc, "TimeoutSeconds", out TimeoutSeconds, defaultValue: 5, Compulsory: false);
        LoadConfigurationElement(cfgDoc, "Port", out Port, defaultValue: 53, Compulsory: false);
        LoadConfigurationElement(cfgDoc, "UseTcpOnly", out UseTcpOnly, defaultValue: false, Compulsory: false);
        LoadConfigurationElement(cfgDoc, "ResultEvaluation", out ResultEvaluation, defaultValue: "{00000000-0000-0000-0000-000000000000}", Compulsory: false);
        LoadConfigurationElement(cfgDoc, "ResultEvaluationExpression", out string strResultEvaluationExpression);
        // parse
        if (!Enum.TryParse(strQueryType, true, out queryType))
          throw new Exception($"Unexpected query type: {strQueryType}");
        if (string.IsNullOrWhiteSpace(HostOrDomainName))
          throw new Exception($"Empty query.");
        if (ResultEvaluation == Ids.DNSDigResultEnumMembers.RegexMatchEnumId)
        {
          RegExExpression = strResultEvaluationExpression;
          if (string.IsNullOrEmpty(RegExExpression))
          {
            RegExExpression = "";
            ModInit.Logger.WriteWarning("Regex result evaluation mode is selected, but the configured regular expression is either empty or null.", this);
          }
        }
        else if (ResultEvaluation == Ids.DNSDigResultEnumMembers.SpecificIPListEnumId)
        {
          if (string.IsNullOrWhiteSpace(strResultEvaluationExpression))
            ModInit.Logger.WriteWarning("IP List result evaluation mode is selected, but the configured IP list is either empty or null.", this);
          else
          {
            foreach (string strIP in strResultEvaluationExpression.Split(ModInit.Separators, StringSplitOptions.RemoveEmptyEntries))
              if (IPAddress.TryParse(strIP.Trim(), out IPAddress newIP))
                IPList.Add(newIP);
              else
                ModInit.Logger.WriteWarning($"'{strIP}' cannot be parsed as a valid IP address and skipped.", this);
          }
        }
        else if (ResultEvaluation == Ids.DNSDigResultEnumMembers.SpecificNameListEnumId)
        {
          if (string.IsNullOrWhiteSpace(strResultEvaluationExpression))
            ModInit.Logger.WriteWarning("Name List result evaluation mode is selected, but the configured name list is either empty or null.", this);
          else
          {
            foreach (string strName in strResultEvaluationExpression.Split(ModInit.Separators, StringSplitOptions.RemoveEmptyEntries))
              NameList.Add(strName.Trim());
          }
        }
      }
      catch (Exception e)
      {
        ModuleErrorSignalReceiver(ModuleErrorSeverity.FatalError, ModuleErrorCriticality.Stop, e, "Failed to load module configuration.");
        throw new ModuleException("Failed to load module configuration.", e);
      }
    }
  }
}
