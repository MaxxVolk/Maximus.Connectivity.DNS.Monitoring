# Maximus Connectivity DNS Monitoring

Monitor DNS Server or zone health with synthetic transactions.

This management pack extends Maximus Connectivity Monitoring SCOM management pack by adding DNS Server connectivity monitoring. See https://github.com/MaxxVolk/Maximus.Connectivity.Monitoring for details.

Read the blog article for detailed description and screenshot examples: https://maxcoreblog.com/2021/06/27/dns-server-extension-for-maximus-connectivity-monitoring-scom-mp/

This management pack adds one monitor:
  - DNS Lookup (Dig) Monitor

and one performance collection rule to collect DNS Server response time.

The 'DNS Lookup (Dig) Monitor' monitor has four result evaluation modes:
  - 'Operation Completed Successfully': DNS query completed successfully
  - 'Expect IP Address List': DNS query completed successfully and return all IP addresses from the list (supported for A and AAAA queries)
  - 'Expect Name List': DNS query completed successfully and return all names from the list (supported for MX query)
  - 'Match Regular Expression': DNS query completed successfully and its answer matches the specified regular expression