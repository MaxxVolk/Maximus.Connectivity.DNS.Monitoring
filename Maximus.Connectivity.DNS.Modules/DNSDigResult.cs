using Microsoft.EnterpriseManagement.Configuration;

using Maximus.Library.DataItemCollection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maximus.Connectivity.DNS.Modules
{
  class DNSDigResult : PropertyBagObject
  {
    private HealthState _healthState = Microsoft.EnterpriseManagement.Configuration.HealthState.Uninitialized;
    private DateTime _startTime = DateTime.UtcNow;
    public string HealthState => _healthState.ToString().ToUpperInvariant();
    public string Answer { get; set; } = "";
    public string Message { get; set; } = "";
    public double TimeTakenMs => DateTime.UtcNow.Subtract(_startTime).TotalMilliseconds;
    public void SetHealth(HealthState healthState)
    {
      _healthState = healthState;
    }
  }
}
