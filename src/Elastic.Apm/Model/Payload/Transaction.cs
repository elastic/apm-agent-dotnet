using System;
using System.Collections.Generic;

namespace Elastic.Apm.Model.Payload
{
    public class Transaction
    {
        internal Service service;

        public Transaction(String name, string type)
        {
            this.Name = name;
            this.Type = type;
        }

        public Guid Id { get; set; }
        public long Duration { get; set; } //TODO datatype?

        public String Type { get; set; }

        public String Name { get; set; }

        public String Result { get; set; }

        public String Timestamp => StartDate.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");

        //TODO: not part of intake, hide from serialization
        public DateTime StartDate { get; set; }

        public Context Context { get; set; }

        public List<Span> Spans { get; set; } = new List<Span>(); //TODO: make lazy

        public const string TYPE_REQUEST = "request";

        public void End()
        {
            var duration = DateTime.UtcNow - StartDate;
            this.Duration = (long)duration.TotalMilliseconds;

            Apm.Agent.PayloadSender.QueuePayload(new Payload
            {
                Transactions = new List<Transaction>{
                    this
                },
                Service =  this.service
            });

            TransactionContainer.Transactions.Value = null;
        }
    }
}