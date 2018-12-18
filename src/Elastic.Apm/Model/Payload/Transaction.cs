using System;
using System.Collections.Generic;

namespace Elastic.Apm.Model.Payload
{
    public class Transaction
    {
        internal Service service;

        public Transaction(String name, string type)
        {
            _startDate = DateTime.UtcNow;
            this.Name = name;
            this.Type = type;
        }

        public Guid Id { get; set; }
        public long Duration { get; set; } //TODO datatype?, TODO: Greg, imo should be internal, TBD!

        public String Type { get; set; }

        public String Name { get; set; }

        /// <summary>
        /// A string describing the result of the transaction. 
        /// This is typically the HTTP status code, or e.g. "success" for a background task.
        /// </summary>
        /// <value>The result.</value>
        public String Result { get; set; }

        public String Timestamp => _startDate.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ");
        internal readonly DateTime _startDate;

        public Context Context { get; set; }

        public List<Span> Spans { get; set; } = new List<Span>(); //TODO: make lazy, TODO: make it internal & make sure serialization works

        public const string TYPE_REQUEST = "request";

        public void End()
        {
            var duration = DateTime.UtcNow - _startDate;
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

        public Span StartSpan(string name, string type, string subType = null, String action = null)
        {
            var retVal = new Span(name, type);
           
            if(subType != null)
            {
                retVal.Subtype = subType;
            }

            if(action != null)
            {
                retVal.Action = action;
            }

            var currentTime = DateTime.UtcNow;
            retVal.Start = (Decimal)(currentTime - this._startDate).TotalMilliseconds;
            retVal.Transaction_id = this.Id;
            retVal.transaction = this;
            return retVal;
        }
    }
}