using System;
using System.Reflection;
using Elastic.Apm.Model.Payload;

namespace Elastic.Apm.Api
{
    public static class ElasticApm
    {
        private static Service service;

        public static Service Service
        {
            get
            {
                if(service == null)
                {
                    service = new Service
                    {
                        Name = Assembly.GetEntryAssembly()?.GetName()?.Name,
                        Agent = new Model.Payload.Agent
                        {
                            Name = Consts.AgentName,
                            Version = Consts.AgentVersion
                        }
                    };
                }

                return service;
            }
            set
            {
                service = value;
            }
        }

        public static Transaction CurrentTransaction
        {
            get
            {
                return TransactionContainer.Transactions.Value;
            }
        }

        public static Transaction StartTransaction(string name, string type)
        {
            var retVal = new Transaction(name, type)
            {
                StartDate = DateTime.UtcNow
            };
            retVal.Name = name;
            retVal.Type = type;
            retVal.service = Service;
            retVal.Id = Guid.NewGuid();

            TransactionContainer.Transactions.Value = retVal;
            return retVal;
        }
    }
}
