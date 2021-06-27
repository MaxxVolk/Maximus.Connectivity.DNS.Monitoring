using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maximus.Connectivity.DNS.Modules
{
  public static class Ids
  {
    #region Maximus.Connectivity.DNS.Dig.Result.Enumeration
    /// <summary>
    /// DNS Dig Result Interpretation (Maximus.Connectivity.DNS.Dig.Result.Enumeration)
    /// </summary>
    public static class DNSDigResultEnumMembers
    {
      public static Guid RootEnumId { get; } = new Guid("60487bb0-e0da-9cea-6ea7-d0e19f18f839");
      /// <summary>
      /// Maximus.Connectivity.DNS.Dig.Result.Enumeration.SpecificNameList (Expect Name List)
      /// Operation should return a specific list of names.
      /// </summary>
      public static Guid SpecificNameListEnumId { get; } = new Guid("899cab71-de5b-5a58-9da9-7558d8d079ba");
      /// <summary>
      /// Maximus.Connectivity.DNS.Dig.Result.Enumeration.OperationSuccessful (Operation Completed Successfully)
      /// Operation shlould completed successfully, regarless of results.
      /// </summary>
      public static Guid OperationSuccessfulEnumId { get; } = new Guid("dd469b2e-9c86-b151-b18a-393a54172467");
      /// <summary>
      /// Maximus.Connectivity.DNS.Dig.Result.Enumeration.SpecificIPList (Expect IP Address List)
      /// Operation should return a specific list of IP adresses.
      /// </summary>
      public static Guid SpecificIPListEnumId { get; } = new Guid("63d14ad3-fcb6-bf64-e784-af522e787189");
      /// <summary>
      /// Maximus.Connectivity.DNS.Dig.Result.Enumeration.RegexMatch (Match Regular Expression)
      /// Operation result should match a regular expression.
      /// </summary>
      public static Guid RegexMatchEnumId { get; } = new Guid("52e3e172-22a6-72ab-2cc1-aa45721f30e9");
    }
    #endregion
  }
}
