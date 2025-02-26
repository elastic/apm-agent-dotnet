---
mapped_pages:
  - https://www.elastic.co/guide/en/apm/agent/dotnet/current/nlog.html
---

# NLog [nlog]

For NLog, we offer two [LayoutRenderers](https://github.com/NLog/NLog/wiki/Layout-Renderers) that inject the current trace and transaction id into logs.

In order to use them, you need to add the [Elastic.Apm.NLog](https://www.nuget.org/packages/Elastic.Apm.NLog) NuGet package to your application and load it in the `<extensions>` section of your NLog config file:

```xml
<nlog>
<extensions>
   <add assembly="Elastic.Apm.NLog"/>
</extensions>
<targets>
<target type="file" name="logfile" fileName="myfile.txt">
    <layout type="jsonlayout">
        <attribute name="traceid" layout="${ElasticApmTraceId}" />
        <attribute name="transactionid" layout="${ElasticApmTransactionId}" />
    </layout>
</target>
</targets>
<rules>
    <logger name="*" minLevel="Trace" writeTo="logfile" />
</rules>
</nlog>
```

As you can see in the sample file above, you can reference the current transaction id with `${ElasticApmTransactionId}` and the trace id with `${ElasticApmTraceId}`.

