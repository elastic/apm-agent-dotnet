using System;
using System.Collections.Generic;
using System.Text;

namespace Elastic.Agent.Core
{
    internal static class Consts
    {
        public static string IntakeV1Transactions = "/v1/transactions";

        public static string AgentName => "dotNet";
        public static String AgentVersion => "0.1"; //TODO: read assembly version
    }
}
